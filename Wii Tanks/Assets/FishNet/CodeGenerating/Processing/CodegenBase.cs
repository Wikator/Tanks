using System;
using MonoFN.Cecil;
using SR = System.Reflection;


namespace FishNet.CodeGenerating
{
    internal abstract class CodegenBase
    {
        public CodegenSession Session { get; private set; }
        public ModuleDefinition Module { get; private set; }

        public virtual bool ImportReferences()
        {
            return true;
        }

        public void Initialize(CodegenSession session)
        {
            Session = session;
            Module = session.Module;
        }

        /// <summary>
        ///     Returns class of type if found within Session.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T GetClass<T>() where T : CodegenBase
        {
            return Session.GetClass<T>();
        }

        #region Logging.

        /// <summary>
        ///     Logs a warning.
        /// </summary>
        /// <param name="msg"></param>
        internal void LogWarning(string msg)
        {
            Session.LogWarning(msg);
        }

        /// <summary>
        ///     Logs an error.
        /// </summary>
        /// <param name="msg"></param>
        internal void LogError(string msg)
        {
            Session.LogError(msg);
        }

        #endregion

        #region ImportReference.

        public MethodReference ImportReference(SR.MethodBase method)
        {
            return Session.ImportReference(method);
        }

        public MethodReference ImportReference(SR.MethodBase method, IGenericParameterProvider context)
        {
            return Session.ImportReference(method, context);
        }

        public TypeReference ImportReference(TypeReference type)
        {
            return Session.ImportReference(type);
        }

        public TypeReference ImportReference(TypeReference type, IGenericParameterProvider context)
        {
            return Session.ImportReference(type, context);
        }

        public FieldReference ImportReference(FieldReference field)
        {
            return Session.ImportReference(field);
        }

        public FieldReference ImportReference(FieldReference field, IGenericParameterProvider context)
        {
            return Session.ImportReference(field, context);
        }

        public FieldReference ImportReference(SR.FieldInfo field)
        {
            return Session.ImportReference(field);
        }

        public FieldReference ImportReference(SR.FieldInfo field, IGenericParameterProvider context)
        {
            return Session.ImportReference(field, context);
        }

        public MethodReference ImportReference(MethodReference method)
        {
            return Session.ImportReference(method);
        }

        public MethodReference ImportReference(MethodReference method, IGenericParameterProvider context)
        {
            return Session.ImportReference(method, context);
        }

        public TypeReference ImportReference(Type type)
        {
            return Session.ImportReference(type, null);
        }

        public TypeReference ImportReference(Type type, IGenericParameterProvider context)
        {
            return Session.ImportReference(type, context);
        }

        #endregion
    }
}