namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(202012012224L, "Add new chickens")>]
type Add2020Chickens() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            INSERT INTO Chicken
                (Id, [Name], Breed, ImageUrl, Created, LastModified)
                VALUES
                    ( '71aaad83-257e-49bb-8044-7d2b34cc14dd'
                    , 'Rut-Knäcke'
                    , 'Hedemora'
                    , '/Images/RutKnacke1.jpg'
                    , date('now')
                    , date('now')),

                    ( '935a0c8c-468e-4963-8fef-cc3c1085ed2d'
                    , 'Baggle'
                    , 'Hedemora'
                    , '/Images/Baggle1.jpg'
                    , date('now')
                    , date('now')),

                    ( 'f7848e0b-033f-49f3-8452-c09df509c6ef'
                    , 'Frallan'
                    , 'Hedemora'
                    , '/Images/Frallan1.jpg'
                    , date('now')
                    , date('now')),

                    ( 'c44e8e7e-c2ee-4a66-a1c8-856698ad7916'
                    , 'Mai'
                    , 'Maran'
                    , '/Images/Mai1.jpg'
                    , date('now')
                    , date('now'))
            ")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Chicken]")
