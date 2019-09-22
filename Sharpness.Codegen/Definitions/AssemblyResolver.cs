using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Sharpness.Codegen.Definitions
{
    // https://www.codeproject.com/Articles/1194332/WebControls/
    internal sealed class AssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly DependencyContext _dependencyContext;
        private readonly AssemblyLoadContext _loadContext;

        public AssemblyResolver(string path)
        {
            Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            _dependencyContext = DependencyContext.Load(Assembly);

            _assemblyResolver = new CompositeCompilationAssemblyResolver(
                new ICompilationAssemblyResolver[]
                {
                    new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver()
                }
            );

            _loadContext = AssemblyLoadContext.GetLoadContext(Assembly);
            _loadContext.Resolving += OnResolving;
        }

        public Assembly Assembly { get; }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            bool NamesMatch(Library library)
            {
                return string.Equals(library.Name, name.Name, StringComparison.OrdinalIgnoreCase);
            }

            CompilationLibrary compilationLibrary = _dependencyContext
                .CompileLibraries.FirstOrDefault(NamesMatch);

            if (compilationLibrary == null)
            {
                RuntimeLibrary runtimeLibrary = _dependencyContext
                    .RuntimeLibraries.FirstOrDefault(NamesMatch);

                if (runtimeLibrary != null)
                {
                    compilationLibrary = new CompilationLibrary(
                        runtimeLibrary.Type,
                        runtimeLibrary.Name,
                        runtimeLibrary.Version,
                        runtimeLibrary.Hash,
                        runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                        runtimeLibrary.Dependencies,
                        runtimeLibrary.Serviceable
                    );
                }
            }

            if (compilationLibrary != null)
            {
                var assemblies = new List<string>();

                _assemblyResolver.TryResolveAssemblyPaths(compilationLibrary, assemblies);

                if (assemblies.Count > 0)
                {
                    return _loadContext.LoadFromAssemblyPath(assemblies[0]);
                }
            }

            return null;
        }
    }
}
