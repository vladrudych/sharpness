using System;
using System.Diagnostics;
using Sharpness.Build;
using Sharpness.Codegen.Definitions;
using Sharpness.Codegen.Renderers;

namespace Sharpness.Test
{
    class Program
    {
        static void TestCodegen()
        {
            string assemblyPath = "/path/to.dll";
            string outputDirectory = "/output/directory";

            using (ApiAssembly apiAssembly = new ApiAssembly(assemblyPath))
            {
                AngularApiRenderer angularApiRenderer = new AngularApiRenderer(outputDirectory);

                angularApiRenderer.Render(apiAssembly);

                Console.WriteLine("Done");
            }
        }

        static void TestBuild()
        {
            Debug.Assert(BuildServices.Resolve<DirectoryService>() != null, "Unable to find DirectoryService");
            Debug.Assert(BuildServices.Resolve<GitService>() != null, "Unable to find GitService");
            Debug.Assert(BuildServices.Resolve<GitService>().Branch == "master", "Unable to find a branch");
        }


        static void Main(string[] args)
        {
            TestBuild();
        }
    }
}
