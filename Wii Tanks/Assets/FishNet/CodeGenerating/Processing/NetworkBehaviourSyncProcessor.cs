using System.Collections.Generic;
using FishNet.CodeGenerating.Extension;
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Configuring;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using FishNet.Serializing;
using FishNet.Transporting;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;
using MonoFN.Cecil.Rocks;

namespace FishNet.CodeGenerating.Processing
{
    internal class NetworkBehaviourSyncProcessor : CodegenBase
    {
        #region Reflection references.

        private TypeDefinition SyncBase_TypeDef;

        #endregion

        public override bool ImportReferences()
        {
            var syncBaseType = typeof(SyncBase);
            SyncBase_TypeDef = ImportReference(syncBaseType).Resolve();

            return true;
        }

        /// <summary>
        ///     Processes SyncVars and Objects.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="diagnostics"></param>
        internal bool Process(TypeDefinition typeDef, List<(SyncType, ProcessedSync)> allProcessedSyncs,
            ref uint syncTypeStartCount)
        {
            var modified = false;
            _createdSyncTypeMethodDefinitions.Clear();
            _lastReadInstruction = null;

            var fieldDefs = typeDef.Fields.ToArray();
            foreach (var fd in fieldDefs)
            {
                CustomAttribute syncAttribute;
                var st = GetSyncType(fd, true, out syncAttribute);
                //Not a sync type field.
                if (st == SyncType.Unset)
                    continue;

                if (st == SyncType.Variable)
                {
                    if (TryCreateSyncVar(syncTypeStartCount, allProcessedSyncs, typeDef, fd, syncAttribute))
                        syncTypeStartCount++;
                }
                else if (st == SyncType.List || st == SyncType.HashSet)
                {
                    if (TryCreateSyncList_SyncHashSet(syncTypeStartCount, allProcessedSyncs, typeDef, fd, syncAttribute,
                            st))
                        syncTypeStartCount++;
                }
                else if (st == SyncType.Dictionary)
                {
                    if (TryCreateSyncDictionary(syncTypeStartCount, allProcessedSyncs, typeDef, fd, syncAttribute))
                        syncTypeStartCount++;
                }
                else if (st == SyncType.Custom)
                {
                    if (TryCreateCustom(syncTypeStartCount, allProcessedSyncs, typeDef, fd, syncAttribute))
                        syncTypeStartCount++;
                }

                modified = true;
            }

            return modified;
        }


        /// <summary>
        ///     Gets number of SyncTypes by checking for SyncVar/Object attributes. This does not perform error checking.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        internal uint GetSyncTypeCount(TypeDefinition typeDef)
        {
            uint count = 0;
            foreach (var fd in typeDef.Fields)
                if (HasSyncTypeAttributeUnchecked(fd))
                    count++;

            return count;
        }

        /// <summary>
        ///     Replaces GetSets for methods which may use a SyncType.
        /// </summary>
        internal bool ReplaceGetSets(TypeDefinition typeDef, List<(SyncType, ProcessedSync)> allProcessedSyncs)
        {
            var modified = false;

            var modifiableMethods = GetModifiableMethods(typeDef);
            modified |= ReplaceGetSetDirties(modifiableMethods, allProcessedSyncs);

            return modified;
        }

        /// <summary>
        ///     Gets SyncType fieldDef is.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        internal SyncType GetSyncType(FieldDefinition fieldDef, bool validate, out CustomAttribute syncAttribute)
        {
            syncAttribute = null;
            //If the generated field for syncvars ignore it.
            if (fieldDef.Name.StartsWith(SYNCVAR_PREFIX))
                return SyncType.Unset;

            bool syncObject;
            bool error;
            syncAttribute = GetSyncTypeAttribute(fieldDef, out syncObject, out error);
            //Do not perform further checks if an error occurred.
            if (error)
                return SyncType.Unset;
            /* If if attribute is null the code must progress
             * to throw errors when user creates a sync type
             * without using the attribute. */
            if (!validate) return syncAttribute == null ? SyncType.Unset : SyncType.Custom;

            /* If no attribute make sure the field does not implement
             * ISyncType. If it does then a SyncObject or SyncVar attribute
             * should exist. */
            if (syncAttribute == null)
            {
                var foundSyncBaseTd = fieldDef.FieldType.CachedResolve(Session)
                    .GetClassInInheritance(Session, SyncBase_TypeDef);
                if (foundSyncBaseTd != null && foundSyncBaseTd.ImplementsInterface<ISyncType>())
                    LogError(
                        $"{fieldDef.Name} within {fieldDef.DeclaringType.Name} is a SyncType but is missing the [SyncVar] or [SyncObject] attribute.");

                return SyncType.Unset;
            }

            /* If the attribute is not [SyncObject] then the attribute
             * is [SyncVar]. Only checks that need to be made is to make sure
             * the user is not using a SyncVar attribute when they should be using a SyncObject attribute. */
            if (syncAttribute != null && !syncObject)
            {
                //Make sure syncvar attribute isnt on a sync object.
                if (GetSyncObjectSyncType(syncAttribute) != SyncType.Unset)
                {
                    LogError(
                        $"{fieldDef.Name} within {fieldDef.DeclaringType.Name} uses a [SyncVar] attribute but should be using [SyncObject].");
                    return SyncType.Unset;
                }

                return SyncType.Variable;
            }

            /* If here could be syncObject
             * or attribute might be null. */
            if (fieldDef.FieldType.CachedResolve(Session).ImplementsInterfaceRecursive<ISyncType>(Session))
                return GetSyncObjectSyncType(syncAttribute);

            SyncType GetSyncObjectSyncType(CustomAttribute sa)
            {
                //If attribute is null then throw error.
                if (sa == null)
                {
                    LogError(
                        $"{fieldDef.Name} within {fieldDef.DeclaringType.Name} is a SyncType but [SyncObject] attribute was not found.");
                    return SyncType.Unset;
                }

                if (fieldDef.FieldType.Name == GetClass<ObjectHelper>().SyncList_Name)
                    return SyncType.List;
                if (fieldDef.FieldType.Name == GetClass<ObjectHelper>().SyncDictionary_Name)
                    return SyncType.Dictionary;
                if (fieldDef.FieldType.Name == GetClass<ObjectHelper>().SyncHashSet_Name)
                    return SyncType.HashSet;
                //Custom types must also implement ICustomSync.
                if (fieldDef.FieldType.CachedResolve(Session).ImplementsInterfaceRecursive<ICustomSync>(Session))
                    return SyncType.Custom;
                return SyncType.Unset;
            }

            //Fall through.
            if (syncAttribute != null)
                LogError(
                    $"SyncObject attribute found on {fieldDef.Name} within {fieldDef.DeclaringType.Name} but type {fieldDef.FieldType.Name} does not inherit from SyncBase, or if a custom type does not implement ICustomSync.");

            return SyncType.Unset;
        }


