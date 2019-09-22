using System;
using System.IO;
using System.Reflection;
using Ocb.Web.Codegen.Definitions;
using Ocb.Web.Codegen.Renderers;

namespace Ocb.Web.Codegen
{
    class Program
    {
        static string LocateSolutionDirectory()
        {
            string LocateDirectoryRecursive(string directory)
            {
                if (File.Exists(Path.Combine(directory, "Ocb.All.sln")))
                {
                    return directory;
                }
                if (Path.GetFileName(directory) == "Ocb.Web.Codegen")
                {
                    return Path.GetDirectoryName(directory);
                }
                return LocateDirectoryRecursive(Path.GetDirectoryName(directory));
            }

            return LocateDirectoryRecursive(Directory.GetCurrentDirectory());
        }

        static string GetConfig(string[] args)
        {
            if(args != null && args.Length == 1)
            {
                return args[0].Replace("'", "");
            }
            return "Debug";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Generating API for mobile application and website client...");


            DateTime start = DateTime.UtcNow;
            string solutionDirectory = LocateSolutionDirectory();
            string resourcesDirectory = Path.Combine(solutionDirectory, "Ocb.Web.Codegen", "Resources");
            string config = GetConfig(args);

            string assemblyPath = Path.Combine(solutionDirectory, "Ocb.Web.Server", "bin", config, "netcoreapp2.2", "Ocb.Web.Server.dll");
            var apiAssembly = new ApiAssembly(Assembly.LoadFrom(assemblyPath));


            var angularApiPath = Path.Combine(solutionDirectory, "Ocb.Web.Client", "src", "api");
            var angularRenderer = new AngularApiRenderer(resourcesDirectory, angularApiPath)
            {
                ControllersToIgnore = new string[] { "ZohoVerification", "ScheduleNotification", "Proxy" }
            };
            angularRenderer.Render(apiAssembly);


            var sharpApiPath = Path.Combine(solutionDirectory, "Ocb.Mobile.Api");
            var sharpRenderer = new SharpApiRenderer(resourcesDirectory, sharpApiPath, "Ocb.Mobile.Api")
            {
                ControllersToIgnore = new string[] { "ZohoVerification", "ScheduleNotification", "Organization", "Proxy", "Reports", "Users" }
            };
            sharpRenderer.Render(apiAssembly);


            Console.WriteLine($"API generated successfully: {(DateTime.UtcNow - start).TotalSeconds:f1}s");
        }
    }
}
