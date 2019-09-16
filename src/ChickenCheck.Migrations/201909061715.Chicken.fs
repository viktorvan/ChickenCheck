namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909061715L, "Create Chicken Table")>]
type CreateChickenTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [Chicken]
                ( Id UNIQUEIDENTIFIER NOT NULL
                , [Name] NVARCHAR(200) NOT NULL
                , Breed NVARCHAR(200) NOT NULL
                , ImageUrl NVARCHAR(1000) NULL
                , Created DateTime2(0) NOT NULL
                , LastModified DateTime2(0) NOT NULL
                )
            CREATE UNIQUE INDEX IX_Chicken_Id
                ON [Chicken] (Id)
            INSERT INTO Chicken
                (Id, [Name], Breed, ImageUrl, Created, LastModified)
                VALUES
                    ( 'b65e8809-06dd-4338-a55e-418837072c0f'
                    , 'Bjork'
                    , 'Skånsk blommehöna'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Bjork1.jpg'
                    , GETDATE()
                    , GETDATE()),

                    ( 'dedc5301-b404-49f4-8d5c-7bfac10c6950'
                    , 'Bodil'
                    , 'Skånsk blommehöna'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Bodil1.jpg'
                    , GETDATE()
                    , GETDATE()),

                    ( '309671cc-b23d-46db-bf3c-545a95d3f949'
                    , 'Siouxsie Sioux'
                    , 'Appenzeller'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Siouxsie1.jpg'
                    , GETDATE()
                    , GETDATE()),

                    ( '5287cec6-feb8-49c2-aef1-2dfe4d822366'
                    , 'Malin'
                    , 'Maran'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Malin1.jpg'
                    , GETDATE()
                    , GETDATE()),

                    ( 'ed5366d0-70f7-4e54-84a2-1281a23dafb6'
                    , 'Lina'
                    , 'Cream Legbar'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Lina1.jpg'
                    , GETDATE()
                    , GETDATE()),

                    ( '0103b370-3714-4c44-b5a9-a83dd0231b53'
                    , 'Vivianne'
                    , 'Wyandotte'
                    , 'https://chickencheck.z6.web.core.windows.net/Images/Vivianne1.jpg'
                    , GETDATE()
                    , GETDATE())
            ")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [Chicken]")