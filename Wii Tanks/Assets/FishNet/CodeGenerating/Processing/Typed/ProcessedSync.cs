using MonoFN.Cecil;

namespace FishNet.CodeGenerating.Processing
{
    public class ProcessedSync
    {
        public FieldReference GeneratedFieldRef;
        public MethodReference GetMethodRef;
        public FieldReference OriginalFieldRef;
        public MethodReference SetMethodRef;

        public ProcessedSync(FieldReference originalFieldRef, FieldReference generatedFieldRef,
            MethodReference setMethodRef, MethodReference getMethodRef)
        {
            OriginalFieldRef = originalFieldRef;
            GeneratedFieldRef = generatedFieldRef;
            SetMethodRef = setMethodRef;
            GetMethodRef = getMethodRef;
        }
    }
}