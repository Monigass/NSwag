//-----------------------------------------------------------------------
// <copyright file="SnakeCaseEnumNameGenerator.cs" company="NSwag">
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
    /// Custom enum name generator to not have underscores in the name
    /// </summary>
    public class SnakeCaseEnumNameGenerator : IEnumNameGenerator
    {
        /// <summary>Generates the enum name.</summary>
        /// <param name="index">The index.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="schema">The schema.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(int index, string name, object value, JsonSchema schema)
        {
            if (name.StartsWith("-"))
            {
                name = "Minus" + name;
            }

            return ConversionUtilities.ConvertToUpperCamelCase(name
                .Replace('_', '-')
                .Replace("@", "")
                .Replace(".", "-"), true);
        }
    }
}
