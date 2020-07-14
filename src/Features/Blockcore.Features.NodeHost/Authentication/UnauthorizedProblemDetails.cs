using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Features.NodeHost.Authentication
{
    public class UnauthorizedProblemDetails : ProblemDetails
    {
        public UnauthorizedProblemDetails(string details = null)
        {
            this.Title = "Unauthorized";
            this.Detail = details;
            this.Status = 401;
            this.Type = "https://httpstatuses.com/401";
        }
    }
}
