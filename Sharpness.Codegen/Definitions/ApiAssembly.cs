using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Sharpness.Codegen.Definitions
{
    public class ApiAssembly
    {
        readonly Assembly _assembly;
        readonly PluginLoadContext _assemblyResolver;

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

            _assemblyResolver = new PluginLoadContext(dllPath);

            _assembly = _assemblyResolver.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(dllPath)));

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
    }
}
