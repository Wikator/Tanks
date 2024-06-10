using System.Collections.Generic;
using MonoFN.Cecil.Cil;

namespace FishNet.CodeGenerating.Helping
{
    /// <summary>
    ///     Data used to modify an RpcIndex should the class have to be rebuilt.
    /// </summary>
    internal class SyncIndexData
    {
        public List<Instruction> DelegateInstructions = new();
        public uint SyncCount = 0;
    }
}