using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Ocb.Web.Codegen.Definitions
{
    public class ApiAssembly
    {
        private readonly Assembly _assembly;

        public List<ApiController> Controllers { get; }

        public ApiAssembly(Assembly assembly)
        {
            Controllers = new List<ApiController>();

            _assembly = assembly;

            Parse();
        }

        void Parse()
        {
            var controllerTypes = _assembly.GetExportedTypes()
                .Where(t => t.IsSubclassOf(typeof(Controller)) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                Controllers.Add(new ApiController(controllerType));
            }
        }
    }
}
