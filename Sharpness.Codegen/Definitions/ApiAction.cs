using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Sharpness.Codegen.Definitions
{
    public class ApiAction
    {
        public List<Type> Types { get; }
        public MethodInfo MethodInfo { get; }
        public List<ApiParameter> Parameters { get; }
        public List<IRouteTemplateProvider> TemplateProviders { get; }

        public Type ReturnType { get; private set; }
        public string Method { get; private set; }

        public ApiAction(MethodInfo methodInfo, List<IRouteTemplateProvider> controllerTemplateProviders)
        {
            MethodInfo = methodInfo;
            Types = new List<Type>();
            Parameters = new List<ApiParameter>();
            TemplateProviders = new List<IRouteTemplateProvider>();

            Parse(controllerTemplateProviders);
        }

        public static Type GetCleanReturnType(Type type)
        {
            if (typeof(Task).IsAssignableFrom(type) || typeof(IActionResult).IsAssignableFrom(type))
            {
                if (type.IsGenericType)
                {
                    return GetCleanReturnType(type.GenericTypeArguments[0]);
                }

                return null;
            }

            return type;
        }

        void Parse(List<IRouteTemplateProvider> controllerTemplateProviders)
        {
            ReturnType = GetCleanReturnType(MethodInfo.ReturnType);

            Types.Add(ReturnType);

            var actionTemplateProviders = MethodInfo
                    .GetCustomAttributes(typeof(IRouteTemplateProvider), true)
                    .Cast<IRouteTemplateProvider>();

            TemplateProviders.AddRange(controllerTemplateProviders
                .Where(p => !string.IsNullOrEmpty(p.Template)));

            TemplateProviders.AddRange(actionTemplateProviders
                .Where(p => !string.IsNullOrEmpty(p.Template)));

            Method = MethodInfo
                .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>()
                .FirstOrDefault()
                ?.HttpMethods.FirstOrDefault();

            if (string.IsNullOrEmpty(Method))
            {
                if (MethodInfo.Name.StartsWith("Post", StringComparison.InvariantCulture)
                    || MethodInfo.Name.EndsWith("Post", StringComparison.InvariantCulture))
                {
                    Method = "POST";
                }
                else
                {
                    Method = "GET";
                }
            }

            foreach (var parameter in MethodInfo.GetParameters())
            {
                var attr = parameter
                    .GetCustomAttributes(typeof(IBindingSourceMetadata), true)
                    .Cast<IBindingSourceMetadata>()
                    .FirstOrDefault();

                ApiParameterLocation location;

                if (parameter.ParameterType == typeof(IFormFile))
                {
                    location = ApiParameterLocation.Body;
                }
                else
                {
                    Types.Add(parameter.ParameterType);

                    if (attr is FromBodyAttribute)
                    {
                        location = ApiParameterLocation.Body;
                    }
                    else
                    {
                        bool hasInPath = TemplateProviders
                            .Any(p => p.Template.Replace("?", "")
                                .Contains($"{{{parameter.Name}}}"));

                        location = hasInPath
                            ? ApiParameterLocation.Path
                            : ApiParameterLocation.Query;
                    }
                }

                Parameters.Add(new ApiParameter(
                    parameter.Name, parameter.ParameterType, location
                ));
            }
        }
    }
}
