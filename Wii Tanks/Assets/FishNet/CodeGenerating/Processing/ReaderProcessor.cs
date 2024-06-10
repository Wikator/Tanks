using System;
using System.Collections.Generic;
using FishNet.CodeGenerating.Extension;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.ILCore;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Utility.Performance;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;
using MonoFN.Cecil.Rocks;
using SR = System.Reflection;

namespace FishNet.CodeGenerating.Helping
{
    internal class ReaderProcessor : CodegenBase
    {
        #region Misc.

        /// <summary>
        ///     TypeReferences which have already had delegates made for.
        /// </summary>
        private readonly HashSet<TypeReference> _delegatedTypes = new();

        #endregion

        public override bool ImportReferences()
        {
            return true;
        }

        public bool Process()
        {
            var gh = GetClass<GeneralHelper>();

            CreateGeneratedClassData();
            FindInstancedReaders();
            CreateInstancedReaderExtensions();

            void CreateGeneratedClassData()
            {
                GeneratedReaderClassTypeDef = gh.GetOrCreateClass(out _, GENERATED_TYPE_ATTRIBUTES,
                    GENERATED_READERS_CLASS_NAME, null);
                /* If constructor isn't set then try to get or create it
                 * and also add it to methods if were created. */
                GeneratedReaderOnLoadMethodDef = gh.GetOrCreateMethod(GeneratedReaderClassTypeDef, out _,
                    INITIALIZEONCE_METHOD_ATTRIBUTES, INITIALIZEONCE_METHOD_NAME, Module.TypeSystem.Void);
                gh.CreateRuntimeInitializeOnLoadMethodAttribute(GeneratedReaderOnLoadMethodDef);

                var ppp = GeneratedReaderOnLoadMethodDef.Body.GetILProcessor();
                ppp.Emit(OpCodes.Ret);
                //GeneratedReaderOnLoadMethodDef.DeclaringType.Methods.Remove(GeneratedReaderOnLoadMethodDef);
            }

            void FindInstancedReaders()
            {
                var pooledWriterType = typeof(PooledReader);
                foreach (var methodInfo in pooledWriterType.GetMethods())
                {
                    if (IsSpecialReadMethod(methodInfo))
                        continue;
                    bool autoPackMethod;
                    if (IsIgnoredWriteMethod(methodInfo, out autoPackMethod))
                        continue;

                    var methodRef = ImportReference(methodInfo);
                    /* TypeReference for the return type
                     * of the read method. */
                    var typeRef = ImportReference(methodRef.ReturnType);

                    /* If here all checks pass. */
                    AddReaderMethod(typeRef, methodRef, true, true);
                    if (autoPackMethod)
                        AutoPackedMethods.Add(typeRef);
                }
            }

            return true;
        }


        /// <summary>
        ///     Returns if a MethodInfo is considered a special write method.
        ///     Special read methods have declared references within this class, and will not have extensions made for them.
        /// </summary>
        public bool IsSpecialReadMethod(SR.MethodInfo methodInfo)
        {
            /* Special methods. */
            if (methodInfo.Name == nameof(PooledReader.ReadPackedWhole))
                return true;
            if (methodInfo.Name == nameof(PooledReader.ReadArray))
                return true;
            if (methodInfo.Name == nameof(PooledReader.ReadDictionary))
                return true;

            return false;
        }

        /// <summary>
        ///     Returns if a read method should be ignored.
        /// </summary>
        public bool IsIgnoredWriteMethod(SR.MethodInfo methodInfo, out bool autoPackMethod)
        {
            autoPackMethod = false;

            if (GetClass<GeneralHelper>().CodegenExclude(methodInfo))
                return true;
            //Not long enough to be a write method.
            if (methodInfo.Name.Length < READ_PREFIX.Length)
                return true;
            //Method name doesn't start with writePrefix.
            if (methodInfo.Name.Substring(0, READ_PREFIX.Length) != READ_PREFIX)
                return true;
            var parameterInfos = methodInfo.GetParameters();
            //Can have at most one parameter for packing.
            if (parameterInfos.Length > 1)
                return true;
            //If has one parameter make sure it's a packing type.
            if (parameterInfos.Length == 1)
            {
                autoPackMethod = parameterInfos[0].ParameterType == typeof(AutoPackType);
                if (!autoPackMethod)
                    return true;
            }

            return false;
        }


        /// <summary>
        ///     Adds typeRef, methodDef to instanced or readerMethods.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="methodRef"></param>
        /// <param name="useAdd"></param>
        internal void AddReaderMethod(TypeReference typeRef, MethodReference methodRef, bool instanced, bool useAdd)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            var dict = instanced ? InstancedReaderMethods : StaticReaderMethods;

            if (useAdd)
                dict.Add(fullName, methodRef);
            else
                dict[fullName] = methodRef;
        }


