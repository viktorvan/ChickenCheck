namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(202107291909L, "Initial")>]
type Initial() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE Chicken
                ( Id UUID PRIMARY KEY
                , Name TEXT NOT NULL
                , Breed TEXT NOT NULL
                , ImageUrl TEXT NULL
                , Created TIMESTAMP NOT NULL
                , LastModified TIMESTAMP NOT NULL
                , EndDate TIMESTAMP NULL
                );

            CREATE TABLE Egg
                ( ChickenId UUID NOT NULL
                , Date DATE NOT NULL
                , EggCount SMALLINT NOT NULL
                , Created TIMESTAMP NOT NULL
                , LastModified TIMESTAMP NOT NULL
                , FOREIGN KEY (ChickenId) REFERENCES Chicken(Id),
                UNIQUE (ChickenId, Date)
                );
	    ")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE Egg
        DROP TABLE Chicken")
