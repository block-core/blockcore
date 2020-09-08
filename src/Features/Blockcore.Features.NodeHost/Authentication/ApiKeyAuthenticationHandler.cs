using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Blockcore.Features.NodeHost.Authorization;
using Blockcore.Features.NodeHost.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Blockcore.Features.NodeHost.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ProblemDetailsContentType = "application/problem+json";
        private readonly IGetApiKeyQuery getApiKeyQuery;
        private readonly NodeHostSettings nodeSettings;

        public ApiKeyAuthenticationHandler(
            NodeHostSettings nodeSettings,
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IGetApiKeyQuery getApiKeyQuery) : base(options, logger, encoder, clock)
        {
            this.nodeSettings = nodeSettings;
            this.getApiKeyQuery = getApiKeyQuery ?? throw new ArgumentNullException(nameof(getApiKeyQuery));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!this.Request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out StringValues apiKeyHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return AuthenticateResult.NoResult();
            }

            ApiKey existingApiKey = await this.getApiKeyQuery.Execute(providedApiKey);

            if (existingApiKey != null)
            {
                // First verify the path access is enabled, if so we'll perform a validation here.
                if (this.Request.Path.HasValue && existingApiKey.Paths != null && existingApiKey.Paths.Count > 0)
                {
                    string path = this.Request.Path.Value;
                    bool hasAccess = existingApiKey.Paths.Any(p => path.StartsWith(p));

                    if (!hasAccess)
                    {
                        // Return NoResult and return standard 401 Unauthorized result.
                        return AuthenticateResult.NoResult();
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, existingApiKey.Owner)
                };

                claims.AddRange(existingApiKey.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(claims, this.Options.AuthenticationType);
                var identities = new List<ClaimsIdentity> { identity };
                var principal = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(principal, this.Options.Scheme);

                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.Fail("Invalid API Key provided.");
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            this.Response.StatusCode = 401;
            this.Response.ContentType = ProblemDetailsContentType;

            var problemDetails = new UnauthorizedProblemDetails();

            await this.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, DefaultJsonSerializerOptions.Options));
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            this.Response.StatusCode = 403;
            this.Response.ContentType = ProblemDetailsContentType;

            var problemDetails = new ForbiddenProblemDetails();

            await this.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, DefaultJsonSerializerOptions.Options));
        }
    }
}
