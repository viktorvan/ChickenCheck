namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909090858L, "Create Egg Table")>]
type CreateEggTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [Egg]
            ( ChickenId TEXT NOT NULL
            , Date TEXT NOT NULL
            , EggCount INTEGER NOT NULL
            , Created TEXT NOT NULL
            , LastModified TEXT NOT NULL
            , FOREIGN KEY (ChickenId) REFERENCES Chicken(Id)
            )")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Egg]")