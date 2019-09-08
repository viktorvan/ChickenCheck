namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909061715L, "Create Chicken Table")>]
type CreateChickenTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [Chicken]
            (
                Id UNIQUEIDENTIFIER NOT NULL,
                [Name] NVARCHAR(200) NOT NULL,
                Breed NVARCHAR(200) NOT NULL,
                ImageUrl NVARCHAR(1000) NULL,
                Created DateTime2(0) NOT NULL,
                LastModified DateTime2(0) NOT NULL
            )
            CREATE UNIQUE INDEX IX_Chicken_Id
                ON [Chicken] (Id)")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Chicken]")