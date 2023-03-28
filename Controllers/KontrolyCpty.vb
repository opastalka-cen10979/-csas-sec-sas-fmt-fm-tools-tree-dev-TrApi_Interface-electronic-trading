Imports System.Data.SqlClient
Namespace Controllers
    Friend Class KontrolyCpty

        Public Class CLUIDcheckResponse
            Public ICO As String = ""
            Public CLUID As String = ""
            Public ErrText As String = ""
        End Class

        Public Shared Function KontrolaCpty(ClientCLUID As String, ClientID As String) As CLUIDcheckResponse
            Dim odpoved As New CLUIDcheckResponse
            Dim strSQL As String = ""
            Dim cn As SqlConnection = New SqlConnection
            cn.ConnectionString = SQLConnectionString
            cn.Open()
            If ClientCLUID > "" Then    'CLUID ma prednost, dohledame v tabulce. Pokud CLUID neni, overime ICO, pokud neni nic, mozna se jen potvrzuje deal, takze zatim neresime. Pozdeji se podivame, zda mame platne ICO
                strSQL = "select * from RETclients where CLUID='" & ClientCLUID & "'"
                Dim Command As SqlCommand = New SqlCommand(strSQL, cn)

                Dim dr As SqlClient.SqlDataReader = Command.ExecuteReader()
                System.Diagnostics.Debug.WriteLine(Command.CommandText)

                Try
                    dr.Read()
                    odpoved.ICO = dr("ICO")
                    odpoved.CLUID = dr("CLUID")
                Catch
                    System.Diagnostics.Debug.WriteLine(Err.Description)
                    odpoved.ErrText = "Unknown CLUID " & ClientCLUID
                End Try
                dr.Close()
            ElseIf ClientID > "" Then
                strSQL = "select * from RETclients where ICO='" & ClientID & "'"
                Dim Command As SqlCommand = New SqlCommand(strSQL, cn)
                Dim dr As SqlClient.SqlDataReader = Command.ExecuteReader()
                System.Diagnostics.Debug.WriteLine(strSQL)
                Try
                    dr.Read()
                    odpoved.ICO = dr("ICO")
                    odpoved.CLUID = dr("CLUID")
                Catch
                    System.Diagnostics.Debug.WriteLine(Err.Description)
                    odpoved.ErrText = "Unknown ICO" & ClientID
                End Try
                dr.Close()
            Else
                odpoved.ErrText = "Missing CLUID or ICO"
            End If

            Return odpoved
        End Function

    End Class
End Namespace
