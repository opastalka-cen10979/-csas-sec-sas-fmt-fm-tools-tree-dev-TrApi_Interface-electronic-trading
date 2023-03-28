Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports System.Web.Http
Imports System.Web.Script.Serialization
Imports electronic_trading.Controllers.KontrolyCpty


Namespace Controllers
    Public Class RequestOrders
        Public orderNumber As String = ""
        Public takerBuysBase As String = ""
        Public orderAmount As String = ""
        Public orderValueDate As String = ""
    End Class

    Public Class JSONdealRequest
        Public autoAccept As String = ""
        Public clientCluid As String = ""
        Public clientID As String = ""
        Public cptyCluid As String = ""
        Public orderType As String = ""
        Public orderSubType As String = ""
        Public baseCurrency As String = ""
        Public quoteCurrency As String = ""
        Public dealtCurrencyBase As String = ""
        Public dealOrders As Integer = 0
        Public orderList As List(Of RequestOrders) = New List(Of RequestOrders)
    End Class

    Public Class ResponseOrders
        Public orderNumber As String
        Public quoteRate As Decimal
        Public quoteAmount As Decimal
    End Class
    Public Class JSONfxDealResponse
        Public streamId As String
        Public result As String = ""
        Public errorCode As String = ""
        Public errorMessage As String
        Public timeout As String = ""
        Public orderList As List(Of ResponseOrders) = New List(Of ResponseOrders)
    End Class


    Public Class FXdealController
        Inherits ApiController

        ' GET: api/fxdeal
        Public Function GetValues() As String
            Return "GET neni implementovano, pouzijde POST, PUT, nebo DELETE"
        End Function

        ' GET: api/fxdeal/5
        Public Function GetValue(streamId As String) As String
            Return "GET neni implementovano, pouzijde POST, PUT, nebo DELETE"
        End Function

        ' POST: api/fxdeal
        Public Function PostValue(<FromBody()> ByVal jsonRequest As JSONdealRequest) As JSONfxDealResponse
            Dim odpovedJSON As New JSONfxDealResponse
            Dim jss As New JavaScriptSerializer()
            Dim ErrText As String = ""
            Dim SourceSystem As String = ""
            Dim SourceDealID As String = ""
            Dim cn As SqlConnection = New SqlConnection
            Dim strSQL As String = ""
            Dim ICO As String = ""
            Dim CLUID As String = ""
            Dim DealCCY As String = ""
            Dim ValueDate As Date
            Dim Amount As Double = 0

            cn.ConnectionString = SQLConnectionString
            cn.Open()

            If Request.Headers.Contains("application-system") Then
                SourceSystem = Request.Headers.GetValues("application-system").First()
                System.Diagnostics.Debug.WriteLine("application-system: " & SourceSystem)
            Else
                ErrText = "Missing application-system header"
                System.Diagnostics.Debug.WriteLine("Missing application-system header")
            End If
            If Request.Headers.Contains("transaction-id") Then
                SourceDealID = Request.Headers.GetValues("transaction-id").First()
                System.Diagnostics.Debug.WriteLine("transaction-id: " & SourceDealID)
            Else
                ErrText = "Missing transaction-id header"
                System.Diagnostics.Debug.WriteLine("Missing transaction-id header")
            End If
            Dim kontrolaOdpoved As New CLUIDcheckResponse

            Try
                ICO = jsonRequest.clientID
                CLUID = jsonRequest.clientCluid
            Catch
                ErrText = "Chybna vstupni data"
                System.Diagnostics.Debug.WriteLine("Chybna vstupni data")
            End Try

            If ErrText = "" Then
                kontrolaOdpoved = KontrolaCpty(CLUID, ICO)
                If kontrolaOdpoved.ErrText > "" Then
                    ErrText = kontrolaOdpoved.ErrText
                Else
                    ICO = kontrolaOdpoved.ICO
                    CLUID = kontrolaOdpoved.CLUID
                End If

                If Len(jsonRequest.baseCurrency) <> 3 Then
                    ErrText = "Missing baseCurrency"
                ElseIf Len(jsonRequest.quoteCurrency) <> 3 Then
                    ErrText = "Missing quoteCurrency"
                ElseIf Not jsonRequest.dealOrders > 0 Then
                    ErrText = "Missing dealOrders counter"
                End If

                If jsonRequest.dealtCurrencyBase = "1" Then
                    DealCCY = jsonRequest.baseCurrency
                ElseIf jsonRequest.dealtCurrencyBase = "0" Then
                    DealCCY = jsonRequest.quoteCurrency
                Else
                    ErrText = "dealtCurrencyBase (" & jsonRequest.dealtCurrencyBase & ") must be 0 or 1"
                End If
            End If
            strSQL = "set dateformat YMD;"
            Dim pocet As Integer = 0
            Dim stavdealu As Integer = 0
            If ErrText = "" Then
                For Each order As RequestOrders In jsonRequest.orderList
                    pocet = pocet + 1
                    System.Diagnostics.Debug.WriteLine("Amount= " & order.orderAmount)
                    Try
                        ValueDate = Date.Parse(order.orderValueDate)
                    Catch
                        ErrText = "ValueDate " & order.orderValueDate & " is not yyyy-mm-dd"
                    End Try
                    Try
                        Amount = CDbl(Replace(order.orderAmount, ".", Mid(3 / 2, 2, 1)))
                    Catch
                        ErrText = "Amount is not number"
                    End Try
                    If Not order.orderNumber > 0 Then
                        ErrText = "Wrong orderNumber " & order.orderNumber & ", must be integer"
                    End If
                    If order.takerBuysBase <> "1" And order.takerBuysBase <> "0" Then
                        ErrText = "Wrong TakerBuysBase " & order.takerBuysBase & ", must be 1 or 0"
                    End If
                    If ErrText > "" Then
                        strSQL = ""
                        Exit For
                    Else
                        strSQL = strSQL & "insert into RetDealFronta(zdroj,zdrojID,ordersCelkem,orderNumber,stavDealu,klientICO,klientCLUID,baseCCY,termCCY,DealCCY,TakerBuysBase,objem,ValueDate) values ('" & SourceSystem & "','" & SourceDealID & "'," & jsonRequest.dealOrders & "," & order.orderNumber & ",0,'" & ICO & "','" & CLUID & "','" & jsonRequest.baseCurrency.ToUpper & "','" & jsonRequest.quoteCurrency.ToUpper & "','" & DealCCY.ToUpper & "','" & order.takerBuysBase.ToUpper & "','" & Replace(Amount, ",", ".") & "','" & ValueDate.ToString("yyyy-MM-dd") & "');" & Chr(13) & Chr(10)
                    End If
                Next
                If strSQL = "" Then
                    ErrText = "Chyba pri tvorbe zaznamu."
                Else
                    System.Diagnostics.Debug.WriteLine("Zalozeni dealu: " & strSQL)
                End If
                If pocet <> jsonRequest.dealOrders Then
                    ErrText = "Nesouhlasí dealOrders (" & jsonRequest.dealOrders & ") a počet záznamů v orderList (" & pocet & ")"
                End If
            End If
            If ErrText = "" And strSQL > "" Then
                Dim VlozitCmd As SqlCommand = New SqlCommand(strSQL, cn)     'Vlozime vsechny dealy najednou do databaze, nechceme vice transakci, aby nedoslo jen k castecnemu zpracovani
                Try
                    VlozitCmd.ExecuteNonQuery()
                    VlozitCmd.Dispose()
                Catch e As Exception
                    ErrText = e.Message
                End Try
            End If

            If Not ErrText > "" Then
                strSQL = "Select sum(CASE WHEN stavDealu=0 THEN 1 ELSE 0 END) As Nic, sum(Case When stavDealu=10 THEN 1 ELSE 0 END) As odeslano, sum(Case When stavDealu=20 THEN 1 ELSE 0 END) As zpracovano, sum(Case When stavDealu>=100 THEN 1 ELSE 0 END) As chyby From RetDealFronta where zdroj='" & SourceSystem & "' and zdrojID='" & SourceDealID & "'"
                Dim strSQLok As String = "select RetID,orderNumber,retKurz,protihodnota from RetDealFronta where zdroj='" & SourceSystem & "' and zdrojID='" & SourceDealID & "' and stavDealu=20 order by id"
                Dim strSQLerror As String = "select top 1 ErrorCode,ErrorText from RetDealFronta where zdroj='" & SourceSystem & "' and zdrojID='" & SourceDealID & "' and stavdealu>=100 and ErrorCode>'' order by id"  'Nacteme prvni dostupnou chybu.
                Threading.Thread.Sleep(50)
                Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
                System.Diagnostics.Debug.WriteLine("strSQL= " & strSQL)
                Try
                    Dim ok As Boolean = 0
                    Dim dr As SqlClient.SqlDataReader
                    Dim pokusu As Integer = 0
                    stavdealu = -1   'pokud projde s -1, tak je to chyba DB, nebo nejede RET.
                    Do While Not ok
                        Threading.Thread.Sleep(50)   'pockame chvilku, a pak budeme kontrolovat
                        dr = Command.ExecuteReader()
                        dr.Read()
                        System.Diagnostics.Debug.WriteLine("Nic = " & dr("Nic") & ", Chyby = " & dr("chyby") & ", zpracovano = " & dr("zpracovano"))
                        If dr("Nic") > 0 And pokusu > 20 Then       'odeslano do RET, zatim cekame na kurz, to nema cenu dele jak vterinu na zmenu stavu na 10 (odeslano do RET). Pak cekame jeste chvilku na kurz.
                            ok = 1
                        ElseIf dr("chyby") > 0 Then
                            stavdealu = 110
                            ok = 1
                        ElseIf dr("zpracovano") = pocet Then
                            stavdealu = 20
                            ok = 1
                        Else
                            pokusu += 1
                            If pokusu > 50 Then ok = 1
                        End If
                        If Not dr.IsClosed() Then dr.Close()
                    Loop
                Catch ex As Exception
                    ErrText = ex.Message
                End Try
                System.Diagnostics.Debug.WriteLine("stavdealu = " & stavdealu & ", ErrText = " & ErrText)

                Dim streamId As String = ""
                Dim zaznam As New ResponseOrders With {
                .quoteAmount = 0
            }
                odpovedJSON.orderList = New List(Of ResponseOrders)
                If stavdealu = 20 Then
                    Command = New SqlCommand(strSQLok, cn)
                    System.Diagnostics.Debug.WriteLine("strSQL= " & strSQLok)
                    Dim DBDT = New DataTable
                    DBDT.Load(Command.ExecuteReader)
                    Command.Dispose()
                    For Each row In DBDT.Rows
                        If odpovedJSON.streamId = "" Then odpovedJSON.streamId = row("RetID")
                        zaznam.orderNumber = row("orderNumber")
                        zaznam.quoteAmount = row("protihodnota")
                        zaznam.quoteRate = row("retKurz")
                        odpovedJSON.orderList.Add(zaznam)
                    Next
                    odpovedJSON.result = "Y"
                ElseIf stavdealu = 110 Then
                    Command = New SqlCommand(strSQLok, cn)
                    Dim DBDT = New DataTable
                    DBDT.Load(Command.ExecuteReader)
                    Command.Dispose()
                    For Each row In DBDT.Rows
                        zaznam.orderNumber = row("orderNumber")
                        zaznam.quoteRate = row("retKurz")
                        odpovedJSON.orderList.Insert(row("orderNumber"), zaznam)
                        If odpovedJSON.streamId = "" Then odpovedJSON.streamId = row("RetID")
                    Next
                    Command = New SqlCommand(strSQLerror, cn)
                    System.Diagnostics.Debug.WriteLine("strSQL= " & strSQLerror)
                    Dim dr As SqlClient.SqlDataReader
                    dr = Command.ExecuteReader()
                    dr.Read()
                    odpovedJSON.result = "N"
                    odpovedJSON.errorCode = "ET-" & dr("ErrorCode")
                    odpovedJSON.errorMessage = dr("ErrorText")
                    dr.Close()
                    Command.Dispose()
                End If

                If stavdealu = -1 And ErrText = "" Then
                    ErrText = "BackEnd time out"
                End If
            End If


            If ErrText > "" Then
                odpovedJSON.result = "N"
                If odpovedJSON.errorCode = "" Then odpovedJSON.errorCode = "1"
                odpovedJSON.errorMessage = ErrText
                strSQL = "update RetDealFronta set stavDealu=100, Ukonceno=getdate() where stavDealu=0 and zdroj='" & SourceSystem & "' and zdrojID='" & SourceDealID & "'"
                System.Diagnostics.Debug.WriteLine(strSQL)
                Dim Command1 As SqlCommand = New SqlCommand(strSQL, cn)
                Command1.ExecuteNonQuery()
            Else
                If odpovedJSON.errorCode = "" Then odpovedJSON.errorCode = "0"
                strSQL = "update RetDealFronta set stavDealu=30, kurzOdeslan=getdate() where stavDealu=20 and zdroj='" & SourceSystem & "' and zdrojID='" & SourceDealID & "'"
                Dim UpdateCmd As SqlCommand = New SqlCommand(strSQL, cn)
                UpdateCmd.ExecuteNonQuery()
                UpdateCmd.Dispose()
            End If

            Return odpovedJSON
        End Function

        ' PUT: api/fxdeal/5
        Public Function PutValue(ByVal streamId As String) As Http.HttpResponseMessage
            Dim SourceSystem As String = ""
            Dim SourceDealID As String = ""
            Dim ErrText As String = ""
            Dim cn As SqlConnection = New SqlConnection
            Dim strSQL As String = ""
            Dim response As New Http.HttpResponseMessage
            cn.ConnectionString = SQLConnectionString
            cn.Open()

            If Request.Headers.Contains("application-system") Then
                SourceSystem = Request.Headers.GetValues("application-system").First()
            Else
                ErrText = "Missing application-system header"
            End If
            If Request.Headers.Contains("transaction-id") Then
                SourceDealID = Request.Headers.GetValues("transaction-id").First()
            Else
                ErrText = "Missing transaction-id header"
            End If
            If Not streamId > "" Then
                ErrText = "Missing streamId"
            End If
            If ErrText > "" Then
                response.StatusCode = HttpStatusCode.BadRequest
                response.ReasonPhrase = ErrText
                System.Diagnostics.Debug.WriteLine(ErrText)
                Return response
            Else

                strSQL = "select count(*) as pocet from RetDealFronta where stavDealu=30 and retID='" & streamId & "' and zdroj='" & SourceSystem & "'"
                Dim CheckCommand As SqlCommand = New SqlCommand(strSQL, cn)
                Dim CheckDr As SqlClient.SqlDataReader
                CheckDr = CheckCommand.ExecuteReader()
                CheckDr.Read()
                If CheckDr("pocet") > 0 Then
                    CheckDr.Close()
                    strSQL = "set dateformat YMD update RetDealFronta set stavDealu=40, OdpovedKlienta=getdate() where stavDealu=30 and retID='" & streamId & "' and zdroj='" & SourceSystem & "'"
                    System.Diagnostics.Debug.WriteLine(strSQL)
                    Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
                    Command.ExecuteNonQuery()
                    strSQL = "Select stavdealu,ErrorCode, ErrorText From RetDealFronta where orderNumber=1 and zdroj='" & SourceSystem & "' and retID='" & streamId & "'"
                    Dim ok As Boolean = 0
                    Command = New SqlCommand(strSQL, cn)
                    Dim dr As SqlClient.SqlDataReader
                    Threading.Thread.Sleep(50)
                    Dim pokusu As Integer = 0
                    Dim stavdealu As Integer = -1   'pokud projde s -1, tak je to chyba DB, nebo nejede RET.
                    Do While Not ok
                        Threading.Thread.Sleep(50)   'pockame chvilku a pak budeme kontrolovat
                        dr = Command.ExecuteReader()
                        dr.Read()
                        If dr("stavdealu") = 40 And pokusu > 20 Then       'odeslano do RET, zatim cekame na kurz, to nema cenu dele jak vterinu na zmenu stavu na 10 (odeslano do RET). Pak cekame jeste chvilku na kurz.
                            stavdealu = 40
                            ok = 1
                            response.Headers.Add("ErrorNumber", "408")
                            response.Headers.Add("ErrorText", "BackEnd pozadavek vubec neprevzal, asi nejede")
                            response.StatusCode = HttpStatusCode.RequestTimeout
                            response.ReasonPhrase = "BackEnd error, no response"
                        ElseIf dr("stavdealu") = 110 Then
                            response.StatusCode = HttpStatusCode.InternalServerError
                            response.ReasonPhrase = dr("ErrorText")
                            response.Headers.Add("ErrorNumber", dr("ErrorCode"))
                            response.Headers.Add("ErrorText", dr("ErrorText"))
                            stavdealu = 110
                            ok = 1
                        ElseIf dr("stavdealu") = 70 Then
                            response.StatusCode = HttpStatusCode.OK
                            response.Headers.Add("ErrorNumber", "0")
                            response.Headers.Add("ErrorText", "")
                            response.ReasonPhrase = "Deal potvrzen"
                            dr.Close()
                            strSQL = "update RetDealFronta set stavDealu=80, Ukonceno=getdate() where  zdroj='" & SourceSystem & "' and retID='" & streamId & "' and stavDealu=70"
                            System.Diagnostics.Debug.WriteLine(strSQL)
                            Dim Command1 As SqlCommand = New SqlCommand(strSQL, cn)
                            Command1.ExecuteNonQuery()
                            stavdealu = 80
                            ok = 1
                        Else
                            pokusu += 1
                            If pokusu > 50 Then ok = 1
                        End If
                        If Not dr.IsClosed() Then dr.Close()
                    Loop
                    If stavdealu = -1 Then
                        response.StatusCode = HttpStatusCode.RequestTimeout
                        response.Headers.Add("ErrorNumber", "408")
                        response.Headers.Add("ErrorText", "BackEnd pozadavek prevzal, ale vcas nereaval")
                        response.ReasonPhrase = "BackEnd time out"
                    End If
                Else
                    response.StatusCode = HttpStatusCode.NotFound
                    response.ReasonPhrase = "Deal not found"
                End If

            End If
            Return response
        End Function

        ' DELETE: api/fxdeal/5
        Public Function DeleteValue(ByVal streamId As String) As Http.HttpResponseMessage
            Dim SourceSystem As String = ""
            Dim SourceDealID As String = ""
            Dim ErrText As String = ""
            Dim cn As SqlConnection = New SqlConnection
            Dim strSQL As String = ""
            Dim response As New Http.HttpResponseMessage
            cn.ConnectionString = SQLConnectionString
            cn.Open()

            If Request.Headers.Contains("application-system") Then
                SourceSystem = Request.Headers.GetValues("application-system").First()
            Else
                ErrText = "Missing application-system header"
            End If
            If Request.Headers.Contains("transaction-id") Then
                SourceDealID = Request.Headers.GetValues("transaction-id").First()
            Else
                ErrText = "Missing transaction-id header"
            End If
            If Not streamId > "" Then
                ErrText = "Missing streamId"
            End If
            If ErrText > "" Then
                response.StatusCode = HttpStatusCode.BadRequest
                response.ReasonPhrase = ErrText
                System.Diagnostics.Debug.WriteLine(ErrText)
            Else
                System.Diagnostics.Debug.WriteLine("DELETE: application-system " & SourceSystem & ", transaction-id " & SourceDealID & ", streamId " & streamId)
                strSQL = "select count(*) as pocet from RetDealFronta where stavDealu= 30 And retID ='" & streamId & "' and zdroj='" & SourceSystem & "'"
                Dim CheckCommand As SqlCommand = New SqlCommand(strSQL, cn)
                Dim CheckDr As SqlClient.SqlDataReader
                CheckDr = CheckCommand.ExecuteReader()
                CheckDr.Read()

                System.Diagnostics.Debug.WriteLine("Akce: smazat deal " & streamId)

                If CheckDr("pocet") > 0 Then
                    CheckDr.Close()
                    strSQL = "set dateformat YMD update RetDealFronta set stavDealu=50, OdpovedKlienta=getdate(), Ukonceno=getdate() where stavDealu=30 and zdroj='" & SourceSystem & "' and retID='" & streamId & "'"
                    System.Diagnostics.Debug.WriteLine(strSQL)
                    Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
                    Command.ExecuteNonQuery()
                    response.StatusCode = HttpStatusCode.OK
                    response.ReasonPhrase = "Deal deleted"
                    System.Diagnostics.Debug.WriteLine("OK")
                Else
                    CheckDr.Close()
                    response.StatusCode = HttpStatusCode.NotFound
                    response.ReasonPhrase = "Deal not found"
                    response.Headers.Add("ErrorNumber", "404")
                    response.Headers.Add("ErrorText", "Deal nenalezen.")

                    System.Diagnostics.Debug.WriteLine("not ok, nenalezen - " & strSQL)
                End If
            End If
            Return response
        End Function

    End Class
End Namespace