using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Blockcore.Features.NodeHost.Authorization
{
    public class OnlyUsersAuthorizationHandler : AuthorizationHandler<OnlyUsersRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OnlyUsersRequirement requirement)
        {
            if (context.User.IsInRole(Roles.User))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
