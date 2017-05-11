// <copyright file="ErrorController.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Handles the error page.
    /// </summary>
    public class ErrorController : Controller
    {
        public ActionResult Index(string message)
        {
            this.ViewBag.ErrorMessage = message;
            return this.View("Error");
        }
    }
}