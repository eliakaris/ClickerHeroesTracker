// <copyright file="Startup.Authentication.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite
{
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using ClickerHeroesTrackerWebsite.Models;
    using ClickerHeroesTrackerWebsite.Services.Authentication;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.AspNetCore.Authentication.Twitter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configure authentication
    /// </summary>
    public partial class Startup
    {
        private void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddCookieAuthentication(options => options.LoginPath = new PathString("/Account/Login"));

            // Register the OpenIddict services.
            services.AddOpenIddict(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                options.AddMvcBinders();

                // Enable the token endpoint (required to use the password flow).
                options.EnableTokenEndpoint("/api/auth/token");

                // Allow client applications to use the grant_type=password flow.
                options.AllowPasswordFlow();

                // Allow Http on devbox
                if (this.Environment.IsDevelopment())
                {
                    options.DisableHttpsRequirement();
                }
            });

            var microsoftClientId = this.Configuration["Authentication:Microsoft:ClientId"];
            var microsoftClientSecret = this.Configuration["Authentication:Microsoft:ClientSecret"];
            if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
            {
                services.AddOAuthAuthentication("Microsoft-AccessToken", options =>
                {
                    options.DisplayName = "MicrosoftAccount-AccessToken";
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = microsoftClientSecret;
                    options.CallbackPath = new PathString("/signin-microsoft-token");
                    options.AuthorizationEndpoint = MicrosoftAccountDefaults.AuthorizationEndpoint;
                    options.TokenEndpoint = MicrosoftAccountDefaults.TokenEndpoint;
                    options.Scope.Add("https://graph.microsoft.com/user.read");
                    options.SaveTokens = true;
                });

                services.AddMicrosoftAccountAuthentication(options =>
                {
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = microsoftClientSecret;
                    options.SaveTokens = true;
                });
            }

            var facebookAppId = this.Configuration["Authentication:Facebook:AppId"];
            var facebookAppSecret = this.Configuration["Authentication:Facebook:AppSecret"];
            if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
            {
                services.AddFacebookAuthentication(options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.Scope.Add("email");
                    options.Fields.Add("name");
                    options.Fields.Add("email");
                    options.SaveTokens = true;
                });
            }

            var googleClientId = this.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = this.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                services.AddOAuthAuthentication("Google-AccessToken", options =>
                {
                    options.DisplayName = "Google-AccessToken";
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.CallbackPath = new PathString("/signin-google-token");
                    options.AuthorizationEndpoint = GoogleDefaults.AuthorizationEndpoint;
                    options.TokenEndpoint = GoogleDefaults.TokenEndpoint;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.SaveTokens = true;
                });

                services.AddGoogleAuthentication(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.SaveTokens = true;
                    options.Events = new OAuthEvents()
                    {
                        OnRemoteFailure = ctx =>
                        {
                            ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                            ctx.HandleResponse();
                            return Task.CompletedTask;
                        },
                    };
                    options.ClaimActions.MapJsonSubKey("urn:google:image", "image", "url");
                    options.ClaimActions.Remove(ClaimTypes.GivenName);
                });
            }

            var twitterConsumerKey = this.Configuration["Authentication:Facebook:ConsumerKey"];
            var twitterConsumerSecret = this.Configuration["Authentication:Facebook:ConsumerSecret"];
            if (!string.IsNullOrEmpty(twitterConsumerKey) && !string.IsNullOrEmpty(twitterConsumerSecret))
            {
                services.AddTwitterAuthentication(options =>
                {
                    options.ConsumerKey = twitterConsumerKey;
                    options.ConsumerSecret = twitterConsumerSecret;
                    options.RetrieveUserDetails = true;
                    options.SaveTokens = true;
                    options.ClaimActions.MapJsonKey("urn:twitter:profilepicture", "profile_image_url", ClaimTypes.Uri);
                    options.Events = new TwitterEvents()
                    {
                        OnRemoteFailure = ctx =>
                        {
                            ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                            ctx.HandleResponse();
                            return Task.CompletedTask;
                        },
                    };
                });
            }

            // Allow auth mocking when not in prod
            if (!this.Environment.IsProduction())
            {
                services.AddScheme<MockAuthenticationOptions, MockAuthenticationHandler>("Mock", _ => { });
            }
        }
    }
}