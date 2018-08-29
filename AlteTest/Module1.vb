Imports System.Net
Imports Alte
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Table

Module Module1

    Sub Main()

        Console.WriteLine("Creating Session")
        Dim Session As New AlteSession("aversa", "c2VugIOsSXK3AR8rnfheaADFHynUQm5WK78OB516Y4X74Uwf3BL3fXvfDsBDBIavbiGqE5f8EsCCo2tql7C6XA==") ', "company")

        Dim sw = New Stopwatch()
        sw.Start()
        Console.WriteLine("Creating Store")
        Session.CreateStore(Of Company)()
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Dim C As New Company(Session)
        C.CompanyNumber = "123456"
        C.CompanyName = "Aversa"
        C.AddressLine1 = "The Rise"
        C.AddressLine2 = ""
        C.Town = "Stafford"
        C.Country = "UK"
        C.PostCode = "ST17 0LH"
        C.Price = -10
        C.ID = Guid.NewGuid.ToString


        Console.WriteLine("Saving Company")
        sw.Reset() : sw.Start()

        'Session.SaveObject(Of Company)(C)
        C.Save()
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Console.WriteLine("Getting Company")

        sw.Reset() : sw.Start()
        Dim C2 = Session.GetObjectByKey(Of Company)(C.ID)
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Console.WriteLine("Company name : " + C2.CompanyName)

        Console.ReadLine()



    End Sub


    Sub Main2()



        'https://www.nuget.org/packages/ObjectFlattenerRecomposer/#

        'normal azure support for byte(),Boolean,DateTime,Double,Guid,Int32/Int,Int64/Long,String
        'adds support for Enum, TimeSpan, DateTimeOffset, Nullable
        'still max 64k per item though

        ' we could pass in the batch - for transaction type things but MUST be same partition key! only for mission critical stuff


        'Dim MyStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=aversa;AccountKey=c2VugIOsSXK3AR8rnfheaADFHynUQm5WK78OB516Y4X74Uwf3BL3fXvfDsBDBIavbiGqE5f8EsCCo2tql7C6XA==")

        'Dim tableServicePoint As ServicePoint = ServicePointManager.FindServicePoint(MyStorageAccount.TableEndpoint)
        'tableServicePoint.UseNagleAlgorithm = False

        'Dim tableClient As CloudTableClient
        'tableClient = MyStorageAccount.CreateCloudTableClient()

        'Dim sw = New Stopwatch()
        'Dim myTable = tableClient.GetTableReference("company")


        'myTable.CreateIfNotExists()


        'If True Then


        '    Console.WriteLine("Loading sample data")
        '    Dim Lines = System.IO.File.ReadAllLines("c:\sampledata.csv")
        '    Console.WriteLine("Importing to tables")

        '    Dim CNT As Integer = 0
        '    Dim batch As New TableBatchOperation

        '    sw.Start()



        '    Lines = Lines.ToList.Skip(60000).Take(100).ToArray
        '    Dim lincnt = Lines.Count


        '    For Each line In Lines

        '        Dim cols = Split(line, ",")
        '        For i = 0 To UBound(cols)
        '            cols(i) = Trim(cols(i))
        '            If Left(cols(i), 1) = """" Then cols(i) = Mid(cols(i), 2)
        '            If Right(cols(i), 1) = """" Then cols(i) = Left(cols(i), Len(cols(i)) - 1)
        '            cols(i) = Replace(cols(i), """""", """")
        '        Next
        '        Dim c As New Company

        '        c.CompanyNumber = cols(1)
        '        c.CompanyName = cols(0)
        '        c.AddressLine1 = cols(4)
        '        c.AddressLine2 = cols(5)
        '        c.Town = cols(6)
        '        c.Country = cols(8)
        '        c.PostCode = cols(9)
        '        c.ID = Guid.NewGuid.ToString


        '        Dim dict = ObjectFlattenerRecomposer.EntityPropertyConverter.Flatten(c)

        '        ' store one copy PER indexed field, here ID, postcode and companynumber
        '        ' one declared as primary key

        '        Dim DTE1 As New DynamicTableEntity("company", "PK_" + c.ID, "*", dict)
        '        Dim TableOp1 As TableOperation = TableOperation.InsertOrReplace(DTE1)
        '        batch.Add(TableOp1)

        '        Dim DTE2 As New DynamicTableEntity("company", "CompanyNumber_" + c.CompanyNumber + "_" + c.ID, "*", dict)
        '        Dim TableOp2 As TableOperation = TableOperation.InsertOrReplace(DTE2)
        '        batch.Add(TableOp2)

        '        Dim DTE3 As New DynamicTableEntity("company", "PostCode_" + c.PostCode + "_" + c.ID, "*", dict)
        '        Dim TableOp3 As TableOperation = TableOperation.InsertOrReplace(DTE3)
        '        batch.Add(TableOp3)


        '        CNT = CNT + 1
        '        If CNT = 1 Then

        '            lincnt = lincnt - CNT

        '            myTable.ExecuteBatch(batch)
        '            Console.WriteLine("Written " + CNT.ToString + " records in " + sw.ElapsedMilliseconds.ToString + "ms. " + lincnt.ToString + " remaining...")
        '            sw.Reset()
        '            sw.Start()

        '            batch = New TableBatchOperation
        '            CNT = 0
        '        End If

        '    Next
        '    '
        '    If CNT > 0 Then
        '        myTable.ExecuteBatch(batch)
        '        Console.WriteLine("Written remaining records")
        '    End If

        'End If

        'Console.WriteLine("Reading records")
        'sw.Reset()
        'sw.Start()
        '' if we read one other than the main key - we need to possibly get the etag of the main record OR
        '' keep the etag of the one we do get, then as long as in a batch, the WHOLE lot will fail if we pass the etag
        '' for that save or delete etc...

        'Dim TQ As New TableQuery(Of DynamicTableEntity)
        'Dim Qstr = "(PartitionKey eq 'main' and RowKey ge 'CompanyNumber_01339220_' and RowKey le 'CompanyNumber_01949320_z')"
        'TQ.Where(Qstr)

        'Dim Rets = myTable.ExecuteQuery(Of DynamicTableEntity)(TQ).ToList

        'Dim Objs As New List(Of Company)
        'For Each Ret In Rets
        '    Dim Obj = ObjectFlattenerRecomposer.EntityPropertyConverter.ConvertBack(Of Company)(Ret.Properties)
        '    'Obj.AzureTimestamp = Ret.Timestamp
        '    Objs.Add(Obj)
        'Next

        'sw.Stop()
        'Console.WriteLine("Read " + Objs.Count.ToString + " records in " + sw.ElapsedMilliseconds.ToString + "ms")
        'Console.ReadLine()

        ''Dim C1 As New Company With {.CompanyName = "aversa", .FirstName = "Nick", .LastName = "Denker", .Price = 1000}
        ''C1.Addresses = New List(Of Address)
        ''C1.Addresses.Add(New Address With {.Country = "UK"})

        ''Dim dict = TableEntity.Flatten(C1, New OperationContext)
        ''Dim dict = ObjectFlattenerRecomposer.EntityPropertyConverter.Flatten(C1)


        ''Dim DTE As New DynamicTableEntity("test", "test", "*", dict)
        ''Dim TableOp2 As TableOperation = TableOperation.InsertOrReplace(DTE)

        ''myTable.Execute(TableOp2)
        ''we could use above to change types say decimal to integer etc....
        ''and maybe make extended strings or byte arrays, which is all we really need
        ''then we don't need to use json and we can still use the built in azure query IF not indexed property


        ''Dim objectToWrite As New TableEntityAdapter(Of Company)(C1, "test2", "test2")

        ''Dim TableOp2 As TableOperation = TableOperation.InsertOrReplace(objectToWrite)

        ''myTable.Execute(TableOp2)



        ''get data
        '' Get Keys will project only the ID to minimise payload
        ''GetByPrimaryKey 
        ''GetByPrimaryKeys
        ''GetBySecondaryKey
        ''GetByTableQuery
        ''GetKeysBySecondaryKey
        ''(GetKeysBySecondaryKey("FirstName","Fred",Equals).Or.GetKeysBySecondaryKey("FirstName","John",Equals)).And.GetKeysBySecondaryKey("LastName","Jones",Equals).GetByPrimaryKey

        'Dim KC As New KeyCollection
        'KC.GetKeysBySecondaryKey.GetKeysBySecondaryKey()
    End Sub




    Class KeyCollection

        Function GetKeysBySecondaryKey() As KeyCollection

            Return New KeyCollection

        End Function

    End Class

    'A batch operation may contain up To 100 individual table operations, With the requirement that Each operation entity must have same partition key. 
    'A batch With a retrieve operation cannot contain any other operations. Note that the total payload Of a batch operation Is limited To 4MB.


    'https://blogs.msdn.microsoft.com/avkashchauhan/2011/11/30/how-the-size-of-an-entity-is-caclulated-in-windows-azure-table-storage/

    'While working With a partner, I had an opportunity To dig about how Azure Table storage size Is calculated With respect To entities. As you may know Each entity In Windows Azure Table Storage, can have maximum 1 MB space For Each individual entity instance. The following expressions shows how To estimate the amount Of storage consumed per entity:
    'Total Entity Size
    '4 bytes + Len (PartitionKey + RowKey) * 2 bytes + For-Each Property(8 bytes + Len(Property Name) * 2 bytes + Sizeof(.Net Property Type))

    'The following Is the breakdown
    '4 bytes overhead for each entity, which includes the Timestamp, along with some system metadata.
    'The number Of characters In the PartitionKey And RowKey values, which are stored As Unicode (times 2 bytes).
    'Then for each property we have an 8 byte overhead, plus the name of the property * 2 bytes, plus the size of the property type as derived from the list below.

    'The Sizeof(.Net Property Type) for the different types Is
    'String – # Of Characters * 2 bytes + 4 bytes For length Of String
    'DateTime – 8 bytes
    'GUID – 16 bytes
    'Double – 8 bytes
    'Int – 4 bytes
    'INT64 – 8 bytes
    'Bool – 1 byte
    'Binary – sizeof(value) in bytes + 4 bytes for length of binary array


    'store etag and current saved copy for an object alongside
    'be able to have a collection of objects OR a session.... 

    'attempt to batch as many saves as possible in one go? only downside is that one could fail and not all will wind back
    'to "Upgrade" to cosmos would be as simple as removing any indexes in this and should all still work using normal queries

End Module
