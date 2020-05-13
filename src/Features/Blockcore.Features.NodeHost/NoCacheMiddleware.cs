using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Blockcore.Features.NodeHost
{
    /// <summary>
    /// Middleware to set the response Cache-Control to no-cache.
    /// </summary>
    public class NoCacheMiddleware
    {
        private readonly RequestDelegate next;

        public NoCacheMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["Cache-Control"] = "no-cache";

            await this.next(context);
        }
    }
}