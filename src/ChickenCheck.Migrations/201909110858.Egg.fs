namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909090858L, "Create Egg Table")>]
type CreateEggTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [Egg]
            ( ChickenId UNIQUEIDENTIFIER NOT NULL
            , Date Date NOT NULL
            , EggCount INT NOT NULL
            , Created DateTime2(0) NOT NULL
            , LastModified DateTime2(0) NOT NULL
            , FOREIGN KEY (ChickenId) REFERENCES Chicken(Id)
            )")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Egg]")