        /// <summary>
        ///     Tries to create a SyncList.
        /// </summary>
        private bool TryCreateCustom(uint syncTypeCount, List<(SyncType, ProcessedSync)> allProcessedSyncs,
            TypeDefinition typeDef, FieldDefinition originalFieldDef, CustomAttribute syncAttribute)
        {
            //Get the serialized type.
            var getSerialziedTypeMd = originalFieldDef.FieldType.CachedResolve(Session)
                .GetMethod(GETSERIALIZEDTYPE_METHOD_NAME);
            var getSerialziedTypeMr = ImportReference(getSerialziedTypeMd);
            var instructions = getSerialziedTypeMr.CachedResolve(Session).Body.Instructions;

            var canSerialize = false;
            TypeReference serializedDataTypeRef = null;
            /* If the user is returning null then
             * they are indicating a custom serializer does not
             * have to be implemented. */
            if (instructions.Count == 2 && instructions[0].OpCode == OpCodes.Ldnull &&
                instructions[1].OpCode == OpCodes.Ret)
                canSerialize = true;
            //If not returning null then make a serializer for return type.
            else
                foreach (var item in instructions)
                    //This token references the type.
                    if (item.OpCode == OpCodes.Ldtoken)
                    {
                        TypeReference importedTr = null;
                        if (item.Operand is TypeDefinition td)
                            importedTr = ImportReference(td);
                        else if (item.Operand is TypeReference tr)
                            importedTr = ImportReference(tr);

                        if (importedTr != null)
                        {
                            serializedDataTypeRef = importedTr;
                            canSerialize = GetClass<GeneralHelper>()
                                .HasSerializerAndDeserializer(serializedDataTypeRef, true);
                        }
                    }

            //Wasn't able to determine serialized type, or create it.
            if (!canSerialize)
            {
                LogError(
                    $"Custom SyncObject {originalFieldDef.Name} data type {serializedDataTypeRef.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return false;
            }

            var result = InitializeCustom(syncTypeCount, typeDef, originalFieldDef, syncAttribute);
            if (result)
                allProcessedSyncs.Add((SyncType.Custom, null));
            return result;
        }


        /// <summary>
        ///     Tries to create a SyncList.
        /// </summary>
        private bool TryCreateSyncList_SyncHashSet(uint syncTypeCount,
            List<(SyncType, ProcessedSync)> allProcessedSyncs, TypeDefinition typeDef, FieldDefinition originalFieldDef,
            CustomAttribute syncAttribute, SyncType syncType)
        {
            //Import fieldType to module.
            var fieldTypeTr = ImportReference(originalFieldDef.FieldType);
            //Make sure type can be serialized.
            var tmpGenerinstanceType = fieldTypeTr as GenericInstanceType;
            //this returns the correct data type, eg SyncList<int> would return int.
            var dataTypeRef = ImportReference(tmpGenerinstanceType.GenericArguments[0]);

            var canSerialize = GetClass<GeneralHelper>().HasSerializerAndDeserializer(dataTypeRef, true);
            if (!canSerialize)
            {
                LogError(
                    $"SyncObject {originalFieldDef.Name} data type {dataTypeRef.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return false;
            }

            var result = InitializeSyncList_SyncHashSet(syncTypeCount, typeDef, originalFieldDef, syncAttribute);
            if (result)
                allProcessedSyncs.Add((syncType, null));
            return result;
        }

        /// <summary>
        ///     Tries to create a SyncDictionary.
        /// </summary>
        private bool TryCreateSyncDictionary(uint syncTypeCount, List<(SyncType, ProcessedSync)> allProcessedSyncs,
            TypeDefinition typeDef, FieldDefinition originalFieldDef, CustomAttribute syncAttribute)
        {
            //Make sure type can be serialized.
            var tmpGenerinstanceType = originalFieldDef.FieldType as GenericInstanceType;
            //this returns the correct data type, eg SyncList<int> would return int.
            var keyTypeRef = tmpGenerinstanceType.GenericArguments[0];
            var valueTypeRef = tmpGenerinstanceType.GenericArguments[1];

            bool canSerialize;
            //Check key serializer.
            canSerialize = GetClass<GeneralHelper>().HasSerializerAndDeserializer(keyTypeRef, true);
            if (!canSerialize)
            {
                LogError(
                    $"SyncObject {originalFieldDef.Name} key type {keyTypeRef.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return false;
            }

            //Check value serializer.
            canSerialize = GetClass<GeneralHelper>().HasSerializerAndDeserializer(valueTypeRef, true);
            if (!canSerialize)
            {
                LogError(
                    $"SyncObject {originalFieldDef.Name} value type {valueTypeRef.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return false;
            }

            var result = InitializeSyncDictionary(syncTypeCount, typeDef, originalFieldDef, syncAttribute);
            if (result)
                allProcessedSyncs.Add((SyncType.Dictionary, null));
            return result;
        }


        /// <summary>
        ///     Tries to create a SyncVar.
        /// </summary>
        private bool TryCreateSyncVar(uint syncCount, List<(SyncType, ProcessedSync)> allProcessedSyncs,
            TypeDefinition typeDef, FieldDefinition fieldDef, CustomAttribute syncAttribute)
        {
            var canSerialize = GetClass<GeneralHelper>().HasSerializerAndDeserializer(fieldDef.FieldType, true);
            if (!canSerialize)
            {
                LogError(
                    $"SyncVar {fieldDef.FullName} field type {fieldDef.FieldType.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return false;
            }

            if (Module != typeDef.Module)
            {
                //Only display warning if field is exposed.
                if (!fieldDef.Attributes.HasFlag(FieldAttributes.Private))
                    Session.DifferentAssemblySyncVars.Add(fieldDef);
                return false;
            }

            FieldDefinition syncVarFd;
            MethodReference accessorSetValueMr;
            MethodReference accessorGetValueMr;

            var created = CreateSyncVar(syncCount, typeDef, fieldDef, syncAttribute, out syncVarFd,
                out accessorSetValueMr, out accessorGetValueMr);
            if (created)
            {
                var originalFr = ImportReference(fieldDef);
                allProcessedSyncs.Add((SyncType.Variable,
                    new ProcessedSync(originalFr, syncVarFd, accessorSetValueMr, accessorGetValueMr)));
            }

            return created;
        }


        /// <summary>
        ///     Returns if fieldDef has a SyncType attribute. No error checking is performed.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        private bool HasSyncTypeAttributeUnchecked(FieldDefinition fieldDef)
        {
            foreach (var customAttribute in fieldDef.CustomAttributes)
                if (GetClass<AttributeHelper>().IsSyncVarAttribute(customAttribute.AttributeType.FullName))
                    return true;
                else if (GetClass<AttributeHelper>().IsSyncObjectAttribute(customAttribute.AttributeType.FullName))
                    return true;

            return false;
        }


        /// <summary>
        ///     Returns the syncvar attribute on a method, if one exist. Otherwise returns null.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        private CustomAttribute GetSyncTypeAttribute(FieldDefinition fieldDef, out bool syncObject, out bool error)
        {
            CustomAttribute foundAttribute = null;
            //Becomes true if an error occurred during this process.
            error = false;
            syncObject = false;

            foreach (var customAttribute in fieldDef.CustomAttributes)
            {
                if (GetClass<AttributeHelper>().IsSyncVarAttribute(customAttribute.AttributeType.FullName))
                    syncObject = false;
                else if (GetClass<AttributeHelper>().IsSyncObjectAttribute(customAttribute.AttributeType.FullName))
                    syncObject = true;
                else
                    continue;

                //A syncvar attribute already exist.
                if (foundAttribute != null)
                {
                    LogError($"{fieldDef.Name} cannot have multiple SyncType attributes.");
                    error = true;
                }

                //Static.
                if (fieldDef.IsStatic)
                {
                    LogError($"{fieldDef.Name} SyncType cannot be static.");
                    error = true;
                }

                //Generic.
                if (fieldDef.FieldType.IsGenericParameter)
                {
                    LogError($"{fieldDef.Name} SyncType cannot be be generic.");
                    error = true;
                }

                //SyncObject readonly check.
                if (syncObject && !fieldDef.Attributes.HasFlag(FieldAttributes.InitOnly))
                {
                    /* If missing readonly see if the user specified
                     * they want the object to be serialized. */
                    var requireReadOnly = customAttribute.GetField(nameof(SyncObjectAttribute.RequireReadOnly), true);
                    if (requireReadOnly)
                        LogError($"{fieldDef.Name} SyncObject must be readonly.");
                    error = true;
                }


                //If all checks passed.
                if (!error)
                    foundAttribute = customAttribute;
            }

            //If an error occurred then reset results.
            if (error)
                foundAttribute = null;

            return foundAttribute;
        }

        /// <summary>
        ///     Creates a syncVar class for the user's syncvar.
        /// </summary>
        /// <param name="originalFieldDef"></param>
        /// <param name="syncTypeAttribute"></param>
        /// <returns></returns>
        private bool CreateSyncVar(uint syncCount, TypeDefinition typeDef, FieldDefinition originalFieldDef,
            CustomAttribute syncTypeAttribute, out FieldDefinition createdSyncVarFd,
            out MethodReference accessorSetValueMethodRef, out MethodReference accessorGetValueMethodRef)
        {
            accessorGetValueMethodRef = null;
            accessorSetValueMethodRef = null;
            CreatedSyncVar createdSyncVar;
            createdSyncVarFd = CreateSyncVarFieldDefinition(typeDef, originalFieldDef, out createdSyncVar);

            if (createdSyncVarFd != null)
            {
                var hookMr = GetSyncVarHookMethodReference(typeDef, originalFieldDef, syncTypeAttribute);
                createdSyncVar.HookMr = hookMr;

                //If accessor was made add it's methods to createdSyncTypeObjects.
                if (CreateSyncVarAccessor(originalFieldDef, createdSyncVarFd, createdSyncVar,
                        out accessorGetValueMethodRef,
                        out accessorSetValueMethodRef, hookMr) != null)
                {
                    _createdSyncTypeMethodDefinitions.Add(accessorGetValueMethodRef.CachedResolve(Session));
                    _createdSyncTypeMethodDefinitions.Add(accessorSetValueMethodRef.CachedResolve(Session));
                }

                InitializeSyncVar(syncCount, createdSyncVarFd, typeDef, originalFieldDef, syncTypeAttribute,
                    createdSyncVar);

                var syncVarReadMd = CreateSyncVarRead(typeDef, syncCount, originalFieldDef, accessorSetValueMethodRef);
                if (syncVarReadMd != null)
                    _createdSyncTypeMethodDefinitions.Add(syncVarReadMd);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Creates or gets a SyncType class for originalFieldDef.
        /// </summary>
        /// <returns></returns>
        private FieldDefinition CreateSyncVarFieldDefinition(TypeDefinition typeDef, FieldDefinition originalFieldDef,
            out CreatedSyncVar createdSyncVar)
        {
            createdSyncVar = GetClass<CreatedSyncVarGenerator>().GetCreatedSyncVar(originalFieldDef, true);
            if (createdSyncVar == null)
                return null;

            originalFieldDef.Attributes &= ~FieldAttributes.Private;
            originalFieldDef.Attributes |= FieldAttributes.Public;

            var createdFieldDef = new FieldDefinition($"{SYNCVAR_PREFIX}{originalFieldDef.Name}",
                originalFieldDef.Attributes, createdSyncVar.SyncVarGit);
            if (createdFieldDef == null)
            {
                LogError(
                    $"Could not create field for Sync type {originalFieldDef.FieldType.FullName}, name of {originalFieldDef.Name}.");
                return null;
            }

            typeDef.Fields.Add(createdFieldDef);
            return createdFieldDef;
        }

        /// <summary>
        ///     Validates and gets the hook MethodReference for a SyncVar if available.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="typeDef"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private MethodReference GetSyncVarHookMethodReference(TypeDefinition typeDef, FieldDefinition originalFieldDef,
            CustomAttribute attribute)
        {
            var hook = attribute.GetField("OnChange", string.Empty);
            //No hook is specified.
            if (string.IsNullOrEmpty(hook))
                return null;

            var md = typeDef.GetMethod(hook);

            if (md != null)
            {
                var incorrectParametersMsg =
                    $"OnChange method for {originalFieldDef.FullName} must contain 3 parameters in order of {originalFieldDef.FieldType.Name} oldValue, {originalFieldDef.FieldType.Name} newValue, {Module.TypeSystem.Boolean} asServer.";
                //Not correct number of parameters.
                if (md.Parameters.Count != 3)
                {
                    LogError(incorrectParametersMsg);
                    return null;
                }

                /* Check if any parameters are not
                 * the expected type. */
                if (md.Parameters[0].ParameterType.CachedResolve(Session) !=
                    originalFieldDef.FieldType.CachedResolve(Session) ||
                    md.Parameters[1].ParameterType.CachedResolve(Session) !=
                    originalFieldDef.FieldType.CachedResolve(Session) ||
                    md.Parameters[2].ParameterType.CachedResolve(Session) !=
                    Module.TypeSystem.Boolean.CachedResolve(Session))
                {
                    LogError(incorrectParametersMsg);
                    return null;
                }

                //If here everything checks out, return a method reference to hook method.
                return ImportReference(md);
            }
            //Hook specified but no method found.

            LogError($"Could not find method name {hook} for SyncType {originalFieldDef.FullName}.");
            return null;
        }

        /// <summary>
        ///     Creates accessor for a SyncVar.
        /// </summary>
        /// <returns></returns>
        private FieldDefinition CreateSyncVarAccessor(FieldDefinition originalFd, FieldDefinition createdSyncVarFd,
            CreatedSyncVar createdSyncVar, out MethodReference accessorGetValueMr,
            out MethodReference accessorSetValueMr, MethodReference hookMr)
        {
            /* Create and add property definition. */
            var createdPropertyDef = new PropertyDefinition($"SyncAccessor_{originalFd.Name}", PropertyAttributes.None,
                originalFd.FieldType);
            createdPropertyDef.DeclaringType = originalFd.DeclaringType;
            //add the methods and property to the type.
            originalFd.DeclaringType.Properties.Add(createdPropertyDef);

            ILProcessor processor;

            /* Get method for property definition. */
            var createdGetMethodDef = originalFd.DeclaringType.AddMethod(
                $"{ACCESSOR_PREFIX}get_value_{originalFd.Name}", MethodAttributes.Public |
                                                                 MethodAttributes.SpecialName |
                                                                 MethodAttributes.HideBySig,
                originalFd.FieldType);
            createdGetMethodDef.SemanticsAttributes = MethodSemanticsAttributes.Getter;

            processor = createdGetMethodDef.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, originalFd);
            processor.Emit(OpCodes.Ret);
            accessorGetValueMr = ImportReference(createdGetMethodDef);
            //Add getter to properties.
            createdPropertyDef.GetMethod = createdGetMethodDef;

            /* Set method. */
            //Create the set method
            var createdSetMethodDef = originalFd.DeclaringType.AddMethod(
                $"{ACCESSOR_PREFIX}set_value_{originalFd.Name}", MethodAttributes.Public |
                                                                 MethodAttributes.SpecialName |
                                                                 MethodAttributes.HideBySig);
            createdSetMethodDef.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            var valueParameterDef = GetClass<GeneralHelper>()
                .CreateParameter(createdSetMethodDef, originalFd.FieldType, "value");
            var calledByUserParameterDef =
                GetClass<GeneralHelper>().CreateParameter(createdSetMethodDef, typeof(bool), "asServer");
            processor = createdSetMethodDef.Body.GetILProcessor();

            /* Assign to new value. Do this first because SyncVar<T> calls hook
             * and value needs to be updated before hook. Only update
             * value if calledByUser(asServer) or (!calledByUser && !base.IsServer).
             * This ensures clientHost will not overwrite server value. */

            var afterChangeFieldInst = processor.Create(OpCodes.Nop);
            var beforeChangeFieldInst = processor.Create(OpCodes.Nop);
            //if (calledByUser || !base.IsServer)
            processor.Emit(OpCodes.Ldarg, calledByUserParameterDef);
            processor.Emit(OpCodes.Brtrue, beforeChangeFieldInst);
            processor.Emit(OpCodes.Ldarg_0); //this.            
            processor.Emit(OpCodes.Call, GetClass<NetworkBehaviourHelper>().IsServer_MethodRef);
            processor.Emit(OpCodes.Brtrue, afterChangeFieldInst);

            //      _originalField = value;
            processor.Append(beforeChangeFieldInst);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            processor.Emit(OpCodes.Stfld, originalFd);
            processor.Append(afterChangeFieldInst);

            var retInst = processor.Create(OpCodes.Ret);

            if (!Configuration.Configurations.CodeStripping.IsBuilding)
            {
                processor.Emit(OpCodes.Call, GetClass<GeneralHelper>().Application_IsPlaying_MethodRef);
                processor.Emit(OpCodes.Brfalse_S, retInst);
            }

            //      SyncVar<>.SetValue(....);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, createdSyncVarFd);
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            processor.Emit(OpCodes.Ldarg, calledByUserParameterDef);
            processor.Emit(createdSyncVar.SetValueMr.GetCallOpCode(Session), createdSyncVar.SetValueMr);

            processor.Append(retInst);
            accessorSetValueMr = ImportReference(createdSetMethodDef);
            //Add setter to properties.
            createdPropertyDef.SetMethod = createdSetMethodDef;

            return originalFd;
        }

        /// <summary>
        ///     Sets methods used from SyncBase for typeDef.
        /// </summary>
        /// <returns></returns>
        internal bool SetSyncBaseMethods(TypeDefinition typeDef, out MethodReference setRegisteredMr,
            out MethodReference initializeInstanceMr)
        {
            setRegisteredMr = null;
            initializeInstanceMr = null;
            //Find the SyncBase class.
            TypeDefinition syncBaseTd = null;
            var copyTd = typeDef;
            do
            {
                if (copyTd.Name == nameof(SyncBase))
                {
                    syncBaseTd = copyTd;
                    break;
                }

                copyTd = copyTd.GetNextBaseTypeDefinition(Session);
            } while (copyTd != null);

            //If SyncBase isn't found.
            if (syncBaseTd == null)
            {
                LogError($"Could not find SyncBase within type {typeDef.FullName}.");
                return false;
            }

            //InitializeInstance.
            initializeInstanceMr = syncBaseTd.GetMethodReference(Session, INITIALIZEINSTANCE_METHOD_NAME);
            //SetSyncIndex.
            setRegisteredMr = syncBaseTd.GetMethodReference(Session, SETREGISTERED_METHOD_NAME);
            return true;
        }

        /// <summary>
        ///     Initializes a custom SyncObject.
        /// </summary>
        internal bool InitializeCustom(uint syncCount, TypeDefinition typeDef, FieldDefinition originalFieldDef,
            CustomAttribute attribute)
        {
            var sendRate = 0.1f;
            var writePermissions = WritePermission.ServerOnly;
            var readPermissions = ReadPermission.Observers;
            var channel = Channel.Reliable;
            //If attribute isn't null then override values.
            if (attribute != null)
            {
                sendRate = attribute.GetField("SendRate", 0.1f);
                writePermissions = WritePermission.ServerOnly;
                readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
                channel = Channel.Reliable; //attribute.GetField("Channel", Channel.Reliable);
            }

            //Set needed methods from syncbase.
            MethodReference setSyncIndexMr;
            MethodReference initializeInstanceMr;
            if (!SetSyncBaseMethods(originalFieldDef.FieldType.CachedResolve(Session), out setSyncIndexMr,
                    out initializeInstanceMr))
                return false;

            MethodDefinition injectionMethodDef;
            ILProcessor processor;

            var hash = syncCount;
            var insts = new List<Instruction>();

            /* Initialize with attribute settings. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_EARLY_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();
            //

            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)hash));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            insts.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            insts.Add(processor.Create(OpCodes.Ldc_I4_1)); //true for syncObject.
            insts.Add(processor.Create(OpCodes.Call, initializeInstanceMr));
            processor.InsertFirst(insts);

            insts.Clear();
            /* Set NetworkBehaviour and SyncIndex to use. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_LATE_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();
            //
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(setSyncIndexMr.GetCallOpCode(Session), setSyncIndexMr));

            processor.InsertLast(insts);

            return true;
        }


        /// <summary>
        ///     Initializes a SyncList.
        /// </summary>
        internal bool InitializeSyncList_SyncHashSet(uint syncCount, TypeDefinition typeDef,
            FieldDefinition originalFieldDef, CustomAttribute attribute)
        {
            var sendRate = 0.1f;
            var writePermissions = WritePermission.ServerOnly;
            var readPermissions = ReadPermission.Observers;
            var channel = Channel.Reliable;
            //If attribute isn't null then override values.
            if (attribute != null)
            {
                sendRate = attribute.GetField("SendRate", 0.1f);
                writePermissions = WritePermission.ServerOnly;
                readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
                channel = Channel.Reliable; //attribute.GetField("Channel", Channel.Reliable);
            }

            //This import shouldn't be needed but cecil is stingy so rather be safe than sorry.
            ImportReference(originalFieldDef);

            //Set needed methods from syncbase.
            MethodReference setSyncIndexMr;
            MethodReference initializeInstanceMr;
            if (!SetSyncBaseMethods(originalFieldDef.FieldType.CachedResolve(Session), out setSyncIndexMr,
                    out initializeInstanceMr))
                return false;

            MethodDefinition injectionMethodDef;
            ILProcessor processor;

            var hash = syncCount;
            var insts = new List<Instruction>();

            /* Initialize with attribute settings. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_EARLY_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();

            //InitializeInstance.
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)hash));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            insts.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            insts.Add(processor.Create(OpCodes.Ldc_I4_1)); //true for syncObject.
            insts.Add(processor.Create(OpCodes.Call, initializeInstanceMr));
            processor.InsertFirst(insts);

            insts.Clear();
            /* Set NetworkBehaviour and SyncIndex to use. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_LATE_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();

            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(setSyncIndexMr.GetCallOpCode(Session), setSyncIndexMr));

            processor.InsertLast(insts);

            return true;
        }


        /// <summary>
        ///     Initializes a SyncDictionary.
        /// </summary>
        internal bool InitializeSyncDictionary(uint syncCount, TypeDefinition typeDef, FieldDefinition originalFieldDef,
            CustomAttribute attribute)
        {
            var sendRate = 0.1f;
            var writePermissions = WritePermission.ServerOnly;
            var readPermissions = ReadPermission.Observers;
            var channel = Channel.Reliable;
            //If attribute isn't null then override values.
            if (attribute != null)
            {
                sendRate = attribute.GetField("SendRate", 0.1f);
                writePermissions = WritePermission.ServerOnly;
                readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
                channel = Channel.Reliable; //attribute.GetField("Channel", Channel.Reliable);
            }

            //This import shouldn't be needed but cecil is stingy so rather be safe than sorry.
            ImportReference(originalFieldDef);

            //Set needed methods from syncbase.
            MethodReference setRegisteredMr;
            MethodReference initializeInstanceMr;
            if (!SetSyncBaseMethods(originalFieldDef.FieldType.CachedResolve(Session), out setRegisteredMr,
                    out initializeInstanceMr))
                return false;

            var injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_EARLY_INTERNAL_NAME);
            var processor = injectionMethodDef.Body.GetILProcessor();

            var hash = syncCount;
            var insts = new List<Instruction>();

            /* Initialize with attribute settings. */
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)hash));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            insts.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            insts.Add(processor.Create(OpCodes.Ldc_I4_1)); //true for syncObject.
            insts.Add(processor.Create(OpCodes.Call, initializeInstanceMr));
            processor.InsertFirst(insts);

            insts.Clear();
            /* Set NetworkBehaviour and SyncIndex to use. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_LATE_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();

            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            insts.Add(processor.Create(setRegisteredMr.GetCallOpCode(Session), setRegisteredMr));

            processor.InsertFirst(insts);

            return true;
        }


        /// <summary>
        ///     Initializes a SyncVar<>.
        /// </summary>
        internal void InitializeSyncVar(uint syncCount, FieldDefinition createdFd, TypeDefinition typeDef,
            FieldDefinition originalFd, CustomAttribute attribute, CreatedSyncVar createdSyncVar)
        {
            var gh = GetClass<GeneralHelper>();

            //Get all possible attributes.
            var sendRate = attribute.GetField("SendRate", 0.1f);
            var writePermissions = WritePermission.ServerOnly;
            var readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
            var channel = attribute.GetField("Channel", Channel.Reliable);

            var injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_EARLY_INTERNAL_NAME);
            var processor = injectionMethodDef.Body.GetILProcessor();

            var hash = syncCount;
            var insts = new List<Instruction>();
            //Initialize fieldDef with values from attribute.
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)hash));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            insts.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            insts.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.

            insts.Add(processor.Create(OpCodes.Ldfld, originalFd.MakeHostGenericIfNeeded(Session))); //initial value.
            insts.Add(processor.Create(OpCodes.Newobj, createdSyncVar.ConstructorMr));
            insts.Add(processor.Create(OpCodes.Stfld, createdFd.MakeHostGenericIfNeeded(Session)));

            //If there is a hook method.
            if (createdSyncVar.HookMr != null)
            {
                //SyncVar<dataType>.add_OnChanged (event).
                var svTd = GetClass<CreatedSyncVarGenerator>().SyncVar_TypeRef.CachedResolve(Session);
                var svGit = svTd.MakeGenericInstanceType(originalFd.FieldType);
                var addMd = svTd.GetMethod("add_OnChange");
                var syncVarAddMr = addMd.MakeHostInstanceGeneric(Session, svGit);

                //Action<dataType, dataType, bool> constructor.
                var actionGit = gh.ActionT3TypeRef.MakeGenericInstanceType(
                    originalFd.FieldType, originalFd.FieldType,
                    GetClass<GeneralHelper>().GetTypeReference(typeof(bool)));
                var gitActionCtorMr = gh.ActionT3ConstructorMethodRef.MakeHostInstanceGeneric(Session, actionGit);

                //      syncVar___field.OnChanged += UserHookMethod;
                insts.Add(processor.Create(OpCodes.Ldarg_0));
                insts.Add(processor.Create(OpCodes.Ldfld, createdFd));
                insts.Add(processor.Create(OpCodes.Ldarg_0));

                //Load the callback function.
                var hookMd = createdSyncVar.HookMr.CachedResolve(Session);
                OpCode ldOpCode;
                if (hookMd.IsVirtual)
                {
                    insts.Add(processor.Create(OpCodes.Dup));
                    ldOpCode = OpCodes.Ldvirtftn;
                }
                else
                {
                    ldOpCode = OpCodes.Ldftn;
                }

                insts.Add(processor.Create(ldOpCode, hookMd));

                insts.Add(processor.Create(OpCodes.Newobj, gitActionCtorMr));
                insts.Add(processor.Create(syncVarAddMr.GetCallOpCode(Session), syncVarAddMr));
            }

            processor.InsertFirst(insts);

            insts.Clear();
            /* Set NetworkBehaviour and SyncIndex to use. */
            injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_LATE_INTERNAL_NAME);
            processor = injectionMethodDef.Body.GetILProcessor();

            //Set NB and SyncIndex to SyncVar<>.
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, createdFd));
            insts.Add(processor.Create(createdSyncVar.SetSyncIndexMr.GetCallOpCode(Session),
                createdSyncVar.SetSyncIndexMr));

