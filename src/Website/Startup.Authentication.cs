// <copyright file="Startup.Authentication.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite
{
    using System.Threading.Tasks;
    using AspNet.Security.OpenIdConnect.Primitives;
    using ClickerHeroesTrackerWebsite.Services.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Configure authentication
    /// </summary>
    public partial class Startup
    {
        private void ConfigureAuthentication(IApplicationBuilder app, IHostingEnvironment env)
        {
            // While we transition, support both Cookie-based and Bearer-based authentication
            app.UseWhen(context => !context.Request.Headers.ContainsKey("Authorization"), branch => branch.UseIdentity());
            app.UseWhen(context => context.Request.Headers.ContainsKey("Authorization"), branch => branch.UseOAuthValidation());

            var authenticationSettingsOptions = app.ApplicationServices.GetService<IOptions<AuthenticationSettings>>();
            if (authenticationSettingsOptions.Value != null)
            {
                var openIdConnectAuthenticationSettings = authenticationSettingsOptions.Value.OpenIdConnect;
                if (openIdConnectAuthenticationSettings != null
                    && !string.IsNullOrEmpty(openIdConnectAuthenticationSettings.ClientId)
                    && !string.IsNullOrEmpty(openIdConnectAuthenticationSettings.ClientSecret))
                {
                    app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
                    {
                        Authority = "https://login.microsoftonline.com/common/v2.0",
                        ClientId = openIdConnectAuthenticationSettings.ClientId,
                        ClientSecret = openIdConnectAuthenticationSettings.ClientSecret,
                        Events = new OpenIdConnectEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.HandleResponse();
                                context.Response.Redirect("/Error?message=" + context.Failure.Message);
                                return Task.FromResult(0);
                            },
                        },
                        TokenValidationParameters = new TokenValidationParameters
                        {
                            // Instead of using the default validation (validating against
                            // a single issuer value, as we do in line of business apps),
                            // we inject our own multitenant validation logic
                            ValidateIssuer = false,

                            NameClaimType = OpenIdConnectConstants.Claims.Name,
                        },
                    });
                }

                var facebookAuthenticationSettings = authenticationSettingsOptions.Value.Facebook;
                if (facebookAuthenticationSettings != null
                    && !string.IsNullOrEmpty(facebookAuthenticationSettings.AppId)
                    && !string.IsNullOrEmpty(facebookAuthenticationSettings.AppSecret))
                {
                    app.UseFacebookAuthentication(new FacebookOptions
                    {
                        AppId = facebookAuthenticationSettings.AppId,
                        AppSecret = facebookAuthenticationSettings.AppSecret,
                    });
                }

                var googleAuthenticationSettings = authenticationSettingsOptions.Value.Google;
                if (googleAuthenticationSettings != null
                    && !string.IsNullOrEmpty(googleAuthenticationSettings.ClientId)
                    && !string.IsNullOrEmpty(googleAuthenticationSettings.ClientSecret))
                {
                    app.UseGoogleAuthentication(new GoogleOptions
                    {
                        ClientId = googleAuthenticationSettings.ClientId,
                        ClientSecret = googleAuthenticationSettings.ClientSecret,
                    });
                }

                var twitterAuthenticationSettings = authenticationSettingsOptions.Value.Twitter;
                if (twitterAuthenticationSettings != null
                    && !string.IsNullOrEmpty(twitterAuthenticationSettings.ConsumerKey)
                    && !string.IsNullOrEmpty(twitterAuthenticationSettings.ConsumerSecret))
                {
                    app.UseTwitterAuthentication(new TwitterOptions
                    {
                        ConsumerKey = twitterAuthenticationSettings.ConsumerKey,
                        ConsumerSecret = twitterAuthenticationSettings.ConsumerSecret,
                    });
                }
            }

            // Note: UseOpenIddict() must be registered after app.UseIdentity() and the external social providers.
            app.UseOpenIddict();

            // Allow auth mocking when not in prod
            if (!env.IsProduction())
            {
                app.UseMiddleware<MockAuthenticationOwinMiddleware>();
            }
        }
    }
}