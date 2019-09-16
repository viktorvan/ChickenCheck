namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909061610L, "Create User Table")>]
type CreateUserTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE [User]
            ( Id UNIQUEIDENTIFIER NOT NULL
            , [Name] NVARCHAR(200) NOT NULL
            , Email NVARCHAR(1000) NOT NULL
            , PasswordHash NVARCHAR(MAX) NOT NULL
            , Salt NVARCHAR(MAX) NOT NULL
            , Created DateTime2(0) NOT NULL
            , LastModified DateTime2(0) NOT NULL
            )
            CREATE UNIQUE INDEX IX_User_Id
                ON [User] (Id)
                INCLUDE (Email)")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE [User]")