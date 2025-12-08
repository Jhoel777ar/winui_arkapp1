
-- Tabla para registrar números de serie (Tecnología/Garantía)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VentasSeries')
BEGIN
    CREATE TABLE VentasSeries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VentaId INT NOT NULL,
        ProductoId INT NOT NULL,
        NumeroSerie NVARCHAR(100) NOT NULL,
        FechaRegistro DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_VentasSeries_Venta FOREIGN KEY (VentaId) REFERENCES Ventas(Id) ON DELETE CASCADE,
        CONSTRAINT FK_VentasSeries_Producto FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
    );
    CREATE INDEX IX_VentasSeries_Venta ON VentasSeries(VentaId);
    CREATE INDEX IX_VentasSeries_Serie ON VentasSeries(NumeroSerie);
END
GO

-- Procedimiento para registrar series de una venta
CREATE OR ALTER PROCEDURE sp_RegistrarSeriesVenta
    @VentaId INT,
    @JsonSeries NVARCHAR(MAX), -- [{"ProductoId": 1, "NumeroSerie": "SN123"}, ...]
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Ventas WHERE Id = @VentaId)
        BEGIN
            THROW 51000, 'La venta no existe.', 1;
        END

        INSERT INTO VentasSeries (VentaId, ProductoId, NumeroSerie)
        SELECT @VentaId, ProductoId, NumeroSerie
        FROM OPENJSON(@JsonSeries)
        WITH (
            ProductoId INT '$.ProductoId',
            NumeroSerie NVARCHAR(100) '$.NumeroSerie'
        );

        SET @Resultado = 1;
        SET @Mensaje = 'Series registradas correctamente.';
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

-- Procedimiento para Corte de Caja / Arqueo
CREATE OR ALTER PROCEDURE sp_ObtenerArqueoCaja
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Determinar el inicio del turno:
    -- Buscamos la primera venta del día actual para este usuario.
    -- O simplemente filtramos por el día actual (00:00:00)
    DECLARE @InicioDia DATETIME2 = CAST(CAST(GETDATE() AS DATE) AS DATETIME2);

    -- Totales
    SELECT
        u.NombreCompleto AS Usuario,
        COUNT(v.Id) AS CantidadVentas,
        ISNULL(SUM(v.Total), 0) AS TotalVendido,
        ISNULL(SUM(CASE WHEN v.TipoPago = 'Efectivo' THEN v.Total ELSE 0 END), 0) AS TotalEfectivo,
        ISNULL(SUM(CASE WHEN v.TipoPago = 'Tarjeta' THEN v.Total ELSE 0 END), 0) AS TotalTarjeta,
        ISNULL(SUM(CASE WHEN v.TipoPago = 'QR' THEN v.Total ELSE 0 END), 0) AS TotalQR,
        ISNULL(SUM(CASE WHEN v.TipoPago = 'Transferencia' THEN v.Total ELSE 0 END), 0) AS TotalTransferencia,
        @InicioDia AS FechaInicio
    FROM Ventas v
    INNER JOIN Usuarios u ON v.UsuarioId = u.Id
    WHERE v.UsuarioId = @UsuarioId
      AND v.Fecha >= @InicioDia;

    -- Detalle rápido de las últimas ventas
    SELECT TOP 20
        v.Id,
        v.Fecha,
        ISNULL(c.Nombre, 'Ocasional') AS Cliente,
        v.Total,
        v.TipoPago
    FROM Ventas v
    LEFT JOIN Clientes c ON v.ClienteId = c.Id
    WHERE v.UsuarioId = @UsuarioId
      AND v.Fecha >= @InicioDia
    ORDER BY v.Fecha DESC;
END
GO
