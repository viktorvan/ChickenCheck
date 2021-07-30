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
                );
				
            INSERT INTO Chicken
                (Id, Name, Breed, ImageUrl, Created, LastModified)
                VALUES
                    ( 'b65e8809-06dd-4338-a55e-418837072c0f'
                    , 'Bjork'
                    , 'Skånsk blommehöna'
                    , '/Images/Bjork1.jpg'
                    , NOW()
                    , NOW()),

                    ( 'dedc5301-b404-49f4-8d5c-7bfac10c6950'
                    , 'Bodil'
                    , 'Skånsk blommehöna'
                    , '/Images/Bodil1.jpg'
                    , NOW()
                    , NOW()),

                    ( '309671cc-b23d-46db-bf3c-545a95d3f949'
                    , 'Siouxsie Sioux'
                    , 'Appenzeller'
                    , '/Images/Siouxsie1.jpg'
                    , NOW()
                    , NOW()),

                    ( '5287cec6-feb8-49c2-aef1-2dfe4d822366'
                    , 'Malin'
                    , 'Maran'
                    , '/Images/Malin1.jpg'
                    , NOW()
                    , NOW()),

                    ( 'ed5366d0-70f7-4e54-84a2-1281a23dafb6'
                    , 'Lina'
                    , 'Cream Legbar'
                    , '/Images/Lina1.jpg'
                    , NOW()
                    , NOW()),

                    ( '0103b370-3714-4c44-b5a9-a83dd0231b53'
                    , 'Vivianne'
                    , 'Wyandotte'
                    , '/Images/Vivianne1.jpg'
                    , NOW()
                    , NOW()),

                    ( '71aaad83-257e-49bb-8044-7d2b34cc14dd'
                    , 'Rut-Knäcke'
                    , 'Hedemora'
                    , '/Images/RutKnacke1.jpg'
                    , NOW()
                    , NOW()),

                    ( '935a0c8c-468e-4963-8fef-cc3c1085ed2d'
                    , 'Baggle'
                    , 'Hedemora'
                    , '/Images/Baggle1.jpg'
                    , NOW()
                    , NOW()),

                    ( 'f7848e0b-033f-49f3-8452-c09df509c6ef'
                    , 'Frallan'
                    , 'Hedemora'
                    , '/Images/Frallan1.jpg'
                    , NOW()
                    , NOW()),

                    ( 'c44e8e7e-c2ee-4a66-a1c8-856698ad7916'
                    , 'Mai'
                    , 'Maran'
                    , '/Images/Mai1.jpg'
                    , NOW()
                    , NOW());

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
