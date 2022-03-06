//-----------------------------------------------------------------------
// <copyright file="SnakeCasePropertyNameGenerator.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rafael Medina, Monigass</author>
//-----------------------------------------------------------------------

using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace NSwag.CodeGeneration.CSharp
{
    /// <summary>
    /// Custom property name generator to not have underscores in the name
    /// </summary>
    public class SnakeCasePropertyNameGenerator : IPropertyNameGenerator
    {
        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonSchemaProperty property)
        {
            return ConversionUtilities.ConvertToUpperCamelCase(property.Name
                .Replace('_', '-')
                .Replace("@", "")
                .Replace(".", "-"), true);
        }
    }
}
