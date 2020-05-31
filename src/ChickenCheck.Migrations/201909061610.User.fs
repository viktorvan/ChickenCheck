namespace ChickenCheck.Migrations
open SimpleMigrations

[<Migration(201909061610L, "Create User Table")>]
type CreateUserTable() =
    inherit Migration()

    override __.Up() =
        base.Execute(@"
            CREATE TABLE User
            ( Id TEXT PRIMARY KEY
            , Name TEXT NOT NULL
            , Email TEXT NOT NULL
            , PasswordHash TEXT NOT NULL
            , Salt TEXT NOT NULL
            , Created TEXT NOT NULL
            , LastModified TEXT NOT NULL
            ) WITHOUT ROWID;
            CREATE UNIQUE INDEX IX_User_Id
                ON User (Email);")
        
    override __.Down() =
        base.Execute(@"
        DROP TABLE User")