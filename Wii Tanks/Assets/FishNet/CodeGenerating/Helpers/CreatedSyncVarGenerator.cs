using System.Collections.Generic;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.Processing;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using MonoFN.Cecil;
using MonoFN.Cecil.Rocks;

namespace FishNet.CodeGenerating.Helping
{
    internal class CreatedSyncVarGenerator : CodegenBase
    {
        private readonly Dictionary<string, CreatedSyncVar> _createdSyncVars = new();

        /* //feature add and test the dirty boolean changes
         * eg... instead of base.Dirty()
         * do if (!base.Dirty()) return false;
         * See synclist for more info. */

        /// <summary>
        ///     Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        public override bool ImportReferences()
        {
            SyncVar_TypeRef = ImportReference(typeof(SyncVar<>));
            var svConstructor = SyncVar_TypeRef.GetFirstConstructor(Session, true);
            _syncVar_Constructor_MethodRef = ImportReference(svConstructor);

            var syncBaseType = typeof(SyncBase);
            _syncBase_TypeRef = ImportReference(syncBaseType);

            return true;
        }

        /// <summary>
        ///     Gets and optionally creates data for SyncVar<typeOfField>
        /// </summary>
        /// <param name="dataTr"></param>
        /// <returns></returns>
        internal CreatedSyncVar GetCreatedSyncVar(FieldDefinition originalFd, bool createMissing)
        {
            var dataTr = originalFd.FieldType;
            var dataTd = dataTr.CachedResolve(Session);

            var typeHash = dataTr.FullName + dataTr.IsArray;

            if (_createdSyncVars.TryGetValue(typeHash, out var createdSyncVar)) return createdSyncVar;

            if (!createMissing)
                return null;

            ImportReference(dataTd);

            var syncVarGit = SyncVar_TypeRef.MakeGenericInstanceType(dataTr);
            var genericDataTr = syncVarGit.GenericArguments[0];

            //Make sure can serialize.
            var canSerialize = GetClass<GeneralHelper>().HasSerializerAndDeserializer(genericDataTr, true);
            if (!canSerialize)
            {
                LogError(
                    $"SyncVar {originalFd.Name} data type {genericDataTr.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return null;
            }

            //Set needed methods from syncbase.
            MethodReference setSyncIndexMr;
            var genericSyncVarCtor = _syncVar_Constructor_MethodRef.MakeHostInstanceGeneric(Session, syncVarGit);

            if (!GetClass<NetworkBehaviourSyncProcessor>()
                    .SetSyncBaseMethods(_syncBase_TypeRef.CachedResolve(Session), out setSyncIndexMr, out _))
                return null;

            MethodReference setValueMr = null;
            MethodReference getValueMr = null;
            foreach (var md in SyncVar_TypeRef.CachedResolve(Session).Methods)
                //GetValue.
                if (md.Name == GETVALUE_NAME)
                {
                    var mr = ImportReference(md);
                    getValueMr = mr.MakeHostInstanceGeneric(Session, syncVarGit);
                }
                //SetValue.
                else if (md.Name == SETVALUE_NAME)
                {
                    var mr = ImportReference(md);
                    setValueMr = mr.MakeHostInstanceGeneric(Session, syncVarGit);
                }

            if (setValueMr == null || getValueMr == null)
                return null;

            var csv = new CreatedSyncVar(syncVarGit, dataTd, getValueMr, setValueMr, setSyncIndexMr, null,
                genericSyncVarCtor);
            _createdSyncVars.Add(typeHash, csv);
            return csv;
        }

        #region Relfection references.

        private TypeReference _syncBase_TypeRef;
        internal TypeReference SyncVar_TypeRef;
        private MethodReference _syncVar_Constructor_MethodRef;

        #endregion

        #region Const.

        private const string GETVALUE_NAME = "GetValue";
        private const string SETVALUE_NAME = "SetValue";

        #endregion
    }
}