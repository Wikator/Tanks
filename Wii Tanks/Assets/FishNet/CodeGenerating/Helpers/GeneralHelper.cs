using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FishNet.CodeGenerating.Extension;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.ILCore;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Object.Helping;
using FishNet.Serializing;
using FishNet.Serializing.Helping;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;
using MonoFN.Cecil.Rocks;
using UnityEngine;
using SR = System.Reflection;

namespace FishNet.CodeGenerating.Helping
{
    internal class GeneralHelper : CodegenBase
    {
        #region Const.

        public const string UNITYENGINE_ASSEMBLY_PREFIX = "UnityEngine.";

        #endregion

        public override bool ImportReferences()
        {
            Type tmpType;
            TypeReference tmpTr;
            SR.MethodInfo tmpMi;
            SR.PropertyInfo tmpPi;

            NonSerialized_Attribute_FullName = typeof(NonSerializedAttribute).FullName;
            Single_FullName = typeof(float).FullName;

            ActionT2TypeRef = ImportReference(typeof(Action<,>));
            ActionT3TypeRef = ImportReference(typeof(Action<,,>));
            ActionT2ConstructorMethodRef = ImportReference(typeof(Action<,>).GetConstructors()[0]);
            ActionT3ConstructorMethodRef = ImportReference(typeof(Action<,,>).GetConstructors()[0]);

            CodegenExcludeAttribute_FullName = typeof(CodegenExcludeAttribute).FullName;
            CodegenIncludeAttribute_FullName = typeof(CodegenIncludeAttribute).FullName;

            tmpType = typeof(Queue<>);
            ImportReference(tmpType);
            tmpMi = tmpType.GetMethod("get_Count");
            Queue_get_Count_MethodRef = ImportReference(tmpMi);
            foreach (var mi in tmpType.GetMethods())
                if (mi.Name == nameof(Queue<int>.Enqueue))
                    Queue_Enqueue_MethodRef = ImportReference(mi);
                else if (mi.Name == nameof(Queue<int>.Dequeue))
                    Queue_Dequeue_MethodRef = ImportReference(mi);
                else if (mi.Name == nameof(Queue<int>.Clear))
                    Queue_Clear_MethodRef = ImportReference(mi);

            /* MISC */
            //
            tmpType = typeof(Application);
            tmpPi = tmpType.GetProperty(nameof(Application.isPlaying));
            if (tmpPi != null)
                Application_IsPlaying_MethodRef = ImportReference(tmpPi.GetMethod);
            //
            tmpType = typeof(ExtensionAttribute);
            tmpTr = ImportReference(tmpType);
            Extension_Attribute_Ctor_MethodRef = ImportReference(tmpTr.GetConstructor(Session));

            //Networkbehaviour.
            var networkBehaviourType = typeof(NetworkBehaviour);
            foreach (var methodInfo in networkBehaviourType.GetMethods())
                if (methodInfo.Name == nameof(NetworkBehaviour.CanLog))
                    NetworkBehaviour_CanLog_MethodRef = ImportReference(methodInfo);
            foreach (var propertyInfo in networkBehaviourType.GetProperties())
                if (propertyInfo.Name == nameof(NetworkBehaviour.NetworkManager))
                    NetworkBehaviour_NetworkManager_MethodRef = ImportReference(propertyInfo.GetMethod);

            //Instancefinder.
            var instanceFinderType = typeof(InstanceFinder);
            var getNetworkManagerPropertyInfo = instanceFinderType.GetProperty(nameof(InstanceFinder.NetworkManager));
            InstanceFinder_NetworkManager_MethodRef = ImportReference(getNetworkManagerPropertyInfo.GetMethod);

            //NetworkManager debug logs. 
            var networkManagerType = typeof(NetworkManager);
            foreach (var methodInfo in networkManagerType.GetMethods())
                if (methodInfo.Name == nameof(NetworkManager.Log) && methodInfo.GetParameters().Length == 1)
                    NetworkManager_LogCommon_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkManager.LogWarning))
                    NetworkManager_LogWarning_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkManager.LogError))
                    NetworkManager_LogError_MethodRef = ImportReference(methodInfo);

            //Lists.
            tmpType = typeof(List<>);
            List_TypeRef = ImportReference(tmpType);
            SR.MethodInfo lstMi;
            lstMi = tmpType.GetMethod("Add");
            List_Add_MethodRef = ImportReference(lstMi);
            lstMi = tmpType.GetMethod("RemoveRange");
            List_RemoveRange_MethodRef = ImportReference(lstMi);
            lstMi = tmpType.GetMethod("get_Count");
            List_get_Count_MethodRef = ImportReference(lstMi);
            lstMi = tmpType.GetMethod("get_Item");
            List_get_Item_MethodRef = ImportReference(lstMi);
            lstMi = tmpType.GetMethod("Clear");
            List_Clear_MethodRef = ImportReference(lstMi);

            //Unity debug logs.
            var debugType = typeof(Debug);
            foreach (var methodInfo in debugType.GetMethods())
                if (methodInfo.Name == nameof(Debug.LogWarning) && methodInfo.GetParameters().Length == 1)
                    Debug_LogWarning_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(Debug.LogError) && methodInfo.GetParameters().Length == 1)
                    Debug_LogError_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(Debug.Log) && methodInfo.GetParameters().Length == 1)
                    Debug_LogCommon_MethodRef = ImportReference(methodInfo);

            var codegenHelper = typeof(CodegenHelper);
            foreach (var methodInfo in codegenHelper.GetMethods())
                if (methodInfo.Name == nameof(CodegenHelper.NetworkObject_Deinitializing))
                    NetworkObject_Deinitializing_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(CodegenHelper.IsClient))
                    IsClient_MethodRef = ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(CodegenHelper.IsServer))
                    IsServer_MethodRef = ImportReference(methodInfo);

            //Generic functions.
            FunctionT2TypeRef = ImportReference(typeof(Func<,>));
            FunctionT3TypeRef = ImportReference(typeof(Func<,,>));
            FunctionT2ConstructorMethodRef = ImportReference(typeof(Func<,>).GetConstructors()[0]);
            FunctionT3ConstructorMethodRef = ImportReference(typeof(Func<,,>).GetConstructors()[0]);

            GeneratedComparers();

            //Sets up for generated comparers.
            void GeneratedComparers()
            {
                var gh = GetClass<GeneralHelper>();
                GeneratedComparer_ClassTypeDef = gh.GetOrCreateClass(out _, WriterProcessor.GENERATED_TYPE_ATTRIBUTES,
                    "GeneratedComparers___Internal", null);
                bool created;
                GeneratedComparer_OnLoadMethodDef = gh.GetOrCreateMethod(GeneratedComparer_ClassTypeDef, out created,
                    WriterProcessor.INITIALIZEONCE_METHOD_ATTRIBUTES, WriterProcessor.INITIALIZEONCE_METHOD_NAME,
                    Module.TypeSystem.Void);
                if (created)
                {
                    gh.CreateRuntimeInitializeOnLoadMethodAttribute(GeneratedComparer_OnLoadMethodDef);
                    GeneratedComparer_OnLoadMethodDef.Body.GetILProcessor().Emit(OpCodes.Ret);
                }

                var repComparerType = typeof(GeneratedComparer<>);
                GeneratedComparer_TypeRef = ImportReference(repComparerType);
                SR.PropertyInfo pi;
                pi = repComparerType.GetProperty(nameof(GeneratedComparer<int>.Compare));
                GeneratedComparer_Compare_Set_MethodRef = ImportReference(pi.GetSetMethod());
                pi = repComparerType.GetProperty(nameof(GeneratedComparer<int>.IsDefault));
                GeneratedComparer_IsDefault_Set_MethodRef = ImportReference(pi.GetSetMethod());

                var iEquatableType = typeof(IEquatable<>);
                IEquatable_TypeRef = ImportReference(iEquatableType);
            }

            return true;
        }


        /// <summary>
        ///     Makes a method an extension method.
        /// </summary>
        public void MakeExtensionMethod(MethodDefinition md)
        {
            if (md.Parameters.Count == 0)
            {
                LogError($"Method {md.FullName} cannot be made an extension method because it has no parameters.");
                return;
            }

            md.Attributes |= MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
            var ca = new CustomAttribute(Extension_Attribute_Ctor_MethodRef);
            md.CustomAttributes.Add(ca);
        }

        /// <summary>
        ///     Removes characters which would create invalid comparisons when trying to compare generics.
        /// </summary>
        public string RemoveGenericBrackets(string str)
        {
            /* Fix example...
             * List`1<T> converts to...
             *  List`1.
             * System.Nullable`1<System.Int> converts to...
             *  System.Nullable`1System.Int */
            if (str.Contains(typeof(Nullable).FullName))
                return str;

            //Find bracket areas to remove.
            var startIndex = str.IndexOf("<");
            var endIndex = str.IndexOf(">");
            //If found.
            if (startIndex >= 0 && endIndex >= 0)
            {
                var result = str.Substring(0, startIndex);
                result += str.Substring(endIndex + 1);
                return result;
            }

            return str;
        }

        /// <summary>
        ///     Returns if typeDef should be ignored.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        public bool IgnoreTypeDefinition(TypeDefinition typeDef)
        {
            foreach (var item in typeDef.CustomAttributes)
                if (item.AttributeType.FullName == typeof(CodegenExcludeAttribute).FullName)
                    return true;

            return false;
        }

        /// <summary>
        ///     Returns if type uses CodegenExcludeAttribute.
        /// </summary>
        public bool CodegenExclude(SR.MethodInfo methodInfo)
        {
            foreach (var item in methodInfo.CustomAttributes)
                if (item.AttributeType == typeof(CodegenExcludeAttribute))
                    return true;

            return false;
        }

        /// <summary>
        ///     Returns if type uses CodegenExcludeAttribute.
        /// </summary>
        public bool CodegenExclude(MethodDefinition methodDef)
        {
            foreach (var item in methodDef.CustomAttributes)
                if (item.AttributeType.FullName == CodegenExcludeAttribute_FullName)
                    return true;

            return false;
        }

        /// <summary>
        ///     Returns if type uses CodegenExcludeAttribute.
        /// </summary>
        public bool CodegenExclude(FieldDefinition fieldDef)
        {
            foreach (var item in fieldDef.CustomAttributes)
                if (item.AttributeType.FullName == CodegenExcludeAttribute_FullName)
                    return true;

            return false;
        }

        /// <summary>
        ///     Returns if type uses CodegenIncludeAttribute.
        /// </summary>
        public bool CodegenInclude(FieldDefinition fieldDef)
        {
            foreach (var item in fieldDef.CustomAttributes)
                if (item.AttributeType.FullName == CodegenIncludeAttribute_FullName)
                    return true;

            return false;
        }

        /// <summary>
        ///     Returns if type uses CodegenExcludeAttribute.
        /// </summary>
        public bool CodegenExclude(PropertyDefinition propDef)
        {
            foreach (var item in propDef.CustomAttributes)
                if (item.AttributeType.FullName == CodegenExcludeAttribute_FullName)
                    return true;

            return false;
        }


        /// <summary>
        ///     Returns if type uses CodegenExcludeAttribute.
        /// </summary>
        public bool CodegenInclude(PropertyDefinition propDef)
        {
            foreach (var item in propDef.CustomAttributes)
                if (item.AttributeType.FullName == CodegenIncludeAttribute_FullName)
                    return true;

            return false;
        }


        /// <summary>
        ///     Calls copiedMd with the assumption md shares the same parameters.
        /// </summary>
        public void CallCopiedMethod(MethodDefinition md, MethodDefinition copiedMd)
        {
            var processor = md.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            foreach (var item in copiedMd.Parameters)
                processor.Emit(OpCodes.Ldarg, item);

            var mr = copiedMd.GetMethodReference(Session);
            processor.Emit(OpCodes.Call, mr);
        }

        /// <summary>
        ///     Removes countVd from list of dataFd starting at index 0.
        /// </summary>
        public List<Instruction> ListRemoveRange(MethodDefinition methodDef, FieldDefinition dataFd,
            TypeReference dataTr, VariableDefinition countVd)
        {
            /* Remove entries which exceed maximum buffer. */
            //Method references for uint/data list:
            //get_count, RemoveRange. */
            GenericInstanceType dataListGit;
            GetGenericLists(dataTr, out dataListGit);
            var lstDataRemoveRangeMr = GetClass<GeneralHelper>().List_RemoveRange_MethodRef
                .MakeHostInstanceGeneric(Session, dataListGit);

            var insts = new List<Instruction>();
            var processor = methodDef.Body.GetILProcessor();

            //Index 1 is the uint, 0 is the data.
            insts.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            insts.Add(processor.Create(OpCodes.Ldfld, dataFd));
            insts.Add(processor.Create(OpCodes.Ldc_I4_0));
            insts.Add(processor.Create(OpCodes.Ldloc, countVd));
            insts.Add(processor.Create(lstDataRemoveRangeMr.GetCallOpCode(Session), lstDataRemoveRangeMr));

            return insts;
        }

        /// <summary>
        ///     Outputs generic lists for dataTr and uint.
        /// </summary>
        public void GetGenericLists(TypeReference dataTr, out GenericInstanceType lstData)
        {
            var listDataTr = ImportReference(typeof(List<>));
            lstData = listDataTr.MakeGenericInstanceType(dataTr);
        }

        /// <summary>
        ///     Outputs generic lists for dataTr and uint.
        /// </summary>
        public void GetGenericQueues(TypeReference dataTr, out GenericInstanceType queueData)
        {
            var queueDataTr = ImportReference(typeof(Queue<>));
            queueData = queueDataTr.MakeGenericInstanceType(dataTr);
        }

        /// <summary>
        ///     Copies one method to another while transferring diagnostic paths.
        /// </summary>
        public MethodDefinition CopyIntoNewMethod(MethodDefinition originalMd, string toMethodName,
            out bool alreadyCreated)
        {
            var typeDef = originalMd.DeclaringType;

            var md = typeDef.GetOrCreateMethodDefinition(Session, toMethodName, originalMd, true, out var created);
            alreadyCreated = !created;
            if (alreadyCreated)
                return md;

            (md.Body, originalMd.Body) = (originalMd.Body, md.Body);
            //Move over all the debugging information
            foreach (var sequencePoint in originalMd.DebugInformation.SequencePoints)
                md.DebugInformation.SequencePoints.Add(sequencePoint);
            originalMd.DebugInformation.SequencePoints.Clear();

            foreach (var customInfo in originalMd.CustomDebugInformations)
                md.CustomDebugInformations.Add(customInfo);
            originalMd.CustomDebugInformations.Clear();
            //Swap debuginformation scope.
            (originalMd.DebugInformation.Scope, md.DebugInformation.Scope) =
                (md.DebugInformation.Scope, originalMd.DebugInformation.Scope);

            return md;
        }

        /// <summary>
        ///     Creates the RuntimeInitializeOnLoadMethod attribute for a method.
        /// </summary>
        public void CreateRuntimeInitializeOnLoadMethodAttribute(MethodDefinition methodDef, string loadType = "")
        {
            var attTypeRef = GetTypeReference(typeof(RuntimeInitializeOnLoadMethodAttribute));
            foreach (var item in methodDef.CustomAttributes)
                //Already exist.
                if (item.AttributeType.FullName == attTypeRef.FullName)
                    return;

            var parameterRequirement = loadType.Length == 0 ? 0 : 1;
            var constructorMethodDef = attTypeRef.GetConstructor(Session, parameterRequirement);
            var constructorMethodRef = ImportReference(constructorMethodDef);
            var ca = new CustomAttribute(constructorMethodRef);
            /* If load type isn't null then it
             * has to be passed in as the first argument. */
            if (loadType.Length > 0)
            {
                var t = typeof(RuntimeInitializeLoadType);
                foreach (RuntimeInitializeLoadType value in t.GetEnumValues())
                    if (loadType == value.ToString())
                    {
                        var tr = ImportReference(t);
                        var arg = new CustomAttributeArgument(tr, value);
                        ca.ConstructorArguments.Add(arg);
                    }
            }

            methodDef.CustomAttributes.Add(ca);
        }

        /// <summary>
        ///     Gets the default AutoPackType to use for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public AutoPackType GetDefaultAutoPackType(TypeReference typeRef)
        {
            //Singles are defauled to unpacked.
            if (typeRef.FullName == Single_FullName)
                return AutoPackType.Unpacked;
            return AutoPackType.Packed;
        }

        /// <summary>
        ///     Gets the InitializeOnce method in typeDef or creates the method should it not exist.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        public MethodDefinition GetOrCreateMethod(TypeDefinition typeDef, out bool created, MethodAttributes methodAttr,
            string methodName, TypeReference returnType)
        {
            var result = typeDef.GetMethod(methodName);
            if (result == null)
            {
                created = true;
                result = new MethodDefinition(methodName, methodAttr, returnType);
                typeDef.Methods.Add(result);
            }
            else
            {
                created = false;
            }

            return result;
        }


        /// <summary>
        ///     Gets a class within moduleDef or creates and returns the class if it does not already exist.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        public TypeDefinition GetOrCreateClass(out bool created, TypeAttributes typeAttr, string className,
            TypeReference baseTypeRef, string namespaceName = WriterProcessor.GENERATED_WRITER_NAMESPACE)
        {
            if (namespaceName.Length == 0)
                namespaceName = FishNetILPP.RUNTIME_ASSEMBLY_NAME;

            var type = Module.GetClass(className, namespaceName);
            if (type != null)
            {
                created = false;
                return type;
            }

            created = true;
            type = new TypeDefinition(namespaceName, className,
                typeAttr, ImportReference(typeof(object)));
            //Add base class if specified.
            if (baseTypeRef != null)
                type.BaseType = ImportReference(baseTypeRef);

            Module.Types.Add(type);
            return type;
        }

        /// <summary>
        ///     Gets a TypeReference for a type.
        /// </summary>
        /// <param name="type"></param>
        public TypeReference GetTypeReference(Type type)
        {
            TypeReference result;
            if (!_importedTypeReferences.TryGetValue(type, out result))
            {
                result = ImportReference(type);
                _importedTypeReferences.Add(type, result);
            }

            return result;
        }

        /// <summary>
        ///     Gets a FieldReference for a type.
        /// </summary>
        /// <param name="type"></param>
        public FieldReference GetFieldReference(FieldDefinition fieldDef)
        {
            FieldReference result;
            if (!_importedFieldReferences.TryGetValue(fieldDef, out result))
            {
                result = ImportReference(fieldDef);
                _importedFieldReferences.Add(fieldDef, result);
            }

            return result;
        }

        /// <summary>
        ///     Gets the current constructor for typeDef, or makes a new one if constructor doesn't exist.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        public MethodDefinition GetOrCreateConstructor(TypeDefinition typeDef, out bool created, bool makeStatic)
        {
            // find constructor
            var constructorMethodDef = typeDef.GetMethod(".cctor");
            if (constructorMethodDef == null)
                constructorMethodDef = typeDef.GetMethod(".ctor");

            //Constructor already exist.
            if (constructorMethodDef != null)
            {
                if (!makeStatic)
                    constructorMethodDef.Attributes &= ~MethodAttributes.Static;

                created = false;
            }
            //Static constructor does not exist yet.
            else
            {
                created = true;
                var methodAttr = MethodAttributes.HideBySig |
                                 MethodAttributes.SpecialName |
                                 MethodAttributes.RTSpecialName;
                if (makeStatic)
                    methodAttr |= MethodAttributes.Static;

                //Create a constructor.
                constructorMethodDef = new MethodDefinition(".ctor", methodAttr,
                    typeDef.Module.TypeSystem.Void
                );

                typeDef.Methods.Add(constructorMethodDef);

                //Add ret.
                var processor = constructorMethodDef.Body.GetILProcessor();
                processor.Emit(OpCodes.Ret);
            }

            return constructorMethodDef;
        }

        /// <summary>
        ///     Creates a return of boolean type.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="result"></param>
        public void CreateRetBoolean(ILProcessor processor, bool result)
        {
            var code = result ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            processor.Emit(code);
            processor.Emit(OpCodes.Ret);
        }

        /// <summary>
        ///     Returns if an instruction is a call to a method.
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="calledMethod"></param>
        /// <returns></returns>
        public bool IsCallToMethod(Instruction instruction, out MethodDefinition calledMethod)
        {
            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition method)
            {
                calledMethod = method;
                return true;
            }

            calledMethod = null;
            return false;
        }


        /// <summary>
        ///     Returns if a serializer and deserializer exist for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="create">True to create if missing.</param>
        /// <returns></returns>
        public bool HasSerializerAndDeserializer(TypeReference typeRef, bool create)
        {
            //Make sure it's imported into current module.
            typeRef = ImportReference(typeRef);
            //Can be serialized/deserialized.
            var hasWriter = GetClass<WriterProcessor>().HasSerializer(typeRef, create);
            var hasReader = GetClass<ReaderProcessor>().HasDeserializer(typeRef, create);

            return hasWriter && hasReader;
        }

        /// <summary>
        ///     Creates a return of default value for methodDef.
        /// </summary>
        /// <returns></returns>
        public List<Instruction> CreateRetDefault(MethodDefinition methodDef,
            ModuleDefinition importReturnModule = null)
        {
            var processor = methodDef.Body.GetILProcessor();
            var instructions = new List<Instruction>();
            //If requires a value return.
            if (methodDef.ReturnType != methodDef.Module.TypeSystem.Void)
            {
                //Import type first.
                methodDef.Module.ImportReference(methodDef.ReturnType);
                if (importReturnModule != null)
                    importReturnModule.ImportReference(methodDef.ReturnType);
                var vd = GetClass<GeneralHelper>().CreateVariable(methodDef, methodDef.ReturnType);
                instructions.Add(processor.Create(OpCodes.Ldloca_S, vd));
                instructions.Add(processor.Create(OpCodes.Initobj, vd.VariableType));
                instructions.Add(processor.Create(OpCodes.Ldloc, vd));
            }

            instructions.Add(processor.Create(OpCodes.Ret));

            return instructions;
        }

        #region Reflection references.

        public string CodegenExcludeAttribute_FullName;
        public string CodegenIncludeAttribute_FullName;
        public MethodReference Extension_Attribute_Ctor_MethodRef;
        public MethodReference Queue_Enqueue_MethodRef;
        public MethodReference Queue_get_Count_MethodRef;
        public MethodReference Queue_Dequeue_MethodRef;
        public MethodReference Queue_Clear_MethodRef;
        public TypeReference List_TypeRef;
        public MethodReference List_Clear_MethodRef;
        public MethodReference List_get_Item_MethodRef;
        public MethodReference List_get_Count_MethodRef;
        public MethodReference List_Add_MethodRef;
        public MethodReference List_RemoveRange_MethodRef;
        public MethodReference InstanceFinder_NetworkManager_MethodRef;
        public MethodReference NetworkBehaviour_CanLog_MethodRef;
        public MethodReference NetworkBehaviour_NetworkManager_MethodRef;
        public MethodReference NetworkManager_LogCommon_MethodRef;
        public MethodReference NetworkManager_LogWarning_MethodRef;
        public MethodReference NetworkManager_LogError_MethodRef;
        public MethodReference Debug_LogCommon_MethodRef;
        public MethodReference Debug_LogWarning_MethodRef;
        public MethodReference Debug_LogError_MethodRef;
        public MethodReference IsServer_MethodRef;
        public MethodReference IsClient_MethodRef;
        public MethodReference NetworkObject_Deinitializing_MethodRef;
        public MethodReference Application_IsPlaying_MethodRef;
        public string NonSerialized_Attribute_FullName;
        public string Single_FullName;
        public TypeReference FunctionT2TypeRef;
        public TypeReference FunctionT3TypeRef;
        public MethodReference FunctionT2ConstructorMethodRef;

        public MethodReference FunctionT3ConstructorMethodRef;

        //GeneratedComparer
        public MethodReference GeneratedComparer_Compare_Set_MethodRef;
        public MethodReference GeneratedComparer_IsDefault_Set_MethodRef;
        public TypeReference GeneratedComparer_TypeRef;
        public TypeDefinition GeneratedComparer_ClassTypeDef;
        public MethodDefinition GeneratedComparer_OnLoadMethodDef;

        public TypeReference IEquatable_TypeRef;

        //Actions.
        public TypeReference ActionT2TypeRef;
        public TypeReference ActionT3TypeRef;
        public MethodReference ActionT2ConstructorMethodRef;
        public MethodReference ActionT3ConstructorMethodRef;

        private readonly Dictionary<Type, TypeReference> _importedTypeReferences = new();
        private readonly Dictionary<FieldDefinition, FieldReference> _importedFieldReferences = new();
        private readonly Dictionary<MethodReference, MethodDefinition> _methodReferenceResolves = new();
        private readonly Dictionary<TypeReference, TypeDefinition> _typeReferenceResolves = new();
        private readonly Dictionary<FieldReference, FieldDefinition> _fieldReferenceResolves = new();

        #endregion


        #region Resolves.

        /// <summary>
        ///     Adds a typeRef to TypeReferenceResolves.
        /// </summary>
        public void AddTypeReferenceResolve(TypeReference typeRef, TypeDefinition typeDef)
        {
            _typeReferenceResolves[typeRef] = typeDef;
        }

        /// <summary>
        ///     Gets a TypeDefinition for typeRef.
        /// </summary>
        public TypeDefinition GetTypeReferenceResolve(TypeReference typeRef)
        {
            TypeDefinition result;
            if (_typeReferenceResolves.TryGetValue(typeRef, out result)) return result;

            result = typeRef.Resolve();
            AddTypeReferenceResolve(typeRef, result);

            return result;
        }

        /// <summary>
        ///     Adds a methodRef to MethodReferenceResolves.
        /// </summary>
        public void AddMethodReferenceResolve(MethodReference methodRef, MethodDefinition methodDef)
        {
            _methodReferenceResolves[methodRef] = methodDef;
        }

        /// <summary>
        ///     Gets a TypeDefinition for typeRef.
        /// </summary>
        public MethodDefinition GetMethodReferenceResolve(MethodReference methodRef)
        {
            MethodDefinition result;
            if (_methodReferenceResolves.TryGetValue(methodRef, out result)) return result;

            result = methodRef.Resolve();
            AddMethodReferenceResolve(methodRef, result);

            return result;
        }


        /// <summary>
        ///     Adds a fieldRef to FieldReferenceResolves.
        /// </summary>
        public void AddFieldReferenceResolve(FieldReference fieldRef, FieldDefinition fieldDef)
        {
            _fieldReferenceResolves[fieldRef] = fieldDef;
        }

        /// <summary>
        ///     Gets a FieldDefinition for fieldRef.
        /// </summary>
        public FieldDefinition GetFieldReferenceResolve(FieldReference fieldRef)
        {
            FieldDefinition result;
            if (_fieldReferenceResolves.TryGetValue(fieldRef, out result)) return result;

            result = fieldRef.Resolve();
            AddFieldReferenceResolve(fieldRef, result);

            return result;
        }

        #endregion

        #region HasNonSerializableAttribute

        /// <summary>
        ///     Returns if fieldDef has a NonSerialized attribute.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        public bool HasNonSerializableAttribute(FieldDefinition fieldDef)
        {
            foreach (var customAttribute in fieldDef.CustomAttributes)
                if (customAttribute.AttributeType.FullName == NonSerialized_Attribute_FullName)
                    return true;

            //Fall through, no matches.
            return false;
        }

        /// <summary>
        ///     Returns if typeDef has a NonSerialized attribute.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        public bool HasNonSerializableAttribute(TypeDefinition typeDef)
        {
            foreach (var customAttribute in typeDef.CustomAttributes)
                if (customAttribute.AttributeType.FullName == NonSerialized_Attribute_FullName)
                    return true;

            //Fall through, no matches.
            return false;
        }

        #endregion

        #region Debug logging.

        /// <summary>
        ///     Creates instructions to log using a NetworkManager or Unity logging.
        /// </summary>
        /// <param name="preferNetworkManager">
        ///     NetworkManager will be used to log first. If the NetworkManager is unavailable Unity
        ///     logging will be used.
        /// </param>
        public List<Instruction> LogMessage(MethodDefinition md, string message, LoggingType loggingType)
        {
            var processor = md.Body.GetILProcessor();
            var instructions = new List<Instruction>();
            if (loggingType == LoggingType.Off)
            {
                LogError("LogMessage called with LoggingType.Off.");
                return instructions;
            }

            /* Try to store NetworkManager from base to a variable.
             * If the base does not exist, such as not inheriting from NetworkBehaviour,
             * or if null because the object is not initialized, then use InstanceFinder to
             * retrieve the NetworkManager. Then if NetworkManager was found, perform the log. */
            var networkManagerVd = CreateVariable(processor.Body.Method, typeof(NetworkManager));

            var useStatic = md.IsStatic || !md.DeclaringType.InheritsFrom<NetworkBehaviour>(Session);
            //If does not inherit NB then use InstanceFinder.
            if (useStatic)
            {
                SetNetworkManagerFromInstanceFinder();
            }
            //Inherits NB, load from base.NetworkManager.
            else
            {
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_NetworkManager_MethodRef));
                instructions.Add(processor.Create(OpCodes.Stloc, networkManagerVd));

                //If null from NB then use instancefinder.
                var skipSetFromInstanceFinderInst = processor.Create(OpCodes.Nop);
                //if (nmVd == null) nmVd = InstanceFinder.NetworkManager.
                instructions.Add(processor.Create(OpCodes.Ldloc, networkManagerVd));
                instructions.Add(processor.Create(OpCodes.Brtrue_S, skipSetFromInstanceFinderInst));
                SetNetworkManagerFromInstanceFinder();
                instructions.Add(skipSetFromInstanceFinderInst);
            }

            //Sets NetworkManager variable from instancefinder.
            void SetNetworkManagerFromInstanceFinder()
            {
                instructions.Add(processor.Create(OpCodes.Call, InstanceFinder_NetworkManager_MethodRef));
                instructions.Add(processor.Create(OpCodes.Stloc, networkManagerVd));
            }

            var networkManagerIsNullVd = CreateVariable(md, typeof(bool));
            //bool networkManagerIsNull = (networkManager == null);
            instructions.Add(processor.Create(OpCodes.Ldloc, networkManagerVd));
            instructions.Add(processor.Create(OpCodes.Ldnull));
            instructions.Add(processor.Create(OpCodes.Ceq));
            instructions.Add(processor.Create(OpCodes.Stloc, networkManagerIsNullVd));

            /* If (networkManagerIsNull)
             *      networkManager.Log...
             * else
             *      UnityEngine.Debug.Log... */
            var afterNetworkManagerLogInst = processor.Create(OpCodes.Nop);
            var afterUnityLogInst = processor.Create(OpCodes.Nop);
            instructions.Add(processor.Create(OpCodes.Ldloc, networkManagerIsNullVd));
            instructions.Add(processor.Create(OpCodes.Brtrue, afterNetworkManagerLogInst));
            instructions.AddRange(LogNetworkManagerMessage(md, networkManagerVd, message, loggingType));
            instructions.Add(processor.Create(OpCodes.Br, afterUnityLogInst));
            instructions.Add(afterNetworkManagerLogInst);
            instructions.AddRange(LogUnityDebugMessage(md, message, loggingType));
            instructions.Add(afterUnityLogInst);

            return instructions;
        }

        /// <summary>
        ///     Creates instructions to log using NetworkManager without error checking.
        /// </summary>
        public List<Instruction> LogNetworkManagerMessage(MethodDefinition md, VariableDefinition networkManagerVd,
            string message, LoggingType loggingType)
        {
            var instructions = new List<Instruction>();
            if (!CanUseLogging(loggingType))
                return instructions;

            var processor = md.Body.GetILProcessor();

            MethodReference methodRef;
            if (loggingType == LoggingType.Common)
                methodRef = NetworkManager_LogCommon_MethodRef;
            else if (loggingType == LoggingType.Warning)
                methodRef = NetworkManager_LogWarning_MethodRef;
            else
                methodRef = NetworkManager_LogError_MethodRef;

            instructions.Add(processor.Create(OpCodes.Ldloc, networkManagerVd));
            instructions.Add(processor.Create(OpCodes.Ldstr, message));
            instructions.Add(processor.Create(OpCodes.Call, methodRef));

            return instructions;
        }

        /// <summary>
        ///     Creates instructions to log using Unity logging.
        /// </summary>
        public List<Instruction> LogUnityDebugMessage(MethodDefinition md, string message, LoggingType loggingType)
        {
            var instructions = new List<Instruction>();
            if (!CanUseLogging(loggingType))
                return instructions;

            var processor = md.Body.GetILProcessor();

            MethodReference methodRef;
            if (loggingType == LoggingType.Common)
                methodRef = Debug_LogCommon_MethodRef;
            else if (loggingType == LoggingType.Warning)
                methodRef = Debug_LogWarning_MethodRef;
            else
                methodRef = Debug_LogError_MethodRef;

            instructions.Add(processor.Create(OpCodes.Ldstr, message));
            instructions.Add(processor.Create(OpCodes.Call, methodRef));
            return instructions;
        }

        /// <summary>
        ///     Returns if logging can be done using a LoggingType.
        /// </summary>
        public bool CanUseLogging(LoggingType lt)
        {
            if (lt == LoggingType.Off)
            {
                LogError("Log attempt called with LoggingType.Off.");
                return false;
            }

            return true;
        }

        #endregion

        #region CreateVariable / CreateParameter.

        /// <summary>
        ///     Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        public ParameterDefinition CreateParameter(MethodDefinition methodDef, TypeDefinition parameterTypeDef,
            string name = "", ParameterAttributes attributes = ParameterAttributes.None, int index = -1)
        {
            var typeRef = methodDef.Module.ImportReference(parameterTypeDef);
            return CreateParameter(methodDef, typeRef, name, attributes, index);
        }

        /// <summary>
        ///     Creates a parameter within methodDef as the next index, with the same data as passed in parameter definition.
        /// </summary>
        public ParameterDefinition CreateParameter(MethodDefinition methodDef, ParameterDefinition parameterTypeDef)
        {
            ImportReference(parameterTypeDef.ParameterType);

            var currentCount = methodDef.Parameters.Count;
            var name = parameterTypeDef.Name + currentCount;
            var parameterDef =
                new ParameterDefinition(name, parameterTypeDef.Attributes, parameterTypeDef.ParameterType);
            methodDef.Parameters.Add(parameterDef);

            return parameterDef;
        }

        /// <summary>
        ///     Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        public ParameterDefinition CreateParameter(MethodDefinition methodDef, TypeReference parameterTypeRef,
            string name = "", ParameterAttributes attributes = ParameterAttributes.None, int index = -1)
        {
            var currentCount = methodDef.Parameters.Count;
            if (string.IsNullOrEmpty(name))
                name = parameterTypeRef.Name + currentCount;
            var parameterDef = new ParameterDefinition(name, attributes, parameterTypeRef);
            if (index == -1)
                methodDef.Parameters.Add(parameterDef);
            else
                methodDef.Parameters.Insert(index, parameterDef);
            return parameterDef;
        }

        /// <summary>
        ///     Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        public ParameterDefinition CreateParameter(MethodDefinition methodDef, Type parameterType, string name = "",
            ParameterAttributes attributes = ParameterAttributes.None, int index = -1)
        {
            return CreateParameter(methodDef, GetTypeReference(parameterType), name, attributes, index);
        }

        /// <summary>
        ///     Creates a variable type within the body and returns it's VariableDef.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="variableTypeRef"></param>
        /// <returns></returns>
        public VariableDefinition CreateVariable(MethodDefinition methodDef, TypeReference variableTypeRef)
        {
            var variableDef = new VariableDefinition(variableTypeRef);
            methodDef.Body.Variables.Add(variableDef);
            return variableDef;
        }

        /// Creates a variable type within the body and returns it's VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <param name="variableTypeRef"></param>
        /// <returns></returns>
        public VariableDefinition CreateVariable(MethodDefinition methodDef, Type variableType)
        {
            return CreateVariable(methodDef, GetTypeReference(variableType));
        }

        #endregion

        #region SetVariableDef.

        /// <summary>
        ///     Initializes variableDef as a new object or collection of typeDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="typeDef"></param>
        public void SetVariableDefinitionFromObject(ILProcessor processor, VariableDefinition variableDef,
            TypeDefinition typeDef)
        {
            var type = variableDef.VariableType;
            if (type.IsValueType)
            {
                // structs are created with Initobj
                processor.Emit(OpCodes.Ldloca, variableDef);
                processor.Emit(OpCodes.Initobj, type);
            }
            else if (typeDef.InheritsFrom<ScriptableObject>(Session))
            {
                var soCreateInstanceMr =
                    processor.Body.Method.Module.ImportReference(() =>
                        ScriptableObject.CreateInstance<ScriptableObject>());
                var genericInstanceMethod = soCreateInstanceMr.GetElementMethod().MakeGenericMethod(type);
                processor.Emit(OpCodes.Call, genericInstanceMethod);
                processor.Emit(OpCodes.Stloc, variableDef);
            }
            else
            {
                var constructorMethodDef = type.GetConstructor(Session);
                if (constructorMethodDef == null)
                {
                    LogError(
                        $"{type.Name} can't be deserialized because a default constructor could not be found. Create a default constructor or a custom serializer/deserializer.");
                    return;
                }

                var constructorMethodRef = processor.Body.Method.Module.ImportReference(constructorMethodDef);
                processor.Emit(OpCodes.Newobj, constructorMethodRef);
                processor.Emit(OpCodes.Stloc, variableDef);
            }
        }

        /// <summary>
        ///     Assigns value to a VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="value"></param>
        public void SetVariableDefinitionFromInt(ILProcessor processor, VariableDefinition variableDef, int value)
        {
            processor.Emit(OpCodes.Ldc_I4, value);
            processor.Emit(OpCodes.Stloc, variableDef);
        }

        /// <summary>
        ///     Assigns value to a VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="value"></param>
        public void SetVariableDefinitionFromParameter(ILProcessor processor, VariableDefinition variableDef,
            ParameterDefinition value)
        {
            processor.Emit(OpCodes.Ldarg, value);
            processor.Emit(OpCodes.Stloc, variableDef);
        }

        #endregion.

        #region GeneratedComparers

        /// <summary>
        ///     Creates an equality comparer for dataTr.
        /// </summary>
        public MethodDefinition CreateEqualityComparer(TypeReference dataTr)
        {
            var gh = GetClass<GeneralHelper>();
            var comparerMd = gh.GetOrCreateMethod(GeneratedComparer_ClassTypeDef, out var created,
                WriterProcessor.GENERATED_METHOD_ATTRIBUTES,
                $"Comparer___{dataTr.FullName}", Module.TypeSystem.Boolean);
            //Already done. This can happen if the same replicate data is used in multiple places.
            if (created)
            {
                CreateComparerMethod();
                CreateComparerDelegate();
            }

            return comparerMd;

            void CreateComparerMethod()
            {
                //GeneratedComparer_ClassTypeDef.Methods.Add(comparerMd);

                //Add parameters.
                var v0Pd = gh.CreateParameter(comparerMd, dataTr, "value0");
                var v1Pd = gh.CreateParameter(comparerMd, dataTr, "value1");
                var processor = comparerMd.Body.GetILProcessor();
                comparerMd.Body.InitLocals = true;

                var exitMethodInst = processor.Create(OpCodes.Ldc_I4_0);

                //Fields.
                foreach (var fieldDef in dataTr.FindAllSerializableFields(Session
                             , null, WriterProcessor.EXCLUDED_ASSEMBLY_PREFIXES))
                {
                    ImportReference(fieldDef);
                    processor.Append(GetLoadParameterInstruction(comparerMd, v0Pd));
                    processor.Emit(OpCodes.Ldfld, fieldDef);
                    processor.Append(GetLoadParameterInstruction(comparerMd, v1Pd));
                    processor.Emit(OpCodes.Ldfld, fieldDef);
                    FinishTypeReferenceCompare(fieldDef.FieldType);
                    //processor.Emit(OpCodes.Bne_Un, exitMethodInst);
                }

                //Properties.
                foreach (var propertyDef in dataTr.FindAllSerializableProperties(Session
                             , null, WriterProcessor.EXCLUDED_ASSEMBLY_PREFIXES))
                {
                    var getMr = Module.ImportReference(propertyDef.GetMethod);
                    processor.Append(GetLoadParameterInstruction(comparerMd, v0Pd));
                    processor.Emit(OpCodes.Call, getMr);
                    processor.Append(GetLoadParameterInstruction(comparerMd, v1Pd));
                    processor.Emit(OpCodes.Call, getMr);
                    FinishTypeReferenceCompare(propertyDef.PropertyType);
                }

                //Return true;
                processor.Emit(OpCodes.Ldc_I4_1);
                processor.Emit(OpCodes.Ret);
                processor.Append(exitMethodInst);
                processor.Emit(OpCodes.Ret);

                void FinishTypeReferenceCompare(TypeReference tr)
                {
                    /* If a class or struct see if it already has a comparer
                     * using IEquatable. If so then call the comparer method.
                     * Otherwise make a new comparer and call it. */
                    if (tr.IsClassOrStruct(Session))
                    {
                        //Make equatable for type.
                        var git = IEquatable_TypeRef.MakeGenericInstanceType(tr);
                        var createNestedComparer = !tr.CachedResolve(Session).ImplementsInterface(git.FullName);

                        //Create new.
                        if (createNestedComparer)
                        {
                            var cMd = CreateEqualityComparer(tr);
                            processor.Emit(OpCodes.Call, cMd);
                            processor.Emit(OpCodes.Brfalse, exitMethodInst);
                        }
                        //Call existing.
                        else
                        {
                            var cMd = tr.CachedResolve(Session).GetMethod("op_Equality");
                            if (cMd == null)
                            {
                                LogError(
                                    $"Type {tr.FullName} implements IEquatable but the comparer method could not be found.");
                                return;
                            }

                            var mr = ImportReference(cMd);
                            processor.Emit(OpCodes.Call, mr);
                            processor.Emit(OpCodes.Brfalse, exitMethodInst);
                        }
                    }
                    //Value types do not need to check custom comparers.
                    else
                    {
                        processor.Emit(OpCodes.Bne_Un, exitMethodInst);
                    }
                }
            }

            //Creates a delegate to compare two of replicateTr.
            void CreateComparerDelegate()
            {
                //Initialize delegate for made comparer.
                var insts = new List<Instruction>();
                var processor = GeneratedComparer_OnLoadMethodDef.Body.GetILProcessor();
                //Create a Func<Reader, T> delegate 
                insts.Add(processor.Create(OpCodes.Ldnull));
                insts.Add(processor.Create(OpCodes.Ldftn, comparerMd));

                GenericInstanceType git;
                git = gh.FunctionT3TypeRef.MakeGenericInstanceType(dataTr, dataTr, gh.GetTypeReference(typeof(bool)));
                var functionConstructorInstanceMethodRef =
                    gh.FunctionT3ConstructorMethodRef.MakeHostInstanceGeneric(Session, git);
                insts.Add(processor.Create(OpCodes.Newobj, functionConstructorInstanceMethodRef));

                //Call delegate to ReplicateComparer.Compare(T, T);
                git = GeneratedComparer_TypeRef.MakeGenericInstanceType(dataTr);
                var comparerMr = GeneratedComparer_Compare_Set_MethodRef.MakeHostInstanceGeneric(Session, git);
                insts.Add(processor.Create(OpCodes.Call, comparerMr));
                processor.InsertFirst(insts);
            }
        }

        /// <summary>
        ///     Returns an OpCode for loading a parameter.
        /// </summary>
        public OpCode GetLoadParameterOpCode(ParameterDefinition pd)
        {
            return pd.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg;
        }

        /// <summary>
        ///     Returns an instruction for loading a parameter.s
        /// </summary>
        public Instruction GetLoadParameterInstruction(MethodDefinition md, ParameterDefinition pd)
        {
            var processor = md.Body.GetILProcessor();
            var oc = GetLoadParameterOpCode(pd);
            return processor.Create(oc, pd);
        }

        /// <summary>
        ///     Creates an IsDefault comparer for dataTr.
        /// </summary>
        public void CreateIsDefaultComparer(TypeReference dataTr, MethodDefinition compareMethodDef)
        {
            var gh = GetClass<GeneralHelper>();

            var isDefaultMd = gh.GetOrCreateMethod(GeneratedComparer_ClassTypeDef, out var created,
                WriterProcessor.GENERATED_METHOD_ATTRIBUTES,
                $"IsDefault___{dataTr.FullName}", Module.TypeSystem.Boolean);
            //Already done. This can happen if the same replicate data is used in multiple places.
            if (!created)
                return;

            var compareMr = ImportReference(compareMethodDef);
            CreateIsDefaultMethod();
            CreateIsDefaultDelegate();

            void CreateIsDefaultMethod()
            {
                //Add parameters.
                var v0Pd = gh.CreateParameter(isDefaultMd, dataTr, "value0");
                var processor = isDefaultMd.Body.GetILProcessor();
                isDefaultMd.Body.InitLocals = true;


                processor.Emit(OpCodes.Ldarg, v0Pd);
                //If a struct.
                if (dataTr.IsValueType)
                {
                    //Init a default local.
                    var defaultVd = gh.CreateVariable(isDefaultMd, dataTr);
                    processor.Emit(OpCodes.Ldloca, defaultVd);
                    processor.Emit(OpCodes.Initobj, dataTr);
                    processor.Emit(OpCodes.Ldloc, defaultVd);
                }
                //If a class.
                else
                {
                    processor.Emit(OpCodes.Ldnull);
                }

                processor.Emit(OpCodes.Call, compareMr);
                processor.Emit(OpCodes.Ret);
            }

            //Creates a delegate to compare two of replicateTr.
            void CreateIsDefaultDelegate()
            {
                //Initialize delegate for made comparer.
                var insts = new List<Instruction>();
                var processor = GeneratedComparer_OnLoadMethodDef.Body.GetILProcessor();
                //Create a Func<Reader, T> delegate 
                insts.Add(processor.Create(OpCodes.Ldnull));
                insts.Add(processor.Create(OpCodes.Ldftn, isDefaultMd));

                GenericInstanceType git;
                git = gh.FunctionT2TypeRef.MakeGenericInstanceType(dataTr, gh.GetTypeReference(typeof(bool)));
                var funcCtorMethodRef = gh.FunctionT2ConstructorMethodRef.MakeHostInstanceGeneric(Session, git);
                insts.Add(processor.Create(OpCodes.Newobj, funcCtorMethodRef));

                //Call delegate to ReplicateComparer.IsDefault(T).
                git = GeneratedComparer_TypeRef.MakeGenericInstanceType(dataTr);
                var isDefaultMr = GeneratedComparer_IsDefault_Set_MethodRef.MakeHostInstanceGeneric(Session, git);
                insts.Add(processor.Create(OpCodes.Call, isDefaultMr));
                processor.InsertFirst(insts);
            }
        }

        #endregion
    }
}