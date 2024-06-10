using System.Collections.Generic;
using FishNet.CodeGenerating.Helping.Extension;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;

namespace FishNet.CodeGenerating.Extension
{
    internal static class MethodDefinitionExtensions
    {
        /// <summary>
        ///     Returns the proper OpCode to use for call methods.
        /// </summary>
        public static OpCode GetCallOpCode(this MethodDefinition md)
        {
            if (md.Attributes.HasFlag(MethodAttributes.Virtual))
                return OpCodes.Callvirt;
            return OpCodes.Call;
        }

        /// <summary>
        ///     Returns the proper OpCode to use for call methods.
        /// </summary>
        public static OpCode GetCallOpCode(this MethodReference mr, CodegenSession session)
        {
            return mr.CachedResolve(session).GetCallOpCode();
        }

        /// <summary>
        ///     Adds otherMd parameters to thisMR and returns added parameters.
        /// </summary>
        public static List<ParameterDefinition> CreateParameters(this MethodReference thisMr, CodegenSession session,
            MethodDefinition otherMd)
        {
            return thisMr.CachedResolve(session).CreateParameters(session, otherMd);
        }

        /// <summary>
        ///     Adds otherMr parameters to thisMR and returns added parameters.
        /// </summary>
        public static List<ParameterDefinition> CreateParameters(this MethodReference thisMr, CodegenSession session,
            MethodReference otherMr)
        {
            return thisMr.CachedResolve(session).CreateParameters(session, otherMr.CachedResolve(session));
        }

        /// <summary>
        ///     Adds otherMd parameters to thisMd and returns added parameters.
        /// </summary>
        public static List<ParameterDefinition> CreateParameters(this MethodDefinition thisMd, CodegenSession session,
            MethodDefinition otherMd)
        {
            var results = new List<ParameterDefinition>();

            foreach (var pd in otherMd.Parameters)
            {
                session.ImportReference(pd.ParameterType);
                var currentCount = thisMd.Parameters.Count;
                var name = pd.Name + currentCount;
                var parameterDef = new ParameterDefinition(name, pd.Attributes, pd.ParameterType);
                //Set any default values.
                parameterDef.Constant = pd.Constant;
                parameterDef.IsReturnValue = pd.IsReturnValue;
                parameterDef.IsOut = pd.IsOut;
                foreach (var item in pd.CustomAttributes)
                    parameterDef.CustomAttributes.Add(item);
                parameterDef.HasConstant = pd.HasConstant;
                parameterDef.HasDefault = pd.HasDefault;

                thisMd.Parameters.Add(parameterDef);

                results.Add(parameterDef);
            }

            return results;
        }

        /// <summary>
        ///     Returns a method reference while considering if declaring type is generic.
        /// </summary>
        public static MethodReference GetMethodReference(this MethodDefinition md, CodegenSession session)
        {
            var methodRef = session.ImportReference(md);

            //Is generic.
            if (md.DeclaringType.HasGenericParameters)
            {
                var git = methodRef.DeclaringType.MakeGenericInstanceType();
                var result = new MethodReference(md.Name, md.ReturnType)
                {
                    HasThis = md.HasThis,
                    ExplicitThis = md.ExplicitThis,
                    DeclaringType = git,
                    CallingConvention = md.CallingConvention
                };
                foreach (var pd in md.Parameters)
                {
                    session.ImportReference(pd.ParameterType);
                    result.Parameters.Add(pd);
                }

                return result;
            }

            return methodRef;
        }


        /// <summary>
        ///     Returns a method reference for a generic method.
        /// </summary>
        public static MethodReference GetMethodReference(this MethodDefinition md, CodegenSession session,
            TypeReference typeReference)
        {
            var methodRef = session.ImportReference(md);
            return methodRef.GetMethodReference(session, typeReference);
        }


        /// <summary>
        ///     Returns a method reference for a generic method.
        /// </summary>
        public static MethodReference GetMethodReference(this MethodDefinition md, CodegenSession session,
            TypeReference[] typeReferences)
        {
            var methodRef = session.ImportReference(md);
            return methodRef.GetMethodReference(session, typeReferences);
        }
    }
}