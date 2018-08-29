Imports Alte

Module Module1

    Sub Main()

        Console.WriteLine("Creating Session")
        Dim Session As New AlteSession("azurestorageaccountname", "storageaccountsecret")

        Dim sw = New Stopwatch()
        sw.Start()
        Console.WriteLine("Creating Store")
        Session.CreateStore(Of Company)()
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Dim C As New Company(Session)
        C.CompanyNumber = "123456"
        C.CompanyName = "Aversa"
        C.AddressLine1 = "The Road"
        C.AddressLine2 = ""
        C.Town = "Stafford"
        C.Country = "UK"
        C.PostCode = "ST17 000"
        C.Price = -10
        C.ID = Guid.NewGuid.ToString


        Console.WriteLine("Saving Company")
        sw.Reset() : sw.Start()

        C.Save()
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Console.WriteLine("Getting Company")

        sw.Reset() : sw.Start()
        Dim C2 = Session.GetObjectByID(Of Company)(C.ID)
        sw.Stop()
        Console.WriteLine("Done in " + sw.ElapsedMilliseconds.ToString + "ms")

        Console.WriteLine("Company name : " + C2.CompanyName)

        Console.ReadLine()



    End Sub




End Module
