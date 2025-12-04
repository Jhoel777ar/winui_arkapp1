USE [arkdbsisventas]
GO

-- 1. SP Registrar Compra (CORREGIDO: Permite productos existentes)
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

        -- Validaciones
        IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Id = @UsuarioId) THROW 51000, 'El usuario no existe', 1;
        IF @ProveedorId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = @ProveedorId) THROW 51000, 'El proveedor no existe', 1;
        IF @Productos IS NULL OR @Productos = '' THROW 51000, 'Debe enviar productos', 1;

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
            LTRIM(RTRIM(Codigo)), -- Trimming para evitar duplicados por espacios
            Nombre, CategoriaId, Talla, Color,
            ISNULL(PrecioCompra, 0.00), PrecioVenta, Cantidad,
            ISNULL(UnidadMedida, 'Unidad'), ISNULL(StockMinimo, 5.00)
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

        IF NOT EXISTS (SELECT 1 FROM @TempProductos) THROW 51000, 'JSON vacío', 1;

        -- Vincular existentes
        UPDATE t
        SET t.ProductoExiste = 1, t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo;

        -- Actualizar productos EXISTENTES (Precios, Nombre, etc.)
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

        -- Insertar NUEVOS productos
        INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo, Activo, FechaRegistro)
        SELECT
            t.Codigo, t.Nombre, t.CategoriaId, t.Talla, t.Color,
            t.PrecioCompra, t.PrecioVenta, 0.00, -- Stock inicial 0, se suma abajo
            t.UnidadMedida, t.StockMinimo, 1, GETDATE()
        FROM @TempProductos t
        WHERE t.ProductoExiste = 0;

        -- Vincular los nuevos IDs
        UPDATE t
        SET t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo
        WHERE t.ProductoId IS NULL;

        -- Registrar Compra
        DECLARE @TotalCompra DECIMAL(10,2) = (SELECT SUM(Cantidad * PrecioCompra) FROM @TempProductos);

        INSERT INTO Compras (Fecha, ProveedorId, Total, UsuarioId, Estado)
        VALUES (GETDATE(), @ProveedorId, @TotalCompra, @UsuarioId, 'Completada');

        SET @CompraId = SCOPE_IDENTITY();

        -- Registrar Detalle
        INSERT INTO ComprasDetalle (CompraId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT @CompraId, ProductoId, Cantidad, PrecioCompra, Cantidad * PrecioCompra
        FROM @TempProductos;

        -- Actualizar Stock (Sumar cantidad comprada)
        UPDATE p
        SET p.Stock = p.Stock + t.Cantidad
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Compra registrada ID: ' + CAST(@CompraId AS NVARCHAR(10));
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = 'Error: ' + ERROR_MESSAGE();
        SET @CompraId = NULL;
    END CATCH
END
GO

-- 2. SP Modificar Compra (CORREGIDO: Actualiza productos existentes y usa Trim)
CREATE OR ALTER PROCEDURE sp_ModificarCompra
    @CompraId INT,
    @ProveedorId INT,
    @UsuarioId INT,
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

        IF NOT EXISTS (SELECT 1 FROM Compras WHERE Id = @CompraId) THROW 51000, 'La compra no existe', 1;

        -- A. Revertir Stock
        UPDATE p
        SET p.Stock = p.Stock - cd.Cantidad
        FROM Productos p
        INNER JOIN ComprasDetalle cd ON p.Id = cd.ProductoId
        WHERE cd.CompraId = @CompraId;

        -- B. Limpiar Detalle
        DELETE FROM ComprasDetalle WHERE CompraId = @CompraId;

        -- C. Procesar JSON
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
            ProductoId INT NULL,
            ProductoExiste BIT DEFAULT 0
        );

        INSERT INTO @TempProductos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Cantidad, UnidadMedida, StockMinimo)
        SELECT
            LTRIM(RTRIM(Codigo)),
            Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Cantidad,
            ISNULL(UnidadMedida, 'Unidad'), ISNULL(StockMinimo, 5)
        FROM OPENJSON(@Productos)
        WITH (
            Codigo NVARCHAR(50), Nombre NVARCHAR(150), CategoriaId INT, Talla NVARCHAR(20), Color NVARCHAR(30),
            PrecioCompra DECIMAL(10,2), PrecioVenta DECIMAL(10,2), Cantidad DECIMAL(10,2),
            UnidadMedida NVARCHAR(20), StockMinimo DECIMAL(10,2)
        );

        -- D. Vincular / Insertar / Actualizar
        UPDATE t
        SET t.ProductoExiste = 1, t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo;

        -- Actualizar datos del producto (Nombre, precios, etc) al editar la compra
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

        -- Insertar si es un código nuevo
        INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo)
        SELECT Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, 0, UnidadMedida, StockMinimo
        FROM @TempProductos WHERE ProductoExiste = 0;

        UPDATE t
        SET t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo
        WHERE t.ProductoId IS NULL;

        -- E. Actualizar Compra
        DECLARE @Total DECIMAL(10,2) = (SELECT SUM(Cantidad * PrecioCompra) FROM @TempProductos);

        UPDATE Compras
        SET ProveedorId = @ProveedorId, Total = @Total, UsuarioId = @UsuarioId, Fecha = GETDATE()
        WHERE Id = @CompraId;

        -- F. Insertar Detalle
        INSERT INTO ComprasDetalle (CompraId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT @CompraId, ProductoId, Cantidad, PrecioCompra, (Cantidad * PrecioCompra)
        FROM @TempProductos;

        -- G. Aplicar Stock
        UPDATE p
        SET p.Stock = p.Stock + t.Cantidad
        FROM Productos p
        INNER JOIN @TempProductos t ON p.Id = t.ProductoId;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Compra modificada correctamente.';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = 'Error al modificar: ' + ERROR_MESSAGE();
    END CATCH
END
GO
