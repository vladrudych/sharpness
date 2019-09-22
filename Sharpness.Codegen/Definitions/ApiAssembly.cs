using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Sharpness.Codegen.Definitions
{
    public class ApiAssembly : IDisposable
    {
        readonly Assembly _assembly;
        readonly AssemblyResolver _assemblyResolver;

        public List<ApiController> Controllers { get; }

        public ApiAssembly(Assembly assembly)
        {
            Controllers = new List<ApiController>();

            _assembly = assembly;

            Parse();
        }

        public ApiAssembly(string dllPath)
        {
            Controllers = new List<ApiController>();

            _assemblyResolver = new AssemblyResolver(dllPath);

            _assembly = _assemblyResolver.Assembly;

            Parse();
        }

        void Parse()
        {
            var controllerTypes = _assembly.GetExportedTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                Controllers.Add(new ApiController(controllerType));
            }
        }

        public void Dispose()
        {
            _assemblyResolver?.Dispose();
        }
    }
}
