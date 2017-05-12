﻿// <copyright file="MockAuthenticationHandler.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Services.Authentication
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using AspNet.Security.OpenIdConnect.Primitives;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Middleware which mocks the authentication with the identity data from the request.
    /// </summary>
    /// <remarks>
    /// This should never be used in production, as it allows complete bypass of authentication.
    /// </remarks>
    internal sealed class MockAuthenticationHandler : AuthenticationHandler<MockAuthenticationOptions>
    {
        private static readonly char[] AuthorizationTokenDelimeter = new[] { ':' };

        private static readonly char[] RoleDelimeter = new[] { ',' };

        public MockAuthenticationHandler(IOptionsSnapshot<MockAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            AuthenticationHeaderValue authorization;

            // Supported format: "Authorization: <Scheme==AuthenticationType> <Parameter>"
            var authorizationHeaderRaw = this.Request.Headers["Authorization"];
            if (!AuthenticationHeaderValue.TryParse(authorizationHeaderRaw, out authorization)
                || authorization == null
                || string.IsNullOrWhiteSpace(authorization.Scheme)
                || !authorization.Scheme.Equals(this.Scheme.Name, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(authorization.Parameter))
            {
                return Task.FromResult(AuthenticateResult.Fail("Unexpected AuthenticationScheme"));
            }

            // Supportered parameter format: "<UserId>:<UserName>:[<Role1>,<Role2>,...]"
            var parts = authorization.Parameter.Split(AuthorizationTokenDelimeter);
            if (parts.Length != 3)
            {
                return Task.FromResult(AuthenticateResult.Fail("Unexpected Format"));
            }

            // Create the mock identity
            var identity = new ClaimsIdentity(
                new Claim[]
                {
                    new Claim(OpenIdConnectConstants.Claims.Subject, parts[0]),
                    new Claim(OpenIdConnectConstants.Claims.Name, parts[1]),
                    new Claim(ClaimTypes.Email, parts[1] + "@test.com"),
                }
                .Concat(parts[2].Split(RoleDelimeter, StringSplitOptions.RemoveEmptyEntries).Select(role => new Claim(OpenIdConnectConstants.Claims.Role, role))),
                new IdentityOptions().Cookies.ApplicationCookieAuthenticationScheme,
                OpenIdConnectConstants.Claims.Name,
                OpenIdConnectConstants.Claims.Role);

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), null, this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}