        /// <summary>
        ///     Creates a Read delegate for readMethodRef and places it within the generated reader/writer constructor.
        /// </summary>
        /// <param name="readMr"></param>
        /// <param name="diagnostics"></param>
        internal void CreateReadDelegate(MethodReference readMr, bool isStatic)
        {
            var gh = GetClass<GeneralHelper>();
            var ri = GetClass<ReaderImports>();

            if (!isStatic)
                //Supporting Write<T> with types containing generics is more trouble than it's worth.
                if (readMr.IsGenericInstance || readMr.HasGenericParameters)
                    return;

            //Check if ret already exist, if so remove it; ret will be added on again in this method.
            if (GeneratedReaderOnLoadMethodDef.Body.Instructions.Count != 0)
            {
                var lastIndex = GeneratedReaderOnLoadMethodDef.Body.Instructions.Count - 1;
                if (GeneratedReaderOnLoadMethodDef.Body.Instructions[lastIndex].OpCode == OpCodes.Ret)
                    GeneratedReaderOnLoadMethodDef.Body.Instructions.RemoveAt(lastIndex);
            }

            //Check if already exist.
            var processor = GeneratedReaderOnLoadMethodDef.Body.GetILProcessor();
            var dataTypeRef = readMr.ReturnType;
            if (_delegatedTypes.Contains(dataTypeRef))
            {
                LogError($"Generic read already created for {dataTypeRef.FullName}.");
                return;
            }

            _delegatedTypes.Add(dataTypeRef);

            //Create a Func<Reader, T> delegate 
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, readMr);

            GenericInstanceType functionGenericInstance;
            MethodReference functionConstructorInstanceMethodRef;
            var isAutoPacked = IsAutoPackedType(dataTypeRef);

            //Generate for autopacktype.
            if (isAutoPacked)
            {
                functionGenericInstance = gh.FunctionT3TypeRef.MakeGenericInstanceType(ri.ReaderTypeRef,
                    GetClass<WriterImports>().AutoPackTypeRef, dataTypeRef);
                functionConstructorInstanceMethodRef =
                    gh.FunctionT3ConstructorMethodRef.MakeHostInstanceGeneric(Session, functionGenericInstance);
            }
            //Not autopacked.
            else
            {
                functionGenericInstance = gh.FunctionT2TypeRef.MakeGenericInstanceType(ri.ReaderTypeRef, dataTypeRef);
                functionConstructorInstanceMethodRef =
                    gh.FunctionT2ConstructorMethodRef.MakeHostInstanceGeneric(Session, functionGenericInstance);
            }

            processor.Emit(OpCodes.Newobj, functionConstructorInstanceMethodRef);

            //Call delegate to GeneratedReader<T>.Read
            var genericInstance = ri.GenericReaderTypeRef.MakeGenericInstanceType(dataTypeRef);
            var genericReadMethodRef = isAutoPacked
                ? ri.ReadAutoPackSetMethodRef.MakeHostInstanceGeneric(Session, genericInstance)
                : ri.ReadSetMethodRef.MakeHostInstanceGeneric(Session, genericInstance);
            processor.Emit(OpCodes.Call, genericReadMethodRef);

