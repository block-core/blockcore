using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Utilities.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Converts a long that represents a number of bytes to be represented in MB.
        /// </summary>
        public static bool RemoveSingleton<T>(this IServiceCollection services)
        {
            // Remove the service if it exists.
            return services.Remove(services.Where(sd => sd.ServiceType == typeof(T)).FirstOrDefault());
        }
    }
}