Imports System.Security.Cryptography
Imports vb = Microsoft.VisualBasic
Imports System.Runtime.InteropServices

Public Module Globals
    Enum ID_Style
        EasyBase30_12 = 0
        EasyBase30_6 = 1
        NumericLong12 = 2
        NumericLong12Timed = 5
        GUID = 3
        EasyBase30_12_Reversed = 4
    End Enum

    Public Function CreateID(Optional IDType As ID_Style = ID_Style.EasyBase30_12) As String

        Select Case IDType
            Case ID_Style.NumericLong12

                Return RandDigits(12)

            Case ID_Style.NumericLong12Timed

                ' Granualar to half hours
                Dim Mins As Integer = DateDiff(DateInterval.Minute, CDate("2018-01-01"), Now.ToUniversalTime)
                Return Format(CInt(Mins / 30), "000000") + RandDigits(6)

            Case ID_Style.GUID

                Return New Guid().ToString

            Case ID_Style.EasyBase30_6

                Return RandLetts(6)

            Case ID_Style.EasyBase30_12_Reversed

                Dim Secs As Integer
                Secs = Math.Abs(DateDiff(DateInterval.Second, CDate("2080-01-01"), Now.ToUniversalTime))
                Return SplitIntoSixes(ConvertBase30(Secs / 5) + RandLetts(6))

            Case Else
                ' new ID first section ABC-123 is incremented every 5 seconds
                ' second section DEF-456 is random out of 1 in 729,000,000

                ' this gives us about 70 years worth

                Dim Secs As Integer
                Secs = DateDiff(DateInterval.Second, CDate("2010-01-01"), Now.ToUniversalTime)
                Return SplitIntoSixes(ConvertBase30(Secs / 5) + RandLetts(6))

        End Select


    End Function

    Public Function RandLetts(ByVal Chars As Integer) As String

        ' letters that can easily be distinguished visually and audibally
        ' need to build routine to convert S to F for example and block others
        Dim V As String = "23456789ABCDEFGHJKLMNPRTUVWXYZ"
        Dim O As String = ""
        For I As Integer = 1 To Chars
            O = O + Mid(V, RollDice(Len(V)), 1)
        Next

        Return O
    End Function

    Public Function RandDigits(ByVal Chars As Integer) As String


        Dim V As String = "0123456789"
        Dim O As String = ""
        For I As Integer = 1 To Chars
            O = O + Mid(V, RollDice(Len(V)), 1)
        Next

        Return O
    End Function


    Public Function RollDice(ByVal NumSides As Integer) As Integer
        ' Create a byte array to hold the random value.
        Dim randomNumber(0) As Byte

        ' Create a new instance of the RNGCryptoServiceProvider. 
        Dim Gen As New RNGCryptoServiceProvider()

        ' Fill the array with a random value.
        Gen.GetBytes(randomNumber)

        ' Convert the byte to an integer value to make the modulus operation easier.
        Dim rand As Integer = Convert.ToInt32(randomNumber(0))

        ' Return the random number mod the number
        ' of sides.  The possible values are zero-
        ' based, so we add one.
        Return rand Mod NumSides + 1

    End Function 'RollDice

    Public Function SplitIntoSixes(ByVal CharsIn As String) As String

        Dim i As Integer
        Dim O As String = ""
        For i = 1 To Len(CharsIn)
            If (i - 1) Mod 6 = 0 And i <> 1 Then O = O + "-"
            O = O + Mid(CharsIn, i, 1)
        Next i
        Return O

    End Function

    Public Function ConvertBase30(ByVal d As Integer) As String

        Dim sNewBaseDigits As String
        sNewBaseDigits = "23456789ABCDEFGHJKLMNPRTUVWXYZ"

        Dim S As String = ""
        Dim tmp As Double, i As Integer, lastI As Integer
        Dim BaseSize As Integer
        BaseSize = Len(sNewBaseDigits)
        Do While Val(d) <> 0
            tmp = d
            i = 0
            Do While tmp >= BaseSize
                i = i + 1
                tmp = tmp / BaseSize
            Loop
            If i <> lastI - 1 And lastI <> 0 Then S = S & vb.StrDup(lastI - i - 1, vb.Left(sNewBaseDigits, 1)) 'get the zero digits inside the number
            tmp = Int(tmp) 'truncate decimals
            S = S + Mid(sNewBaseDigits, tmp + 1, 1)
            d = d - tmp * (BaseSize ^ i)
            lastI = i
        Loop
        S = S & vb.StrDup(i, vb.Left(sNewBaseDigits, 1)) 'get the zero digits at the end of the number
        ConvertBase30 = S

    End Function


    Private Declare Unicode Function NetRemoteTOD Lib "netapi32" (
  <MarshalAs(UnmanagedType.LPWStr)> ByVal ServerName As String,
  ByRef BufferPtr As IntPtr) As Integer
    Private Declare Function NetApiBufferFree Lib _
      "netapi32" (ByVal Buffer As IntPtr) As Integer

    Structure TIME_OF_DAY_INFO
        Dim tod_elapsedt As Integer
        Dim tod_msecs As Integer
        Dim tod_hours As Integer
        Dim tod_mins As Integer
        Dim tod_secs As Integer
        Dim tod_hunds As Integer
        Dim tod_timezone As Integer
        Dim tod_tinterval As Integer
        Dim tod_day As Integer
        Dim tod_month As Integer
        Dim tod_year As Integer
        Dim tod_weekday As Integer
    End Structure

    Function GetServerTime() As Date

        ' taken from http://www.codeproject.com/KB/vb/NetRemoteTOD.aspx
        ' removed daylight saving part as we want UTC

        ' see http://support.microsoft.com/kb/249716 for full usage info

        ' reverts to local PC time if can't connect

        'Try
        '    Dim iRet As Integer
        '    Dim ptodi As IntPtr
        '    Dim todi As TIME_OF_DAY_INFO
        '    Dim dDate As Date
        '    Dim strServerName As String = "db.xenex.local" & vbNullChar
        '    iRet = NetRemoteTOD(strServerName, ptodi)
        '    If iRet = 0 Then
        '        todi = CType(Marshal.PtrToStructure(ptodi, GetType(TIME_OF_DAY_INFO)),
        '          TIME_OF_DAY_INFO)
        '        NetApiBufferFree(ptodi)
        '        dDate = DateSerial(todi.tod_year, todi.tod_month, todi.tod_day) + " " +
        '        TimeSerial(todi.tod_hours, todi.tod_mins, todi.tod_secs)
        '        GetServerTime = dDate
        '    Else
        '        GetServerTime = Now.ToUniversalTime
        '    End If
        'Catch
        GetServerTime = Now.ToUniversalTime
        'End Try

    End Function

    Function SplitString(InputString As String, SplitSize As Integer, Section As Integer) As String

        Dim StartPos As Integer = (Section * SplitSize) + 1
        Dim Length As Integer = SplitSize

        If Len(InputString) >= StartPos Then
            Return Mid(InputString, StartPos, Length)
        Else
            Return ""
        End If

    End Function

    Function RemoveHTML(HtmlString As String) As String

        Return Net.WebUtility.HtmlDecode(Text.RegularExpressions.Regex.Replace(HtmlString, "<(.|\n)*?>", ""))

    End Function
End Module
