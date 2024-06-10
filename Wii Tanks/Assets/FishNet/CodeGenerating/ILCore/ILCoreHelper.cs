using System.IO;
using MonoFN.Cecil;
using MonoFN.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace FishNet.CodeGenerating.ILCore
{
    internal static class ILCoreHelper
    {
        /// <summary>
        ///     Returns AssembleDefinition for compiledAssembly.
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
        internal static AssemblyDefinition GetAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition =
                AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData),
                    readerParameters);
            //Allows us to resolve inside FishNet assembly, such as for components.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}