            processor.InsertFirst(insts);
        }

        /// <summary>
        ///     Replaces GetSets for methods which may use a SyncType.
        /// </summary>
        /// <param name="modifiableMethods"></param>
        /// <param name="processedSyncs"></param>
        internal bool ReplaceGetSetDirties(List<MethodDefinition> modifiableMethods,
            List<(SyncType, ProcessedSync)> processedSyncs)
        {
            //Build processed syncs into dictionary for quicker loookups.
            var processedLookup = new Dictionary<FieldReference, List<ProcessedSync>>();
            foreach (var (st, ps) in processedSyncs)
            {
                if (st != SyncType.Variable)
                    continue;

                List<ProcessedSync> result;
                if (!processedLookup.TryGetValue(ps.OriginalFieldRef, out result))
                {
                    result = new List<ProcessedSync> { ps };
                    processedLookup.Add(ps.OriginalFieldRef, result);
                }

                result.Add(ps);
            }

            var modified = false;
            foreach (var methodDef in modifiableMethods)
                modified |= ReplaceGetSetDirty(methodDef, processedLookup);

            return modified;
        }

        /// <summary>
        ///     Replaces GetSets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="processedLookup"></param>
        private bool ReplaceGetSetDirty(MethodDefinition methodDef,
            Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            if (methodDef == null)
            {
                LogError("An object expecting value was null. Please try saving your script again.");
                return false;
            }

            if (methodDef.IsAbstract)
                return false;
            if (_createdSyncTypeMethodDefinitions.Contains(methodDef))
                return false;
            if (methodDef.Name == NetworkBehaviourProcessor.NETWORKINITIALIZE_EARLY_INTERNAL_NAME)
                return false;


            var modified = false;

            for (var i = 0; i < methodDef.Body.Instructions.Count; i++)
            {
                var inst = methodDef.Body.Instructions[i];

                /* Loading a field. (Getter) */
                if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference opFieldld)
                {
                    FieldReference resolvedOpField = opFieldld.CachedResolve(Session);
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldld.DeclaringType.CachedResolve(Session)
                            .GetFieldReference(opFieldld.Name, Session);

                    modified |= ProcessGetField(methodDef, i, resolvedOpField, processedLookup);
                }
                /* Load address, reference field. */
                else if (inst.OpCode == OpCodes.Ldflda && inst.Operand is FieldReference opFieldlda)
                {
                    FieldReference resolvedOpField = opFieldlda.CachedResolve(Session);
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldlda.DeclaringType.CachedResolve(Session)
                            .GetFieldReference(opFieldlda.Name, Session);

                    modified |= ProcessAddressField(methodDef, i, resolvedOpField, processedLookup);
                }
                /* Setting a field. (Setter) */
                else if (inst.OpCode == OpCodes.Stfld && inst.Operand is FieldReference opFieldst)
                {
                    FieldReference resolvedOpField = opFieldst.CachedResolve(Session);
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldst.DeclaringType.CachedResolve(Session)
                            .GetFieldReference(opFieldst.Name, Session);

                    modified |= ProcessSetField(methodDef, i, resolvedOpField, processedLookup);
                }
            }

