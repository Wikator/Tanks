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
    internal class WriterProcessor : CodegenBase
    {
        #region Misc.

        /// <summary>
        ///     TypeReferences which have already had delegates made for.
        /// </summary>
        private readonly HashSet<TypeReference> _delegatedTypes = new();

        #endregion

        private int _instancedCount = 0;

        public override bool ImportReferences()
        {
            return true;
        }

        /// <summary>
        ///     Processes data. To be used after everything else has called ImportReferences.
        /// </summary>
        /// <returns></returns>
        public bool Process()
        {
            var gh = GetClass<GeneralHelper>();

            CreateGeneratedClassData();
            FindInstancedWriters();
            CreateInstancedWriterExtensions();

            //Creates class for generated writers, and init on load method.
            void CreateGeneratedClassData()
            {
                GeneratedWriterClassTypeDef = gh.GetOrCreateClass(out _, GENERATED_TYPE_ATTRIBUTES,
                    GENERATED_WRITERS_CLASS_NAME, null);
                /* If constructor isn't set then try to get or create it
                 * and also add it to methods if were created. */
                GeneratedWriterOnLoadMethodDef = gh.GetOrCreateMethod(GeneratedWriterClassTypeDef, out _,
                    INITIALIZEONCE_METHOD_ATTRIBUTES, INITIALIZEONCE_METHOD_NAME, Module.TypeSystem.Void);
                var pp = GeneratedWriterOnLoadMethodDef.Body.GetILProcessor();
                pp.Emit(OpCodes.Ret);
                gh.CreateRuntimeInitializeOnLoadMethodAttribute(GeneratedWriterOnLoadMethodDef);
            }

            //Finds all instanced writers and autopack types.
            void FindInstancedWriters()
            {
                var pooledWriterType = typeof(PooledWriter);
                foreach (var methodInfo in pooledWriterType.GetMethods())
                {
                    if (IsSpecialWriteMethod(methodInfo))
                        continue;
                    bool autoPackMethod;
                    if (IsIgnoredWriteMethod(methodInfo, out autoPackMethod))
                        continue;

                    var methodRef = ImportReference(methodInfo);
                    /* TypeReference for the first parameter in the write method.
                     * The first parameter will always be the type written. */
                    var typeRef = ImportReference(methodRef.Parameters[0].ParameterType);
                    /* If here all checks pass. */
                    AddWriterMethod(typeRef, methodRef, true, true);
                    if (autoPackMethod)
                        AutoPackedMethods.Add(typeRef);
                }
            }

            return true;
        }

        /// <summary>
        ///     Returns if a MethodInfo is considered a special write method.
        ///     Special write methods have declared references within this class, and will not have extensions made for them.
        /// </summary>
        public bool IsSpecialWriteMethod(SR.MethodInfo methodInfo)
        {
            /* Special methods. */
            if (methodInfo.Name == nameof(PooledWriter.Dispose))
                return true;
            if (methodInfo.Name == nameof(PooledWriter.WritePackedWhole))
                return true;
            if (methodInfo.Name == nameof(PooledWriter.WriteDictionary))
                return true;

            return false;
        }

        /// <summary>
        ///     Returns if a write method should be ignored.
        /// </summary>
        public bool IsIgnoredWriteMethod(SR.MethodInfo methodInfo, out bool autoPackMethod)
        {
            autoPackMethod = false;

            if (GetClass<GeneralHelper>().CodegenExclude(methodInfo))
                return true;
            //Not long enough to be a write method.
            if (methodInfo.Name.Length < WRITE_PREFIX.Length)
                return true;
            //Method name doesn't start with writePrefix.
            if (methodInfo.Name.Substring(0, WRITE_PREFIX.Length) != WRITE_PREFIX)
                return true;

            var parameterInfos = methodInfo.GetParameters();
            /* No parameters or more than 2 parameters. Most Write methods
             * will have only 1 parameter but some will have 2 if
             * there is a pack option. */
            if (parameterInfos.Length < 1 || parameterInfos.Length > 2)
                return true;
            /* If two parameters make sure the second parameter
             * is a pack parameter. */
            if (parameterInfos.Length == 2)
            {
                autoPackMethod = parameterInfos[1].ParameterType == typeof(AutoPackType);
                if (!autoPackMethod)
                    return true;
            }

            return false;
        }


        /// <summary>
        ///     Creates writer extension methods for built-in writers.
        /// </summary>
        private void CreateInstancedWriterExtensions()
        {
            //return;
            if (!FishNetILPP.IsFishNetAssembly(Session))
                return;

            var gh = GetClass<GeneralHelper>();
            var gwh = GetClass<WriterProcessor>();

            //List<MethodReference> staticReaders = new List<MethodReference>();
            foreach (var item in InstancedWriterMethods)
            {
                var instancedWriteMr = item.Value;
                if (instancedWriteMr.HasGenericParameters)
                    continue;

                var valueTr = instancedWriteMr.Parameters[0].ParameterType;

                var md = new MethodDefinition($"InstancedExtension___{instancedWriteMr.Name}",
                    GENERATED_METHOD_ATTRIBUTES,
                    Module.TypeSystem.Void);

                //Add extension parameter.
                var writerPd = gh.CreateParameter(md, typeof(Writer), "writer");
                //Add parameters needed by instanced writer.
                var otherPds = md.CreateParameters(Session, instancedWriteMr);
                gh.MakeExtensionMethod(md);
                //
                gwh.GeneratedWriterClassTypeDef.Methods.Add(md);

                var processor = md.Body.GetILProcessor();
                //Load writer.
                processor.Emit(OpCodes.Ldarg, writerPd);
                //Load args.
                foreach (var pd in otherPds)
                    processor.Emit(OpCodes.Ldarg, pd);
                //Call instanced.
                processor.Emit(instancedWriteMr.GetCallOpCode(Session), instancedWriteMr);
                processor.Emit(OpCodes.Ret);
                AddWriterMethod(valueTr, md, false, true);
            }
        }

        /// <summary>
        ///     Adds typeRef, methodDef to Instanced or Static write methods.
        /// </summary>
        public void AddWriterMethod(TypeReference typeRef, MethodReference methodRef, bool instanced, bool useAdd)
        {
            var dict = instanced ? InstancedWriterMethods : StaticWriterMethods;
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            if (useAdd)
                dict.Add(fullName, methodRef);
            else
                dict[fullName] = methodRef;
        }

        /// <summary>
        ///     Removes typeRef from Instanced or Static write methods.
        /// </summary>
        internal void RemoveWriterMethod(TypeReference typeRef, bool instanced)
        {
            var dict = instanced ? InstancedWriterMethods : StaticWriterMethods;

            dict.Remove(typeRef.FullName);
        }

        /// <summary>
        ///     Returns if typeRef supports auto packing.
        /// </summary>
        public bool IsAutoPackedType(TypeReference typeRef)
        {
            return AutoPackedMethods.Contains(typeRef);
        }


        /// <summary>
        ///     Creates Write<T> delegates for known static methods.
        /// </summary>
        public void CreateStaticMethodDelegates()
        {
            foreach (var item in StaticWriterMethods)
                GetClass<WriterProcessor>().CreateStaticMethodWriteDelegate(item.Value);
        }

        /// <summary>
        ///     Creates a Write delegate for writeMethodRef and places it within the generated reader/writer constructor.
        /// </summary>
        /// <param name="writeMr"></param>
        private void CreateStaticMethodWriteDelegate(MethodReference writeMr)
        {
            var gh = GetClass<GeneralHelper>();
            var wi = GetClass<WriterImports>();

            //Check if ret already exist, if so remove it; ret will be added on again in this method.
            if (GeneratedWriterOnLoadMethodDef.Body.Instructions.Count != 0)
            {
                var lastIndex = GeneratedWriterOnLoadMethodDef.Body.Instructions.Count - 1;
                if (GeneratedWriterOnLoadMethodDef.Body.Instructions[lastIndex].OpCode == OpCodes.Ret)
                    GeneratedWriterOnLoadMethodDef.Body.Instructions.RemoveAt(lastIndex);
            }

            var processor = GeneratedWriterOnLoadMethodDef.Body.GetILProcessor();
            TypeReference dataTypeRef;
            dataTypeRef = writeMr.Parameters[1].ParameterType;

            //Check if writer already exist.
            if (_delegatedTypes.Contains(dataTypeRef))
            {
                LogError($"Generic write already created for {dataTypeRef.FullName}.");
                return;
            }

            _delegatedTypes.Add(dataTypeRef);

            /* Create a Action<Writer, T> delegate.
             * May also be Action<Writer, AutoPackType, T> delegate
             * for packed types. */
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, writeMr);

            GenericInstanceType actionGenericInstance;
            MethodReference actionConstructorInstanceMethodRef;
            var isAutoPacked = GetClass<WriterProcessor>().IsAutoPackedType(dataTypeRef);

            //Generate for auto pack type.
            if (isAutoPacked)
            {
                actionGenericInstance = gh.ActionT3TypeRef.MakeGenericInstanceType(wi.WriterTypeRef, dataTypeRef,
                    GetClass<WriterImports>().AutoPackTypeRef);
                actionConstructorInstanceMethodRef =
                    gh.ActionT3ConstructorMethodRef.MakeHostInstanceGeneric(Session, actionGenericInstance);
            }
            //Generate for normal type.
            else
            {
                actionGenericInstance = gh.ActionT2TypeRef.MakeGenericInstanceType(wi.WriterTypeRef, dataTypeRef);
                actionConstructorInstanceMethodRef =
                    gh.ActionT2ConstructorMethodRef.MakeHostInstanceGeneric(Session, actionGenericInstance);
            }

            processor.Emit(OpCodes.Newobj, actionConstructorInstanceMethodRef);
            //Call delegate to GenericWriter<T>.Write
            var genericInstance = wi.GenericWriterTypeRef.MakeGenericInstanceType(dataTypeRef);
            var genericrWriteMethodRef = isAutoPacked
                ? wi.WriteAutoPackGetSetMethodRef.MakeHostInstanceGeneric(Session, genericInstance)
                : wi.WriteGetSetMethodRef.MakeHostInstanceGeneric(Session, genericInstance);
            processor.Emit(OpCodes.Call, genericrWriteMethodRef);

            processor.Emit(OpCodes.Ret);
        }


        /// <summary>
        ///     Returns if typeRef has a serializer.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal bool HasSerializer(TypeReference typeRef, bool createMissing)
        {
            var result = GetInstancedWriteMethodReference(typeRef) != null ||
                         GetStaticWriteMethodReference(typeRef) != null;

            if (!result && createMissing)
                if (!GetClass<GeneralHelper>().HasNonSerializableAttribute(typeRef.CachedResolve(Session)))
                {
                    var methodRef = CreateWriter(typeRef);
                    result = methodRef != null;
                }

            return result;
        }


        /// <summary>
        ///     Creates a PooledWriter within the body/ and returns its variable index.
        ///     EG: PooledWriter writer = WriterPool.GetWriter();
        /// </summary>
        internal VariableDefinition CreatePooledWriter(MethodDefinition methodDef, int length)
        {
            VariableDefinition resultVd;
            var insts = CreatePooledWriter(methodDef, length, out resultVd);

            var processor = methodDef.Body.GetILProcessor();
            processor.Add(insts);
            return resultVd;
        }

        /// <summary>
        ///     Creates a PooledWriter within the body/ and returns its variable index.
        ///     EG: PooledWriter writer = WriterPool.GetWriter();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <returns></returns>
        internal List<Instruction> CreatePooledWriter(MethodDefinition methodDef, int length,
            out VariableDefinition resultVd)
        {
            var wi = GetClass<WriterImports>();

            var insts = new List<Instruction>();
            var processor = methodDef.Body.GetILProcessor();

            resultVd = GetClass<GeneralHelper>().CreateVariable(methodDef, wi.PooledWriter_TypeRef);
            //If length is specified then pass in length.
            if (length > 0)
            {
                insts.Add(processor.Create(OpCodes.Ldc_I4, length));
                insts.Add(processor.Create(OpCodes.Call, wi.WriterPool_GetWriterLength_MethodRef));
            }
            //Use parameter-less method if no length.
            else
            {
                insts.Add(processor.Create(OpCodes.Call, wi.WriterPool_GetWriter_MethodRef));
            }

            //Set value to variable definition.
            insts.Add(processor.Create(OpCodes.Stloc, resultVd));
            return insts;
        }


        /// <summary>
        ///     Calls Dispose on a PooledWriter.
        ///     EG: writer.Dispose();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerDefinition"></param>
        internal List<Instruction> DisposePooledWriter(MethodDefinition methodDef, VariableDefinition writerDefinition)
        {
            var wi = GetClass<WriterImports>();

            var insts = new List<Instruction>();
            var processor = methodDef.Body.GetILProcessor();

            insts.Add(processor.Create(OpCodes.Ldloc, writerDefinition));
            insts.Add(processor.Create(wi.PooledWriter_Dispose_MethodRef.GetCallOpCode(Session),
                wi.PooledWriter_Dispose_MethodRef));

            return insts;
        }


        /// <summary>
        ///     Creates a null check on the second argument using a boolean.
        /// </summary>
        internal void CreateRetOnNull(ILProcessor processor, ParameterDefinition writerParameterDef,
            ParameterDefinition checkedParameterDef, bool useBool)
        {
            var endIf = processor.Create(OpCodes.Nop);
            //If (value) jmp to endIf.
            processor.Emit(OpCodes.Ldarg, checkedParameterDef);
            processor.Emit(OpCodes.Brtrue, endIf);
            //writer.WriteBool / writer.WritePackedWhole
            if (useBool)
                CreateWriteBool(processor, writerParameterDef, true);
            else
                CreateWritePackedWhole(processor, writerParameterDef, -1);
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
        internal void CreateWriteBool(ILProcessor processor, ParameterDefinition writerParameterDef, bool value)
        {
            var writeBoolMethodRef = GetWriteMethodReference(GetClass<GeneralHelper>().GetTypeReference(typeof(bool)));
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            var intValue = value ? 1 : 0;
            processor.Emit(OpCodes.Ldc_I4, intValue);
            processor.Emit(writeBoolMethodRef.GetCallOpCode(Session), writeBoolMethodRef);
        }

        /// <summary>
        ///     Creates a Write call on a PooledWriter variable for parameterDef.
        ///     EG: writer.WriteBool(xxxxx);
        /// </summary>
        internal List<Instruction> CreateWriteInstructions(MethodDefinition methodDef, object pooledWriterDef,
            ParameterDefinition valueParameterDef, MethodReference writeMr)
        {
            var insts = new List<Instruction>();
            var processor = methodDef.Body.GetILProcessor();

            if (writeMr != null)
            {
                if (pooledWriterDef is VariableDefinition)
                {
                    insts.Add(processor.Create(OpCodes.Ldloc, (VariableDefinition)pooledWriterDef));
                }
                else if (pooledWriterDef is ParameterDefinition)
                {
                    insts.Add(processor.Create(OpCodes.Ldarg, (ParameterDefinition)pooledWriterDef));
                }
                else
                {
                    LogError(
                        $"{pooledWriterDef.GetType().FullName} is not a valid writerDef. Type must be VariableDefinition or ParameterDefinition.");
                    return new List<Instruction>();
                }

                insts.Add(processor.Create(OpCodes.Ldarg, valueParameterDef));
                //If an auto pack method then insert default value.
                if (AutoPackedMethods.Contains(valueParameterDef.ParameterType))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(valueParameterDef.ParameterType);
                    insts.Add(processor.Create(OpCodes.Ldc_I4, (int)packType));
                }

                var valueTr = valueParameterDef.ParameterType;
                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (valueTr.IsGenericInstance)
                {
                    var git = (GenericInstanceType)valueTr;
                    var genericTr = git.GenericArguments[0];
                    writeMr = writeMr.GetMethodReference(Session, genericTr);
                }

                insts.Add(processor.Create(OpCodes.Call, writeMr));
                return insts;
            }

            LogError($"Writer not found for {valueParameterDef.ParameterType.FullName}.");
            return new List<Instruction>();
        }

        /// <summary>
        ///     Creates a Write call on a PooledWriter variable for parameterDef.
        ///     EG: writer.WriteBool(xxxxx);
        /// </summary>
        internal void CreateWrite(MethodDefinition methodDef, object writerDef, ParameterDefinition valuePd,
            MethodReference writeMr)
        {
            var insts = CreateWriteInstructions(methodDef, writerDef, valuePd, writeMr);
            var processor = methodDef.Body.GetILProcessor();
            processor.Add(insts);
        }

        /// <summary>
        ///     Creates a Write call to a writer.
        ///     EG: StaticClass.WriteBool(xxxxx);
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="fieldDef"></param>
        internal void CreateWrite(MethodDefinition writerMd, ParameterDefinition valuePd, FieldDefinition fieldDef,
            MethodReference writeMr)
        {
            if (writeMr != null)
            {
                var processor = writerMd.Body.GetILProcessor();
                var writerPd = writerMd.Parameters[0];

                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (fieldDef.FieldType.IsGenericInstance)
                {
                    var git = (GenericInstanceType)fieldDef.FieldType;
                    var genericTr = git.GenericArguments[0];
                    writeMr = writeMr.GetMethodReference(Session, genericTr);
                }

                var fieldRef = GetClass<GeneralHelper>().GetFieldReference(fieldDef);
                processor.Emit(OpCodes.Ldarg, writerPd);
                processor.Emit(OpCodes.Ldarg, valuePd);
                processor.Emit(OpCodes.Ldfld, fieldRef);
                //If an auto pack method then insert default value.
                if (AutoPackedMethods.Contains(fieldDef.FieldType))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(fieldDef.FieldType);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }

                processor.Emit(OpCodes.Call, writeMr);
            }
            else
            {
                LogError($"Writer not found for {fieldDef.FieldType.FullName}.");
            }
        }

        /// <summary>
        ///     Creates a Write call to a writer.
        ///     EG: StaticClass.WriteBool(xxxxx);
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="propertyDef"></param>
        internal void CreateWrite(MethodDefinition writerMd, ParameterDefinition valuePd, MethodReference getMr,
            MethodReference writeMr)
        {
            var returnTr = ImportReference(getMr.ReturnType);

            if (writeMr != null)
            {
                var processor = writerMd.Body.GetILProcessor();
                var writerPd = writerMd.Parameters[0];

                /* If generic then find write class for
                 * data type. Currently we only support one generic
                 * for this. */
                if (returnTr.IsGenericInstance)
                {
                    var git = (GenericInstanceType)returnTr;
                    var genericTr = git.GenericArguments[0];
                    writeMr = writeMr.GetMethodReference(Session, genericTr);
                }

                processor.Emit(OpCodes.Ldarg, writerPd);
                var ldArgOC0 = valuePd.ParameterType.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg;
                processor.Emit(ldArgOC0, valuePd);
                processor.Emit(OpCodes.Call, getMr);
                //If an auto pack method then insert default value.
                if (AutoPackedMethods.Contains(returnTr))
                {
                    var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(returnTr);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }

                processor.Emit(OpCodes.Call, writeMr);
            }
            else
            {
                LogError($"Writer not found for {returnTr.FullName}.");
            }
        }

        #region Reflection references.

        public readonly Dictionary<string, MethodReference> InstancedWriterMethods = new();
        public readonly Dictionary<string, MethodReference> StaticWriterMethods = new();
        public HashSet<TypeReference> AutoPackedMethods = new(new TypeReferenceComparer());

        public TypeDefinition GeneratedWriterClassTypeDef;
        public MethodDefinition GeneratedWriterOnLoadMethodDef;

        #endregion

        #region Const.

        /// <summary>
        ///     Namespace to use for generated serializers and delegates.
        /// </summary>
        public const string GENERATED_WRITER_NAMESPACE = "FishNet.Serializing.Generated";

        /// <summary>
        ///     Name to use for generated serializers class.
        /// </summary>
        public const string GENERATED_WRITERS_CLASS_NAME = "GeneratedWriters___Internal";

        /// <summary>
        ///     Attributes to use for generated serializers class.
        /// </summary>
        public const TypeAttributes GENERATED_TYPE_ATTRIBUTES =
            TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass |
            TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed;

        /// <summary>
        ///     Name to use for InitializeOnce method.
        /// </summary>
        public const string INITIALIZEONCE_METHOD_NAME = "InitializeOnce";

        /// <summary>
        ///     Attributes to use for InitializeOnce method within generated serializer classes.
        /// </summary>
        public const MethodAttributes INITIALIZEONCE_METHOD_ATTRIBUTES =
            MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig;

        /// <summary>
        ///     Attritbutes to use for generated serializers.
        /// </summary>
        public const MethodAttributes GENERATED_METHOD_ATTRIBUTES =
            MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig;

        /// <summary>
        ///     Prefix all built-in and user created write methods should begin with.
        /// </summary>
        internal const string WRITE_PREFIX = "Write";

        /// <summary>
        ///     Prefix all built-in and user created write methods should begin with.
        /// </summary>
        internal const string GENERATED_WRITE_PREFIX = "Write___";

        /// <summary>
        ///     Types to exclude from being scanned for auto serialization.
        /// </summary>
        public static readonly Type[] EXCLUDED_AUTO_SERIALIZER_TYPES =
        {
            typeof(NetworkBehaviour)
        };

        /// <summary>
        ///     Types within assemblies which begin with these prefixes will not have serializers created for them.
        /// </summary>
        public static readonly string[] EXCLUDED_ASSEMBLY_PREFIXES =
        {
            "UnityEngine."
        };

        #endregion


        #region GetWriterMethodReference.

        /// <summary>
        ///     Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal MethodReference GetInstancedWriteMethodReference(TypeReference typeRef)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            InstancedWriterMethods.TryGetValue(fullName, out var methodRef);
            return methodRef;
        }

        /// <summary>
        ///     Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal MethodReference GetStaticWriteMethodReference(TypeReference typeRef)
        {
            var fullName = GetClass<GeneralHelper>().RemoveGenericBrackets(typeRef.FullName);
            StaticWriterMethods.TryGetValue(fullName, out var methodRef);
            return methodRef;
        }

        /// <summary>
        ///     Returns the MethodReference for typeRef favoring instanced or static.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal MethodReference GetWriteMethodReference(TypeReference typeRef)
        {
            var favorInstanced = false;

            MethodReference result;
            if (favorInstanced)
            {
                result = GetInstancedWriteMethodReference(typeRef);
                if (result == null)
                    result = GetStaticWriteMethodReference(typeRef);
            }
            else
            {
                result = GetStaticWriteMethodReference(typeRef);
                if (result == null)
                    result = GetInstancedWriteMethodReference(typeRef);
            }

            return result;
        }

        /// <summary>
        ///     Gets the write MethodRef for typeRef, or tries to create it if not present.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal MethodReference GetOrCreateWriteMethodReference(TypeReference typeRef)
        {
            var favorInstanced = false;
            //Try to get existing writer, if not present make one.
            var writeMethodRef = GetWriteMethodReference(typeRef);
            if (writeMethodRef == null)
                writeMethodRef = CreateWriter(typeRef);

            //If still null then return could not be generated.
            if (writeMethodRef == null)
            {
                LogError($"Could not create serializer for {typeRef.FullName}.");
            }
            //Otherwise, check if generic and create writes for generic pararameters.
            else if (typeRef.IsGenericInstance)
            {
                var git = (GenericInstanceType)typeRef;
                foreach (var item in git.GenericArguments)
                {
                    var result = GetOrCreateWriteMethodReference(item);
                    if (result == null)
                    {
                        LogError($"Could not create serializer for {item.FullName}.");
                        return null;
                    }
                }
            }

            return writeMethodRef;
        }

        #endregion

        #region CreateWritePackWhole

        /// <summary>
        ///     Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal void CreateWritePackedWhole(ILProcessor processor, ParameterDefinition writerParameterDef, int value)
        {
            var wi = GetClass<WriterImports>();

            //Create local int and set it to value.
            var intVariableDef = GetClass<GeneralHelper>().CreateVariable(processor.Body.Method, typeof(int));
            GetClass<GeneralHelper>().SetVariableDefinitionFromInt(processor, intVariableDef, value);
            //Writer.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            //Writer.WritePackedWhole(value).
            processor.Emit(OpCodes.Ldloc, intVariableDef);
            processor.Emit(OpCodes.Conv_U8);
            processor.Emit(wi.Writer_WritePackedWhole_MethodRef.GetCallOpCode(Session),
                wi.Writer_WritePackedWhole_MethodRef);
        }

        /// <summary>
        ///     Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal void CreateWritePackedWhole(ILProcessor processor, ParameterDefinition writerParameterDef,
            VariableDefinition value)
        {
            var wi = GetClass<WriterImports>();

            //Writer.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            //Writer.WritePackedWhole(value).
            processor.Emit(OpCodes.Ldloc, value);
            processor.Emit(OpCodes.Conv_U8);
            processor.Emit(wi.Writer_WritePackedWhole_MethodRef.GetCallOpCode(Session),
                wi.Writer_WritePackedWhole_MethodRef);
        }

        #endregion


        #region TypeReference writer generators.

        /// <summary>
        ///     Generates a writer for objectTypeReference if one does not already exist.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        internal MethodReference CreateWriter(TypeReference objectTr)
        {
            MethodReference methodRefResult = null;
            TypeDefinition objectTd;
            var serializerType = GetClass<GeneratorHelper>().GetSerializerType(objectTr, true, out objectTd);

            if (serializerType != SerializerType.Invalid)
            {
                //Array.
                if (serializerType == SerializerType.Array)
                    methodRefResult = CreateArrayWriterMethodDefinition(objectTr);
                //Enum.
                else if (serializerType == SerializerType.Enum)
                    methodRefResult = CreateEnumWriterMethodDefinition(objectTr);
                //Dictionary.
                else if (serializerType == SerializerType.Dictionary)
                    methodRefResult = CreateDictionaryWriterMethodReference(objectTr);
                //List, ListCache.
                else if (serializerType == SerializerType.List || serializerType == SerializerType.ListCache)
                    methodRefResult = CreateGenericTypeWriter(objectTr, serializerType);
                //NetworkBehaviour.
                else if (serializerType == SerializerType.NetworkBehaviour)
                    methodRefResult = CreateNetworkBehaviourWriterMethodReference(objectTd);
                //Nullable type.
                else if (serializerType == SerializerType.Nullable)
                    methodRefResult = CreateNullableWriterMethodReference(objectTr, objectTd);
                //Class or struct.
                else if (serializerType == SerializerType.ClassOrStruct)
                    methodRefResult = CreateClassOrStructWriterMethodDefinition(objectTr);
            }

            //If was not created.
            if (methodRefResult == null)
                RemoveFromStaticWriters(objectTr);

            return methodRefResult;
        }

        /// <summary>
        ///     Removes from static writers.
        /// </summary>
        private void RemoveFromStaticWriters(TypeReference tr)
        {
            GetClass<WriterProcessor>().RemoveWriterMethod(tr, false);
        }

        /// <summary>
        ///     Adds to static writers.
        /// </summary>
        private void AddToStaticWriters(TypeReference tr, MethodReference mr)
        {
            GetClass<WriterProcessor>().AddWriterMethod(tr, mr.CachedResolve(Session), false, true);
        }

        /// <summary>
        ///     Adds a write for a NetworkBehaviour class type to WriterMethods.
        /// </summary>
        /// <param name="classTypeRef"></param>
        private MethodReference CreateNetworkBehaviourWriterMethodReference(TypeReference objectTr)
        {
            var oh = GetClass<ObjectHelper>();

            objectTr = ImportReference(objectTr.Resolve());
            //All NetworkBehaviour types will simply WriteNetworkBehaviour/ReadNetworkBehaviour.
            //Create generated reader/writer class. This class holds all generated reader/writers.
            GetClass<GeneralHelper>()
                .GetOrCreateClass(out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_WRITERS_CLASS_NAME, null);

            var createdWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);
            AddToStaticWriters(objectTr, createdWriterMd);

            var processor = createdWriterMd.Body.GetILProcessor();

            var writeMethodRef =
                GetClass<WriterProcessor>().GetOrCreateWriteMethodReference(oh.NetworkBehaviour_TypeRef);
            //Get parameters for method.
            var writerParameterDef = createdWriterMd.Parameters[0];
            var classParameterDef = createdWriterMd.Parameters[1];

            //Load parameters as arguments.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg, classParameterDef);
            //writer.WriteNetworkBehaviour(arg1);
            processor.Emit(OpCodes.Call, writeMethodRef);

            processor.Emit(OpCodes.Ret);

            return ImportReference(createdWriterMd);
        }

        /// <summary>
        ///     Gets the length of a collection and writes the value to a variable.
        /// </summary>
        private void CreateCollectionLength(ILProcessor processor, ParameterDefinition collectionParameterDef,
            VariableDefinition storeVariableDef)
        {
            processor.Emit(OpCodes.Ldarg, collectionParameterDef);
            processor.Emit(OpCodes.Ldlen);
            processor.Emit(OpCodes.Conv_I4);
            processor.Emit(OpCodes.Stloc, storeVariableDef);
        }


        /// <summary>
        ///     Creates a writer for a class or struct of objectTypeRef.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference CreateNullableWriterMethodReference(TypeReference objectTr, TypeDefinition objectTd)
        {
            var wh = GetClass<WriterProcessor>();

            var objectGit = objectTr as GenericInstanceType;
            var valueTr = objectGit.GenericArguments[0];

            //Get the writer for the value.
            var valueWriterMr = wh.GetOrCreateWriteMethodReference(valueTr);
            if (valueWriterMr == null)
                return null;


            MethodDefinition tmpMd;
            tmpMd = objectTd.GetMethod("get_Value");
            var genericGetValueMr = tmpMd.MakeHostInstanceGeneric(Session, objectGit);
            tmpMd = objectTd.GetMethod("get_HasValue");
            var genericHasValueMr = tmpMd.MakeHostInstanceGeneric(Session, objectGit);

            /* Stubs generate Method(Writer writer, T value). */
            var createdWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);
            AddToStaticWriters(objectTr, createdWriterMd);

            var processor = createdWriterMd.Body.GetILProcessor();

            //Value parameter.
            var valuePd = createdWriterMd.Parameters[1];
            var writerPd = createdWriterMd.Parameters[0];

            //Have to write a new ret on null because nullables use hasValue for null checks.
            var afterNullRetInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Ldarga, valuePd);
            processor.Emit(OpCodes.Call, genericHasValueMr);
            processor.Emit(OpCodes.Brtrue_S, afterNullRetInst);
            wh.CreateWriteBool(processor, writerPd, true);
            processor.Emit(OpCodes.Ret);
            processor.Append(afterNullRetInst);

            //Code will only execute here and below if not null.
            wh.CreateWriteBool(processor, writerPd, false);

            processor.Emit(OpCodes.Ldarg, writerPd);
            processor.Emit(OpCodes.Ldarga, valuePd);
            processor.Emit(OpCodes.Call, genericGetValueMr);
            //If an auto pack method then insert default value.
            if (wh.IsAutoPackedType(valueTr))
            {
                var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(valueTr);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }

            processor.Emit(OpCodes.Call, valueWriterMr);

            processor.Emit(OpCodes.Ret);
            return ImportReference(createdWriterMd);
        }


        /// <summary>
        ///     Creates a writer for a class or struct of objectTypeRef.
        /// </summary>
        /// <param name="objectTr"></param>
        /// <returns></returns>
        private MethodReference CreateClassOrStructWriterMethodDefinition(TypeReference objectTr)
        {
            var wh = GetClass<WriterProcessor>();

            /*Stubs generate Method(Writer writer, T value). */
            var createdWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);
            AddToStaticWriters(objectTr, createdWriterMd);
            var processor = createdWriterMd.Body.GetILProcessor();

            //If not a value type then add a null check.
            if (!objectTr.CachedResolve(Session).IsValueType)
            {
                var writerPd = createdWriterMd.Parameters[0];
                wh.CreateRetOnNull(processor, writerPd, createdWriterMd.Parameters[1], true);
                //Code will only execute here and below if not null.
                wh.CreateWriteBool(processor, writerPd, false);
            }

            //Write all fields for the class or struct.
            var valueParameterDef = createdWriterMd.Parameters[1];
            if (!WriteFieldsAndProperties(createdWriterMd, valueParameterDef, objectTr))
                return null;

            processor.Emit(OpCodes.Ret);
            return ImportReference(createdWriterMd);
        }

        /// <summary>
        ///     Find all fields in type and write them
        /// </summary>
        /// <param name="objectTr"></param>
        /// <param name="processor"></param>
        /// <returns>false if fail</returns>
        private bool WriteFieldsAndProperties(MethodDefinition generatedWriteMd, ParameterDefinition valuePd,
            TypeReference objectTr)
        {
            var wh = GetClass<WriterProcessor>();

            //This probably isn't needed but I'm too afraid to remove it.
            if (objectTr.Module != Module)
                objectTr = ImportReference(objectTr.CachedResolve(Session));

            //Fields
            foreach (var fieldDef in
                     objectTr.FindAllSerializableFields(Session)) //, WriterHelper.EXCLUDED_AUTO_SERIALIZER_TYPES))
            {
                TypeReference tr;
                if (fieldDef.FieldType.IsGenericInstance)
                {
                    var genericTr = (GenericInstanceType)fieldDef.FieldType;
                    tr = genericTr.GenericArguments[0];
                }
                else
                {
                    tr = fieldDef.FieldType;
                }

                if (GetWriteMethod(fieldDef.FieldType, out var writeMr))
                    wh.CreateWrite(generatedWriteMd, valuePd, fieldDef, writeMr);
            }

            //Properties.
            foreach (var propertyDef in objectTr.FindAllSerializableProperties(Session
                         , EXCLUDED_AUTO_SERIALIZER_TYPES, EXCLUDED_ASSEMBLY_PREFIXES))
                if (GetWriteMethod(propertyDef.PropertyType, out var writerMr))
                {
                    var getMr = Module.ImportReference(propertyDef.GetMethod);
                    wh.CreateWrite(generatedWriteMd, valuePd, getMr, writerMr);
                }

            //Gets or creates writer method and outputs it. Returns true if method is found or created.
            bool GetWriteMethod(TypeReference tr, out MethodReference writeMr)
            {
                tr = ImportReference(tr);
                writeMr = wh.GetOrCreateWriteMethodReference(tr);
                return writeMr != null;
            }

            return true;
        }


        /// <summary>
        ///     Creates a writer for an enum.
        /// </summary>
        /// <param name="enumTr"></param>
        /// <returns></returns>
        private MethodReference CreateEnumWriterMethodDefinition(TypeReference enumTr)
        {
            var wh = GetClass<WriterProcessor>();

            var createdWriterMd = CreateStaticWriterStubMethodDefinition(enumTr);
            AddToStaticWriters(enumTr, createdWriterMd);

            var processor = createdWriterMd.Body.GetILProcessor();

            //Element type for enum. EG: byte int ect
            var underlyingTypeRef = enumTr.CachedResolve(Session).GetEnumUnderlyingTypeReference();
            //Method to write that type.
            var underlyingWriterMethodRef = wh.GetOrCreateWriteMethodReference(underlyingTypeRef);
            if (underlyingWriterMethodRef == null)
                return null;

            var writerParameterDef = createdWriterMd.Parameters[0];
            var valueParameterDef = createdWriterMd.Parameters[1];
            //Push writer and value into call.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            if (wh.IsAutoPackedType(underlyingTypeRef))
                processor.Emit(OpCodes.Ldc_I4, (int)AutoPackType.Packed);

            //writer.WriteXXX(value)
            processor.Emit(OpCodes.Call, underlyingWriterMethodRef);

            processor.Emit(OpCodes.Ret);
            return ImportReference(createdWriterMd);
        }


        /// <summary>
        ///     Creates a writer for an array.
        /// </summary>
        private MethodReference CreateArrayWriterMethodDefinition(TypeReference objectTr)
        {
            var wh = GetClass<WriterProcessor>();

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var elementTypeRef = objectTr.GetElementType();
            var writeMethodRef = wh.GetOrCreateWriteMethodReference(elementTypeRef);
            if (writeMethodRef == null)
                return null;

            var createdWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);
            AddToStaticWriters(objectTr, createdWriterMd);

            var processor = createdWriterMd.Body.GetILProcessor();

            //Null instructions.
            wh.CreateRetOnNull(processor, createdWriterMd.Parameters[0], createdWriterMd.Parameters[1], false);

            //Write length. It only makes it this far if not null.
            //int length = arr[].Length.
            var sizeVariableDef = GetClass<GeneralHelper>().CreateVariable(createdWriterMd, typeof(int));
            CreateCollectionLength(processor, createdWriterMd.Parameters[1], sizeVariableDef);
            //writer.WritePackedWhole(length).
            wh.CreateWritePackedWhole(processor, createdWriterMd.Parameters[0], sizeVariableDef);

            var loopIndex = GetClass<GeneralHelper>().CreateVariable(createdWriterMd, typeof(int));
            var loopComparer = processor.Create(OpCodes.Ldloc, loopIndex);

            //int i = 0
            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Stloc, loopIndex);
            processor.Emit(OpCodes.Br_S, loopComparer);

            //Loop content.
            var contentStart = processor.Create(OpCodes.Ldarg_0);
            processor.Append(contentStart);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Ldloc, loopIndex);

            if (elementTypeRef.IsValueType)
                processor.Emit(OpCodes.Ldelem_Any, elementTypeRef);
            else
                processor.Emit(OpCodes.Ldelem_Ref);
            //If auto pack type then write default auto pack.
            if (wh.IsAutoPackedType(elementTypeRef))
            {
                var packType = GetClass<GeneralHelper>().GetDefaultAutoPackType(elementTypeRef);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }

            //writer.Write
            processor.Emit(OpCodes.Call, writeMethodRef);

            //i++
            processor.Emit(OpCodes.Ldloc, loopIndex);
            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Add);
            processor.Emit(OpCodes.Stloc, loopIndex);
            //if i < length jmp to content start.
            processor.Append(loopComparer); //if i < obj(size).
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            processor.Emit(OpCodes.Blt_S, contentStart);

            processor.Emit(OpCodes.Ret);
            return ImportReference(createdWriterMd);
        }


        /// <summary>
        ///     Creates a writer for a dictionary collection.
        /// </summary>
        private MethodReference CreateDictionaryWriterMethodReference(TypeReference objectTr)
        {
            var wh = GetClass<WriterProcessor>();

            var genericInstance = (GenericInstanceType)objectTr;
            ImportReference(genericInstance);
            var keyTr = genericInstance.GenericArguments[0];
            var valueTr = genericInstance.GenericArguments[1];

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var keyWriteMr = wh.GetOrCreateWriteMethodReference(keyTr);
            var valueWriteMr = wh.GetOrCreateWriteMethodReference(valueTr);
            if (keyWriteMr == null || valueWriteMr == null)
                return null;

            var createdWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);
            AddToStaticWriters(objectTr, createdWriterMd);

            var processor = createdWriterMd.Body.GetILProcessor();
            var writeDictGim = GetClass<WriterImports>().Writer_WriteDictionary_MethodRef
                .MakeGenericMethod(keyTr, valueTr);

            var writerPd = createdWriterMd.Parameters[0];
            var valuePd = createdWriterMd.Parameters[1];
            processor.Emit(OpCodes.Ldarg, writerPd);
            processor.Emit(OpCodes.Ldarg, valuePd);
            processor.Emit(writeDictGim.GetCallOpCode(Session), writeDictGim);
            processor.Emit(OpCodes.Ret);

            return ImportReference(createdWriterMd);
        }

        /// <summary>
        ///     Creates a writer for a listcache.
        /// </summary>
        private MethodReference CreateGenericTypeWriter(TypeReference objectTr, SerializerType st)
        {
            var wh = GetClass<WriterProcessor>();

            if (st != SerializerType.List && st != SerializerType.ListCache)
            {
                LogError($"Writer SerializerType {st} is not implemented");
                return null;
            }

            var genericInstance = (GenericInstanceType)objectTr;
            ImportReference(genericInstance);
            var elementTr = genericInstance.GenericArguments[0];

            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a one. */
            var elementWriteMr = wh.GetOrCreateWriteMethodReference(elementTr);
            if (elementWriteMr == null)
                return null;

            TypeReference genericMethodTr = null;
            if (st == SerializerType.List)
                genericMethodTr = GetClass<GeneralHelper>().GetTypeReference(typeof(List<>));
            else if (st == SerializerType.ListCache)
                genericMethodTr = GetClass<GeneralHelper>().GetTypeReference(typeof(ListCache<>));

            var writerMd = wh.GetWriteMethodReference(genericMethodTr);
            var typedWriterMd = CreateStaticWriterStubMethodDefinition(objectTr);

            AddToStaticWriters(objectTr, typedWriterMd);

            var writerPd = typedWriterMd.Parameters[0];
            var valuePd = typedWriterMd.Parameters[1];

            var writerGim = typedWriterMd.GetMethodReference(Session, elementTr);
            var processor = writerMd.CachedResolve(Session).Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg, writerPd);
            processor.Emit(OpCodes.Ldarg, valuePd);
            processor.Emit(OpCodes.Call, writerGim);

            return elementWriteMr;
        }


        /// <summary>
        ///     Creates a method definition stub for objectTypeRef.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        public MethodDefinition CreateStaticWriterStubMethodDefinition(TypeReference objectTypeRef,
            string nameExtension = GENERATED_WRITER_NAMESPACE)
        {
            var methodName = $"{GENERATED_WRITE_PREFIX}{objectTypeRef.FullName}{nameExtension}";
            // create new writer for this type
            var writerTypeDef = GetClass<GeneralHelper>()
                .GetOrCreateClass(out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_WRITERS_CLASS_NAME, null);

            var writerMethodDef = writerTypeDef.AddMethod(methodName,
                MethodAttributes.Public |
                MethodAttributes.Static |
                MethodAttributes.HideBySig);

            GetClass<GeneralHelper>()
                .CreateParameter(writerMethodDef, GetClass<WriterImports>().Writer_TypeRef, "writer");
            GetClass<GeneralHelper>().CreateParameter(writerMethodDef, objectTypeRef, "value");
            writerMethodDef.Body.InitLocals = true;

            return writerMethodDef;
        }

        #endregion
    }
}