using System.Reflection;
using FishNet.Connection;
using FishNet.Serializing;
using MonoFN.Cecil;

namespace FishNet.CodeGenerating.Helping
{
    internal class ReaderImports : CodegenBase
    {
        /// <summary>
        ///     Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        public override bool ImportReferences()
        {
            var rp = GetClass<ReaderProcessor>();

            PooledReader_TypeRef = ImportReference(typeof(PooledReader));
            Reader_TypeRef = ImportReference(typeof(Reader));
            NetworkConnection_TypeRef = ImportReference(typeof(NetworkConnection));

            GenericReaderTypeRef = ImportReference(typeof(GenericReader<>));
            ReaderTypeRef = ImportReference(typeof(Reader));

            PropertyInfo readPropertyInfo;
            readPropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.Read));
            ReadSetMethodRef = ImportReference(readPropertyInfo.GetSetMethod());
            readPropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.ReadAutoPack));
            ReadAutoPackSetMethodRef = ImportReference(readPropertyInfo.GetSetMethod());


            var pooledReaderType = typeof(PooledReader);
            foreach (var methodInfo in pooledReaderType.GetMethods())
                /* Special methods. */
                if (rp.IsSpecialReadMethod(methodInfo))
                {
                    if (methodInfo.Name == nameof(PooledReader.ReadPackedWhole))
                        Reader_ReadPackedWhole_MethodRef = ImportReference(methodInfo);
                    else if (methodInfo.Name == nameof(PooledReader.ReadArray))
                        Reader_ReadToCollection_MethodRef = ImportReference(methodInfo);
                    else if (methodInfo.Name == nameof(PooledReader.ReadDictionary))
                        Reader_ReadDictionary_MethodRef = ImportReference(methodInfo);
                }

            return true;
        }

        #region Reflection references.

        public TypeReference PooledReader_TypeRef;
        public TypeReference Reader_TypeRef;
        public TypeReference NetworkConnection_TypeRef;
        public MethodReference PooledReader_ReadNetworkBehaviour_MethodRef;

        public MethodReference Reader_ReadPackedWhole_MethodRef;
        public MethodReference Reader_ReadDictionary_MethodRef;
        public MethodReference Reader_ReadToCollection_MethodRef;
        public TypeReference GenericReaderTypeRef;
        public TypeReference ReaderTypeRef;
        public MethodReference ReadSetMethodRef;
        public MethodReference ReadAutoPackSetMethodRef;

        #endregion
    }
}