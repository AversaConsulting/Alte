Imports System.Net
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Table
Imports Microsoft.WindowsAzure.Storage.Blob
Imports System.Text.RegularExpressions


Public Class AlteSession

    Private _findWords As Regex
    Private _stopWords As List(Of String) = GetStopWords()

    Enum IndexMode
        Exact = 0
        StartsWith = 1
    End Enum

    Enum SortDirection
        Ascending = 0
        Descending = 1
    End Enum


    Dim tableClient As CloudTableClient
    Property blobClient As CloudBlobClient

    Sub New(AzureStorageAccountName As String, AzureStorageAccountKey As String, Optional AzureStoragePrefix As String = "") ', AzureStorageTableName As String)

        Me.AzureStorageAccountName = AzureStorageAccountName
        Me.AzureStorageAccountKey = AzureStorageAccountKey
        Me.AzureStoragePrefix = AzureStoragePrefix.ToUpper

        Connect()

    End Sub

    Sub Connect()

        Dim MyStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=" + AzureStorageAccountName + ";AccountKey=" + AzureStorageAccountKey + "")

        Dim tableServicePoint As ServicePoint = ServicePointManager.FindServicePoint(MyStorageAccount.TableEndpoint)
        tableServicePoint.UseNagleAlgorithm = False


        tableClient = MyStorageAccount.CreateCloudTableClient()
        blobClient = MyStorageAccount.CreateCloudBlobClient()


        isConnected = True

    End Sub

    Sub CreateStore(Of T As AlteObject)()

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower())
        myTable.CreateIfNotExists()

        Dim props = GetType(T).GetProperties()
        For Each prop In props
            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(FTSindexAttribute), False), FTSindexAttribute())
            If ATTRi.Count > 0 Then
                Dim myFTStable = tableClient.GetTableReference(AzureStoragePrefix + "FTS" + GetType(T).Name.ToLower())
                myFTStable.CreateIfNotExists()
            End If

        Next

    End Sub

    Property AzureStorageAccountName As String
    Property AzureStorageAccountKey As String
    Property AzureStoragePrefix As String

    Property isConnected As Boolean = False


    Sub SaveObject(Of T As AlteObject)(_AlteObject As T)

        Dim Cancel As Boolean = False
        _AlteObject.onSaving(Cancel)
        If Cancel Then Exit Sub

        If _AlteObject.ID = "" Then
            'Throw New Exception("No ID set") ' or should we just set it?
            _AlteObject.SetID()
        End If


        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + _AlteObject.GetType.Name.ToLower)

        Dim batch As New TableBatchOperation
        Dim dicts = Flatten(_AlteObject)
        Dim dict = dicts.FullDictionary

        ' store one copy PER indexed field
        ' first one declared as primary key
        Dim Etag As String = "*"
        If "PK@" + _AlteObject.ID = _AlteObject.EtagKey Then
            Etag = _AlteObject.EtagValue
        End If

        Dim DTE1 As New DynamicTableEntity("00", "PK@" + _AlteObject.ID, Etag, dict)
        Dim TableOp1 As TableOperation = TableOperation.InsertOrReplace(DTE1)
        batch.Add(TableOp1)

        Dim props = _AlteObject.GetType.GetProperties()
        For Each prop In props
            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IndexAttribute), False), IndexAttribute())
            If ATTRi.Count > 0 Then

                Dim ATTRig() = DirectCast(prop.GetCustomAttributes(GetType(IgnoreAttribute), False), IgnoreAttribute())
                If ATTRig.Count > 0 Then
                    Throw New Exception("Can't ignore an indexed property")
                End If

                Dim StringValue As String = ObjectValueToKey(prop.GetValue(_AlteObject))

                'we need to save empty index fields too
                ' If StringValue <> "" Then
                Dim RowKey As String = prop.Name.ToLower + "@" + StringValue + "@" + _AlteObject.ID

                ' if a rowkey still exists remove from our "delete" list, we will just overwrite
                If _AlteObject.IndexRowKeys.Contains(RowKey) Then
                    _AlteObject.IndexRowKeys.Remove(RowKey)
                End If

                Etag = "*"
                If RowKey = _AlteObject.EtagKey Then
                    Etag = _AlteObject.EtagValue
                End If

                Dim DTE2 As New DynamicTableEntity("00", RowKey, Etag, dict)
                Dim TableOp2 As TableOperation = TableOperation.InsertOrReplace(DTE2)
                batch.Add(TableOp2)
                '  End If

            End If
        Next

        'delete un-used indexes
        For Each DelRowKey In _AlteObject.IndexRowKeys

            Etag = "*"
            If DelRowKey = _AlteObject.EtagKey Then
                Etag = _AlteObject.EtagValue
            End If

            Dim Entity As New DynamicTableEntity With {.ETag = Etag, .RowKey = DelRowKey, .PartitionKey = "00"}
            batch.Add(TableOperation.Delete(Entity))
        Next

        myTable.ExecuteBatch(batch)

        _AlteObject.onSaved(_AlteObject._isNew)
        _AlteObject._isNew = False


        'NOW DO FTS

        If True Then
            _findWords = New Regex("[A-Za-z0-9]+")
        Else
            _findWords = New Regex("[A-Za-z]+")
        End If

        Dim HasFTS As Boolean = False

        Dim WordList As New List(Of String)
        For Each prop In props
            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(FTSindexAttribute), False), FTSindexAttribute())
            If ATTRi.Count > 0 Then
                HasFTS = True

                Dim words = _findWords.Matches(prop.GetValue(_AlteObject).ToString.ToLower)

                Dim Stemmer As New PorterStemmer

                For i = 0 To words.Count - 1
                    Dim word = words(i).Value
                    If Not _stopWords.Contains(word) Then

                        word = Stemmer.stemTerm(word)

                        If Not WordList.Contains(word) Then
                            WordList.Add(word)
                        End If

                    End If
                Next
            End If

        Next

        If HasFTS Then
            Dim myFTStable = tableClient.GetTableReference(AzureStoragePrefix + "FTS" + _AlteObject.GetType.Name.ToLower)

            'delete existing, if not in current list
            Dim TQ As New TableQuery(Of DynamicTableEntity)

            TQ.Where("RowKey eq '" + _AlteObject.ID + "'")
            Dim Cols As New List(Of String)
            Cols.Add("RowKey") ' if we don't add this we get everything
            TQ.Select(Cols)

            Dim Rets = myFTStable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList
            For Each ret In Rets
                If Not WordList.Contains(ret.RowKey) Then
                    Dim DelOp = TableOperation.Delete(ret)
                    myFTStable.Execute(DelOp)
                Else
                    ' we could remove from list here, but probably will add extra params stored later
                End If
            Next

            ' add new
            For Each word In WordList
                Dim IdxRecord As New DynamicTableEntity(word, _AlteObject.ID, "*", dicts.FTSstoredDictionary)
                Dim InsOp = TableOperation.InsertOrReplace(IdxRecord)
                myFTStable.Execute(InsOp)
            Next
        End If



    End Sub

    Sub RebuildIndexes(Of T As AlteObject)()

        'delete all index records then load and save every item
        'after clearning their IndexRowKeys
        'need a way of locking saving while this is running

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower)

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim TQ As New TableQuery(Of DynamicTableEntity)
        Dim Query = "RowKey lt 'PK@' and RowKey gt 'PK@■'"
        TQ.Where("PartitionKey eq '00'") ' and ( " + Query + " )")
        Dim Cols As New List(Of String)
        Cols.Add("ID")
        TQ.Select(Cols)

        Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList

        Dim IDs As New List(Of String)
        Dim RowKeys As New List(Of String)
        For Each Ret In Rets
            Dim RowKey As String = Ret.RowKey
            If Left(RowKey, 3) = "PK@" Then
                IDs.Add(Mid(RowKey, 4))
            Else
                RowKeys.Add(RowKey)
            End If

        Next

        Dim batch As New TableBatchOperation
        Dim batchCnt As Integer = 0
        For Each RowKey In RowKeys
            batchCnt = batchCnt + 1
            Dim PKEntity As New DynamicTableEntity With {.ETag = "*", .RowKey = RowKey, .PartitionKey = "00"}
            batch.Add(TableOperation.Delete(PKEntity))
            If batchCnt = 100 Then
                myTable.ExecuteBatch(batch)
                batch = New TableBatchOperation
                batchCnt = 0
            End If
        Next
        If batchCnt > 0 Then
            myTable.ExecuteBatch(batch)
        End If


        ' now delete FTS entries

        Dim props = GetType(T).GetProperties()
        For Each prop In props
            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(FTSindexAttribute), False), FTSindexAttribute())
            If ATTRi.Count > 0 Then

                Dim myFTStable = tableClient.GetTableReference(AzureStoragePrefix + "FTS" + GetType(T).Name.ToLower)

                TQ = New TableQuery(Of DynamicTableEntity)
                'get all

                Cols = New List(Of String)
                Cols.Add("RowKey") ' if we don't add this we get everything
                TQ.Select(Cols)

                Rets = myFTStable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList
                For Each ret In Rets
                    ret.ETag = "*"
                    Dim DelOp = TableOperation.Delete(ret)
                    myFTStable.Execute(DelOp)
                Next

                Exit For
            End If

        Next



        For Each ID In IDs
            Dim Obj = GetObjectByID(Of T)(ID)
            Obj.IndexRowKeys.Clear()
            Obj.Save()
        Next


    End Sub


    Sub BackupObjects(Of T As AlteObject)(BackupDatabase As AlteSession, TableSuffix As String)

        If TableSuffix = "" Then
            TableSuffix = Format(Now, "yyyyMMddHHmm")
        End If

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower)


        Dim MyStorageAccount2 = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=" + BackupDatabase.AzureStorageAccountName + ";AccountKey=" + BackupDatabase.AzureStorageAccountKey + "")

        Dim tableServicePoint2 As ServicePoint = ServicePointManager.FindServicePoint(MyStorageAccount2.TableEndpoint)
        tableServicePoint2.UseNagleAlgorithm = False


        Dim tableClient2 = MyStorageAccount2.CreateCloudTableClient()

        Dim myTable2 = tableClient2.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower + TableSuffix)
        myTable2.CreateIfNotExists()

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim TQ As New TableQuery(Of DynamicTableEntity)
        Dim Query = "RowKey ge 'PK@' and RowKey lt 'PK@■'"
        TQ.Where("PartitionKey eq '00' and ( " + Query + " )")

        Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList

        Dim batch As New TableBatchOperation
        Dim batchCnt As Integer = 0

        For Each Ret In Rets
            batchCnt = batchCnt + 1

            Dim TableOp1 As TableOperation = TableOperation.InsertOrReplace(Ret)
            batch.Add(TableOp1)

            If batchCnt = 100 Then
                myTable2.ExecuteBatch(batch)
                batch = New TableBatchOperation
                batchCnt = 0
            End If

        Next

        If batchCnt > 0 Then
            myTable2.ExecuteBatch(batch)
        End If


    End Sub


    Sub BackupBlobs(ContainerName As String, BackupDatabase As AlteSession, ContainerSuffix As String)


        If Not isConnected Then
            Throw New Exception("Not connected")
        End If


        If ContainerSuffix = "" Then
            ContainerSuffix = Format(Now, "yyyyMMddHHmm")
        End If

        BackupDatabase.CreateBlobContainer(ContainerName + ContainerSuffix)
        Dim MyStorageAccount2 = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=" + BackupDatabase.AzureStorageAccountName + ";AccountKey=" + BackupDatabase.AzureStorageAccountKey + "")

        Dim blobClient2 = MyStorageAccount2.CreateCloudBlobClient()

        Dim blobs = ListBlobs(ContainerName)
        For Each Blob1 In blobs

            Dim liveBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower)
            Dim liveblob = liveBlobs.GetBlockBlobReference(Blob1)

            Dim backupBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower + ContainerSuffix)
            Dim backupblob = liveBlobs.GetBlockBlobReference(Blob1)

            'Dim res1 = backupblob.StartCopy(liveblob) ' this isn't working

            BackupDatabase.WriteBlob(ContainerName + ContainerSuffix, Blob1, Me.ReadBlob(ContainerName, Blob1))

            'Stop
        Next


    End Sub



    Function Flatten(Of T As AlteObject)(_AlteObject As T) As FlatDictionaries

        Dim Dict As New Dictionary(Of String, EntityProperty)
        Dim DictFTS As New Dictionary(Of String, EntityProperty)

        Dim props = _AlteObject.GetType.GetProperties()
        For Each prop In props

            If prop.CanWrite Then
                ' only writable properties

                Dim isFTSstored As Boolean = False

                Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IgnoreAttribute), False), IgnoreAttribute())
                If ATTRi.Count = 0 Then

                    Dim ATTRiFTS() = DirectCast(prop.GetCustomAttributes(GetType(FTSstoreAttribute), False), FTSstoreAttribute())
                    If ATTRiFTS.Count > 0 Then
                        isFTSstored = True
                    End If


                    Dim ObjectValue = prop.GetValue(_AlteObject)

                    ' set default value
                    Dim ATTRiDV() = DirectCast(prop.GetCustomAttributes(GetType(DefaultValueAttribute), False), DefaultValueAttribute())
                    If ATTRiDV.Count > 0 Then
                        If ObjectValue = GetDefaultValue(prop.GetType) Then
                            ObjectValue = ATTRiDV(0).Value
                            prop.SetValue(_AlteObject, ObjectValue)
                        End If
                    End If

                    Dim EP As EntityProperty

                    Select Case prop.PropertyType.Name

                        Case "Boolean"
                            EP = New EntityProperty(CBool(ObjectValue))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case "DateTime"
                            ' deal with unusal mindate values here
                            Dim DateValue = CDate(ObjectValue)
                            If DateValue < CDate("1601-01-01") Then '= Date.MinValue Then
                                EP = New EntityProperty(CDate("1601-01-01"))
                            Else
                                DateValue = Date.SpecifyKind(DateValue, DateTimeKind.Utc)
                                EP = New EntityProperty(DateValue)
                            End If

                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If


                        Case "Int32"
                            EP = New EntityProperty(CInt(ObjectValue))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case "Int64"
                            EP = New EntityProperty(CLng(ObjectValue))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case "String"
                            EP = New EntityProperty(CStr(ObjectValue))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case "Double"
                            EP = New EntityProperty(CDbl(ObjectValue))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case "Byte[]"
                            Dim ByteArray = CType(ObjectValue, Byte())
                            If Not ByteArray Is Nothing Then
                                Dim Chunk As Integer = 0
                                While (Chunk * (60 * 1024)) < ByteArray.Length
                                    EP = New EntityProperty(ByteArray.Skip(Chunk * (60 * 1024)).Take(60 * 1024).ToArray())

                                    If Chunk = 0 Then
                                        Dict.Add(prop.Name.ToLower, EP)
                                    Else
                                        Dict.Add(prop.Name.ToLower + "__" + Format(Chunk, "00"), EP)
                                    End If
                                    Chunk = Chunk + 1
                                End While
                            End If


                            ' WONT STORE IN FTS

                            If isFTSstored Then
                                Throw New Exception("Can't store byte arrays in FTS")
                            End If

                        ' will add a "long byte" or auto allow longer ones?

                        Case "Guid"
                            EP = New EntityProperty(CType(ObjectValue, Guid))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If


                        Case "Decimal"
                            ' convert to INT64 and multiply by 10000 (4DP)
                            EP = New EntityProperty(CLng(CDec(ObjectValue) * 10000))
                            Dict.Add(prop.Name.ToLower, EP)
                            If isFTSstored Then
                                DictFTS.Add(prop.Name.ToLower, EP)
                            End If

                        Case Else

                            Throw New Exception("Can't store " + prop.PropertyType.Name + " properties at present")

                    End Select




                End If
            End If

        Next

        Return New FlatDictionaries With {.FullDictionary = Dict, .FTSstoredDictionary = DictFTS}

    End Function

    Public Function ConvertBack(Of T As AlteObject)(flattenedEntityProperties As IDictionary(Of String, EntityProperty)) As T

        ' this may need some work to gracefully convert one type to another 
        ' if the model has changed. At the moment it will throw an exception

        Dim _AlteObject As T = CType(Activator.CreateInstance(GetType(T)), T)

        Dim props = _AlteObject.GetType.GetProperties()

        For Each prop In props
            If prop.CanWrite Then
                ' only writable properties

                Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IgnoreAttribute), False), IgnoreAttribute())
                If ATTRi.Count = 0 Then

                    If flattenedEntityProperties.ContainsKey(prop.Name.ToLower) Then

                        Select Case prop.PropertyType.Name

                            Case "Boolean"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).BooleanValue)

                            Case "DateTime"
                                ' deal with unusal mindate values here

                                Dim DateValue = flattenedEntityProperties(prop.Name.ToLower).DateTime
                                If DateValue = CDate("1601-01-01") Then
                                    prop.SetValue(_AlteObject, Date.MinValue)
                                Else
                                    prop.SetValue(_AlteObject, Date.SpecifyKind(DateValue, DateTimeKind.Utc))
                                End If


                            Case "Int32"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).Int32Value)

                            Case "Int64"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).Int64Value)

                            Case "String"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).StringValue)

                            Case "Double"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).DoubleValue)

                            Case "Byte[]"
                                Dim ByteArray As Byte()
                                ByteArray = flattenedEntityProperties(prop.Name.ToLower).BinaryValue

                                Dim Chunk As Integer = 1
                                While flattenedEntityProperties.ContainsKey(prop.Name.ToLower + "__" + Format(Chunk, "00"))

                                    Dim NewArray = flattenedEntityProperties(prop.Name.ToLower + "__" + Format(Chunk, "00")).BinaryValue
                                    ReDim Preserve ByteArray((ByteArray.Length + NewArray.Length) - 1)
                                    NewArray.CopyTo(ByteArray, (ByteArray.Length - NewArray.Length))
                                    Chunk = Chunk + 1
                                End While

                                prop.SetValue(_AlteObject, ByteArray)
                        ' will add a "long byte" or auto allow longer ones?

                            Case "Guid"
                                prop.SetValue(_AlteObject, flattenedEntityProperties(prop.Name.ToLower).GuidValue)

                            Case "Decimal"
                                ' convert to INT64 and divide by 10000 (4DP)
                                prop.SetValue(_AlteObject, CDec(flattenedEntityProperties(prop.Name.ToLower).Int64Value / 10000))

                            Case Else

                                Throw New Exception("Can't store " + prop.PropertyType.Name + " properties at present")

                        End Select

                    End If
                End If
            End If
        Next

        Return _AlteObject

    End Function



    Sub DeleteObject(Of T As AlteObject)(_AlteObject As T)


        If _AlteObject._isNew Then
            Throw New Exception("Item is new, can't delete from datastore")
        End If

        Dim Cancel As Boolean = False
        _AlteObject.onDeleting(Cancel)
        If Cancel Then Exit Sub

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If


        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + _AlteObject.GetType.Name.ToLower)

        'need to deal with schema changes


        Dim batch As New TableBatchOperation

        Dim PKEntity As New DynamicTableEntity With {.ETag = "*", .RowKey = "PK@" + _AlteObject.ID, .PartitionKey = "00"}
        batch.Add(TableOperation.Delete(PKEntity))

        For Each DelRowKey In _AlteObject.IndexRowKeys
            Dim Entity As New DynamicTableEntity With {.ETag = "*", .RowKey = DelRowKey, .PartitionKey = "00"}
            batch.Add(TableOperation.Delete(Entity))
        Next

        myTable.ExecuteBatch(batch)


        ' now delete FTS entries

        Dim props = _AlteObject.GetType.GetProperties()
        For Each prop In props
            Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(FTSindexAttribute), False), FTSindexAttribute())
            If ATTRi.Count > 0 Then

                Dim myFTStable = tableClient.GetTableReference(AzureStoragePrefix + "FTS" + GetType(T).Name.ToLower)

                Dim TQ As New TableQuery(Of DynamicTableEntity)

                TQ.Where("RowKey eq '" + _AlteObject.ID + "'")
                Dim Cols As New List(Of String)
                Cols.Add("RowKey") ' if we don't add this we get everything
                TQ.Select(Cols)

                Dim Rets = myFTStable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList
                For Each ret In Rets
                    Dim DelOp = TableOperation.Delete(ret)
                    myFTStable.Execute(DelOp)
                Next

                Exit For
            End If

        Next


        _AlteObject.onDeleted()

    End Sub

    Function GetObjectByID(Of T As AlteObject)(Id As String) As T

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim Objs = GetObjectsByQuery(Of T)("PartitionKey eq '00' and RowKey eq 'PK@" + Id + "'")
        If Objs.Count = 1 Then
            Return Objs(0)
        Else
            Return Nothing
        End If

    End Function

    Function GetObjectByIDs(Of T As AlteObject)(IDs As List(Of String), Optional PageNumber As Integer = 1, Optional pagesize As Integer = 10000000) As List(Of T)

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If
        Dim Objs As New List(Of T)


        'Dim batch As New TableBatchOperation

        For i = (PageNumber - 1) * pagesize To Math.Min((PageNumber * pagesize) - 1, IDs.Count - 1)

            'batch.Add(TableOperation.Retrieve(Of DynamicTableEntity)("00", "'PK@" + FullTextResults(i).ID + "'"))
            ' at the moment multiple retreives not supported

            Dim Obj = GetObjectsByQuery(Of T)("PartitionKey eq '00' and RowKey eq 'PK@" + IDs(i) + "'")
            If Obj.Count = 1 Then
                Objs.Add(Obj(0))
            End If

        Next
        'Dim myTable = tableClient.GetTableReference(AzureStoragePrefix +GetType(T).Name.ToLower)
        'Dim Rets = myTable.ExecuteBatch(batch)

        'Return DynamicTableEntitiesToObjects(Of T)(Rets)

        Return Objs


    End Function


    Function GetObjectByFullTextResult(Of T As AlteObject)(FullTextResults As List(Of FullTextResult), Optional PageNumber As Integer = 1, Optional pagesize As Integer = 10000000) As List(Of T)

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If
        Dim Objs As New List(Of T)


        'Dim batch As New TableBatchOperation

        For i = (PageNumber - 1) * pagesize To Math.Min((PageNumber * pagesize) - 1, FullTextResults.Count - 1)

            'batch.Add(TableOperation.Retrieve(Of DynamicTableEntity)("00", "'PK@" + FullTextResults(i).ID + "'"))
            ' at the moment multiple retreives not supported

            Dim Obj = GetObjectsByQuery(Of T)("PartitionKey eq '00' and RowKey eq 'PK@" + FullTextResults(i).ID + "'")
            If Obj.Count = 1 Then
                Objs.Add(Obj(0))
            End If

        Next
        'Dim myTable = tableClient.GetTableReference(AzureStoragePrefix +GetType(T).Name.ToLower)
        'Dim Rets = myTable.ExecuteBatch(batch)

        'Return DynamicTableEntitiesToObjects(Of T)(Rets)

        Return Objs


    End Function

    Function GetObjectsByQuery(Of T As AlteObject)(Query As String, Optional Top As Integer = 0) As List(Of T)

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower)

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim TQ As New TableQuery(Of DynamicTableEntity)
        If Query = "" Then
            Query = "RowKey ge 'PK@' and RowKey lt 'PK@■'"
        End If
        TQ.Where("PartitionKey eq '00' and ( " + Query + " )")

        If Top > 0 Then
            TQ.Take(Top)
        End If


        Try
            Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList
            Return DynamicTableEntitiesToObjects(Of T)(Rets)

        Catch ex As Exception
            If ex.HResult = -2146233088 Then
                CreateStore(Of T)()
                Return New List(Of T)
            End If

        End Try

        Return Nothing



    End Function


    Function DynamicTableEntitiesToObjects(Of T As AlteObject)(rets As List(Of DynamicTableEntity)) As List(Of T)

        Dim Objs As New List(Of T)
        For Each ret In rets
            'Dim Obj = ObjectFlattenerRecomposer.EntityPropertyConverter.ConvertBack(Of T)(Ret.Properties)
            Dim Obj = ConvertBack(Of T)(ret.Properties)

            Obj.EtagKey = ret.RowKey
            Obj.EtagValue = ret.ETag
            Obj._isNew = False
            Obj.Session = Me

            ' get what would be the row keys for the indexes and store
            Obj.IndexRowKeys = New List(Of String)
            Dim props = Obj.GetType.GetProperties()
            For Each prop In props
                Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IndexAttribute), False), IndexAttribute())
                If ATTRi.Count > 0 Then

                    Dim StringValue As String = ObjectValueToKey(prop.GetValue(Obj))

                    'If StringValue <> "" Then
                    'need empty ones too
                    Obj.IndexRowKeys.Add(prop.Name.ToLower + "@" + StringValue + "@" + Obj.ID)
                    'End If

                End If
            Next

            Objs.Add(Obj)
        Next

        Return Objs

    End Function

    Function GetIDsByQuery(Of T As AlteObject)(Query As String, Optional Top As Integer = 0) As List(Of String)

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + GetType(T).Name.ToLower)

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Dim TQ As New TableQuery(Of DynamicTableEntity)
        If Query = "" Then
            Query = "RowKey ge 'PK@' and RowKey lt 'PK@■'"
        End If
        TQ.Where("PartitionKey eq '00' and ( " + Query + " )")

        Dim Cols As New List(Of String)
        Cols.Add("ID")

        TQ.Select(Cols)

        If Top > 0 Then
            TQ.Take(Top)
        End If

        Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList

        Dim IDs As New List(Of String)
        For Each Ret In Rets
            Dim ID As String = Ret.Properties("ID").StringValue
            IDs.Add(ID)
        Next

        Return IDs

    End Function



    Function GetObjectsByIndex(Of T As AlteObject)(Matcher As T, Optional Mode As IndexMode = IndexMode.Exact, Optional Top As Integer = 0, Optional AdditionalNonIndexedQuery As String = "") As List(Of T)

        If Matcher Is Nothing Then
            Throw New Exception("No matcher passed to GetObjectsByIndex")
        End If

        Dim props = GetType(T).GetProperties()
        Dim StringValue As String = ""
        Dim PropertyName As String = ""
        Dim Objects As New List(Of T)

        For Each prop In props

            StringValue = ""
            PropertyName = ""

            ' put all known ones here to avoid reflection delays
            If prop.Name.ToLower <> "edits" And prop.Name.ToLower <> "id" And prop.Name.ToLower <> "indexrowkeys" Then
                If prop.CanWrite Then
                    ' only writable properties

                    Dim ObjectValue As Object = prop.GetValue(Matcher)
                    If Not ObjectValue Is Nothing Then
                        Dim DefaultValue As Object = GetDefaultValue(prop.PropertyType())
                        If DefaultValue Is Nothing OrElse ObjectValue <> DefaultValue Then

                            StringValue = ObjectValueToKey(prop.GetValue(Matcher))

                        End If
                    End If

                    If StringValue <> "" Then

                        Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IndexAttribute), False), IndexAttribute())
                        If ATTRi.Count > 0 Then

                            Dim query As String
                            If Mode = IndexMode.Exact Then
                                query = "RowKey ge '" + prop.Name.ToLower + "@" + StringValue + "@' and RowKey lt '" + prop.Name.ToLower + "@" + StringValue + "@■'"
                            Else
                                query = "RowKey ge '" + prop.Name.ToLower + "@" + StringValue + "' and RowKey lt '" + prop.Name.ToLower + "@" + StringValue + "■'"
                            End If

                            If AdditionalNonIndexedQuery <> "" Then
                                query = query + " and ( " + AdditionalNonIndexedQuery + " ) "
                            End If

                            Return GetObjectsByQuery(Of T)(query, Top)

                        Else
                            Throw New Exception("Property " + prop.Name + " is not indexed, used filter instead.")
                        End If

                    End If
                End If
            End If

        Next

        Return Objects

    End Function

    Function GetIDsByIndex(Of T As AlteObject)(Matcher As T, Optional Top As Integer = 0) As List(Of String)



        If Matcher Is Nothing Then
            Throw New Exception("No matcher passed to GetObjectsByIndex")
        End If

        Dim props = GetType(T).GetProperties()
        Dim StringValue As String = ""
        Dim PropertyName As String = ""
        Dim Objects As New List(Of String)

        For Each prop In props

            StringValue = ""
            PropertyName = ""

            If prop.Name.ToLower <> "edits" And prop.Name.ToLower <> "id" And prop.Name.ToLower <> "indexrowkeys" Then

                Dim ObjectValue As Object = prop.GetValue(Matcher)
                If Not ObjectValue Is Nothing Then
                    Dim DefaultValue As Object = GetDefaultValue(prop.PropertyType())
                    If DefaultValue Is Nothing OrElse ObjectValue <> DefaultValue Then

                        StringValue = ObjectValueToKey(prop.GetValue(Matcher))

                    End If
                End If

                If StringValue <> "" Then

                    Dim ATTRi() = DirectCast(prop.GetCustomAttributes(GetType(IndexAttribute), False), IndexAttribute())
                    If ATTRi.Count > 0 Then

                        Return GetIDsByQuery(Of T)("RowKey ge '" + prop.Name.ToLower + "@" + StringValue + "@' and RowKey lt '" + prop.Name.ToLower + "@" + StringValue + "@■'", Top)

                    Else
                        Throw New Exception("Property " + prop.Name + " is not indexed, used filter instead.")
                    End If


                End If
            End If

        Next

        Return Objects

    End Function


    Function GetFullTextResults(Of T As AlteObject)(Keywords As String, Optional Top As Integer = 0, Optional SortProperty As String = "",
                                                    Optional SortDirection As SortDirection = SortDirection.Ascending,
                                                    Optional ByVal SortOrderFunction As Func(Of FullTextResult, Object) = Nothing) As List(Of FullTextResult)
        Keywords = Keywords.ToLower

        Dim Results As IEnumerable(Of FullTextResult) = Nothing

        If Keywords = "" Then
            Throw New Exception("No words passed to GetIDsByFTS")
        End If

        If Not isConnected Then
            Throw New Exception("Not connected")
        End If

        Keywords = Keywords.ToLower

        If True Then
            _findWords = New Regex("[A-Za-z0-9]+")
        Else
            _findWords = New Regex("[A-Za-z]+")
        End If

        Dim words = _findWords.Matches(Keywords)

        Dim myTable = tableClient.GetTableReference(AzureStoragePrefix + "FTS" + GetType(T).Name.ToLower)

        Dim Stemmer As New PorterStemmer
        Dim props = GetType(T).GetProperties()

        For i = 0 To words.Count - 1
            Dim word = words(i).Value
            If Not _stopWords.Contains(word) Then
                word = Stemmer.stemTerm(word)
                Dim TQ As New TableQuery(Of DynamicTableEntity)

                TQ.Where("PartitionKey eq '" + word + "'")

                If Top > 0 Then
                    TQ.Take(Top)
                End If

                Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList
                If Rets.Count > 0 Then
                    Dim FTres As New List(Of FullTextResult)
                    For Each ret In Rets
                        Dim fts1 As New FullTextResult
                        fts1.ID = ret.RowKey

                        For Each prop In props

                            Dim ATTRiFTS() = DirectCast(prop.GetCustomAttributes(GetType(FTSstoreAttribute), False), FTSstoreAttribute())
                            If ATTRiFTS.Count > 0 Then
                                Dim pnam = prop.Name.ToLower
                                If ret.Properties.ContainsKey(pnam) Then

                                    Select Case prop.PropertyType.Name

                                        Case "Boolean"

                                            fts1.Properties.Add(pnam, ret.Properties(pnam).BooleanValue)


                                        Case "DateTime"
                                            ' deal with unusal mindate values here

                                            Dim DateValue = ret.Properties(pnam).DateTime
                                            If DateValue = CDate("1601-01-01") Then
                                                fts1.Properties.Add(pnam, Date.MinValue)
                                            Else
                                                fts1.Properties.Add(pnam, DateValue)
                                            End If


                                        Case "Int32"
                                            fts1.Properties.Add(pnam, ret.Properties(pnam).Int32Value)

                                        Case "Int64"
                                            fts1.Properties.Add(pnam, ret.Properties(pnam).Int64Value)

                                        Case "String"
                                            fts1.Properties.Add(pnam, ret.Properties(pnam).StringValue)

                                        Case "Double"
                                            fts1.Properties.Add(pnam, ret.Properties(pnam).DoubleValue)

                                        Case "Decimal"
                                            ' convert to INT64 and divide by 10000 (4DP)
                                            fts1.Properties.Add(pnam, CDec(ret.Properties(pnam).Int64Value / 10000))

                                        Case Else

                                            Throw New Exception("Can't store FTS " + prop.PropertyType.Name + " properties at present")
                                    End Select

                                Else
                                    ' if not stored
                                    fts1.Properties.Add(prop.Name.ToLower, Nothing)
                                End If




                            End If
                        Next
                        FTres.Add(fts1)
                    Next
                    If Results Is Nothing Then
                        Results = FTres
                    Else
                        Dim comparer = New FullTextResultComparer
                        Results = Results.Intersect(FTres, comparer)
                    End If
                Else
                    Return New List(Of FullTextResult)
                End If


            End If
        Next

        If SortProperty <> "" Then
            If SortDirection = SortDirection.Ascending Then
                Results = Results.OrderBy(Function(x) x.Properties(SortProperty.ToLower))
            Else
                Results = Results.OrderByDescending(Function(x) x.Properties(SortProperty.ToLower))
            End If

        End If

        If Not SortOrderFunction Is Nothing Then
            Results = Results.OrderBy(SortOrderFunction)
        End If

        Return Results.ToList

    End Function



    Function GetDefaultValue(t As Type)

        If t.IsValueType Then
            Return Activator.CreateInstance(t)
        Else
            Return Nothing
        End If

    End Function

    Function MergeObjects(Of T As AlteObject)(MainObject As T, MergeObject As T) As T

        Dim props = GetType(T).GetProperties()

        ' merge any NON NULL values into our object retrieved from the database, but NOT edits
        For Each prop In props
            If prop.Name.ToLower <> "edits" And prop.Name.ToLower <> "id" Then
                Dim PropValue As Object = prop.GetValue(MergeObject)
                If Not PropValue Is Nothing Then
                    GetType(T).GetProperty(prop.Name).SetValue(MainObject, PropValue)
                End If
            End If
        Next

        Return MainObject

    End Function

    Function StringToKey(inp As String) As String

        ' converts each char to 4 digit hex - can encode any string and will be sortable
        ' keep to 500 chars for big leeway on table name and id
        Return Left(String.Join("", inp.Select(Function(c) Conversion.Hex(AscW(c)).PadLeft(4, "0")).ToArray()), 500)

    End Function


    Public Function ObjectValueToKey(ObjectValue As Object) As String

        Dim StringValue As String = ""
        If ObjectValue Is Nothing Then
            Return ""
        End If

        Select Case ObjectValue.GetType.Name

            Case "Boolean"
                StringValue = IIf(ObjectValue, "1", "0")

            Case "DateTime"
                StringValue = Format(ObjectValue, "yyyy-MM-dd HH:mm:ss")

            Case "Int32"
                StringValue = IIf(ObjectValue >= 0, "P" + ObjectValue.ToString("0000000000"), "N" + (ObjectValue - Int32.MinValue).ToString("0000000000"))
                ' 10 characters max

            Case "Int64"
                StringValue = IIf(ObjectValue >= 0, "P" + ObjectValue.ToString("0000000000000000000"), "N" + (ObjectValue - Int64.MinValue).ToString("0000000000000000000"))
                ' 19 characters max

            Case "Decimal"
                StringValue = IIf(ObjectValue >= 0, "P" + (CLng(CDec(ObjectValue) * 10000)).ToString("0000000000000000000"), "N" + ((CLng(CDec(ObjectValue) * 10000)) - Int64.MinValue).ToString("0000000000000000000"))
                ' 19 characters max
                ' convert to INT64 and multiply by 10000 (4DP)

            Case "String"
                StringValue = StringToKey(ObjectValue)

            Case Else
                'StringValue = StringToKey(ObjectValue.ToString)
                StringValue = ""
                Throw New Exception("Can't index on " + ObjectValue.GetType.Name + " properties at present")

        End Select

        Return StringValue

    End Function

    Function RandomizeList(Of T As AlteObject)(AlteObjectList As List(Of T), Optional maxRecords As Integer = 0) As List(Of T)

        ' we want to try and leave the old list as it was
        Dim newList As New List(Of T)
        Dim indexes As New List(Of Integer)
        For i = 0 To AlteObjectList.Count - 1
            indexes.Add(i)
        Next

        Dim Rand As New Random
        If maxRecords = 0 Or maxRecords > indexes.Count Then
            maxRecords = indexes.Count
        End If

        For i = 1 To maxRecords
            Dim idx As Integer = Rand.Next(0, indexes.Count - 1)
            newList.Add(AlteObjectList(indexes(idx)))
            indexes.RemoveAt(idx)
        Next

        Return newList

    End Function


    Sub CreateBlobContainer(ContainerName As String)


        Dim myBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName)
        myBlobs.CreateIfNotExists()


    End Sub

    Sub WriteBlob(ContainerName As String, BlobName As String, ByteArray As Byte())

        Dim myBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower)
        Dim thisblob = myBlobs.GetBlockBlobReference(BlobName)
        thisblob.UploadFromByteArray(ByteArray, 0, ByteArray.Length)

    End Sub

    Sub DeleteBlob(ContainerName As String, BlobName As String)

        Dim myBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower)
        Dim thisblob = myBlobs.GetBlockBlobReference(BlobName)
        thisblob.Delete()

    End Sub

    Function ReadBlob(ContainerName As String, BlobName As String) As Byte()

        Dim myBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower)
        Dim thisblob = myBlobs.GetBlockBlobReference(BlobName)
        If thisblob.Exists Then
            thisblob.FetchAttributes()
            Dim ByteArray(thisblob.Properties.Length - 1) As Byte
            thisblob.DownloadToByteArray(ByteArray, 0)

            Return ByteArray
        Else
            Return Nothing
        End If



    End Function

    Function ListBlobs(ContainerName As String, Optional VirtualFolder As String = "") As List(Of String)

        If VirtualFolder <> "" Then
            VirtualFolder = VirtualFolder + "\"
        End If

        Dim myBlobs = blobClient.GetContainerReference(AzureStoragePrefix + ContainerName.ToLower)
        Dim Blobs = myBlobs.ListBlobs(VirtualFolder, False, BlobListingDetails.None)

        Dim List As New List(Of String)
        For Each Blob1 In Blobs
            If Right(Blob1.Uri.Segments.Last, 1) <> "/" Then
                List.Add(Blob1.Uri.Segments.Last)
            End If
        Next

        Return List


    End Function

    Function GetStopWords() As List(Of String)


        Dim list As List(Of String) = New List(Of String)() From {
            "a", "about", "actually", "after", "also", "am", "an",
            "and", "any", "are", "as", "at", "be", "because", "but", "by",
            "could", "do", "each", "either", "en", "for", "from", "has", "have", "how",
            "i", "if", "in", "is", "it", "its", "just", "of", "or", "so", "some",
            "such", "that", "the", "their", "these", "thing", "this", "to", "too",
            "very", "was", "we", "well", "what", "when", "where", "who", "will",
            "with", "you", "your"
        }

        Return list

    End Function

End Class