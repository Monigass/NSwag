//-----------------------------------------------------------------------
// <copyright file="CodeGeneratorCommandBase.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using NConsole;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration;

namespace NSwag.Commands.CodeGeneration
{
    public abstract class CodeGeneratorCommandBase<TSettings> : InputOutputCommandBase
        where TSettings : ClientGeneratorBaseSettings
    {
        protected CodeGeneratorCommandBase(TSettings settings)
        {
            Settings = settings;
        }

        [JsonIgnore]
        public TSettings Settings { get; }

        [Argument(Name = "TemplateDirectory", IsRequired = false, Description = "The Liquid template directory (experimental).")]
        public string TemplateDirectory
        {
            get { return Settings.CodeGeneratorSettings.TemplateDirectory; }
            set { Settings.CodeGeneratorSettings.TemplateDirectory = value; }
        }

        [Argument(Name = "TypeNameGenerator", IsRequired = false, Description = "The custom ITypeNameGenerator implementation type in the form 'assemblyName:fullTypeName' or 'fullTypeName').")]
        public string TypeNameGeneratorType { get; set; }

        [Argument(Name = "PropertyNameGeneratorType", IsRequired = false, Description = "The custom IPropertyNameGenerator implementation type in the form 'assemblyName:fullTypeName' or 'fullTypeName').")]
        public string PropertyNameGeneratorType { get; set; }

        [Argument(Name = "EnumNameGeneratorType", IsRequired = false, Description = "The custom IEnumNameGenerator implementation type in the form 'assemblyName:fullTypeName' or 'fullTypeName').")]
        public string EnumNameGeneratorType { get; set; }

        public void InitializeCustomTypes()
        {
            if (!string.IsNullOrEmpty(TypeNameGeneratorType))
            {
                Settings.CodeGeneratorSettings.TypeNameGenerator = (ITypeNameGenerator)Activator.CreateInstance(GetType(TypeNameGeneratorType));
            }

            if (!string.IsNullOrEmpty(PropertyNameGeneratorType))
            {
                Settings.CodeGeneratorSettings.PropertyNameGenerator = (IPropertyNameGenerator)Activator.CreateInstance(GetType(PropertyNameGeneratorType));

            }

            if (!string.IsNullOrEmpty(EnumNameGeneratorType))
            {
                Settings.CodeGeneratorSettings.EnumNameGenerator = (IEnumNameGenerator)Activator.CreateInstance(GetType(EnumNameGeneratorType));
            }
        }

        public Type GetType(string typeName)
        {
            try
            {
                var split = typeName.Split(':');
                if (split.Length > 1)
                {
                    var assemblyName = split[0].Trim();
                    typeName = split[1].Trim();
                    var assembly = AppDomain.CurrentDomain.Load(new AssemblyName(assemblyName));
                    return assembly.GetType(typeName, true);
                }

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(typeName, false, true);
                    if (type != null)
                    {
                        return type;
                    }
                }
                throw new InvalidOperationException("Could not find the type '" + typeName + "'.");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not instantiate the type '" + typeName + "'. Try specifying the type with the assembly, e.g 'assemblyName:typeName'.", e);
            }
        }
    }
}