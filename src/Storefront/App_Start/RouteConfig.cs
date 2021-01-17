// -----------------------------------------------------------------------
// <copyright file="RouteConfig.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Storefront
{
    using System.Web.Mvc;
    using System.Web.Routing;

    /// <summary>
    /// Holds routing configuration.
    /// </summary>
    public static class RouteConfig
    {
        /// <summary>
        /// Registers routes.
        /// </summary>
        /// <param name="routes">A collection of routes.</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            //routes.MapRoute(
            //    name: "Default",
            //    url: "{controller}/{action}/{id}",
            //    defaults: new { controller = "AdminConsole", action = "Index", id = UrlParameter.Optional });
            routes.MapRoute(
               name: "Default",
               url: "{controller}/{action}/{id}",
               defaults: new { controller = "AdminUser", action = "Index", id = UrlParameter.Optional });
            routes.MapRoute(
              name: "Customer",
              url: "{controller}/{action}/{id}",
              defaults: new { controller = "Customer", action = "Index", id = UrlParameter.Optional });
            routes.MapRoute(
              name: "Subscriptions",
              url: "{controller}/{action}/{id}",
              defaults: new { controller = "Subscriptions", action = "Index", id = UrlParameter.Optional });
        }
    }
}
