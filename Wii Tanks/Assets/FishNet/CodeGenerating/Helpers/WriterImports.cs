using System.Reflection;
using FishNet.Serializing;
using MonoFN.Cecil;

namespace FishNet.CodeGenerating.Helping
{
    internal class WriterImports : CodegenBase
    {
        /// <summary>
        ///     Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        public override bool ImportReferences()
        {
            PooledWriter_TypeRef = ImportReference(typeof(PooledWriter));
            Writer_TypeRef = ImportReference(typeof(Writer));
            AutoPackTypeRef = ImportReference(typeof(AutoPackType));

            GenericWriterTypeRef = ImportReference(typeof(GenericWriter<>));
            WriterTypeRef = ImportReference(typeof(Writer));

            PropertyInfo writePropertyInfo;
            writePropertyInfo = typeof(GenericWriter<>).GetProperty(nameof(GenericWriter<int>.Write));
            WriteGetSetMethodRef = ImportReference(writePropertyInfo.GetSetMethod());
            writePropertyInfo = typeof(GenericWriter<>).GetProperty(nameof(GenericWriter<int>.WriteAutoPack));
            WriteAutoPackGetSetMethodRef = ImportReference(writePropertyInfo.GetSetMethod());

            //WriterPool.GetWriter
            var writerPoolType = typeof(WriterPool);
            ImportReference(writerPoolType);
            foreach (var methodInfo in writerPoolType.GetMethods())
                if (methodInfo.Name == nameof(WriterPool.GetWriter))
                {
                    //GetWriter().
                    if (methodInfo.GetParameters().Length == 0)
                    {
                        WriterPool_GetWriter_MethodRef = ImportReference(methodInfo);
                    }
                    //GetWriter(?).
                    else if (methodInfo.GetParameters().Length == 1)
                    {
                        var pi = methodInfo.GetParameters()[0];
                        //GetWriter(int).
                        if (pi.ParameterType == typeof(int))
                            WriterPool_GetWriterLength_MethodRef = ImportReference(methodInfo);
                    }
                }

            var gwh = GetClass<WriterProcessor>();
            var pooledWriterType = typeof(PooledWriter);
            foreach (var methodInfo in pooledWriterType.GetMethods())
                if (gwh.IsSpecialWriteMethod(methodInfo))
                {
                    if (methodInfo.Name == nameof(PooledWriter.Dispose))
                        PooledWriter_Dispose_MethodRef = ImportReference(methodInfo);
                    else if (methodInfo.Name == nameof(PooledWriter.WritePackedWhole))
                        Writer_WritePackedWhole_MethodRef = ImportReference(methodInfo);
                    else if (methodInfo.Name == nameof(PooledWriter.WriteDictionary))
                        Writer_WriteDictionary_MethodRef = ImportReference(methodInfo);
                }

            return true;
        }

        #region Reflection references.

        public MethodReference WriterPool_GetWriter_MethodRef;
        public MethodReference WriterPool_GetWriterLength_MethodRef;
        public MethodReference Writer_WritePackedWhole_MethodRef;
        public TypeReference PooledWriter_TypeRef;
        public TypeReference Writer_TypeRef;
        public MethodReference PooledWriter_Dispose_MethodRef;
        public MethodReference Writer_WriteDictionary_MethodRef;
        public TypeReference AutoPackTypeRef;

        public TypeReference GenericWriterTypeRef;
        public TypeReference WriterTypeRef;
        public MethodReference WriteGetSetMethodRef;
        public MethodReference WriteAutoPackGetSetMethodRef;

        #endregion
    }
}