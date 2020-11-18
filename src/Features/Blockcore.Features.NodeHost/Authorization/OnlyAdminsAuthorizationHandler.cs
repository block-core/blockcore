using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Blockcore.Features.NodeHost.Authorization
{
    public class OnlyAdminsAuthorizationHandler : AuthorizationHandler<OnlyAdminsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OnlyAdminsRequirement requirement)
        {
            if (context.User.IsInRole(Roles.Admin))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
