CREATE DATABASE [arkdbsisventas]
GO
USE [arkdbsisventas]
GO

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreCompleto NVARCHAR(100) NOT NULL,
    CI NVARCHAR(20) UNIQUE NULL,
    Email NVARCHAR(100) UNIQUE NULL,
    Telefono NVARCHAR(20) NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FechaRegistro DATETIME2 DEFAULT GETDATE()
)
GO

CREATE TABLE Clientes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(20) NULL,
    CI NVARCHAR(20) NULL,
    Direccion NVARCHAR(200) NULL,
    Notas NVARCHAR(300) NULL,
    FechaRegistro DATETIME2 DEFAULT GETDATE()
)
GO

CREATE TABLE Categorias (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL UNIQUE
)
GO

CREATE TABLE Proveedores (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    RUC NVARCHAR(20) NULL,
    Telefono NVARCHAR(20) NULL,
    Direccion NVARCHAR(200) NULL,
    Email NVARCHAR(100) NULL,
    Contacto NVARCHAR(100) NULL,
    Notas NVARCHAR(300) NULL,
    FechaRegistro DATETIME2 DEFAULT GETDATE()
)
GO

CREATE TABLE Productos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(50) NOT NULL UNIQUE,
    Nombre NVARCHAR(150) NOT NULL,
    CategoriaId INT NULL,
    Talla NVARCHAR(20) NULL,
    Color NVARCHAR(30) NULL,
    PrecioCompra DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    PrecioVenta DECIMAL(10,2) NOT NULL,
    Stock DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    UnidadMedida NVARCHAR(20) DEFAULT 'Unidad',
    StockMinimo DECIMAL(10,2) NOT NULL DEFAULT 5.00,
    FechaRegistro DATETIME2 DEFAULT GETDATE(),
    Activo BIT DEFAULT 1,
    CONSTRAINT FK_Productos_Categoria FOREIGN KEY (CategoriaId)
        REFERENCES Categorias(Id) ON DELETE SET NULL
)
GO

CREATE TABLE Ventas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATETIME2 DEFAULT GETDATE(),
    UsuarioId INT NOT NULL,
    ClienteId INT NULL,
    Total DECIMAL(10,2) NOT NULL,
    DescuentoPorcentaje DECIMAL(5,2) NULL DEFAULT 0.00,
    DescuentoMonto DECIMAL(10,2) NULL DEFAULT 0.00,
    EfectivoRecibido DECIMAL(10,2) NULL,
    Cambio DECIMAL(10,2) NULL,
    Estado NVARCHAR(20) DEFAULT 'Completada',
    TipoPago NVARCHAR(20) DEFAULT 'Efectivo',
    CONSTRAINT FK_Ventas_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_Ventas_Cliente FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
)
GO

CREATE TABLE VentasDetalle (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VentaId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad DECIMAL(10,2) NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    DescuentoPorcentaje DECIMAL(5,2) NULL DEFAULT 0.00,
    DescuentoMonto DECIMAL(10,2) NULL DEFAULT 0.00,
    Subtotal DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_VentasDetalle_Venta FOREIGN KEY (VentaId)
        REFERENCES Ventas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_VentasDetalle_Producto FOREIGN KEY (ProductoId)
        REFERENCES Productos(Id)
)
GO

