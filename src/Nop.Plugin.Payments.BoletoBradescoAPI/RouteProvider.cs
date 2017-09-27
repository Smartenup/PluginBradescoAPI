using Nop.Web.Framework.Mvc.Routes;
using System.Web.Mvc;
using System.Web.Routing;

namespace Nop.Plugin.Payments.BoletoBradescoAPI
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Payments.BoletoBradescoAPI.Configure",
                "Plugins/PaymentBoletoBradescoAPI/Configure",
                new { controller = "PaymentBoletoBradescoAPI", action = "Configure" },
                new[] { "Nop.Plugin.Payments.BoletoBradescoAPI.Controllers" }
           );

            routes.MapRoute("Plugin.Payments.BoletoBradescoAPI.CheckOrder",
                 "Plugins/PaymentBoletoBradescoAPI/CheckOrder",
                 new { controller = "PaymentBoletoBradescoAPI", action = "CheckOrder" },
                 new[] { "Nop.Plugin.Payments.BoletoBradescoAPI.Controllers" }
            );
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
