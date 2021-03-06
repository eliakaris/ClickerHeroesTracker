﻿// <copyright file="UploadControllerTests.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Tests.Controllers
{
    using ClickerHeroesTrackerWebsite.Controllers;
    using Microsoft.AspNetCore.Mvc;
    using Xunit;

    public class UploadControllerTests
    {
        [Fact]
        public void Index()
        {
            var controller = new UploadController();

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
            Assert.NotNull(result);
        }
    }
}
