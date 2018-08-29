Imports Alte

Module Module1

    Sub Main()

        Dim ACC As String = GetSetting("AlteTest", "Database", "AzureName", "")
        Dim SEC As String = GetSetting("AlteTest", "Database", "AzureSecret", "")

        Console.WriteLine("Enter Azure Storage Account Name:" + (IIf(ACC = "", "", "Or press return to use " + ACC)))
        Dim ACC2 As String = Console.ReadLine

        If ACC2 <> "" Then
            ACC = ACC2
            SaveSetting("AlteTest", "Database", "AzureName", ACC)

            Console.WriteLine("Enter Azure Storage Account Secret:")
            SEC = Console.ReadLine
            SaveSetting("AlteTest", "Database", "AzureSecret", SEC)
        End If


        Console.WriteLine("Creating Session")
        Dim Session As New AlteSession(ACC, SEC)

        Console.WriteLine("Creating Store")
        Session.CreateStore(Of Company)()
        ' need to do this for any object type we are saving

        Session.CreateBlobContainer("pictures")
        ' for storing bigger binary data

        Dim C As New Company(Session)
        C.SetID(ID_Style.EasyBase30_12)
        ' set our ID to lots of different types - or set your own
        ' if no ID is set by SAVE time - we will create one for you, but if you
        ' have one here you can use in related data straight away

        C.CompanyNumber = "123456"
        C.CompanyName = "Aversa"
        C.AddressLine1 = "The Road"
        C.AddressLine2 = ""
        C.Town = "Stafford"
        C.Country = "UK"
        C.PostCode = "ST17 000"
        C.Notes = "Some sample text for searching"
        C.Category = "NewCustomer"


        Console.WriteLine("Saving Company")

        C.Save()

        Console.WriteLine("Getting Company By ID")
        Dim C2 = Session.GetObjectByID(Of Company)(C.ID)
        Console.WriteLine("Company name : " + C2.CompanyName)


        Console.WriteLine("Getting Companies By index")
        Dim CS = Session.GetObjectsByIndex(Of Company)(New Company With {.CompanyNumber = "123456"})
        Console.WriteLine("Found " + CS.Count.ToString + " Companies By index")


        Console.WriteLine("Getting Companies By index and extra query")
        Dim CS1 = Session.GetObjectsByIndex(Of Company)(New Company With {.CompanyNumber = "123456"},,, "companyname eq 'Aversa'")
        '(table storage style query using eq etc..., lower case only for property names!)

        Console.WriteLine("Found " + CS.Count.ToString + " Companies By index")


        Console.WriteLine("Getting Companies By Full Text Index")
        Dim FTRs = Session.GetFullTextResults(Of Company)("searching") ' returns a list of full text search result objects


        ' Full text results include the ID of the records and any properties saved IN the index
        For Each FTR In FTRs
            Console.WriteLine("Found ID " + FTR.ID)
            Console.WriteLine("Propery Saved With Index (category) is " + FTR.Properties("category")) ' LOWER CASE PROPERTY NAME HERE
        Next


        ' Now get the actual records - IF WE NEED TO - we may have stored what we need in the index..
        Dim CS2 = Session.GetObjectByFullTextResult(Of Company)(FTRs)
        Console.WriteLine("Found " + CS2.Count.ToString + " Companies By full text index")


        Console.ReadLine()



    End Sub




End Module
