USE [arkdbsisventas]
GO

CREATE OR ALTER PROCEDURE sp_RegistrarVenta_v2
    @UsuarioId INT,
    @ClienteId INT = NULL,
    @Productos NVARCHAR(MAX),
    @EfectivoRecibido DECIMAL(10,2) = NULL,
    @TipoPago NVARCHAR(20) = 'Efectivo',
    @DescuentoGlobalPorcentaje DECIMAL(5,2) = 0,
    @DescuentoGlobalMonto DECIMAL(10,2) = 0,
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT,
    @VentaId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @VentaId = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Validar Usuario
        IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Id = @UsuarioId)
        BEGIN
            THROW 51000, 'El usuario no existe', 1;
        END

        -- 2. Validar Cliente (si se proporciona)
        IF @ClienteId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = @ClienteId)
        BEGIN
            THROW 51000, 'El cliente no existe', 1;
        END

        -- 3. Parsear JSON de Productos
        DECLARE @TempVenta TABLE (
            ProductoId INT,
            Cantidad DECIMAL(10,2),
            PrecioUnitario DECIMAL(10,2),
            DescuentoPorcentaje DECIMAL(5,2),
            DescuentoMonto DECIMAL(10,2),
            Subtotal DECIMAL(10,2),
            StockActual DECIMAL(10,2)
        );

        INSERT INTO @TempVenta (ProductoId, Cantidad, PrecioUnitario, DescuentoPorcentaje, DescuentoMonto)
        SELECT
            ProductoId,
            Cantidad,
            PrecioUnitario,
            ISNULL(DescuentoPorcentaje, 0),
            ISNULL(DescuentoMonto, 0)
        FROM OPENJSON(@Productos)
        WITH (
            ProductoId INT '$.ProductoId',
            Cantidad DECIMAL(10,2) '$.Cantidad',
            PrecioUnitario DECIMAL(10,2) '$.PrecioUnitario',
            DescuentoPorcentaje DECIMAL(5,2) '$.DescuentoPorcentaje',
            DescuentoMonto DECIMAL(10,2) '$.DescuentoMonto'
        );

        -- 4. Validar existencia de productos y obtener stock actual
        UPDATE t
        SET t.StockActual = p.Stock
        FROM @TempVenta t
        INNER JOIN Productos p ON t.ProductoId = p.Id;

        IF EXISTS (SELECT 1 FROM @TempVenta WHERE StockActual IS NULL)
        BEGIN
            THROW 51000, 'Uno o más productos no existen', 1;
        END

        -- 5. Validar Stock Suficiente
        IF EXISTS (SELECT 1 FROM @TempVenta WHERE Cantidad > StockActual)
        BEGIN
            DECLARE @ProdSinStock NVARCHAR(200);
            SELECT TOP 1 @ProdSinStock = p.Nombre
            FROM @TempVenta t
            JOIN Productos p ON t.ProductoId = p.Id
            WHERE t.Cantidad > t.StockActual;

            DECLARE @MsgStock NVARCHAR(250) = 'Stock insuficiente para: ' + @ProdSinStock;
            THROW 51000, @MsgStock, 1;
        END

        -- 6. Calcular Subtotales de línea
        UPDATE @TempVenta
        SET Subtotal = (Cantidad * PrecioUnitario) - DescuentoMonto - ((Cantidad * PrecioUnitario) * (DescuentoPorcentaje / 100.0));

        DECLARE @TotalVenta DECIMAL(10,2);
        SELECT @TotalVenta = SUM(Subtotal) FROM @TempVenta;

        -- 7. Aplicar Descuentos Globales
        IF @DescuentoGlobalMonto > 0
        BEGIN
            SET @TotalVenta = @TotalVenta - @DescuentoGlobalMonto;
        END

        IF @DescuentoGlobalPorcentaje > 0
        BEGIN
            SET @TotalVenta = @TotalVenta - (@TotalVenta * @DescuentoGlobalPorcentaje / 100.0);
        END

        -- Asegurar que el total no sea negativo
        IF @TotalVenta < 0 SET @TotalVenta = 0;

        -- 8. Registrar Venta
        INSERT INTO Ventas (Fecha, UsuarioId, ClienteId, Total, DescuentoPorcentaje, DescuentoMonto, EfectivoRecibido, Cambio, Estado, TipoPago)
        VALUES (GETDATE(), @UsuarioId, @ClienteId, @TotalVenta, @DescuentoGlobalPorcentaje, @DescuentoGlobalMonto, @EfectivoRecibido,
                CASE WHEN @EfectivoRecibido IS NOT NULL THEN @EfectivoRecibido - @TotalVenta ELSE 0 END,
                'Completada', @TipoPago);

        SET @VentaId = SCOPE_IDENTITY();

        -- 9. Registrar Detalle
        INSERT INTO VentasDetalle (VentaId, ProductoId, Cantidad, PrecioUnitario, DescuentoPorcentaje, DescuentoMonto, Subtotal)
        SELECT @VentaId, ProductoId, Cantidad, PrecioUnitario, DescuentoPorcentaje, DescuentoMonto, Subtotal
        FROM @TempVenta;

        -- 10. Actualizar Stock
        UPDATE p
        SET p.Stock = p.Stock - t.Cantidad
        FROM Productos p
        INNER JOIN @TempVenta t ON p.Id = t.ProductoId;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Venta registrada exitosamente. ID: ' + CAST(@VentaId AS NVARCHAR(20));

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = 'Error al registrar venta: ' + ERROR_MESSAGE();
        SET @VentaId = NULL;
    END CATCH
END
GO
