# alte
Tiny Embedded .NET NoSQL Database with cloud storage in Azure Table Storage with massive scaleability

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

- USES:
-- Small local apps
-- Multi-tenant apps
-- Large scale apps

- In use currently on a multi tennant e-commerce platforrm
- In use currently on large scale classified selling platform
