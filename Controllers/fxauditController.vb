Imports System.Net
Imports System.Web.Http
Imports System.Data.SqlClient

Namespace Controllers
    Public Class JSONauditResponse
        Public logid As String = ""
        Public streamId As String = ""
        Public zdroj As String = ""
        Public zdrojId As String = ""
        Public vlozeno As String = ""
        Public odeslanoRET As String = ""
        Public zjistenKurz As String = ""
        Public kurzOdeslan As String = ""
        Public zpracovaniMS As String = ""
        Public rozhodnutiKlienta As String = ""
        Public ukonceno As String = ""
    End Class

    Public Class fxauditController
        Inherits ApiController

        ' GET: api/fxaudit
        Public Function GetValues() As IEnumerable(Of String)
            Return New String() {"value1", "value2"}
        End Function

        ' GET: api/fxaudit/5
        Public Function GetValue(ByVal streamId As String) As JSONauditResponse
            Dim odpovedJSON As JSONauditResponse = New JSONauditResponse
            Dim cn As SqlConnection = New SqlConnection
            cn.ConnectionString = SQLConnectionString
            cn.Open()
            Dim strSQL As String = "select [ID],retID,zdrojID,zdroj,[vlozeno],[odeslanoRET],[zjistenKurz],[kurzOdeslan],datediff(MS,vlozeno,kurzOdeslan) as zpracovaniMS,[OdpovedKlienta],[Ukonceno] from RetDealFronta where retID='" & streamId & "' or zdrojID='" & streamId & "'"
            Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
            Dim dr As SqlClient.SqlDataReader = Command.ExecuteReader()
            System.Diagnostics.Debug.WriteLine(Command.CommandText)
            dr.Read()
            If dr.HasRows Then
                odpovedJSON.logid = dr("ID")
                If Not IsDBNull(dr("retID")) Then odpovedJSON.streamId = dr("retID")
                odpovedJSON.zdrojId = dr("zdrojID")
                odpovedJSON.zdroj = dr("zdroj")
                Dim df As String = "yyyy-MM-dd HH:mm:ss.fff"
                If Not IsDBNull(dr("vlozeno")) Then odpovedJSON.vlozeno = Format(dr("vlozeno"), df)
                If Not IsDBNull(dr("odeslanoRET")) Then odpovedJSON.odeslanoRET = Format(dr("odeslanoRET"), df)
                If Not IsDBNull(dr("zjistenKurz")) Then odpovedJSON.zjistenKurz = Format(dr("zjistenKurz"), df)
                If Not IsDBNull(dr("kurzOdeslan")) Then odpovedJSON.kurzOdeslan = Format(dr("kurzOdeslan"), df)
                If Not IsDBNull(dr("zpracovaniMS")) Then odpovedJSON.zpracovaniMS = dr("zpracovaniMS")
                If Not IsDBNull(dr("odpovedKlienta")) Then odpovedJSON.rozhodnutiKlienta = Format(dr("odpovedKlienta"), df)
                If Not IsDBNull(dr("ukonceno")) Then odpovedJSON.ukonceno = Format(dr("ukonceno"), df)
            Else
                        odpovedJSON.streamId = "NotFound"
            End If
            dr.Close()
            Return odpovedJSON
        End Function

        ' POST: api/fxaudit
        Public Sub PostValue(<FromBody()> ByVal value As String)

        End Sub

        ' PUT: api/fxaudit/5
        Public Sub PutValue(ByVal id As Integer, <FromBody()> ByVal value As String)

        End Sub

        ' DELETE: api/fxaudit/5
        Public Sub DeleteValue(ByVal id As Integer)

        End Sub
    End Class
End Namespace
