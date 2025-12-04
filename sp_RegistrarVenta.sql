CREATE OR ALTER PROCEDURE sp_RegistrarVenta
    @UsuarioId INT,
    @ClienteId INT = NULL,
    @Productos NVARCHAR(MAX),
    @EfectivoRecibido DECIMAL(10,2) = NULL,
    @TipoPago NVARCHAR(20) = 'Efectivo',
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
            SET @Mensaje = 'El usuario no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. Validar Cliente (si se proporciona)
        IF @ClienteId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = @ClienteId)
        BEGIN
            SET @Mensaje = 'El cliente no existe';
            ROLLBACK TRANSACTION;
            RETURN;
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
            SET @Mensaje = 'Uno o más productos no existen';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 5. Validar Stock Suficiente
        IF EXISTS (SELECT 1 FROM @TempVenta WHERE Cantidad > StockActual)
        BEGIN
            DECLARE @ProdSinStock NVARCHAR(200);
            SELECT TOP 1 @ProdSinStock = p.Nombre
            FROM @TempVenta t
            JOIN Productos p ON t.ProductoId = p.Id
            WHERE t.Cantidad > t.StockActual;

            SET @Mensaje = 'Stock insuficiente para: ' + @ProdSinStock;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 6. Calcular Subtotales
        UPDATE @TempVenta
        SET Subtotal = (Cantidad * PrecioUnitario) - DescuentoMonto - ((Cantidad * PrecioUnitario) * (DescuentoPorcentaje / 100.0));

        DECLARE @TotalVenta DECIMAL(10,2);
        SELECT @TotalVenta = SUM(Subtotal) FROM @TempVenta;

        -- 7. Registrar Venta
        INSERT INTO Ventas (Fecha, UsuarioId, ClienteId, Total, EfectivoRecibido, Cambio, Estado, TipoPago)
        VALUES (GETDATE(), @UsuarioId, @ClienteId, @TotalVenta, @EfectivoRecibido,
                CASE WHEN @EfectivoRecibido IS NOT NULL THEN @EfectivoRecibido - @TotalVenta ELSE 0 END,
                'Completada', @TipoPago);

        SET @VentaId = SCOPE_IDENTITY();

        -- 8. Registrar Detalle
        INSERT INTO VentasDetalle (VentaId, ProductoId, Cantidad, PrecioUnitario, DescuentoPorcentaje, DescuentoMonto, Subtotal)
        SELECT @VentaId, ProductoId, Cantidad, PrecioUnitario, DescuentoPorcentaje, DescuentoMonto, Subtotal
        FROM @TempVenta;

        -- 9. Actualizar Stock
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
