using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FishNet.Broadcast;
using FishNet.CodeGenerating.Extension;
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.Processing;
using FishNet.CodeGenerating.Processing.Rpc;
using FishNet.Serializing.Helping;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace FishNet.CodeGenerating.ILCore
{
    public class FishNetILPP : ILPostProcessor
    {
        #region Const.

        internal const string RUNTIME_ASSEMBLY_NAME = "FishNet.Runtime";

        #endregion

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly.Name.StartsWith("Unity."))
                return false;
            if (compiledAssembly.Name.StartsWith("UnityEngine."))
                return false;
            if (compiledAssembly.Name.StartsWith("UnityEditor."))
                return false;
            if (compiledAssembly.Name.Contains("Editor"))
                return false;

            /* This line contradicts the one below where referencesFishNet
             * becomes true if the assembly is FishNetAssembly. This is here
             * intentionally to stop codegen from running on the runtime
             * fishnet assembly, but the option below is for debugging. I would
             * comment out this check if I wanted to compile fishnet runtime. */
            //if (CODEGEN_THIS_NAMESPACE.Length == 0)
            //{
            //    if (compiledAssembly.Name == RUNTIME_ASSEMBLY_NAME)
            //        return false;
            //}
            var referencesFishNet = IsFishNetAssembly(compiledAssembly) || compiledAssembly.References.Any(filePath =>
                Path.GetFileNameWithoutExtension(filePath) == RUNTIME_ASSEMBLY_NAME);
            return referencesFishNet;
        }

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var assemblyDef = ILCoreHelper.GetAssemblyDefinition(compiledAssembly);
            if (assemblyDef == null)
                return null;

            //Check WillProcess again; somehow certain editor scripts skip the WillProcess check.
            if (!WillProcess(compiledAssembly))
                return null;

            var session = new CodegenSession();
            if (!session.Initialize(assemblyDef.MainModule))
                return null;

            var modified = false;

            var fnAssembly = IsFishNetAssembly(compiledAssembly);
            if (fnAssembly)
                modified |= ModifyMakePublicMethods(session);
            /* If one or more scripts use RPCs but don't inherit NetworkBehaviours
             * then don't bother processing the rest. */
            if (session.GetClass<NetworkBehaviourProcessor>()
                .NonNetworkBehaviourHasInvalidAttributes(session.Module.Types))
                return new ILPostProcessResult(null, session.Diagnostics);

            modified |= session.GetClass<WriterProcessor>().Process();
            modified |= session.GetClass<ReaderProcessor>().Process();
            modified |= CreateDeclaredSerializerDelegates(session);
            modified |= CreateDeclaredSerializers(session);
            modified |= CreateIBroadcast(session);
            modified |= CreateQOLAttributes(session);
            modified |= CreateNetworkBehaviours(session);
            modified |= CreateGenericReadWriteDelegates(session);

            if (fnAssembly)
            {
                var anr = session.Module.AssemblyReferences.FirstOrDefault(x =>
                    x.FullName == session.Module.Assembly.FullName);
                if (anr != null)
                    session.Module.AssemblyReferences.Remove(anr);
            }

            /* If there are warnings about SyncVars being in different assemblies.
             * This is awful ... codegen would need to be reworked to save
             * syncvars across all assemblies so that scripts referencing them from
             * another assembly can have it's instructions changed. This however is an immense
             * amount of work so it will have to be put on hold, for... a long.. long while. */
            if (session.DifferentAssemblySyncVars.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(
                    $"Assembly {session.Module.Name} has inherited access to SyncVars in different assemblies. When accessing SyncVars across assemblies be sure to use Get/Set methods withinin the inherited assembly script to change SyncVars. Accessible fields are:");

                foreach (var item in session.DifferentAssemblySyncVars)
                    sb.AppendLine(
                        $"Field {item.Name} within {item.DeclaringType.FullName} in assembly {item.Module.Name}.");

                session.LogWarning("v------- IMPORTANT -------v");
                session.LogWarning(sb.ToString());
                session.DifferentAssemblySyncVars.Clear();
            }

            //session.LogWarning($"Assembly {compiledAssembly.Name} took {stopwatch.ElapsedMilliseconds}.");
            if (!modified) return null;

            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };
            assemblyDef.Write(pe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), session.Diagnostics);
        }

        /// <summary>
        ///     Makees methods public scope which use CodegenMakePublic attribute.
        /// </summary>
        /// <returns></returns>
        private bool ModifyMakePublicMethods(CodegenSession session)
        {
            var makePublicTypeFullName = typeof(CodegenMakePublicAttribute).FullName;
            foreach (var td in session.Module.Types)
            foreach (var md in td.Methods)
            foreach (var ca in md.CustomAttributes)
                if (ca.AttributeType.FullName == makePublicTypeFullName)
                {
                    md.Attributes &= ~MethodAttributes.Assembly;
                    md.Attributes |= MethodAttributes.Public;
                }

            //There is always at least one modified.
            return true;
        }

        /// <summary>
        ///     Creates delegates for user declared serializers.
        /// </summary>
        internal bool CreateDeclaredSerializerDelegates(CodegenSession session)
        {
            var modified = false;

            var readWriteExtensionTypeAttr = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract;
            var allTypeDefs = session.Module.Types.ToList();
            foreach (var td in allTypeDefs)
            {
                if (session.GetClass<GeneralHelper>().IgnoreTypeDefinition(td))
                    continue;

                if (td.Attributes.HasFlag(readWriteExtensionTypeAttr))
                    modified |= session.GetClass<CustomSerializerProcessor>().CreateSerializerDelegates(td, true);
            }

            return modified;
        }

        /// <summary>
        ///     Creates serializers for custom types within user declared serializers.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private bool CreateDeclaredSerializers(CodegenSession session)
        {
            var modified = false;

            var readWriteExtensionTypeAttr = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract;
            var allTypeDefs = session.Module.Types.ToList();
            foreach (var td in allTypeDefs)
            {
                if (session.GetClass<GeneralHelper>().IgnoreTypeDefinition(td))
                    continue;

                if (td.Attributes.HasFlag(readWriteExtensionTypeAttr))
                    modified |= session.GetClass<CustomSerializerProcessor>().CreateSerializers(td);
            }

            return modified;
        }

        /// <summary>
        ///     Creaters serializers and calls for IBroadcast.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private bool CreateIBroadcast(CodegenSession session)
        {
            var modified = false;

            var networkBehaviourFullName = session.GetClass<NetworkBehaviourHelper>().FullName;
            var typeDefs = new HashSet<TypeDefinition>();
            foreach (var td in session.Module.Types)
            {
                var climbTd = td;
                do
                {
                    //Reached NetworkBehaviour class.
                    if (climbTd.FullName == networkBehaviourFullName)
                        break;

                    ///* Check initial class as well all types within
                    // * the class. Then check all of it's base classes. */
                    if (climbTd.ImplementsInterface<IBroadcast>())
                        typeDefs.Add(climbTd);
                    //7ms

                    //Add nested. Only going to go a single layer deep.
                    foreach (var nestedTypeDef in td.NestedTypes)
                        if (nestedTypeDef.ImplementsInterface<IBroadcast>())
                            typeDefs.Add(nestedTypeDef);
                    //0ms

                    climbTd = climbTd.GetNextBaseTypeDefinition(session);
                    //this + name check 40ms
                } while (climbTd != null);
            }


            //Create reader/writers for found typeDefs.
            foreach (var td in typeDefs)
            {
                var typeRef = session.ImportReference(td);
                var canSerialize = session.GetClass<GeneralHelper>().HasSerializerAndDeserializer(typeRef, true);
                if (!canSerialize)
                    session.LogError(
                        $"Broadcast {td.Name} does not support serialization. Use a supported type or create a custom serializer.");
                else
                    modified = true;
            }

            return modified;
        }

        /// <summary>
        ///     Handles QOLAttributes such as [Server].
        /// </summary>
        /// <returns></returns>
        private bool CreateQOLAttributes(CodegenSession session)
        {
            var modified = false;

            var codeStripping = false;

            var allTypeDefs = session.Module.Types.ToList();

            /* First pass, potentially only pass.
             * If code stripping them this will be run again. The first iteration
             * is to ensure things are removed in the proper order. */
            foreach (var td in allTypeDefs)
            {
                if (session.GetClass<GeneralHelper>().IgnoreTypeDefinition(td))
                    continue;

                modified |= session.GetClass<QolAttributeProcessor>().Process(td, codeStripping);
            }


            return modified;
        }

        /// <summary>
        ///     Creates NetworkBehaviour changes.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private bool CreateNetworkBehaviours(CodegenSession session)
        {
            var modified = false;
            //Get all network behaviours to process.
            var networkBehaviourTypeDefs = session.Module.Types
                .Where(td => td.IsSubclassOf(session, session.GetClass<NetworkBehaviourHelper>().FullName))
                .ToList();

            //Moment a NetworkBehaviour exist the assembly is considered modified.
            if (networkBehaviourTypeDefs.Count > 0)
                modified = true;

            /* Remove types which are inherited. This gets the child most networkbehaviours.
             * Since processing iterates all parent classes there's no reason to include them */
            RemoveInheritedTypeDefinitions(networkBehaviourTypeDefs);
            //Set how many rpcs are in children classes for each typedef.
            var inheritedRpcCounts = new Dictionary<TypeDefinition, uint>();
            SetChildRpcCounts(inheritedRpcCounts, networkBehaviourTypeDefs);
            //Set how many synctypes are in children classes for each typedef.
            var inheritedSyncTypeCounts = new Dictionary<TypeDefinition, uint>();
            SetChildSyncTypeCounts(inheritedSyncTypeCounts, networkBehaviourTypeDefs);

            /* This holds all sync types created, synclist, dictionary, var
             * and so on. This data is used after all syncvars are made so
             * other methods can look for references to created synctypes and
             * replace accessors accordingly. */
            var allProcessedSyncs = new List<(SyncType, ProcessedSync)>();
            var allProcessedCallbacks = new HashSet<string>();
            var processedClasses = new List<TypeDefinition>();

            foreach (var typeDef in networkBehaviourTypeDefs)
            {
                session.ImportReference(typeDef);
                //Synctypes processed for this nb and it's inherited classes.
                var processedSyncs = new List<(SyncType, ProcessedSync)>();
                session.GetClass<NetworkBehaviourProcessor>().Process(typeDef, processedSyncs,
                    inheritedSyncTypeCounts, inheritedRpcCounts);
                //Add to all processed.
                allProcessedSyncs.AddRange(processedSyncs);
            }

            /* Must run through all scripts should user change syncvar
             * from outside the networkbehaviour. */
            if (allProcessedSyncs.Count > 0)
                foreach (var td in session.Module.Types)
                {
                    session.GetClass<NetworkBehaviourSyncProcessor>().ReplaceGetSets(td, allProcessedSyncs);
                    session.GetClass<RpcProcessor>().RedirectBaseCalls();
                }

            /* Removes typedefinitions which are inherited by
             * another within tds. For example, if the collection
             * td contains A, B, C and our structure is
             * A : B : C then B and C will be removed from the collection
             *  Since they are both inherited by A. */
            void RemoveInheritedTypeDefinitions(List<TypeDefinition> tds)
            {
                var inheritedTds = new HashSet<TypeDefinition>();
                /* Remove any networkbehaviour typedefs which are inherited by
                 * another networkbehaviour typedef. When a networkbehaviour typedef
                 * is processed so are all of the inherited types. */
                for (var i = 0; i < tds.Count; i++)
                {
                    /* Iterates all base types and
                     * adds them to inheritedTds so long
                     * as the base type is not a NetworkBehaviour. */
                    var copyTd = tds[i].GetNextBaseTypeDefinition(session);
                    while (copyTd != null)
                    {
                        //Class is NB.
                        if (copyTd.FullName == session.GetClass<NetworkBehaviourHelper>().FullName)
                            break;

                        inheritedTds.Add(copyTd);
                        copyTd = copyTd.GetNextBaseTypeDefinition(session);
                    }
                }

                //Remove all inherited types.
                foreach (var item in inheritedTds)
                    tds.Remove(item);
            }

            /* Sets how many Rpcs are within the children
             * of each typedefinition. EG: if our structure is
             * A : B : C, with the following RPC counts...
             * A 3
             * B 1
             * C 2
             * then B child rpc counts will be 3, and C will be 4. */
            void SetChildRpcCounts(Dictionary<TypeDefinition, uint> typeDefCounts, List<TypeDefinition> tds)
            {
                foreach (var typeDef in tds)
                {
                    //Number of RPCs found while climbing typeDef.
                    uint childCount = 0;

                    var copyTd = typeDef;
                    do
                    {
                        //How many RPCs are in copyTd.
                        var copyCount = session.GetClass<RpcProcessor>().GetRpcCount(copyTd);

                        /* If not found it this is the first time being
                         * processed. When this occurs set the value
                         * to 0. It will be overwritten below if baseCount
                         * is higher. */
                        uint previousCopyChildCount = 0;
                        if (!typeDefCounts.TryGetValue(copyTd, out previousCopyChildCount))
                            typeDefCounts[copyTd] = 0;
                        /* If baseCount is higher then replace count for copyTd.
                         * This can occur when a class is inherited by several types
                         * and the first processed type might only have 1 rpc, while
                         * the next has 2. This could be better optimized but to keep
                         * the code easier to read, it will stay like this. */
                        if (childCount > previousCopyChildCount)
                            typeDefCounts[copyTd] = childCount;

                        //Increase baseCount with RPCs found here.
                        childCount += copyCount;

                        copyTd = copyTd.GetNextBaseClassToProcess(session);
                    } while (copyTd != null);
                }
            }


            /* This performs the same functionality as SetChildRpcCounts
             * but for SyncTypes. */
            void SetChildSyncTypeCounts(Dictionary<TypeDefinition, uint> typeDefCounts, List<TypeDefinition> tds)
            {
                foreach (var typeDef in tds)
                {
                    //Number of RPCs found while climbing typeDef.
                    uint childCount = 0;

                    var copyTd = typeDef;
                    /* Iterate up to the parent script and then reverse
                     * the order. This is so that the topmost is 0
                     * and each inerhiting script adds onto that.
                     * Setting child types this way makes it so parent
                     * types don't need to have their synctype/rpc counts
                     * rebuilt when scripts are later to be found
                     * inheriting from them. */
                    var reversedTypeDefs = new List<TypeDefinition>();
                    do
                    {
                        reversedTypeDefs.Add(copyTd);
                        copyTd = copyTd.GetNextBaseClassToProcess(session);
                    } while (copyTd != null);

                    reversedTypeDefs.Reverse();

                    foreach (var td in reversedTypeDefs)
                    {
                        //How many RPCs are in copyTd.
                        var copyCount = session.GetClass<NetworkBehaviourSyncProcessor>().GetSyncTypeCount(td);
                        /* If not found it this is the first time being
                         * processed. When this occurs set the value
                         * to 0. It will be overwritten below if baseCount
                         * is higher. */
                        uint previousCopyChildCount = 0;
                        if (!typeDefCounts.TryGetValue(td, out previousCopyChildCount))
                            typeDefCounts[td] = 0;
                        /* If baseCount is higher then replace count for copyTd.
                         * This can occur when a class is inherited by several types
                         * and the first processed type might only have 1 rpc, while
                         * the next has 2. This could be better optimized but to keep
                         * the code easier to read, it will stay like this. */
                        if (childCount > previousCopyChildCount)
                            typeDefCounts[td] = childCount;
                        //Increase baseCount with RPCs found here.
                        childCount += copyCount;
                    }
                }
            }


            return modified;
        }

        /// <summary>
        ///     Creates generic delegates for all read and write methods.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private bool CreateGenericReadWriteDelegates(CodegenSession session)
        {
            session.GetClass<WriterProcessor>().CreateStaticMethodDelegates();
            session.GetClass<ReaderProcessor>().CreateStaticMethodDelegates();

            return true;
        }

        internal static bool IsFishNetAssembly(ICompiledAssembly assembly)
        {
            return assembly.Name == RUNTIME_ASSEMBLY_NAME;
        }

        internal static bool IsFishNetAssembly(CodegenSession session)
        {
            return session.Module.Assembly.Name.Name == RUNTIME_ASSEMBLY_NAME;
        }

        internal static bool IsFishNetAssembly(ModuleDefinition moduleDef)
        {
            return moduleDef.Assembly.Name.Name == RUNTIME_ASSEMBLY_NAME;
        }
    }
}