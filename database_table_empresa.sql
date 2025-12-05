USE [arkdbsisventas]
GO

CREATE TABLE Empresa (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Telefono NVARCHAR(50) NULL,
    TelefonoSecundario NVARCHAR(50) NULL,
    Direccion NVARCHAR(300) NULL,
    Email NVARCHAR(150) NULL,
    SitioWeb NVARCHAR(150) NULL,
    FechaRegistro DATETIME2 DEFAULT SYSDATETIME()
);
GO
