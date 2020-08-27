using DotNetNuke.Web.Api;

namespace Connect.Modules.UserManagement.AccountManagement.Services
{
    public class RouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute("ConnectAccounts", "default", "{controller}/{action}", new string[] { "Connect.Modules.UserManagement.AccountManagement.Services" });
        }
    }
}