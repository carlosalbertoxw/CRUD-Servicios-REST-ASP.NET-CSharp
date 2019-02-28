using System;
using System.Reflection;

namespace CRUD_Servicios_REST_ASP.NET_CSharp.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}