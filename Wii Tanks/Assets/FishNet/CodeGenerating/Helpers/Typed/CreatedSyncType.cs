using MonoFN.Cecil;

namespace FishNet.CodeGenerating.Helping
{
    internal class CreatedSyncVar
    {
        public readonly MethodReference ConstructorMr;
        public readonly MethodReference GetValueMr;
        public readonly MethodReference SetSyncIndexMr;
        public readonly MethodReference SetValueMr;
        public readonly GenericInstanceType SyncVarGit;
        public readonly TypeDefinition VariableTd;
        public MethodReference HookMr;

        public CreatedSyncVar(GenericInstanceType syncVarGit, TypeDefinition variableTd, MethodReference getValueMr,
            MethodReference setValueMr, MethodReference setSyncIndexMr, MethodReference hookMr,
            MethodReference constructorMr)
        {
            SyncVarGit = syncVarGit;
            VariableTd = variableTd;
            GetValueMr = getValueMr;
            SetValueMr = setValueMr;
            SetSyncIndexMr = setSyncIndexMr;
            HookMr = hookMr;
            ConstructorMr = constructorMr;
        }
    }


    internal class CreatedSyncType
    {
        public MethodReference ConstructorMethodReference;
        public MethodReference GetPreviousClientValueMethodReference;
        public MethodReference GetValueMethodReference;
        public MethodReference ReadMethodReference;
        public MethodReference SetValueMethodReference;
        public TypeDefinition StubClassTypeDefinition;

        public CreatedSyncType(TypeDefinition stubClassTypeDef, MethodReference getMethodRef,
            MethodReference setMethodRef, MethodReference getPreviousMethodRef, MethodReference readMethodRef,
            MethodReference constructorMethodRef)
        {
            StubClassTypeDefinition = stubClassTypeDef;
            GetValueMethodReference = getMethodRef;
            SetValueMethodReference = setMethodRef;
            GetPreviousClientValueMethodReference = getPreviousMethodRef;
            ReadMethodReference = readMethodRef;
            ConstructorMethodReference = constructorMethodRef;
        }
    }
}