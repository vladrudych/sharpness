using System.Linq;

namespace Ocb.Web.Codegen.Definitions
{
    public abstract class ApiRenderer
    {
        public string OutputDirectory { get; }
        public string ResourcesDirectory { get; }
        public string[] ControllersToIgnore { get; set; }

        protected ApiRenderer(string resourcesDirectory, string outputDirectory)
        {
            ResourcesDirectory = resourcesDirectory;
            OutputDirectory = outputDirectory;
        }

        public bool ShouldRenderController(ApiController controller)
        {
            string controllerName = controller.Type.Name.Replace("Controller", "");
            return ControllersToIgnore?.Contains(controllerName) != true;
        }

        public virtual void Render(ApiAssembly assembly)
        {
            foreach (var controller in assembly.Controllers)
            {
                if (ShouldRenderController(controller))
                {
                    RenderController(controller);
                }
            }
        }

        protected string GetControllerName(ApiController controller)
        {
            return controller.Type.Name.Replace("Controller", "");
        }

        protected abstract void RenderController(ApiController controller);
    }
}
