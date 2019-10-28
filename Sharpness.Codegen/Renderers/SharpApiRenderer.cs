using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Sharpness.Codegen.Definitions;

namespace Sharpness.Codegen.Renderers
{
	public class SharpApiRenderer : ApiRenderer
	{
		public string RootNamespace { get; }

		readonly CSharpCodeProvider _compiler;

		public SharpApiRenderer(string outputDirectory, string rootNamespace)
			: base(outputDirectory)
		{
			RootNamespace = rootNamespace;
			_compiler = new CSharpCodeProvider();
		}

		string GetTypeName(Type type)
		{
			string name = _compiler.GetTypeOutput(new CodeTypeReference(type));

			if (type.IsGenericType)
			{
				foreach (var t in type.GenericTypeArguments)
				{
					var n = _compiler.GetTypeOutput(new CodeTypeReference(t));

					if (name.Contains(n))
					{
						name = name.Replace(n, GetTypeName(t));
					}
				}
			}

			if (name.StartsWith(type.Namespace, StringComparison.InvariantCulture))
			{
				name = name.Substring(type.Namespace.Length + 1);
			}

			return name;
		}

		string GetParameterTypeName(ApiParameter parameter)
		{
			if (parameter.IsFile)
			{
				return "ApiFile";
			}
			return GetTypeName(parameter.Type);
		}

		string GetActionName(ApiAction action)
		{
			var methodName = action.MethodInfo.Name;

			if (!methodName.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase))
			{
				methodName += "Async";
			}

			return methodName;
		}

		protected override void RenderController(ApiController controller)
		{
			string controllerName = GetControllerName(controller);
			string filePath = Path.Combine(OutputDirectory, $"{controllerName}Service.cs");

			using (StreamWriter writer = new StreamWriter(filePath, false))
			{
				foreach (var ns in controller.Types
					.Select(t => t.Namespace)
					.Distinct()
					.OrderBy(n => n))
				{
					writer.WriteLine($"using {ns};");
				}

				writer.WriteLine($"");
				writer.WriteLine($"namespace {RootNamespace}.Services");
				writer.WriteLine($"{{");

				writer.WriteLine($"    public class {controllerName}Service : ApiService");
				writer.WriteLine($"    {{");

				foreach (var action in controller.Actions)
				{
					string parametersDefinition = string.Join(", ",
						action.Parameters.Select(p => $"{GetParameterTypeName(p)} {p.Name}"));

					string actionName = GetActionName(action);
					string returnTypeName = GetTypeName(action.ReturnType);
					string actionUrl = '/' + string.Join("/", action.TemplateProviders
						.Select(p => p.Template.Replace("?", "")));

					writer.WriteLine($"        public ApiTask<{returnTypeName}> {actionName}({parametersDefinition})");
					writer.WriteLine($"        {{");
					writer.WriteLine($"            string url = $\"{actionUrl}\";");

					writer.Write($"            return ApiTask<{returnTypeName}>.Create(this, \"{action.Method}\", url)");

					foreach (var param in action.Parameters)
					{
						if (param.IsFile)
						{
							writer.Write($"\n                .AddFormParam(\"{param.Name}\", {param.Name})");
						}
						else
						{
							switch (param.Location)
							{
								case ApiParameterLocation.Body:
									writer.Write($"\n                .AddBodyParam({param.Name})");
									break;
								case ApiParameterLocation.Query:
									writer.Write($"\n                .AddQueryParam(\"{param.Name}\", {param.Name})");
									break;
							}
						}
					}

					writer.Write($"\n                .Execute()");
					writer.WriteLine(";");


					writer.WriteLine($"        }}");
				}

				writer.WriteLine($"    }}");
				writer.WriteLine($"}}");
			}
		}

		public override void Render(ApiAssembly assembly)
		{
			base.Render(assembly);
			CopyService();
		}

		void CopyService()
		{
			var assembly = GetType().Assembly;
			string serviceDestinationFilePath = Path.Combine(OutputDirectory, "ApiService.cs");

			using (var stream = assembly.GetManifestResourceStream("Sharpness.Codegen.Resources.CSharp.txt"))
			using (var reader = new StreamReader(stream))
			{
				string code = reader.ReadToEnd();
				code = code.Replace("ApiServiceNamespace", RootNamespace);
				File.WriteAllText(serviceDestinationFilePath, code);
			}
		}
	}
}
