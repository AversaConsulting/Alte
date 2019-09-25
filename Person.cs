using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alte;

namespace AlteDemo
{
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
}
