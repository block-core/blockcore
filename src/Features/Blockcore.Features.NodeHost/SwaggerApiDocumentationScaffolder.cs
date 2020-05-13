using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.Utilities.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Blockcore.Features.NodeHost
{
    /// <summary>
    /// Utility class that adds XML documentation references to the API
    /// </summary>
    public static class SwaggerApiDocumentationScaffolder
    {
        /// <summary>
        /// Scaffolds the folder to obtain documentation related to Controllers.
        /// </summary>
        /// <param name="options">The options.</param>
        public static void Scaffold(SwaggerGenOptions options)
        {
            IEnumerable<string> files = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => asm.GetLoadableTypes().Any(type => typeof(Controller).IsAssignableFrom(type)))
                .Select(asm => Path.ChangeExtension(asm.Location, "xml"));

            RegisterFiles(options, files);
        }

        private static void RegisterFiles(SwaggerGenOptions options, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    options.IncludeXmlComments(file);
                }
            }
        }
    }
}
