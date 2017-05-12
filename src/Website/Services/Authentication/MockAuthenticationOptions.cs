// <copyright file="MockAuthenticationOptions.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Services.Authentication
{
    using Microsoft.AspNetCore.Authentication;

    internal sealed class MockAuthenticationOptions : AuthenticationSchemeOptions
    {
        public MockAuthenticationOptions()
        {
            this.ClaimsIssuer = "Mock";
        }
    }
}