CREATE TABLE Compras (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATETIME2 DEFAULT GETDATE(),
    ProveedorId INT NULL,
    Total DECIMAL(10,2) NOT NULL,
    UsuarioId INT NOT NULL,
    Estado NVARCHAR(20) DEFAULT 'Completada',
    CONSTRAINT FK_Compras_Proveedor FOREIGN KEY (ProveedorId)
        REFERENCES Proveedores(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Compras_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
)
GO

CREATE TABLE ComprasDetalle (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompraId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad DECIMAL(10,2) NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_ComprasDetalle_Compra FOREIGN KEY (CompraId)
        REFERENCES Compras(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ComprasDetalle_Producto FOREIGN KEY (ProductoId)
        REFERENCES Productos(Id)
)
GO

CREATE TABLE InventarioAjustes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UsuarioId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad DECIMAL(10,2) NOT NULL,
    Motivo NVARCHAR(300) NOT NULL,

    CONSTRAINT CK_Ajustes_Cantidad_NoCero CHECK (Cantidad <> 0),

    CONSTRAINT FK_Ajustes_Usuario FOREIGN KEY (UsuarioId)
        REFERENCES Usuarios(Id),

    CONSTRAINT FK_Ajustes_Producto FOREIGN KEY (ProductoId)
        REFERENCES Productos(Id)
);
GO

INSERT INTO Categorias (Nombre) VALUES
('Bebidas'),('Gaseosas'),('Jugos y Néctares'),('Aguas Minerales'),('Energizantes'),
('Cervezas'),('Licores'),('Vinos'),('Dulces y Chocolates'),('Galletas'),
('Snacks'),('Papas Fritas'),('Chifles y Plátanos'),('Chicles y Caramelos'),('Helados'),
('Lácteos'),('Leche'),('Yogures'),('Quesos'),('Mantequilla y Margarina'),
('Huevos'),('Panadería'),('Panes'),('Tortillas'),('Pasteles y Tortas'),
('Abarrotes'),('Arroz'),('Fideos y Pastas'),('Aceites'),('Azúcar y Endulzantes'),
('Sal'),('Café'),('Té e Infusiones'),('Conservas'),('Enlatados'),
('Sopas Instantáneas'),('Salsas y Condimentos'),('Especias'),('Harinas'),('Cereales'),
('Limpieza'),('Detergentes'),('Jabones'),('Suavizantes'),('Desinfectantes'),
('Papel Higiénico'),('Toallas de Cocina'),('Servilletas'),('Higiene Personal'),
('Shampoo'),('Jabón de Baño'),('Pasta Dental'),('Desodorantes'),('Toallas Higiénicas'),
('Pañales'),('Medicinas Básicas'),('Analgésicos'),('Vitaminas'),
('Cuidado del Cabello'),('Cuidado de la Piel'),('Mascotas'),('Alimento para Perros'),
('Alimento para Gatos'),('Accesorios Mascotas'),('Ferretería Básica'),('Pilas y Baterías'),
('Bombillos'),('Cintas Adhesivas'),('Útiles Escolares'),('Cuadernos'),('Lapiceros'),
('Borradores'),('Juguetes'),('Ropa Interior'),('Medias'),('Gorras'),
('Celulares y Accesorios'),('Cargadores'),('Audífonos'),('Fundas de Celular'),
('Recargas'),('Otros'),('Sin Categoría')
GO

CREATE INDEX IX_Productos_Codigo ON Productos(Codigo ASC)
CREATE INDEX IX_Productos_Nombre ON Productos(Nombre ASC)
CREATE INDEX IX_Productos_Categoria ON Productos(CategoriaId)
CREATE INDEX IX_Productos_Talla ON Productos(Talla) WHERE Talla IS NOT NULL
CREATE INDEX IX_Productos_Color ON Productos(Color) WHERE Color IS NOT NULL

CREATE INDEX IX_Ventas_Fecha ON Ventas(Fecha DESC)
CREATE INDEX IX_Ventas_Usuario ON Ventas(UsuarioId)
CREATE INDEX IX_Ventas_Cliente ON Ventas(ClienteId)
CREATE INDEX IX_VentasDetalle_Venta ON VentasDetalle(VentaId)
CREATE INDEX IX_VentasDetalle_Producto ON VentasDetalle(ProductoId)
CREATE INDEX IX_Compras_Fecha ON Compras(Fecha DESC)
CREATE INDEX IX_Compras_Proveedor ON Compras(ProveedorId)
CREATE INDEX IX_Clientes_Nombre ON Clientes(Nombre)
CREATE INDEX IX_Clientes_Telefono ON Clientes(Telefono)
GO

CREATE INDEX IX_Ajustes_Producto ON InventarioAjustes(ProductoId);
CREATE INDEX IX_Ajustes_Usuario ON InventarioAjustes(UsuarioId);
GO

---ejecucion manual

CREATE TRIGGER TR_ActualizarStock_Ajuste
ON InventarioAjustes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM Productos p
        INNER JOIN inserted i ON p.Id = i.ProductoId
        WHERE p.Stock + i.Cantidad < 0
    )
    BEGIN
        RAISERROR ('El ajuste generaría stock negativo. Operación cancelada.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
    UPDATE p
    SET p.Stock = p.Stock + i.Cantidad
    FROM Productos p
    INNER JOIN inserted i ON p.Id = i.ProductoId;

END;
GO



CREATE OR ALTER PROCEDURE sp_RegistrarCompra
    @UsuarioId INT,
    @ProveedorId INT = NULL,
    @Productos NVARCHAR(MAX),
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT,
    @CompraId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @CompraId = NULL;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Id = @UsuarioId)
        BEGIN
            SET @Mensaje = 'El usuario no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF @ProveedorId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = @ProveedorId)
        BEGIN
            SET @Mensaje = 'El proveedor no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF @Productos IS NULL OR @Productos = ''
        BEGIN
            SET @Mensaje = 'Debe enviar productos';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        DECLARE @TempProductos TABLE (
            Codigo NVARCHAR(50),
            Nombre NVARCHAR(150),
            CategoriaId INT,
            Talla NVARCHAR(20),
            Color NVARCHAR(30),
            PrecioCompra DECIMAL(10,2),
            PrecioVenta DECIMAL(10,2),
            Cantidad DECIMAL(10,2),
            UnidadMedida NVARCHAR(20),
            StockMinimo DECIMAL(10,2),
            ProductoExiste BIT DEFAULT 0,
            ProductoId INT NULL
        );
        INSERT INTO @TempProductos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Cantidad, UnidadMedida, StockMinimo)
        SELECT
            Codigo,
            Nombre,
            CategoriaId,
            Talla,
            Color,
            ISNULL(PrecioCompra, 0.00),
            PrecioVenta,
            Cantidad,
            ISNULL(UnidadMedida, 'Unidad'),
            ISNULL(StockMinimo, 5.00)
        FROM OPENJSON(@Productos)
        WITH (
            Codigo NVARCHAR(50) '$.Codigo',
            Nombre NVARCHAR(150) '$.Nombre',
            CategoriaId INT '$.CategoriaId',
            Talla NVARCHAR(20) '$.Talla',
            Color NVARCHAR(30) '$.Color',
            PrecioCompra DECIMAL(10,2) '$.PrecioCompra',
            PrecioVenta DECIMAL(10,2) '$.PrecioVenta',
            Cantidad DECIMAL(10,2) '$.Cantidad',
            UnidadMedida NVARCHAR(20) '$.UnidadMedida',
            StockMinimo DECIMAL(10,2) '$.StockMinimo'
        );
        IF NOT EXISTS (SELECT 1 FROM @TempProductos)
        BEGIN
            SET @Mensaje = 'No se procesaron productos del JSON';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM @TempProductos WHERE Codigo IS NULL OR Codigo = '')
        BEGIN
            SET @Mensaje = 'El Codigo es obligatorio';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM @TempProductos WHERE Nombre IS NULL OR Nombre = '')
        BEGIN
            SET @Mensaje = 'El Nombre es obligatorio';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM @TempProductos WHERE PrecioVenta IS NULL OR PrecioVenta <= 0)
        BEGIN
            SET @Mensaje = 'El PrecioVenta debe ser mayor a 0';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM @TempProductos WHERE Cantidad IS NULL OR Cantidad <= 0)
        BEGIN
            SET @Mensaje = 'La Cantidad debe ser mayor a 0';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (
            SELECT 1 FROM @TempProductos t
            WHERE t.CategoriaId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM Categorias WHERE Id = t.CategoriaId)
        )
        BEGIN
            SET @Mensaje = 'Una categoria no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END
        UPDATE t
        SET t.ProductoExiste = 1,
            t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo;
        IF EXISTS (SELECT 1 FROM @TempProductos WHERE ProductoExiste = 1)
        BEGIN
            DECLARE @CodigosExistentes NVARCHAR(500);
            SELECT @CodigosExistentes = STRING_AGG(Codigo, ', ')
            FROM @TempProductos
            WHERE ProductoExiste = 1;
            SET @Mensaje = 'Productos ya existen: ' + @CodigosExistentes;
            ROLLBACK TRANSACTION;
            RETURN;
        END
        INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo, Activo, FechaRegistro)
        SELECT
            t.Codigo,
            t.Nombre,
            t.CategoriaId,
            t.Talla,
            t.Color,
            t.PrecioCompra,
            t.PrecioVenta,
            0.00,
            t.UnidadMedida,
            t.StockMinimo,
            1,
            GETDATE()
        FROM @TempProductos t;
        UPDATE t
        SET t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo;
        DECLARE @TotalCompra DECIMAL(10,2);
        SELECT @TotalCompra = SUM(Cantidad * PrecioCompra)
        FROM @TempProductos;
        INSERT INTO Compras (Fecha, ProveedorId, Total, UsuarioId, Estado)
        VALUES (GETDATE(), @ProveedorId, @TotalCompra, @UsuarioId, 'Completada');
        SET @CompraId = SCOPE_IDENTITY();
        INSERT INTO ComprasDetalle (CompraId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT
            @CompraId,
            ProductoId,
            Cantidad,
            PrecioCompra,
            Cantidad * PrecioCompra
        FROM @TempProductos;
        UPDATE p
        SET p.Stock = p.Stock + t.Cantidad
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId;
        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Compra registrada ID: ' + CAST(@CompraId AS NVARCHAR(10));
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = 'Error: ' + ERROR_MESSAGE();
        SET @CompraId = NULL;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_EditarCompra
    @CompraId INT,
    @ProveedorId INT = NULL,
    @Productos NVARCHAR(MAX),
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM Compras WHERE Id = @CompraId)
        BEGIN
            SET @Mensaje = 'La compra no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @ProveedorId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = @ProveedorId)
        BEGIN
            SET @Mensaje = 'El proveedor no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Productos IS NULL OR @Productos = ''
        BEGIN
            SET @Mensaje = 'Debe enviar productos';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        DECLARE @TempProductos TABLE (
            ProductoId INT,
            Nombre NVARCHAR(150),
            CategoriaId INT,
            Talla NVARCHAR(20),
            Color NVARCHAR(30),
            PrecioCompra DECIMAL(10,2),
            PrecioVenta DECIMAL(10,2),
            Cantidad DECIMAL(10,2),
            UnidadMedida NVARCHAR(20),
            StockMinimo DECIMAL(10,2),
            CantidadAnterior DECIMAL(10,2) NULL
        );

        INSERT INTO @TempProductos (ProductoId, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Cantidad, UnidadMedida, StockMinimo)
        SELECT
            ProductoId,
            Nombre,
            CategoriaId,
            Talla,
            Color,
            ISNULL(PrecioCompra, 0.00),
            PrecioVenta,
            Cantidad,
            ISNULL(UnidadMedida, 'Unidad'),
            ISNULL(StockMinimo, 5.00)
        FROM OPENJSON(@Productos)
        WITH (
            ProductoId INT '$.ProductoId',
            Nombre NVARCHAR(150) '$.Nombre',
            CategoriaId INT '$.CategoriaId',
            Talla NVARCHAR(20) '$.Talla',
            Color NVARCHAR(30) '$.Color',
            PrecioCompra DECIMAL(10,2) '$.PrecioCompra',
            PrecioVenta DECIMAL(10,2) '$.PrecioVenta',
            Cantidad DECIMAL(10,2) '$.Cantidad',
            UnidadMedida NVARCHAR(20) '$.UnidadMedida',
            StockMinimo DECIMAL(10,2) '$.StockMinimo'
        );

        IF NOT EXISTS (SELECT 1 FROM @TempProductos)
        BEGIN
            SET @Mensaje = 'No se procesaron productos del JSON';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM @TempProductos WHERE ProductoId IS NULL OR ProductoId <= 0)
        BEGIN
            SET @Mensaje = 'El ProductoId es obligatorio';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM @TempProductos WHERE Nombre IS NULL OR Nombre = '')
        BEGIN
            SET @Mensaje = 'El Nombre es obligatorio';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM @TempProductos WHERE PrecioVenta IS NULL OR PrecioVenta <= 0)
        BEGIN
            SET @Mensaje = 'El PrecioVenta debe ser mayor a 0';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM @TempProductos WHERE Cantidad IS NULL OR Cantidad <= 0)
        BEGIN
            SET @Mensaje = 'La Cantidad debe ser mayor a 0';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (
            SELECT 1 FROM @TempProductos t
            WHERE NOT EXISTS (SELECT 1 FROM Productos WHERE Id = t.ProductoId)
        )
        BEGIN
            SET @Mensaje = 'Uno o mas productos no existen';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (
            SELECT 1 FROM @TempProductos t
            WHERE t.CategoriaId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM Categorias WHERE Id = t.CategoriaId)
        )
        BEGIN
            SET @Mensaje = 'Una categoria no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE t
        SET t.CantidadAnterior = cd.Cantidad
        FROM @TempProductos t
        INNER JOIN ComprasDetalle cd ON cd.ProductoId = t.ProductoId AND cd.CompraId = @CompraId;

        UPDATE p
        SET p.Stock = p.Stock - t.CantidadAnterior
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId
        WHERE t.CantidadAnterior IS NOT NULL;

        UPDATE p
        SET
            p.Nombre = t.Nombre,
            p.CategoriaId = t.CategoriaId,
            p.Talla = t.Talla,
            p.Color = t.Color,
            p.PrecioCompra = t.PrecioCompra,
            p.PrecioVenta = t.PrecioVenta,
            p.UnidadMedida = t.UnidadMedida,
            p.StockMinimo = t.StockMinimo
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId;

        UPDATE p
        SET p.Stock = p.Stock + t.Cantidad
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId;

        UPDATE c
        SET c.ProveedorId = @ProveedorId
        FROM Compras c
        WHERE c.Id = @CompraId;

        DECLARE @NuevoTotal DECIMAL(10,2);
        SELECT @NuevoTotal = SUM(Cantidad * PrecioCompra)
        FROM @TempProductos;

        UPDATE c
        SET c.Total = @NuevoTotal
        FROM Compras c
        WHERE c.Id = @CompraId;

        UPDATE cd
        SET
            cd.Cantidad = t.Cantidad,
            cd.PrecioUnitario = t.PrecioCompra,
            cd.Subtotal = t.Cantidad * t.PrecioCompra
        FROM ComprasDetalle cd
        INNER JOIN @TempProductos t ON cd.ProductoId = t.ProductoId
        WHERE cd.CompraId = @CompraId;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Compra editada correctamente ID: ' + CAST(@CompraId AS NVARCHAR(10));

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = 'Error: ' + ERROR_MESSAGE();
    END CATCH
END
GO
