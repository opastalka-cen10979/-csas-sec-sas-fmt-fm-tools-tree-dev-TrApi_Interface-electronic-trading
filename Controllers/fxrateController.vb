Imports System.Net
Imports System.Web.Http
Imports System.Web.Script.Serialization
Imports System.Data.SqlClient
Imports System.Net.Http
Imports electronic_trading.Controllers.KontrolyCpty

Public Class JSONfxRateResponse
    Public streamId As String = ""
    Public result As String = ""
    Public errorCode As String = ""
    Public errorMessage As String = ""
    Public bidRate As Decimal = 0
    Public askRate As Decimal = 0
End Class


Public Class JSONreason
    Public sourceSystem As String = ""
    Public sourceDealID As String = ""
    Public streamId As String = ""
End Class


Namespace Controllers
    Public Class FXrateController
        Inherits ApiController

        ' GET: api/fxrate
        Public Function GetValues(ClientCLUID As String, ClientID As String, baseCurrency As String, quoteCurrency As String, orderValueDate As String) As JSONfxRateResponse
            Dim ErrText As String = ""
            Dim SourceSystem As String = ""
            Dim SourceID As String = ""
            Dim OrderAmount As String = "0"
            If Request.Headers.Contains("application-system") Then
                SourceSystem = Request.Headers.GetValues("application-system").First()
                System.Diagnostics.Debug.WriteLine("application-system: " & SourceSystem)
            Else
                ErrText = "Missing application-system header"
                System.Diagnostics.Debug.WriteLine("Missing application-system header")
            End If
            If Request.Headers.Contains("transaction-id") Then
                SourceID = Request.Headers.GetValues("transaction-id").First()
            Else
                ErrText = "Missing transaction-id header"
            End If

            Dim cn As SqlConnection = New SqlConnection
            Dim strSQL As String = ""
            Dim ICO As String = ""
            Dim CLUID As String = ""
            Dim vystup As String = ""
            Dim ValueDate As Date
            Dim odpovedJSON As New JSONfxRateResponse
            Dim jss As New JavaScriptSerializer()

            cn.ConnectionString = SQLConnectionString
            cn.Open()

            Dim kontrolaOdpoved As New CLUIDcheckResponse
            kontrolaOdpoved = KontrolaCpty(ClientCLUID, ClientID)
            If kontrolaOdpoved.ErrText > "" Then
                ErrText = kontrolaOdpoved.ErrText
            Else
                ICO = kontrolaOdpoved.ICO
                CLUID = kontrolaOdpoved.CLUID
            End If

            If Not SourceSystem > "" Then
                ErrText = "Missing sourceSystem"
            ElseIf Not SourceID > "" Then
                ErrText = "Missing sourceDealID"
            End If

            If Len(baseCurrency) <> 3 Then
                ErrText = "Missing baseCurrency"
            ElseIf Len(quoteCurrency) <> 3 Then
                ErrText = "Missing quoteCurrency"
            End If

            Try
                ValueDate = Date.Parse(orderValueDate)
            Catch
                ErrText = "ValueDate " & orderValueDate & " is not yyyy-mm-dd"
            End Try
            If Not ErrText > "" Then
                Try
                    Dim sObjem As String = Replace(OrderAmount, ",", ".")
                    Dim objem As Long = Long.Parse(sObjem)   'Odstranime desetiny, protoze ty potrebujeme na identifikaci dealu.
                    strSQL = "set dateformat YMD insert into RetRatesPluginFronta(zdroj,zdrojID,stavDealu,klientICO,klientCLUID,baseCCY,termCCY,objem,ValueDate) values ('" & SourceSystem & "','" & SourceID & "',0,'" & ICO & "','" & ClientCLUID & "','" & baseCurrency.ToUpper & "','" & quoteCurrency.ToUpper & "','" & objem & "','" & ValueDate.ToString("yyyy-MM-dd") & "')"
                    System.Diagnostics.Debug.WriteLine(strSQL)
                    Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
                    Command.ExecuteNonQuery()
                    Threading.Thread.Sleep(100)
                    strSQL = "select top 1 * from RetRatesPluginFronta where zdroj='" & SourceSystem & "' and zdrojID='" & SourceID & "'"
                    Dim ok As Boolean = 0
                    Command = New SqlCommand(strSQL, cn)
                    Dim dr As SqlClient.SqlDataReader
                    Dim pokusu As Integer = 0
                    Dim stavdealu As Integer = -1   'pokud projde s -1, tak je to chyba DB, nebo nejede RET.
                    Do While Not ok
                        Threading.Thread.Sleep(100)   'pockame chvilku, a pak budeme kontrolovat
                        dr = Command.ExecuteReader()
                        dr.Read()
                        System.Diagnostics.Debug.WriteLine("stavDealu=" & dr("stavDealu"))
                        If dr("stavDealu") = 20 Then
                            stavdealu = 20
                            odpovedJSON.result = "Y"
                            odpovedJSON.streamId = dr("ID")
                            odpovedJSON.bidRate = dr("KurzBid")
                            odpovedJSON.askRate = dr("KurzAsk")
                            stavdealu = dr("stavDealu")
                            Dim id As String = dr("ID")
                            ok = 1
                            dr.Close()

                            strSQL = "update RetRatesPluginFronta set stavDealu=30, kurzOdeslan=getdate() where stavDealu=20 and id='" & id & "'"
                            System.Diagnostics.Debug.WriteLine(strSQL)
                            Dim Command1 As SqlCommand = New SqlCommand(strSQL, cn)
                            Command1.ExecuteNonQuery()
                        ElseIf dr("stavDealu") = "110" Then
                            stavdealu = 110
                            odpovedJSON.result = "N"
                            odpovedJSON.errorCode = "ET-" & dr("ErrorCode")
                            ErrText = dr("ErrorText")
                            ok = 1
                        ElseIf dr("stavDealu") = 0 And pokusu > 20 Then       'odeslano do RET, zatim cekame na kurz, to nema cenu dele jak dve, tri vteriny cekam na zmenu stavu na 10 (odeslano do RET). Pak cekame na kurz.
                            ok = 1
                            ErrText = "BackEnd time out, RET neprevzal"
                        Else
                            pokusu += 1
                            If pokusu > 50 Then
                                ok = 1
                                ErrText = "BackEnd time out, RET prevzal ale nenacenil"
                            End If
                        End If
                        If Not dr.IsClosed() Then dr.Close()
                    Loop
                Catch ex As Exception
                    ErrText = ex.Message
                End Try
            End If
            If ErrText > "" Or odpovedJSON.errorMessage > "" Then
                odpovedJSON.streamId = "0"
                odpovedJSON.result = "N"
                If Not odpovedJSON.errorMessage > "" Then odpovedJSON.errorMessage = ErrText
                If Not odpovedJSON.errorCode > "" Then odpovedJSON.errorCode = "1"
            Else
                odpovedJSON.errorMessage = ""
                odpovedJSON.errorCode = "0"
            End If

            cn.Close()
            Return odpovedJSON

        End Function

        ' GET: api/fxrate/5
        Public Function GetValue(ByVal streamId As String) As String
            System.Diagnostics.Debug.WriteLine("GetValue event")
            Return "Parametry: ?SourceSystem=test&SourceID=666&ClientCLUID=A0&ClientID=A0&baseCurrency=EUR&quoteCurrency=CZK&orderValueDate=2022-02-22&OrderAmount=0"
        End Function

        ' POST: api/fxrate
        Public Function PostValue(<FromBody()> ByVal value As String) As String
            System.Diagnostics.Debug.WriteLine("PostValue event")
            Return "Post neumime, pouzijte GET"
        End Function

        ' PUT: api/fxrate/5
        'Public Sub PutValue(SourceSystem As String, SourceID As String, <FromBody()> ByVal value As String)
        Public Function PutValue(SourceSystem As String, SourceID As String) As JSONreason
            System.Diagnostics.Debug.WriteLine("PutValue event")
            Dim vysledek As New JSONreason
            Return vysledek

        End Function

        ' DELETE: api/fxrate/5
        Public Function DeleteValue(streamId As String) As JSONreason
            Dim vysledek As New JSONreason
            Return vysledek
        End Function
    End Class

End Namespace

