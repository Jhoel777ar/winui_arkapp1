USE [arkdbsisventas]
GO

CREATE OR ALTER PROCEDURE sp_GestionarProducto
    @Id INT = 0,
    @Codigo NVARCHAR(50),
    @Nombre NVARCHAR(150),
    @CategoriaId INT = NULL,
    @Talla NVARCHAR(20) = NULL,
    @Color NVARCHAR(30) = NULL,
    @PrecioCompra DECIMAL(10,2) = 0,
    @PrecioVenta DECIMAL(10,2),
    @UnidadMedida NVARCHAR(20) = 'Unidad',
    @StockMinimo DECIMAL(10,2) = 5,
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        IF @Id = 0
        BEGIN
             IF EXISTS (SELECT 1 FROM Productos WHERE Codigo = @Codigo)
             BEGIN
                SET @Mensaje = 'El código ya existe';
                RETURN;
             END

             INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo, Activo, FechaRegistro)
             VALUES (@Codigo, @Nombre, @CategoriaId, @Talla, @Color, @PrecioCompra, @PrecioVenta, 0, @UnidadMedida, @StockMinimo, 1, GETDATE());

             SET @Mensaje = 'Producto creado correctamente';
             SET @Resultado = 1;
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = @Id)
            BEGIN
                SET @Mensaje = 'El producto no existe';
                RETURN;
            END

            IF EXISTS (SELECT 1 FROM Productos WHERE Codigo = @Codigo AND Id <> @Id)
            BEGIN
                SET @Mensaje = 'El código ya está en uso por otro producto';
                RETURN;
            END

            UPDATE Productos
            SET
                Codigo = @Codigo,
                Nombre = @Nombre,
                CategoriaId = @CategoriaId,
                Talla = @Talla,
                Color = @Color,
                PrecioCompra = @PrecioCompra,
                PrecioVenta = @PrecioVenta,
                UnidadMedida = @UnidadMedida,
                StockMinimo = @StockMinimo
            WHERE Id = @Id;

            SET @Mensaje = 'Producto actualizado correctamente';
            SET @Resultado = 1;
        END
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO
