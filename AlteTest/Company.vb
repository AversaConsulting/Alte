
Imports Alte


Public Class Company
    Inherits AlteObject
    ' inherit this to get basics
    Sub New()

    End Sub

    Sub New(AlteSession As AlteSession)
        MyBase.New(AlteSession)
    End Sub


    Property Title As String
    Property FirstName As String
    Property LastName As String

    'standard indexed properties
    <Index>
    Property CompanyName As String
    <Index>
    Property CompanyNumber As String

    Property AddressLine1 As String
    Property AddressLine2 As String
    Property Town As String
    Property Country As String
    Property PostCode As String

    ' a full text index searchable property
    <FTSindex>
    Property Notes As String

    ' a full text index searchable property - and we will store the value in the full text index for quick access
    <FTSindex> <FTSstore>
    Property Category As String

    ' See here! We can store decimals in the database - stored internally as large integers...
    Property TurnoverGBP As Decimal


End Class



