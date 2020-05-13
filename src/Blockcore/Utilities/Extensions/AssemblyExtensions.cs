using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Blockcore.Utilities.Extensions
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Gets the loadable types, ignoring assembly that can't be loaded for any reason.
        /// </summary>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}
