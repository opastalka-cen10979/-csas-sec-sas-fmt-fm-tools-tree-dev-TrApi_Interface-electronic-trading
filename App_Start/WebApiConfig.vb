Imports System
Imports System.Collections.Generic
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Web.Http

Public Module WebApiConfig
    Public SQLConnectionString As String = ""
    Public Sub Register(ByVal config As HttpConfiguration)
        ' Služby a konfigurace rozhraní Web API

        ' Trasy rozhraní Web API
        config.MapHttpAttributeRoutes()
        config.Routes.MapHttpRoute(
            name:="DefaultApi",
            routeTemplate:="api/{controller}/{StreamId}",
            defaults:=New With {.StreamId = RouteParameter.Optional}
        )
        config.Formatters.Remove(config.Formatters.XmlFormatter)
        config.Formatters.JsonFormatter.UseDataContractJsonSerializer = True
        config.Formatters.JsonFormatter.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat
        config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        config.Formatters.JsonFormatter.SerializerSettings.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore
        config.Formatters.JsonFormatter.SerializerSettings.StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.EscapeNonAscii
    End Sub

End Module
