# alte
Tiny Embedded .NET NoSQL Database with cloud storage in Azure Table Storage with massive scaleability

Download from NuGet - search for ALTE
Download this project for demo in C#

View web site at www.alte.co.uk - or visit developer at www.aversa.co.uk

- .NET Objects are stored in Azure Table Storage
- Stores one property in one column on Azure for full compatibility and simplicity
- Can store Decimal/Currency type fields not normally supported in Table Storage
- Can store larger byte arrays over multiple columns automatically
- Properties can be indexed for lightning quick retreival
- Pass in an example object with an index property set to lookup objects via index
- Properties can be full text indexed
- Pass in a search phrase to search the full text index
- Optional additional query on other columns
- Easy to read automatic time sequential IDs - similar to GUIDs but more user friendly and shorter - or use several other ID types or roll your own
- Optimistic concurrency built in
- Simple blob storage utilities built in
- Other utilites included - Randomise lists, Re-indexing, Backup of Table and Blobs
- Includes an ASP.NET session provider - although work is still needed on this to clear old session data, it is fully working

- USES:
-- Multi-tenant apps
-- Large scale apps

- In use currently on a multi tennant e-commerce platforrm
- In use currently on large scale classified selling platform

EXAMPLES (See test project for more)

        Dim C As New Company(Session)
        
        C.CompanyNumber = "123456"
        C.CompanyName = "Aversa"
        C.AddressLine1 = "The Road"
        C.Town = "Stafford"
        C.Country = "UK"
        C.PostCode = "ST17 000"
        C.Notes = "Some sample text for searching"
        C.Category = "NewCustomer"
        C.Save()

        Dim C2 = Session.GetObjectByID(Of Company)(C.ID)
        Console.WriteLine("Company name : " + C2.CompanyName)

        Dim CS = Session.GetObjectsByIndex(Of Company)(New Company With {.CompanyNumber = "123456"})
        Console.WriteLine("Found " + CS.Count.ToString + " Companies By index")

        Dim FTRs = Session.GetFullTextResults(Of Company)("searching") ' returns a list of full text search result objects
        Dim CS2 = Session.GetObjectByFullTextResult(Of Company)(FTRs)
