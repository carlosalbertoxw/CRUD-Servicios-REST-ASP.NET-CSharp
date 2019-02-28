using System.Web;
using System.Web.Mvc;

namespace CRUD_Servicios_REST_ASP.NET_CSharp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
