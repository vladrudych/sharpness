﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Sharpness.Codegen.Definitions
{
    public class ApiController
    {
        public Type Type { get; }
        public List<ApiAction> Actions { get; }

        public List<Type> Types { get; }

        public List<IRouteTemplateProvider> TemplateProviders { get; }

        public ApiController(Type type)
        {
            Type = type;
            Types = new List<Type>();
            Actions = new List<ApiAction>();
            TemplateProviders = new List<IRouteTemplateProvider>();

            Parse();
        }

        void Parse()
        {
            TemplateProviders.AddRange(Type
                 .GetCustomAttributes(typeof(IRouteTemplateProvider), true)
                 .Cast<IRouteTemplateProvider>().ToList());

            var actionMethods = Type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(a => a.DeclaringType == Type);

            foreach (var actionMethod in actionMethods)
            {
                var returnType = ApiAction.GetCleanReturnType(actionMethod.ReturnType);

                if (returnType == null
                    || actionMethod.GetCustomAttribute(typeof(NonActionAttribute)) != null)
                {
                    continue;
                }

                var action = new ApiAction(actionMethod, TemplateProviders);

                Actions.Add(action);

                Types.AddRange(action.Types.Where(t => !Types.Contains(t)));
            }
        }

    }

}