            processor.Emit(OpCodes.Ret);
        }

        /// <summary>
        ///     Creates reader extension methods for built-in readers.
        /// </summary>
        private void CreateInstancedReaderExtensions()
        {
            if (!FishNetILPP.IsFishNetAssembly(Session))
                return;

            var gh = GetClass<GeneralHelper>();
            var gwh = GetClass<ReaderProcessor>();

            //List<MethodReference> staticReaders = new List<MethodReference>();
            foreach (var item in InstancedReaderMethods)
            {
                var instancedReadMr = item.Value;
                if (instancedReadMr.ContainsGenericParameter)
                    continue;

                var returnTr = ImportReference(instancedReadMr.ReturnType);

                var md = new MethodDefinition($"InstancedExtension___{instancedReadMr.Name}",
                    WriterProcessor.GENERATED_METHOD_ATTRIBUTES,
                    returnTr);
                //Add extension parameter.
                var readerPd = gh.CreateParameter(md, typeof(Reader), "reader");
                //Add parameters needed by instanced writer.
                var otherPds = md.CreateParameters(Session, instancedReadMr);
                gh.MakeExtensionMethod(md);
                //
                gwh.GeneratedReaderClassTypeDef.Methods.Add(md);

                var processor = md.Body.GetILProcessor();
                //Load writer.
                processor.Emit(OpCodes.Ldarg, readerPd);
                //Load args.
                foreach (var pd in otherPds)
                    processor.Emit(OpCodes.Ldarg, pd);
                //Call instanced.
                processor.Emit(instancedReadMr.GetCallOpCode(Session), instancedReadMr);
                processor.Emit(OpCodes.Ret);

                AddReaderMethod(returnTr, md, false, true);
            }
        }

        /// <summary>
        ///     Removes typeRef from static/instanced reader methods.
        /// </summary>
        internal void RemoveReaderMethod(TypeReference typeRef, bool instanced)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            var dict = instanced ? InstancedReaderMethods : StaticReaderMethods;

            dict.Remove(fullName);
        }

        /// <summary>
        ///     Creates read instructions returning instructions and outputing variable of read result.
        /// </summary>
        internal List<Instruction> CreateRead(MethodDefinition methodDef, ParameterDefinition readerParameterDef,
            TypeReference readTypeRef, out VariableDefinition createdVariableDef)
        {
            var processor = methodDef.Body.GetILProcessor();
            var insts = new List<Instruction>();
            var readMr = GetReadMethodReference(readTypeRef);
            if (readMr != null)
            {
                //Make a local variable. 
                createdVariableDef = GetClass<GeneralHelper>().CreateVariable(methodDef, readTypeRef);
                //pooledReader.ReadBool();
                insts.Add(processor.Create(OpCodes.Ldarg, readerParameterDef));
                //If an auto pack method then insert default value.
                if (AutoPackedMethods.Contains(readTypeRef))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(readTypeRef);
                    insts.Add(processor.Create(OpCodes.Ldc_I4, (int)packType));
                }


                var valueTr = readTypeRef;
                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (valueTr.IsGenericInstance)
                {
                    var git = (GenericInstanceType)valueTr;
                    var genericTr = git.GenericArguments[0];
                    readMr = readMr.GetMethodReference(Session, genericTr);
                }

                insts.Add(processor.Create(OpCodes.Call, readMr));
                //Store into local variable.
                insts.Add(processor.Create(OpCodes.Stloc, createdVariableDef));
                return insts;
            }

            LogError("Reader not found for " + readTypeRef);
            createdVariableDef = null;
            return null;
        }


        /// <summary>
        ///     Creates a read for fieldRef and populates it into a created variable of class or struct type.
        /// </summary>
        internal bool CreateReadIntoClassOrStruct(MethodDefinition readerMd, ParameterDefinition readerPd,
            MethodReference readMr, VariableDefinition objectVd, FieldReference valueFr)
        {
            if (readMr != null)
            {
                var processor = readerMd.Body.GetILProcessor();
                /* How to load object instance. If it's a structure
                 * then it must be loaded by address. Otherwise if
                 * class Ldloc can be used. */
                var loadOpCode = objectVd.VariableType.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;

                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (valueFr.FieldType.IsGenericInstance)
                {
                    var git = (GenericInstanceType)valueFr.FieldType;
                    var genericTr = git.GenericArguments[0];
                    readMr = readMr.GetMethodReference(Session, genericTr);
                }

                processor.Emit(loadOpCode, objectVd);
                //reader.
                processor.Emit(OpCodes.Ldarg, readerPd);
                if (IsAutoPackedType(valueFr.FieldType))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(valueFr.FieldType);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }

                //reader.ReadXXXX().
                processor.Emit(OpCodes.Call, readMr);
                //obj.Field = result / reader.ReadXXXX().
                processor.Emit(OpCodes.Stfld, valueFr);

                return true;
            }

            LogError($"Reader not found for {valueFr.FullName}.");
            return false;
        }


        /// <summary>
        ///     Creates a read for fieldRef and populates it into a created variable of class or struct type.
        /// </summary>
        internal bool CreateReadIntoClassOrStruct(MethodDefinition methodDef, ParameterDefinition readerPd,
            MethodReference readMr, VariableDefinition objectVariableDef, MethodReference setMr, TypeReference readTr)
        {
            if (readMr != null)
            {
                var processor = methodDef.Body.GetILProcessor();

                /* How to load object instance. If it's a structure
                 * then it must be loaded by address. Otherwise if
                 * class Ldloc can be used. */
                var loadOpCode = objectVariableDef.VariableType.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;

                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (readTr.IsGenericInstance)
                {
                    var git = (GenericInstanceType)readTr;
                    var genericTr = git.GenericArguments[0];
                    readMr = readMr.GetMethodReference(Session, genericTr);
                }

                processor.Emit(loadOpCode, objectVariableDef);
                //reader.
                processor.Emit(OpCodes.Ldarg, readerPd);
                if (IsAutoPackedType(readTr))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(readTr);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }

                //reader.ReadXXXX().
                processor.Emit(OpCodes.Call, readMr);
                //obj.Property = result / reader.ReadXXXX().
                processor.Emit(OpCodes.Call, setMr);

                return true;
            }

            LogError($"Reader not found for {readTr.FullName}.");
            return false;
        }


        /// <summary>
        ///     Creates generic write delegates for all currently known write types.
        /// </summary>
        internal void CreateStaticMethodDelegates()
        {
            foreach (var item in StaticReaderMethods)
                GetClass<ReaderProcessor>().CreateReadDelegate(item.Value, true);
        }


        /// <summary>
        ///     Returns if typeRef has a deserializer.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="createMissing"></param>
        /// <returns></returns>
        internal bool HasDeserializer(TypeReference typeRef, bool createMissing)
        {
            var result = GetInstancedReadMethodReference(typeRef) != null ||
                         GetStaticReadMethodReference(typeRef) != null;

            if (!result && createMissing)
                if (!GetClass<GeneralHelper>().HasNonSerializableAttribute(typeRef.CachedResolve(Session)))
                {
                    var methodRef = CreateReader(typeRef);
                    result = methodRef != null;
                }

            return result;
        }


        /// <summary>
        ///     Returns if typeRef supports auto packing.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal bool IsAutoPackedType(TypeReference typeRef)
        {
            return AutoPackedMethods.Contains(typeRef);
        }

        /// <summary>
        ///     Creates a null check on the first argument and returns a null object if result indicates to do so.
        /// </summary>
        internal void CreateRetOnNull(ILProcessor processor, ParameterDefinition readerParameterDef,
            VariableDefinition resultVariableDef, bool useBool)
        {
            var endIf = processor.Create(OpCodes.Nop);

            if (useBool)
                CreateReadBool(processor, readerParameterDef, resultVariableDef);
            else
                CreateReadPackedWhole(processor, readerParameterDef, resultVariableDef);

            //If (true or == -1) jmp to endIf. True is null.
            processor.Emit(OpCodes.Ldloc, resultVariableDef);
            if (useBool)
            {
                processor.Emit(OpCodes.Brfalse, endIf);
            }
            else
            {
                //-1
                processor.Emit(OpCodes.Ldc_I4_M1);
                processor.Emit(OpCodes.Bne_Un_S, endIf);
            }

            //Insert null.
            processor.Emit(OpCodes.Ldnull);
            //Exit method.
            processor.Emit(OpCodes.Ret);
            //End of if check.
            processor.Append(endIf);
        }

        /// <summary>
        ///     Creates a call to WriteBoolean with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerParameterDef"></param>
        /// <param name="value"></param>
        internal void CreateReadBool(ILProcessor processor, ParameterDefinition readerParameterDef,
            VariableDefinition localBoolVariableDef)
        {
            var readBoolMethodRef = GetReadMethodReference(GetClass<GeneralHelper>().GetTypeReference(typeof(bool)));
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            processor.Emit(readBoolMethodRef.GetCallOpCode(Session), readBoolMethodRef);
            processor.Emit(OpCodes.Stloc, localBoolVariableDef);
        }

        /// <summary>
        ///     Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal void CreateReadPackedWhole(ILProcessor processor, ParameterDefinition readerParameterDef,
            VariableDefinition resultVariableDef)
        {
            //Reader.
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            //Reader.ReadPackedWhole().
            var readPwMr = GetClass<ReaderImports>().Reader_ReadPackedWhole_MethodRef;
            processor.Emit(readPwMr.GetCallOpCode(Session), readPwMr);
            processor.Emit(OpCodes.Conv_I4);
            processor.Emit(OpCodes.Stloc, resultVariableDef);
        }


        /// <summary>
        ///     Generates a reader for objectTypeReference if one does not already exist.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        internal MethodReference CreateReader(TypeReference objectTr)
        {
            MethodReference resultMr = null;
            TypeDefinition objectTypeDef;

            var serializerType = GetClass<GeneratorHelper>().GetSerializerType(objectTr, false, out objectTypeDef);
            if (serializerType != SerializerType.Invalid)
            {
                //Array.
                if (serializerType == SerializerType.Array)
                    resultMr = CreateArrayReaderMethodReference(objectTr);
                //Enum.
                else if (serializerType == SerializerType.Enum)
                    resultMr = CreateEnumReaderMethodDefinition(objectTr);
                else if (serializerType == SerializerType.Dictionary)
                    resultMr = CreateDictionaryReaderMethodReference(objectTr);
                //List, ListCache.
                else if (serializerType == SerializerType.List || serializerType == SerializerType.ListCache)
                    resultMr = CreateGenericTypeReader(objectTr, serializerType);
                //NetworkBehaviour.
                else if (serializerType == SerializerType.NetworkBehaviour)
                    resultMr = GetNetworkBehaviourReaderMethodReference(objectTr);
                //Nullable.
                else if (serializerType == SerializerType.Nullable)
                    resultMr = CreateNullableReaderMethodReference(objectTr);
                //Class or struct.
                else if (serializerType == SerializerType.ClassOrStruct)
                    resultMr = CreateClassOrStructReaderMethodReference(objectTr);
            }

            //If was not created.
            if (resultMr == null)
                RemoveFromStaticReaders(objectTr);

            return resultMr;
        }


        /// <summary>
        ///     Removes from static writers.
        /// </summary>
        private void RemoveFromStaticReaders(TypeReference tr)
        {
            GetClass<ReaderProcessor>().RemoveReaderMethod(tr, false);
        }

        /// <summary>
        ///     Adds to static writers.
        /// </summary>
        private void AddToStaticReaders(TypeReference tr, MethodReference mr)
        {
            GetClass<ReaderProcessor>().AddReaderMethod(tr, mr.CachedResolve(Session), false, true);
        }

        /// <summary>
        ///     Generates a reader for objectTypeReference if one does not already exist.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference CreateEnumReaderMethodDefinition(TypeReference objectTr)
        {
            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            var processor = createdReaderMd.Body.GetILProcessor();

            //Get type reference for enum type. eg byte int
            var underlyingTypeRef = objectTr.CachedResolve(Session).GetEnumUnderlyingTypeReference();
            //Get read method for underlying type.
            var readMethodRef = GetClass<ReaderProcessor>().GetOrCreateReadMethodReference(underlyingTypeRef);
            if (readMethodRef == null)
                return null;

            var readerParameterDef = createdReaderMd.Parameters[0];
            //reader.ReadXXX().
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            if (GetClass<WriterProcessor>().IsAutoPackedType(underlyingTypeRef))
                processor.Emit(OpCodes.Ldc_I4, (int)AutoPackType.Packed);

            processor.Emit(OpCodes.Call, readMethodRef);

            processor.Emit(OpCodes.Ret);
            return ImportReference(createdReaderMd);
        }


        /// <summary>
        ///     Creates a read for a class type which inherits NetworkBehaviour.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference GetNetworkBehaviourReaderMethodReference(TypeReference objectTr)
        {
            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            var processor = createdReaderMd.Body.GetILProcessor();
            var networkBehaviourTypeRef = GetClass<GeneralHelper>().GetTypeReference(typeof(NetworkBehaviour));

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, GetClass<ReaderProcessor>().GetReadMethodReference(networkBehaviourTypeRef));
            processor.Emit(OpCodes.Castclass, objectTr);
            processor.Emit(OpCodes.Ret);
            return ImportReference(createdReaderMd);
        }

        /// <summary>
        ///     Create a reader for an array or list.
        /// </summary>
        private MethodReference CreateArrayReaderMethodReference(TypeReference objectTr)
        {
            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var elementTypeRef = objectTr.GetElementType();
            var readMethodRef = GetClass<ReaderProcessor>().GetOrCreateReadMethodReference(elementTypeRef);
            if (readMethodRef == null)
                return null;

            var processor = createdReaderMd.Body.GetILProcessor();

            var readerParameterDef = createdReaderMd.Parameters[0];
            var sizeVariableDef = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, typeof(int));
            //Load packed whole value into sizeVariableDef, exit if null indicator.
            GetClass<ReaderProcessor>().CreateRetOnNull(processor, readerParameterDef, sizeVariableDef, false);

            //Make local variable of array type.
            var collectionVariableDef = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, objectTr);
            //Create new array/list of size.
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            processor.Emit(OpCodes.Newarr, elementTypeRef);
            //Store new object of arr/list into collection variable.
            processor.Emit(OpCodes.Stloc, collectionVariableDef);

            var loopIndex = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, typeof(int));
            var loopComparer = processor.Create(OpCodes.Ldloc, loopIndex);

            //int i = 0
            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Stloc, loopIndex);
            processor.Emit(OpCodes.Br_S, loopComparer);

            //Loop content.
            //Collection[index]
            var contentStart = processor.Create(OpCodes.Ldloc, collectionVariableDef);
            processor.Append(contentStart);
            /* Only arrays load the index since we are setting to that index.
             * List call lst.Add */
            processor.Emit(OpCodes.Ldloc, loopIndex);
            //Collection[index] = reader.
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            //Pass in AutoPackType default.
            if (GetClass<ReaderProcessor>().IsAutoPackedType(elementTypeRef))
            {
                var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(elementTypeRef);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }

            //Collection[index] = reader.ReadType().
            processor.Emit(OpCodes.Call, readMethodRef);
            //Set value to collection.
            processor.Emit(OpCodes.Stelem_Any, elementTypeRef);

            //i++
            processor.Emit(OpCodes.Ldloc, loopIndex);
            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Add);
            processor.Emit(OpCodes.Stloc, loopIndex);
            //if i < length jmp to content start.
            processor.Append(loopComparer); //if i < size
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            processor.Emit(OpCodes.Blt_S, contentStart);

            processor.Emit(OpCodes.Ldloc, collectionVariableDef);
            processor.Emit(OpCodes.Ret);

            return ImportReference(createdReaderMd);
        }

        /// <summary>
        ///     Creates a reader for a dictionary.
        /// </summary>
        private MethodReference CreateDictionaryReaderMethodReference(TypeReference objectTr)
        {
            var rp = GetClass<ReaderProcessor>();

            var genericInstance = (GenericInstanceType)objectTr;
            ImportReference(genericInstance);
            var keyTr = genericInstance.GenericArguments[0];
            var valueTr = genericInstance.GenericArguments[1];

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var keyWriteMr = rp.GetOrCreateReadMethodReference(keyTr);
            var valueWriteMr = rp.GetOrCreateReadMethodReference(valueTr);
            if (keyWriteMr == null || valueWriteMr == null)
                return null;

            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            var processor = createdReaderMd.Body.GetILProcessor();
            var readDictGim = GetClass<ReaderImports>().Reader_ReadDictionary_MethodRef
                .MakeGenericMethod(keyTr, valueTr);

            var readerPd = createdReaderMd.Parameters[0];
            processor.Emit(OpCodes.Ldarg, readerPd);
            processor.Emit(readDictGim.GetCallOpCode(Session), readDictGim);
            processor.Emit(OpCodes.Ret);

            return ImportReference(createdReaderMd);
        }


        /// <summary>
        ///     Create a reader for a list.
        /// </summary>
        private MethodReference CreateGenericTypeReader(TypeReference objectTr, SerializerType st)
        {
            var rp = GetClass<ReaderProcessor>();

            if (st != SerializerType.List && st != SerializerType.ListCache)
            {
                LogError($"Reader SerializerType {st} is not implemented");
                return null;
            }

            var genericInstance = (GenericInstanceType)objectTr;
            ImportReference(genericInstance);
            var elementTr = genericInstance.GenericArguments[0];

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var elementReadMr = rp.GetOrCreateReadMethodReference(elementTr);
            if (elementReadMr == null)
                return null;

            TypeReference readerMethodTr = null;
            if (st == SerializerType.List)
                readerMethodTr = GetClass<GeneralHelper>().GetTypeReference(typeof(List<>));
            else if (st == SerializerType.ListCache)
                readerMethodTr = GetClass<GeneralHelper>().GetTypeReference(typeof(ListCache<>));

            var readerMd = rp.GetReadMethodReference(readerMethodTr);
            var typedReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);

            AddToStaticReaders(objectTr, typedReaderMd);

            var readerPd = typedReaderMd.Parameters[0];

            //Find add method for list.
            var readerGim = readerMd.GetMethodReference(Session, elementTr);
            var processor = readerMd.CachedResolve(Session).Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg, readerPd);
            processor.Emit(OpCodes.Call, readerGim);

            return elementReadMr;
        }


        /// <summary>
        ///     Creates a reader method for a struct or class objectTypeRef.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference CreateNullableReaderMethodReference(TypeReference objectTr)
        {
            var rp = GetClass<ReaderProcessor>();

            var objectGit = objectTr as GenericInstanceType;
            var valueTr = objectGit.GenericArguments[0];

            //Make sure object has a ctor.
            var objectCtorMd = objectTr.GetConstructor(Session, 1);
            if (objectCtorMd == null)
            {
                LogError(
                    $"{objectTr.Name} can't be deserialized because the nullable type does not have a constructor.");
                return null;
            }

            //Get the reader for the value.
            var valueReaderMr = rp.GetOrCreateReadMethodReference(valueTr);
            if (valueReaderMr == null)
                return null;

            var objectTd = objectTr.CachedResolve(Session);
            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            var processor = createdReaderMd.Body.GetILProcessor();

            var readerPd = createdReaderMd.Parameters[0];
            // create local for return value
            var resultVd = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, objectTr);

            //Read if null into boolean.
            var nullBoolVd = createdReaderMd.CreateVariable(Session, typeof(bool));
            rp.CreateReadBool(processor, readerPd, nullBoolVd);

            var afterReturnNullInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Ldloc, nullBoolVd);
            processor.Emit(OpCodes.Brfalse, afterReturnNullInst);
            //Return a null result.
            GetClass<GeneralHelper>().SetVariableDefinitionFromObject(processor, resultVd, objectTd);
            processor.Emit(OpCodes.Ldloc, resultVd);
            processor.Emit(OpCodes.Ret);
            processor.Append(afterReturnNullInst);

            var initMr = objectCtorMd.MakeHostInstanceGeneric(Session, objectGit);
            processor.Emit(OpCodes.Ldarg, readerPd);
            //If an auto pack method then insert default value.
            if (rp.IsAutoPackedType(valueTr))
            {
                var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(valueTr);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }

            processor.Emit(OpCodes.Call, valueReaderMr);
            processor.Emit(OpCodes.Newobj, initMr);
            processor.Emit(OpCodes.Ret);

            return ImportReference(createdReaderMd);
        }


        /// <summary>
        ///     Creates a reader method for a struct or class objectTypeRef.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference CreateClassOrStructReaderMethodReference(TypeReference objectTr)
        {
            var createdReaderMd = CreateStaticReaderStubMethodDefinition(objectTr);
            AddToStaticReaders(objectTr, createdReaderMd);

            var objectTypeDef = objectTr.CachedResolve(Session);
            var processor = createdReaderMd.Body.GetILProcessor();

            var readerParameterDef = createdReaderMd.Parameters[0];
            // create local for return value
            var objectVariableDef = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, objectTr);

            //If not a value type create a return null check.
            if (!objectTypeDef.IsValueType)
            {
                var nullVariableDef = GetClass<GeneralHelper>().CreateVariable(createdReaderMd, typeof(bool));
                //Load packed whole value into sizeVariableDef, exit if null indicator.
                GetClass<ReaderProcessor>().CreateRetOnNull(processor, readerParameterDef, nullVariableDef, true);
            }

            /* If here then not null. */
            //Make a new instance of object type and set to objectVariableDef.
            GetClass<GeneralHelper>().SetVariableDefinitionFromObject(processor, objectVariableDef, objectTypeDef);
            if (!ReadFieldsAndProperties(createdReaderMd, readerParameterDef, objectVariableDef, objectTr))
                return null;
            /* //codegen scriptableobjects seem to climb too high up to UnityEngine.Object when
             * creating serializers/deserialized. Make sure this is not possible. */

            //Load result and return it.
            processor.Emit(OpCodes.Ldloc, objectVariableDef);
            processor.Emit(OpCodes.Ret);

            return ImportReference(createdReaderMd);
        }


        /// <summary>
        ///     Reads all fields of objectTypeRef.
        /// </summary>
        private bool ReadFieldsAndProperties(MethodDefinition readerMd, ParameterDefinition readerPd,
            VariableDefinition objectVd, TypeReference objectTr)
        {
            var rp = GetClass<ReaderProcessor>();

            //This probably isn't needed but I'm too afraid to remove it.
            if (objectTr.Module != Module)
                objectTr = ImportReference(objectTr.CachedResolve(Session));

            //Fields.
            foreach (var fieldDef in objectTr.FindAllSerializableFields(Session
                         , EXCLUDED_AUTO_SERIALIZER_TYPES, EXCLUDED_ASSEMBLY_PREFIXES))
            {
                var importedFr = ImportReference(fieldDef);
                if (GetReadMethod(fieldDef.FieldType, out var readMr))
                    rp.CreateReadIntoClassOrStruct(readerMd, readerPd, readMr, objectVd, importedFr);
            }

            //Properties.
            foreach (var propertyDef in objectTr.FindAllSerializableProperties(Session
                         , EXCLUDED_AUTO_SERIALIZER_TYPES, EXCLUDED_ASSEMBLY_PREFIXES))
                if (GetReadMethod(propertyDef.PropertyType, out var readMr))
                {
                    var setMr = Module.ImportReference(propertyDef.SetMethod);
                    rp.CreateReadIntoClassOrStruct(readerMd, readerPd, readMr, objectVd, setMr,
                        propertyDef.PropertyType);
                }

            //Gets or creates writer method and outputs it. Returns true if method is found or created.
            bool GetReadMethod(TypeReference tr, out MethodReference readMr)
            {
                tr = ImportReference(tr);
                readMr = rp.GetOrCreateReadMethodReference(tr);
                return readMr != null;
            }

            return true;
        }


        /// <summary>
        ///     Creates the stub for a new reader method.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        public MethodDefinition CreateStaticReaderStubMethodDefinition(TypeReference objectTypeRef,
            string nameExtension = WriterProcessor.GENERATED_WRITER_NAMESPACE)
        {
            var methodName = $"{GENERATED_READ_PREFIX}{objectTypeRef.FullName}{nameExtension}s";
            // create new reader for this type
            var readerTypeDef = GetClass<GeneralHelper>()
                .GetOrCreateClass(out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_READERS_CLASS_NAME, null);
            var readerMethodDef = readerTypeDef.AddMethod(methodName,
                GENERATED_METHOD_ATTRIBUTES,
                objectTypeRef);

            GetClass<GeneralHelper>()
                .CreateParameter(readerMethodDef, GetClass<ReaderImports>().Reader_TypeRef, "reader");
            readerMethodDef.Body.InitLocals = true;

            return readerMethodDef;
        }

        #region Reflection references.

        public TypeDefinition GeneratedReaderClassTypeDef;
        public MethodDefinition GeneratedReaderOnLoadMethodDef;
        public readonly Dictionary<string, MethodReference> InstancedReaderMethods = new();
        public readonly Dictionary<string, MethodReference> StaticReaderMethods = new();
        public HashSet<TypeReference> AutoPackedMethods = new(new TypeReferenceComparer());

        #endregion

        #region Const.

        /// <summary>
        ///     Namespace to use for generated serializers and delegates.
        /// </summary>
        public const string GENERATED_READER_NAMESPACE = WriterProcessor.GENERATED_WRITER_NAMESPACE;

        /// <summary>
        ///     Name to use for generated serializers class.
        /// </summary>
        public const string GENERATED_WRITERS_CLASS_NAME = "GeneratedReaders___Internal";

        /// <summary>
        ///     Attributes to use for generated serializers class.
        /// </summary>
        public const TypeAttributes GENERATED_TYPE_ATTRIBUTES =
            TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass |
            TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed;

        /// <summary>
        ///     Name to use for InitializeOnce method.
        /// </summary>
        public const string INITIALIZEONCE_METHOD_NAME = WriterProcessor.INITIALIZEONCE_METHOD_NAME;

        /// <summary>
        ///     Attributes to use for InitializeOnce method within generated serializer classes.
        /// </summary>
        public const MethodAttributes INITIALIZEONCE_METHOD_ATTRIBUTES =
            WriterProcessor.INITIALIZEONCE_METHOD_ATTRIBUTES;

        /// <summary>
        ///     Attritbutes to use for generated serializers.
        /// </summary>
        public const MethodAttributes GENERATED_METHOD_ATTRIBUTES = WriterProcessor.GENERATED_METHOD_ATTRIBUTES;

        /// <summary>
        ///     Prefix used which all instanced and user created serializers should start with.
        /// </summary>
        internal const string READ_PREFIX = "Read";

        /// <summary>
        ///     Class name to use for generated readers.
        /// </summary>
        internal const string GENERATED_READERS_CLASS_NAME = "GeneratedReaders___Internal";

        /// <summary>
        ///     Prefix to use for generated readers.
        /// </summary>
        private const string GENERATED_READ_PREFIX = "Read___";

        /// <summary>
        ///     Types to exclude from being scanned for auto serialization.
        /// </summary>
        public static Type[] EXCLUDED_AUTO_SERIALIZER_TYPES => WriterProcessor.EXCLUDED_AUTO_SERIALIZER_TYPES;

        /// <summary>
        ///     Types to exclude from being scanned for auto serialization.
        /// </summary>
        public static string[] EXCLUDED_ASSEMBLY_PREFIXES => WriterProcessor.EXCLUDED_ASSEMBLY_PREFIXES;

        #endregion


        #region GetReaderMethodReference.

        /// <summary>
        ///     Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal MethodReference GetInstancedReadMethodReference(TypeReference typeRef)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            InstancedReaderMethods.TryGetValue(fullName, out var methodRef);
            return methodRef;
        }

        /// <summary>
        ///     Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal MethodReference GetStaticReadMethodReference(TypeReference typeRef)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            StaticReaderMethods.TryGetValue(fullName, out var methodRef);
            return methodRef;
        }

        /// <summary>
        ///     Returns the MethodReference for typeRef favoring instanced or static. Returns null if not found.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal MethodReference GetReadMethodReference(TypeReference typeRef)
        {
            MethodReference result;
            var favorInstanced = false;
            if (favorInstanced)
            {
                result = GetInstancedReadMethodReference(typeRef);
                if (result == null)
                    result = GetStaticReadMethodReference(typeRef);
            }
            else
            {
                result = GetStaticReadMethodReference(typeRef);
                if (result == null)
                    result = GetInstancedReadMethodReference(typeRef);
            }

            return result;
        }

        /// <summary>
        ///     Returns the MethodReference for typeRef favoring instanced or static.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal MethodReference GetOrCreateReadMethodReference(TypeReference typeRef)
        {
            var favorInstanced = false;
            //Try to get existing writer, if not present make one.
            var readMethodRef = GetReadMethodReference(typeRef);
            if (readMethodRef == null)
                readMethodRef = CreateReader(typeRef);

            //If still null then return could not be generated.
            if (readMethodRef == null)
            {
                LogError($"Could not create deserializer for {typeRef.FullName}.");
            }
            //Otherwise, check if generic and create writes for generic pararameters.
            else if (typeRef.IsGenericInstance)
            {
                var git = (GenericInstanceType)typeRef;
                foreach (var item in git.GenericArguments)
                {
                    var result = GetOrCreateReadMethodReference(item);
                    if (result == null)
                    {
                        LogError($"Could not create deserializer for {item.FullName}.");
                        return null;
                    }
                }
            }

            return readMethodRef;
        }

        #endregion
    }
}