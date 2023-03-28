Imports System.Web.Http
Imports System.Text
Imports System.Text.Json

Public Class Global_asax
    Inherits HttpApplication
    Dim cesta As String

    Sub Application_Start(sender As Object, e As EventArgs)
        cesta = HttpRuntime.AppDomainAppPath & "log\"
        System.Diagnostics.Debug.WriteLine("Logfiles: " & cesta)
        GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear()
        GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        Dim config As ConnectionStringSettingsCollection = ConfigurationManager.ConnectionStrings
        ' Aktivuje se při spuštění aplikace.
        GlobalConfiguration.Configure(AddressOf WebApiConfig.Register)
        Dim settings As ConnectionStringSettingsCollection = ConfigurationManager.ConnectionStrings
        For Each cs In settings
            If cs.Name = "TrAPISQLserver" Then SQLConnectionString = cs.ConnectionString
        Next
        If SQLConnectionString = "" Then
            System.Diagnostics.Debug.WriteLine("V souboru Web.config chybí SQL connectionstring TrAPISQLserver")
        Else
            System.Diagnostics.Debug.WriteLine("ConnectionString: " & SQLConnectionString)
        End If
    End Sub

    Sub Application_BeginRequest(sender As Object, e As EventArgs)
        If Not cesta > "" Then
            cesta = HttpRuntime.AppDomainAppPath & "log\"
            If Not My.Computer.FileSystem.DirectoryExists(cesta) Then My.Computer.FileSystem.CreateDirectory(cesta)
        End If
        Dim logfile As String = cesta & DateTime.Now.Ticks.ToString() & ".txt"
        Request.SaveAs(logfile, True)
    End Sub
End Class