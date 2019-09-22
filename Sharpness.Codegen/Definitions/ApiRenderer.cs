using System.Linq;

namespace Sharpness.Codegen.Definitions
{
    public abstract class ApiRenderer
    {
        public string OutputDirectory { get; }
        public string[] ControllersToIgnore { get; set; }

        protected ApiRenderer(string outputDirectory)
        {
            OutputDirectory = outputDirectory;
        }

        public bool ShouldRenderController(ApiController controller)
        {
            if(controller.Actions.Count == 0)
            {
                return false;
            }

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
