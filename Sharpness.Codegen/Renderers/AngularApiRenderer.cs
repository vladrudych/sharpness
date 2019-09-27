using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using Newtonsoft.Json.Serialization;
using Sharpness.Codegen.Definitions;

namespace Sharpness.Codegen.Renderers
{
    public class AngularApiRenderer : ApiRenderer
    {
        readonly CSharpCodeProvider _compiler;
        readonly List<string> _renderedModels;
        readonly List<string> _renderedControllers;

        string ModelsDirectory => Path.Combine(OutputDirectory, "models");
        string ServicesDirectory => Path.Combine(OutputDirectory, "services");

        public AngularApiRenderer(string outputDirectory) : base(outputDirectory)
        {
            _renderedModels = new List<string>();
            _renderedControllers = new List<string>();
            _compiler = new CSharpCodeProvider();
        }

        bool IsDefaultType(Type type)
        {
            return type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
                || type == typeof(bool)
                || type == typeof(object)
                || type == typeof(DateTime)
                || type == typeof(string);
        }

        string GetTypeName(Type type)
        {
            string name;
            bool nullable = false;

            var nullableType = Nullable.GetUnderlyingType(type);

            if (nullableType != null)
            {
                nullable = true;
                type = nullableType;
            }

            if (IsDefaultType(type))
            {
                if (type == typeof(int)
                    || type == typeof(uint)
                    || type == typeof(long)
                    || type == typeof(ulong)
                    || type == typeof(byte)
                    || type == typeof(sbyte)
                    || type == typeof(short)
                    || type == typeof(ushort)
                    || type == typeof(float)
                    || type == typeof(double)
                    || type == typeof(decimal))
                {
                    name = "number";
                }
                else if (type == typeof(bool))
                {
                    name = "boolean";
                }
                else if (type == typeof(DateTime))
                {
                    name = "Date";
                }
                else if (type == typeof(object))
                {
                    name = "object";
                }
                else
                {
                    name = "string";
                }
            }
            else
            {
                name = _compiler.GetTypeOutput(new CodeTypeReference(type));

                if (type.IsGenericType)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        name = GetTypeName(type.GenericTypeArguments[0]) + "[]";
                    }
                    else
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
                }

                if (name.StartsWith(type.Namespace, StringComparison.InvariantCulture))
                {
                    name = name.Substring(type.Namespace.Length + 1);
                }
            }

            if (nullable)
            {
                return name + "|null";
            }