            return modified;
        }

        /// <summary>
        ///     Replaces Gets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private bool ProcessGetField(MethodDefinition methodDef, int instructionIndex, FieldReference resolvedOpField,
            Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            var inst = methodDef.Body.Instructions[instructionIndex];

            //If was a replaced field.
            if (processedLookup.TryGetValue(resolvedOpField, out var psLst))
            {
                var ps = GetProcessedSync(resolvedOpField, psLst);
                if (ps == null)
                    return false;
                //Don't modify the accessor method.
                if (ps.GetMethodRef.CachedResolve(Session) == methodDef)
                    return false;

                //Generic type.
                if (resolvedOpField.DeclaringType.IsGenericInstance ||
                    resolvedOpField.DeclaringType.HasGenericParameters)
                {
                    var newField = inst.Operand as FieldReference;
                    var git = (GenericInstanceType)newField.DeclaringType;
                    var syncvarGetMr = ps.GetMethodRef.MakeHostInstanceGeneric(Session, git);
                    inst.OpCode = syncvarGetMr.GetCallOpCode(Session);
                    inst.Operand = syncvarGetMr;
                }
                //Strong type.
                else
                {
                    inst.OpCode = OpCodes.Call;
                    inst.Operand = ps.GetMethodRef;
                }

                return true;
            }

            return false;
        }


        /// <summary>
        ///     Replaces Sets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private bool ProcessSetField(MethodDefinition methodDef, int instructionIndex, FieldReference resolvedOpField,
            Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            var inst = methodDef.Body.Instructions[instructionIndex];

            /* Find any instructions that are jmp/breaking to the one we are modifying.
             * These need to be modified to call changed instruction. */
            var brInstructions = new HashSet<Instruction>();
            foreach (var item in methodDef.Body.Instructions)
            {
                var canJmp = item.OpCode == OpCodes.Br || item.OpCode == OpCodes.Brfalse ||
                             item.OpCode == OpCodes.Brfalse_S || item.OpCode == OpCodes.Brtrue ||
                             item.OpCode == OpCodes.Brtrue_S || item.OpCode == OpCodes.Br_S;
                if (!canJmp)
                    continue;
                if (item.Operand == null)
                    continue;
                if (item.Operand is Instruction jmpInst && jmpInst == inst)
                    brInstructions.Add(item);
            }

            //If was a replaced field.
            if (processedLookup.TryGetValue(resolvedOpField, out var psLst))
            {
                var ps = GetProcessedSync(resolvedOpField, psLst);
                if (ps == null)
                    return false;
                //Don't modify the accessor method.
                if (ps.SetMethodRef.CachedResolve(Session) == methodDef)
                    return false;
                var processor = methodDef.Body.GetILProcessor();

                //Generic type.
                if (resolvedOpField.DeclaringType.IsGenericInstance ||
                    resolvedOpField.DeclaringType.HasGenericParameters)
                {
                    //Pass in true for as server.
                    var boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                    methodDef.Body.Instructions.Insert(instructionIndex, boolTrueInst);

                    var newField = inst.Operand as FieldReference;
                    var git = (GenericInstanceType)newField.DeclaringType;
                    inst.OpCode = OpCodes.Call;
                    inst.Operand = ps.SetMethodRef.MakeHostInstanceGeneric(Session, git);
                }
                //Strong typed.
                else
                {
                    //Pass in true for as server.
                    var boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                    methodDef.Body.Instructions.Insert(instructionIndex, boolTrueInst);
                    inst.OpCode = OpCodes.Call;
                    inst.Operand = ps.SetMethodRef;
                }


                /* If any instructions are still pointing
                 * to modified value then they need to be
                 * redirected to the instruction right above it.
                 * This is because the boolTrueInst, to indicate
                 * value is being set as server. */
                foreach (var item in brInstructions)
                    if (item.Operand is Instruction jmpInst && jmpInst == inst)
                    {
                        //Use the same index that was passed in, which is now one before modified instruction.
                        var newInst = methodDef.Body.Instructions[instructionIndex];
                        item.Operand = newInst;
                    }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Replaces address Sets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private bool ProcessAddressField(MethodDefinition methodDef, int instructionIndex,
            FieldReference resolvedOpField, Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            var inst = methodDef.Body.Instructions[instructionIndex];
            //Check if next instruction is Initobj, which would be setting a new instance.
            var nextInstr = inst.Next;
            if (nextInstr.OpCode != OpCodes.Initobj)
                return false;

            //If was a replaced field.
            if (processedLookup.TryGetValue(resolvedOpField, out var psLst))
            {
                var ps = GetProcessedSync(resolvedOpField, psLst);
                if (ps == null)
                    return false;
                //Don't modify the accessor method.
                if (ps.GetMethodRef.CachedResolve(Session) == methodDef ||
                    ps.SetMethodRef.CachedResolve(Session) == methodDef)
                    return false;

                var processor = methodDef.Body.GetILProcessor();

                var tmpVariableDef = GetClass<GeneralHelper>().CreateVariable(methodDef, resolvedOpField.FieldType);
                processor.InsertBefore(inst, processor.Create(OpCodes.Ldloca, tmpVariableDef));
                processor.InsertBefore(inst, processor.Create(OpCodes.Initobj, resolvedOpField.FieldType));
                processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, tmpVariableDef));
                var newInstr = processor.Create(OpCodes.Call, ps.SetMethodRef);
                processor.InsertBefore(inst, newInstr);

                /* Pass in true for as server.
                 * The instruction index is 3 past ld. */
                var boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                methodDef.Body.Instructions.Insert(instructionIndex + 3, boolTrueInst);

                processor.Remove(inst);
                processor.Remove(nextInstr);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Calls ReadSyncVar going up the hierarchy.
        /// </summary>
        /// <param name="firstTypeDef"></param>
        internal void CallBaseReadSyncVar(TypeDefinition firstTypeDef)
        {
            var readSyncVarName = GetClass<NetworkBehaviourHelper>().ReadSyncVar_MethodRef.Name;
            //TypeDef which needs to make the base call.
            MethodDefinition callerMd = null;
            var copyTd = firstTypeDef;
            do
            {
                MethodDefinition readMd;

                readMd = copyTd.GetMethod(readSyncVarName);
                if (readMd != null)
                    callerMd = readMd;

                /* If baseType exist and it's not networkbehaviour
                 * look into calling the ReadSyncVar method. */
                if (copyTd.BaseType != null && copyTd.BaseType.FullName != GetClass<NetworkBehaviourHelper>().FullName)
                {
                    readMd = copyTd.BaseType.CachedResolve(Session).GetMethod(readSyncVarName);
                    //Not all classes will have syncvars to read.
                    if (!_baseCalledReadSyncVars.Contains(callerMd) && readMd != null && callerMd != null)
                    {
                        var baseReadMr =
                            copyTd.GetMethodReferenceInBase(Session,
                                readSyncVarName); //  readMd.GetMethodReferenceInBase (base.Session, base.ImportReference(readMd);
                        var processor = callerMd.Body.GetILProcessor();
                        /* Calls base.ReadSyncVar and if result is true
                         * then exit methods. This is because a true return means the base
                         * was able to process the syncvar. */
                        var baseCallInsts = new List<Instruction>();
                        var skipBaseReturn = processor.Create(OpCodes.Nop);
                        baseCallInsts.Add(processor.Create(OpCodes.Ldarg_0)); //This.
                        baseCallInsts.Add(processor.Create(OpCodes.Ldarg_1)); //PooledReader.
                        baseCallInsts.Add(processor.Create(OpCodes.Ldarg_2)); //Index.
                        baseCallInsts.Add(processor.Create(OpCodes.Call, baseReadMr));
                        baseCallInsts.Add(processor.Create(OpCodes.Brfalse_S, skipBaseReturn));
                        baseCallInsts.Add(processor.Create(OpCodes.Ldc_I4_1));
                        baseCallInsts.Add(processor.Create(OpCodes.Ret));
                        baseCallInsts.Add(skipBaseReturn);
                        processor.InsertFirst(baseCallInsts);

                        _baseCalledReadSyncVars.Add(callerMd);
                    }
                }

                copyTd = copyTd.GetNextBaseClassToProcess(Session);
            } while (copyTd != null);
        }

        /// <summary>
        ///     Reads a PooledReader locally then sets value to the SyncVars accessor.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="syncIndex"></param>
        /// <param name="originalFieldDef"></param>
        private MethodDefinition CreateSyncVarRead(TypeDefinition typeDef, uint syncIndex,
            FieldDefinition originalFieldDef, MethodReference accessorSetMethodRef)
        {
            Instruction jmpGoalInst;
            ILProcessor processor;

            //Get the read sync method, or create it if not present.
            var readSyncMethodDef = typeDef.GetMethod(GetClass<NetworkBehaviourHelper>().ReadSyncVar_MethodRef.Name);
            if (readSyncMethodDef == null)
            {
                readSyncMethodDef = new MethodDefinition(GetClass<NetworkBehaviourHelper>().ReadSyncVar_MethodRef.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    typeDef.Module.TypeSystem.Void);
                readSyncMethodDef.ReturnType = GetClass<GeneralHelper>().GetTypeReference(typeof(bool));

                GetClass<GeneralHelper>().CreateParameter(readSyncMethodDef, typeof(PooledReader));
                GetClass<GeneralHelper>().CreateParameter(readSyncMethodDef, typeof(uint));
                readSyncMethodDef.Body.InitLocals = true;

                processor = readSyncMethodDef.Body.GetILProcessor();
                //Return false as fall through.
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Ret);

                typeDef.Methods.Add(readSyncMethodDef);
            }
            //Already created. 
            else
            {
                processor = readSyncMethodDef.Body.GetILProcessor();
            }

            var pooledReaderParameterDef = readSyncMethodDef.Parameters[0];
            var indexParameterDef = readSyncMethodDef.Parameters[1];
            VariableDefinition nextValueVariableDef;
            List<Instruction> readInsts;

            /* Create a nop instruction placed at the first index of the method.
             * All instructions will be added before this, then the nop will be
             * removed afterwards. This ensures the newer instructions will
             * be above the previous. This let's the IL jump to a previously
             * created read instruction when the latest one fails conditions. */
            var nopPlaceHolderInst = processor.Create(OpCodes.Nop);

            readSyncMethodDef.Body.Instructions.Insert(0, nopPlaceHolderInst);

            /* If there was a previously made read then set jmp goal to the first
             * condition for it. Otherwise set it to the last instruction, which would
             * be a ret. Keep in mind if ret has a value we must go back 2 index
             * rather than one. */
            jmpGoalInst = _lastReadInstruction != null
                ? _lastReadInstruction
                : readSyncMethodDef.Body.Instructions[readSyncMethodDef.Body.Instructions.Count - 2];

            //Check index first. if (index != syncIndex) return
            var nextLastReadInstruction = processor.Create(OpCodes.Ldarg, indexParameterDef);
            processor.InsertBefore(jmpGoalInst, nextLastReadInstruction);

            var hash = syncIndex;
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4, (int)hash));
            //processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4, syncIndex));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Bne_Un, jmpGoalInst));
            //PooledReader.ReadXXXX()
            readInsts = GetClass<ReaderProcessor>().CreateRead(readSyncMethodDef, pooledReaderParameterDef,
                originalFieldDef.FieldType, out nextValueVariableDef);
            if (readInsts == null)
                return null;
            //Add each instruction from CreateRead.
            foreach (var i in readInsts)
                processor.InsertBefore(jmpGoalInst, i);

            //Call accessor with new value and false for asServer
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldarg_0)); //this.
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldloc, nextValueVariableDef));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4_0));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Call, accessorSetMethodRef));
            //Return true when able to process.
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4_1));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ret));

            _lastReadInstruction = nextLastReadInstruction;
            processor.Remove(nopPlaceHolderInst);

            return readSyncMethodDef;
        }

        /// <summary>
        ///     Returns methods which may be modified by code generation.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        private List<MethodDefinition> GetModifiableMethods(TypeDefinition typeDef)
        {
            var results = new List<MethodDefinition>();

            CheckTypeDefinition(typeDef);
            //Have to add nested types because this are where courotines are stored.
            foreach (var nestedTd in typeDef.NestedTypes)
                CheckTypeDefinition(nestedTd);

            void CheckTypeDefinition(TypeDefinition td)
            {
                foreach (var methodDef in td.Methods)
                {
                    if (methodDef.Name == ".cctor")
                        continue;
                    if (methodDef.IsConstructor)
                        continue;
                    if (methodDef.Body == null)
                        continue;

                    results.Add(methodDef);
                }

                foreach (var propertyDef in td.Properties)
                {
                    if (propertyDef.GetMethod != null)
                        results.Add(propertyDef.GetMethod);
                    if (propertyDef.SetMethod != null)
                        results.Add(propertyDef.SetMethod);
                }
            }

            return results;
        }

        /// <summary>
        ///     Returns the ProcessedSync entry for resolvedOpField.
        /// </summary>
        /// <param name="resolvedOpField"></param>
        /// <param name="psLst"></param>
        /// <returns></returns>
        private ProcessedSync GetProcessedSync(FieldReference resolvedOpField, List<ProcessedSync> psLst)
        {
            for (var i = 0; i < psLst.Count; i++)
                if (psLst[i].OriginalFieldRef == resolvedOpField)
                    return psLst[i];

            /* Fall through, not found. */
            LogError($"Unable to find user referenced field for {resolvedOpField.Name}.");
            return null;
        }

        #region Private.

        /// <summary>
        ///     Last instruction to read a sync type.
        /// </summary>
        private Instruction _lastReadInstruction;

        /// <summary>
        ///     Sync objects, such as get and set, created during this process. Used to skip modifying created methods.
        /// </summary>
        private readonly List<object> _createdSyncTypeMethodDefinitions = new();

        /// <summary>
        ///     ReadSyncVar methods which have had their base call already made.
        /// </summary>
        private readonly HashSet<MethodDefinition> _baseCalledReadSyncVars = new();

        #endregion

        #region Const.

        private const string SYNCVAR_PREFIX = "syncVar___";
        private const string ACCESSOR_PREFIX = "sync___";
        private const string SETREGISTERED_METHOD_NAME = "SetRegistered";
        private const string INITIALIZEINSTANCE_METHOD_NAME = "InitializeInstance";
        private const string GETSERIALIZEDTYPE_METHOD_NAME = "GetSerializedType";

        #endregion
    }
}