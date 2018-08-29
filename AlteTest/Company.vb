Imports System.Net
Imports Alte
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Table

Public Class Company
    Inherits AlteObject
    Sub New(AlteSession As AlteSession)
        MyBase.New(AlteSession)
    End Sub


    Property Title As String
    Property FirstName As String
    Property LastName As String

    <Index>
    Property CompanyName As String

    '<ATIOS.Index(True)>
    Property CompanyNumber As String

    <Index>
    Property Price As Integer
    'Property Addresses As List(Of Address)


    Property AddressLine1 As String
    Property AddressLine2 As String

    '<BlobDatabase.Index(True)>
    Property Town As String

    Property Country As String

    '<BlobDatabase.Index(True)>
    Property PostCode As String
End Class

Public Class Person
    Inherits AlteObject
    Sub New(AlteSession As AlteSession)
        MyBase.New(AlteSession)
    End Sub

End Class

Public Class Address

    Property AddressLine1 As String
    Property AddressLine2 As String

    '<BlobDatabase.Index(True)>
    Property Town As String

    Property Country As String

    '<BlobDatabase.Index(True)>
    Property PostCode As String

End Class

'Public Interface IAlteObject

'    Property Session As AlteSession
'    Property ID As String

'End Interface

'Public Class AlteObject
'    'Implements IAlteObject

'    Sub New(AlteSession As AlteSession)
'        Me.Session = AlteSession
'    End Sub

'    Sub Save()
'        If Me.Session Is Nothing Then
'            Throw New Exception("No session set")
'        End If

'        Me.Session.SaveObject(Of AlteObject)(Me)
'    End Sub

'    <IgnoreProperty>
'    Property Session As AlteSession 'Implements IAlteObject.Session

'    Property ID As String 'Implements IAlteObject.ID

'End Class

'Public Class AlteSession

'    Dim tableClient As CloudTableClient
'    Dim myTable As CloudTable

'    Sub New(AzureStorageAccountName As String, AzureStorageAccountKey As String, AzureStorageTableName As String)

'        Me.AzureStorageAccountName = AzureStorageAccountName
'        Me.AzureStorageAccountKey = AzureStorageAccountKey
'        Me.AzureStorageTableName = AzureStorageTableName

'        Connect()

'    End Sub

'    Sub Connect()

'        Dim MyStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=" + AzureStorageAccountName + ";AccountKey=" + AzureStorageAccountKey + "")

'        Dim tableServicePoint As ServicePoint = ServicePointManager.FindServicePoint(MyStorageAccount.TableEndpoint)
'        tableServicePoint.UseNagleAlgorithm = False


'        tableClient = MyStorageAccount.CreateCloudTableClient()
'        myTable = tableClient.GetTableReference(AzureStorageTableName.ToLower)

'        isConnected = True

'    End Sub

'    Sub CreateStore()

'        If Not isConnected Then
'            Throw New Exception("Not connected")
'        End If

'        myTable.CreateIfNotExists()

'    End Sub

'    Property AzureStorageAccountName As String
'    Property AzureStorageAccountKey As String

'    Property AzureStorageTableName As String
'    'partition key is used for the object name

'    Property isConnected As Boolean = False


'    Sub SaveObject(Of T As AlteObject)(_AlteObject As T)

'        Dim batch As New TableBatchOperation
'        Dim dict = ObjectFlattenerRecomposer.EntityPropertyConverter.Flatten(_AlteObject)

'        ' store one copy PER indexed field
'        ' first one declared as primary key

'        Dim DTE1 As New DynamicTableEntity(_AlteObject.GetType.Name.ToLower, "PK@" + _AlteObject.ID, "*", dict)
'        Dim TableOp1 As TableOperation = TableOperation.InsertOrReplace(DTE1)
'        batch.Add(TableOp1)

'        Dim props = _AlteObject.GetType.GetProperties()
'        For Each prop In props
'            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IndexAttribute), False), IndexAttribute())
'            If ATTRi.Count > 0 Then

'                Dim StringValue As String = ObjectValueToKey(prop.GetValue(_AlteObject))

'                If StringValue <> "" Then
'                    Dim DTE2 As New DynamicTableEntity(_AlteObject.GetType.Name.ToLower, prop.Name + "@" + StringValue + "@" + _AlteObject.ID, "*", dict)
'                    Dim TableOp2 As TableOperation = TableOperation.InsertOrReplace(DTE2)
'                    batch.Add(TableOp2)
'                End If

'            End If
'        Next

'        myTable.ExecuteBatch(batch)

'    End Sub

'    Sub DeleteObject(Of T As AlteObject)(_AlteObject As T)

'    End Sub

'    Function GetObjectByKey(Of T As AlteObject)(Id As String) As T

'        If Not isConnected Then
'            Throw New Exception("Not connected")
'        End If

'        Dim Objs = GetObjectsByQuery(Of T)("PartitionKey eq '" + GetType(T).Name.ToLower + "' and RowKey eq 'PK@" + Id + "'")
'        If Objs.Count = 1 Then
'            Return Objs(0)
'        Else
'            Return Nothing
'        End If

'    End Function


'    Function GetObjectsByQuery(Of T As AlteObject)(Query As String) As List(Of T)

'        If Not isConnected Then
'            Throw New Exception("Not connected")
'        End If

'        Dim TQ As New TableQuery(Of DynamicTableEntity)
'        TQ.Where("PartitionKey eq '" + GetType(T).Name.ToLower + "' and (" + Query + ")")

'        Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList

'        Dim Objs As New List(Of T)
'        For Each Ret In Rets
'            Dim Obj = ObjectFlattenerRecomposer.EntityPropertyConverter.ConvertBack(Of T)(Ret.Properties)
'            Obj.Session = Me
'            Objs.Add(Obj)
'        Next

'        Return Objs

'    End Function



'    Function StringToKey(inp As String) As String

'        ' converts each char to 4 digit hex - can encode any string and will be sortable
'        ' keep to 400 chars for big leeway on table name and id
'        Return Left(String.Join("", inp.Select(Function(c) Conversion.Hex(AscW(c)).PadLeft(4, "0")).ToArray()), 500)

'    End Function


'    Function ObjectValueToKey(ObjectValue As Object) As String

'        Dim StringValue As String = ""

'        Select Case ObjectValue.GetType.Name

'            Case "Boolean"
'                StringValue = IIf(ObjectValue, "1", "0")

'            Case "DateTime"
'                StringValue = ObjectValue.ToString("u")

'            Case "Int32"
'                StringValue = IIf(ObjectValue >= 0, "P" + ObjectValue.ToString("0000000000"), "N" + (ObjectValue - Int32.MinValue).ToString("0000000000"))

'            Case "String"
'                StringValue = StringToKey(ObjectValue)

'            Case Else
'                'StringValue = StringToKey(ObjectValue.ToString)
'                StringValue = ""
'                Throw New Exception("Can't index on " + ObjectValue.GetType.Name + " properties at present")

'        End Select

'        Return StringValue

'    End Function

'End Class

'Public Class IndexAttribute
'    Inherits Attribute
'End Class

