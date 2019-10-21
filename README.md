# alte
Tiny Embedded .NET NoSQL Style Database with cloud storage in Azure Table Storage with massive scaleability

Download from NuGet - search for ALTE

Download this project for demo in C#

View web site at www.alte.co.uk - or visit developer at www.aversa.co.uk

- .NET Objects are stored in Azure Table Storage
- Stores one property in one column on Azure for full compatibility and simplicity
- Can store Decimal/Currency/TimeSpan etc. type fields not normally supported in Table Storage
- Can store larger byte arrays or strings over multiple columns automatically
- Any number of properties can be indexed for lightning quick retreival
- Simple strongly typed fluent query syntax - easier than LINQ - then use client LINQ after!
- Authomatically uses an index on the query and will warn you if you're not using an index in the output window
- Tables (Objects) can be full text indexed
- Pass in a search phrase to search the full text index
- Easy to read automatic time sequential IDs - similar to GUIDs but more user friendly and shorter - or use several other ID types or roll your own
- Optimistic concurrency built in
- Simple and easy to use blob storage utilities built in
- Other utilites included - Randomise lists, Re-indexing, Backup of Table and Blobs
- Includes an ASP.NET session provider - although work is still needed on this to clear old session data, it is fully working
- Multi-tenant database connection methods allow more than one "database" per table store

- USES:
-- Multi-tenant apps
-- Large scale apps

- In use on our multi tennant e-commerce platform - e.g. www.woodstyle.co.uk
- In use currently on large scale classified selling platform - e.g. www.advertisedelsewhere.co.uk
- In use on hotel booking systems - e.g. www.hengardirect.co.uk

See how easy it is to get going!


        static void Main(string[] args)
                {

                    Console.WriteLine("Open our database");

                    var DB = new Alte.AlteSession("altedemo", "2BQX+KxHC7KbEX5Dd3wFOTV+HHsvmpnsaSJ2/H1Evd7i41tScEP1VFuvB/gd+Jg4iaPfwlhyl2cNhtxip1fNhA==", "mydemo");
                    // we are creating a connection to altedemo azure storage account. V1 or V2 is fine, V2 can be cheaper. The last parameter allows to to prefix
                    // our tables to in all essence have multiple databases within one storage account. This is great for multi tenant apps - but obviously you
                    // will load the account more this way. 

                    DB.CreateStore<Person>();
                    // this isn't 100% necessary but good practice

                    DB.RebuildIndexes<Person>();
                    // if you change object structure - ESPECIALLY indexes - run this to re-index the table


                    Person newperson;

                    Console.WriteLine("Try to load person record with ID DEMO");

                    newperson = DB.GetByID<Person>("DEMO");
                    //try and load our person from the database with ID "DEMO"

                    if (newperson is null)
                    {
                        Console.WriteLine("We haven't got a record with ID DEMO, so lets create and save");

                        newperson = new Person(DB);
                        newperson.ID = "DEMO"; //normally leave this or use one of the standard options - but for this demo we can use later

                    }
                    else
                    {
                        Console.WriteLine("We got our record!");
                    }

                    Console.WriteLine("Changing Properties");

                    newperson.Email = "testbod@alte.co.uk";
                    newperson.FirstName = "Jack";
                    newperson.LastName = "Whitfield";
                    newperson.Title = "Mr";
                    newperson.ProfileNotes = "I am a very proficient .NET programmer - more VB than C#, but pretty good at HTML and JavaScript too!";
                    newperson.HourlyRate = 50.55M;
                    newperson.ServiceLength = new TimeSpan(365, 0, 0, 0, 0);
                    newperson.PersonType = PersonTypes.Freelancer;
                    newperson.Save();


                    //now get record by various methods!

                    //individual record by ID - will return the record or null
                    var p1 = DB.GetByID<Person>("DEMO");

                    //query of records by property name - returns a list (may be empty!). Will automatically use an indexed property
                    //if available, else will fall back to Azure Table queries
                    var p2_list = DB.Query<Person>().Where(nameof(Person.Email), QueryOperand.Equal, "testbod@alte.co.uk").Result();

                    //we can get more complicated
                    var p3_list = DB.Query<Person>().Where(nameof(Person.Email), QueryOperand.Equal, "testbod@alte.co.uk").Or(nameof(Person.LastName), "Whitfield").Result();

                    //we are using nameof to keep it type safe - but you can just use the field name directly as a string
                    //and by default the query is an equals
                    var p4_list = DB.Query<Person>().Where("Email", "testbod@alte.co.uk").Result();

                    //make a change in our list
                    p4_list[0].LastName = "Whittlefields";
                    p4_list[0].Save();


                    //query by full text. Full text indexes can also store specified fields if you'd use them to save retrieving the full item
                    //**NOTE** that the properties saved in the index are LOWER CASE regardless of case in the object
                    List<FullTextResult> i1_list = DB.GetFullTextResults<Person>("html");
                    Console.WriteLine("Got FTS results from FTS :" + i1_list[0].ID + " - " + i1_list[0].Properties["email"]);

                    //get the full object results from the full text results
                    var p5_list = DB.GetByFullTextResult<Person>(i1_list);
                    Console.WriteLine("Got record from FTS list :" + p5_list[0].ID + " - " + p5_list[0].LastName);

                    Console.WriteLine("Press return to exit");
                    Console.ReadLine();
                }
            }


        class Person : Alte.AlteObject
        {
                public Person() { }

                public Person(Alte.AlteSession alteSession) : base(alteSession)
                { }

                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string Title { get; set; }

                //normal index, but store with the full text search
                [Alte.Index(), Alte.FTSstore()]
                public string Email { get; set; }

                //full text search index
                [Alte.FTSindex()]
                public string ProfileNotes { get; set; }

                //DateTime is dealt with properly meaning a minvalue (or null) is correctly stored and returned
                public DateTime DateOfBirth { get; set; }

                //Types not supported normally in Azure Tables
                public decimal HourlyRate { get; set; } // Stored internally as integer / 10000
                public TimeSpan ServiceLength { get; set; } // Stored internally as ticks
                public List<string> Skills {get;set;} // lists stored internally as JSON string
                public PersonTypes PersonType { get; set; }

                }

            enum PersonTypes
            {
                Customer,
                Freelancer
            }
