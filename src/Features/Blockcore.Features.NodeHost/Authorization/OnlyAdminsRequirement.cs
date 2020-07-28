using Microsoft.AspNetCore.Authorization;

namespace Blockcore.Features.NodeHost.Authorization
{
    public class OnlyAdminsRequirement : IAuthorizationRequirement
    {
    }
}
