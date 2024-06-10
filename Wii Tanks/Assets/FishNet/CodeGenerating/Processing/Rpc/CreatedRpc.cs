using System.Collections.Generic;
using FishNet.Object.Helping;
using MonoFN.Cecil;

namespace FishNet.CodeGenerating.Processing.Rpc
{
    internal class CreatedRpc
    {
        public AttributeData AttributeData;
        public MethodDefinition LogicMethodDef;
        public uint MethodHash;
        public MethodDefinition OriginalMethodDef;
        public MethodDefinition ReaderMethodDef;
        public MethodDefinition RedirectMethodDef;
        public bool RunLocally;
        public MethodDefinition WriterMethodDef;

        public RpcType RpcType => AttributeData.RpcType;
        public CustomAttribute Attribute => AttributeData.Attribute;
        public TypeDefinition TypeDef => OriginalMethodDef.DeclaringType;
        public ModuleDefinition Module => OriginalMethodDef.Module;
    }


    internal static class CreatedRpcExtensions
    {
        /// <summary>
        ///     Returns CreatedRpc for rpcType.
        /// </summary>
        /// <returns></returns>
        public static CreatedRpc GetCreatedRpc(this List<CreatedRpc> lst, RpcType rpcType)
        {
            for (var i = 0; i < lst.Count; i++)
                if (lst[i].RpcType == rpcType)
                    return lst[i];
            //Fall through.
            return null;
        }

        /// <summary>
        ///     Returns combined RpcType for all entries.
        /// </summary>
        /// <returns></returns>
        public static RpcType GetCombinedRpcType(this List<CreatedRpc> lst)
        {
            var result = RpcType.None;
            for (var i = 0; i < lst.Count; i++)
                result |= lst[i].RpcType;

            return result;
        }
    }
}