            return name;
        }

        string GetParameterTypeName(ApiParameter parameter)
        {
            if (parameter.IsFile)
            {
                return "Blob";
            }
            return GetTypeName(parameter.Type);
        }

        string GetActionName(ApiAction action)
        {
            var methodName = action.MethodInfo.Name.Substring(0, 1).ToLowerInvariant()
                          + action.MethodInfo.Name.Substring(1);

            return Regex.Replace(methodName, "Async$", "");
        }

        void FillImports(Type type, List<string> imports)
        {
            if (type.IsGenericParameter)
            {
                return;
            }

            if (type.IsArray)
            {
                FillImports(type.GetElementType(), imports);
            }
            else if (type.IsGenericType)
            {
                if (Nullable.GetUnderlyingType(type) != null
                    || typeof(IEnumerable).IsAssignableFrom(type))
                {
                    FillImports(type.GenericTypeArguments[0], imports);
                }
                else
                {
                    var definition = type.GetGenericTypeDefinition();
                    var definitionName = definition.Name.Remove(definition.Name.IndexOf('`'));

                    if (!_renderedModels.Contains(definitionName))
                    {
                        _renderedModels.Add(definitionName);
                        RenderModel(definitionName, definition);
                    }

                    if (!imports.Contains(definitionName))
                    {
                        imports.Add(definitionName);
                    }

                    foreach (var a in type.GenericTypeArguments)
                    {
                        FillImports(a, imports);
                    }
                }
            }
            else
            {
                if (!IsDefaultType(type))
                {
                    if (!_renderedModels.Contains(type.Name))
                    {
                        _renderedModels.Add(type.Name);
                        RenderModel(type.Name, type);
                    }

                    if (!imports.Contains(type.Name))
                    {
                        imports.Add(type.Name);
                    }
                }
            }
        }

        void RecreateDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }

            Directory.CreateDirectory(directory);
        }

        public override void Render(ApiAssembly assembly)
        {
            RecreateDirectory(ModelsDirectory);
            RecreateDirectory(ServicesDirectory);

            base.Render(assembly);

            RenderModule();
            RenderIndex();
            CopyService();
        }

        protected override void RenderController(ApiController controller)
        {
            string controllerName = GetControllerName(controller);
            string filePath = Path.Combine(ServicesDirectory, $"{controllerName.ToLowerInvariant()}.service.ts");
            _renderedControllers.Add(controllerName);

            using (var writer = new StreamWriter(filePath, false))
            {
                List<string> imports = new List<string>();

                foreach (var t in controller.Types)
                {
                    FillImports(t, imports);
                }

                writer.WriteLine($"import {{ Observable }} from 'rxjs';");
                writer.WriteLine($"import {{ Injectable }} from '@angular/core';");
                writer.WriteLine($"import {{ ApiService }} from '../api.service';");
                writer.WriteLine($"");

                foreach (var i in imports.OrderBy(s => s))
                {
                    writer.WriteLine($"import {{ {i} }} from '../models/{i}';");
                }

                writer.WriteLine($"");
                writer.WriteLine($"@Injectable()");
                writer.WriteLine($"export class {controllerName}Service {{");
                writer.WriteLine($"");
                writer.WriteLine($"    constructor(private builder: ApiService) {{ }}");

                writer.WriteLine($"");

                foreach (var action in controller.Actions)
                {
                    string actionName = GetActionName(action);
                    string returnTypeName = GetTypeName(action.ReturnType);

                    string actionUrl = '/' + string.Join("/", action.TemplateProviders
                       .Select(p => Regex.Replace(
                           p.Template.Replace("?", ""),
                           "{(\\w+)}",
                           "${encodeURIComponent(String($1))}")));

                    string parametersDefinition = string.Join(", ",
                           action.Parameters.Select(p => $"{p.Name}: { GetParameterTypeName(p)}"));

                    writer.WriteLine($"    public {actionName}({parametersDefinition}): Observable<{returnTypeName}> {{");
                    writer.WriteLine($"        const url = `{actionUrl}`;");


                    writer.WriteLine($"        return this.builder.request<{returnTypeName}>(url, '{action.Method}')");

                    foreach (var param in action.Parameters)
                    {
                        if (param.IsFile)
                        {
                            writer.WriteLine($"            .addFileParam('{param.Name}', {param.Name})");
                        }
                        else
                        {
                            switch (param.Location)
                            {
                                case ApiParameterLocation.Body:
                                    writer.WriteLine($"            .addBodyParam({param.Name})");
                                    break;
                                case ApiParameterLocation.Query:
                                    writer.WriteLine($"            .addQueryParam('{param.Name}', {param.Name})");
                                    break;
                            }
                        }
                    }

                    writer.WriteLine("            .build();");

                    writer.WriteLine($"    }}");
                    writer.WriteLine($"");
                }

                writer.WriteLine($"}}");

            }
        }

        void RenderModel(string name, Type type)
        {
            string filePath = Path.Combine(ModelsDirectory, $"{name}.ts");

            using (var writer = new StreamWriter(filePath, false))
            {
                if (type.IsEnum)
                {
                    writer.WriteLine($"export enum {name} {{");
                    foreach (var n in type.GetEnumNames())
                    {
                        writer.WriteLine($"    {n} = '{n}',");
                    }
                    writer.WriteLine($"}}");
                }
                else
                {
                    var properties = type.GetProperties().OrderBy(p => p.Name).ToList();

                    List<string> imports = new List<string>();
                    Type[] genericTypeParameters = null;
                    string genericDefinitions = "";

                    if (type.IsGenericTypeDefinition)
                    {
                        genericTypeParameters = type.GetGenericArguments();
                        genericDefinitions = '<' + string.Join(", ", genericTypeParameters.Select(t => t.Name)) + '>';
                    }

                    foreach (var property in properties)
                    {
                        if (genericTypeParameters?.Contains(property.PropertyType) != true)
                        {
                            FillImports(property.PropertyType, imports);
                        }
                    }

                    foreach (var i in imports.Where(s => s != name).OrderBy(s => s))
                    {
                        writer.WriteLine($"import {{ {i} }} from './{i}';");
                    }

                    writer.WriteLine($"");
                    writer.WriteLine($"export interface {name}{genericDefinitions} {{");
                    writer.WriteLine($"");

                    foreach (var property in properties)
                    {
                        Attribute.IsDefined(property, typeof(JsonProperty));
                        
                        var jsonAttributeName = ((JsonProperty)property
                                .GetCustomAttributes(typeof(JsonProperty), false)
                                .FirstOrDefault())
                                ?.PropertyName;

                        string propertyName
                            = string.IsNullOrEmpty(jsonAttributeName)
                                ? property.Name.Substring(0, 1).ToLowerInvariant()
                                    + property.Name.Substring(1)
                                : jsonAttributeName;

                        string propertyType = GetTypeName(property.PropertyType);

                        writer.WriteLine($"    {propertyName}: {propertyType};");
                    }

                    writer.WriteLine($"");
                    writer.WriteLine($"}}");
                }
            }
        }

        void RenderModule()
        {
            string filePath = Path.Combine(OutputDirectory, $"api.module.ts");

            using (var writer = new StreamWriter(filePath, false))
            {

                writer.WriteLine($"import {{ NgModule }} from '@angular/core';");
                writer.WriteLine($"import {{ HttpClientModule }} from '@angular/common/http';");
                writer.WriteLine($"import {{ ApiService }} from './api.service';");

                writer.WriteLine($"");

                foreach (var c in _renderedControllers)
                {
                    writer.WriteLine($"import {{ {c}Service }} from './services/{c.ToLowerInvariant()}.service';");
                }

                writer.WriteLine($"");
                writer.WriteLine($"@NgModule({{");

                writer.WriteLine($"    imports: [HttpClientModule],");
                writer.WriteLine($"    declarations: [],");
                writer.WriteLine($"    exports: [],");
                writer.WriteLine($"    providers: [");

                foreach (var c in _renderedControllers)
                {
                    writer.WriteLine($"        {c}Service,");
                }
                writer.WriteLine($"        ApiService");

                writer.WriteLine($"    ]");

                writer.WriteLine($"}})");
                writer.WriteLine($"export class ApiModule {{");
                writer.WriteLine($"}}");
            }
        }

        void RenderIndex()
        {
            string filePath = Path.Combine(OutputDirectory, $"index.ts");

            using (var writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine($"export {{ ApiModule }} from './api.module';");
                writer.WriteLine($"");

                foreach (var c in _renderedControllers)
                {
                    writer.WriteLine($"export {{ {c}Service }} from './services/{c.ToLowerInvariant()}.service';");
                }

                writer.WriteLine($"");

                foreach (var m in _renderedModels)
                {
                    writer.WriteLine($"export {{ {m} }} from './models/{m}';");
                }
            }
        }


        void CopyService()
        {
            var assembly = GetType().Assembly;
            string serviceDestinationFilePath = Path.Combine(OutputDirectory, "api.service.ts");

            using (var stream = assembly.GetManifestResourceStream("Sharpness.Codegen.Resources.TypeScript.txt"))
            using (var reader = new StreamReader(stream))
            {
                string code = reader.ReadToEnd();
                File.WriteAllText(serviceDestinationFilePath, code);
            }
        }
    }
}
