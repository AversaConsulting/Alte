using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alte;

namespace AlteDemo
{
    class Program
    {
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
}
