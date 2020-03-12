using System;
using Microsoft.AspNetCore.Http;

namespace Sharpness.Codegen.Definitions
{
    public enum ApiParameterLocation
    {
        Query, Path, Body
    }

    public  class ApiParameter
    {
        public Type Type { get; }
        public string Name { get; }
        public ApiParameterLocation Location { get; }

        public bool IsFile => Type.FullName == typeof(IFormFile).FullName;

        public ApiParameter(string name, Type type, ApiParameterLocation location)
        {
            Name = name;
            Type = type;
            Location = location;
        }
    }
}
