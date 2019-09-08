CREATE TABLE [User]
(
    Id UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    Email NVARCHAR(1000) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Salt NVARCHAR(MAX) NOT NULL,
    Created DateTime2(0) NOT NULL,
    LastModified DateTime2(0) NOT NULL
)

CREATE TABLE Customers
(
    Id UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    Email NVARCHAR(1000) NULL,
    SellerId UNIQUEIDENTIFIER NOT NULL,
    Created DateTime2(0) NOT NULL,
    LastModified DateTime2(0) NOT NULL,
    Deleted DateTime2(0) NULL
)

CREATE TABLE Products
(
    Id UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Type] nvarchar(100) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    HarvestDate DATE NULL,
    Created DateTime2(0) NOT NULL,
    LastModified DateTime2(0) NOT NULL,
    Deleted DateTime2(0) NULL
)