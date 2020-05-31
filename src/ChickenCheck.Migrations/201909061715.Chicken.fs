namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909061715L, "Create Chicken Table")>]
type CreateChickenTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [Chicken]
                ( Id TEXT PRIMARY KEY
                , [Name] TEXT NOT NULL
                , Breed TEXT NOT NULL
                , ImageUrl TEXT NULL
                , Created TEXT NOT NULL
                , LastModified TEXT NOT NULL
                ) WITHOUT ROWID;
            INSERT INTO Chicken
                (Id, [Name], Breed, ImageUrl, Created, LastModified)
                VALUES
                    ( 'b65e8809-06dd-4338-a55e-418837072c0f'
                    , 'Bjork'
                    , 'Skånsk blommehöna'
                    , '/Images/Bjork1.jpg'
                    , date('now')
                    , date('now')),

                    ( 'dedc5301-b404-49f4-8d5c-7bfac10c6950'
                    , 'Bodil'
                    , 'Skånsk blommehöna'
                    , '/Images/Bodil1.jpg'
                    , date('now')
                    , date('now')),

                    ( '309671cc-b23d-46db-bf3c-545a95d3f949'
                    , 'Siouxsie Sioux'
                    , 'Appenzeller'
                    , '/Images/Siouxsie1.jpg'
                    , date('now')
                    , date('now')),

                    ( '5287cec6-feb8-49c2-aef1-2dfe4d822366'
                    , 'Malin'
                    , 'Maran'
                    , '/Images/Malin1.jpg'
                    , date('now')
                    , date('now')),

                    ( 'ed5366d0-70f7-4e54-84a2-1281a23dafb6'
                    , 'Lina'
                    , 'Cream Legbar'
                    , '/Images/Lina1.jpg'
                    , date('now')
                    , date('now')),

                    ( '0103b370-3714-4c44-b5a9-a83dd0231b53'
                    , 'Vivianne'
                    , 'Wyandotte'
                    , '/Images/Vivianne1.jpg'
                    , date('now')
                    , date('now'))
            ")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Chicken]")