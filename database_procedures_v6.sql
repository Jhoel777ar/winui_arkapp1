USE [arkdbsisventas]
GO

CREATE OR ALTER PROCEDURE sp_GestionarEmpresa
    @Nombre NVARCHAR(200),
    @Telefono NVARCHAR(50) = NULL,
    @TelefonoSecundario NVARCHAR(50) = NULL,
    @Direccion NVARCHAR(300) = NULL,
    @Email NVARCHAR(150) = NULL,
    @SitioWeb NVARCHAR(150) = NULL,
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Empresa)
        BEGIN
            UPDATE Empresa
            SET Nombre = @Nombre,
                Telefono = @Telefono,
                TelefonoSecundario = @TelefonoSecundario,
                Direccion = @Direccion,
                Email = @Email,
                SitioWeb = @SitioWeb;
            SET @Mensaje = 'Datos de empresa actualizados.';
        END
        ELSE
        BEGIN
            INSERT INTO Empresa (Nombre, Telefono, TelefonoSecundario, Direccion, Email, SitioWeb)
            VALUES (@Nombre, @Telefono, @TelefonoSecundario, @Direccion, @Email, @SitioWeb);
            SET @Mensaje = 'Empresa registrada.';
        END
        SET @Resultado = 1;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerDatosReporte
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Ventas ultimos 7 dias (Para Graficos)
    SELECT
        CAST(Fecha AS DATE) as Fecha,
        SUM(Total) as TotalVentas,
        SUM(Total - (Total * 0.7)) as EstimadoGanancia -- Dummy logic for profit if Cost is not easily aggregate
    FROM Ventas
    WHERE Fecha >= DATEADD(day, -7, GETDATE())
    GROUP BY CAST(Fecha AS DATE)
    ORDER BY CAST(Fecha AS DATE);

    -- 2. Resumen Inventario (Bajo Stock)
    SELECT TOP 10 Codigo, Nombre, Stock, StockMinimo
    FROM Productos
    WHERE Stock <= StockMinimo AND Activo = 1;

    -- 3. Ultimas 20 Ventas
    SELECT TOP 20 v.Id, v.Fecha, c.Nombre as Cliente, v.Total, v.Estado
    FROM Ventas v
    LEFT JOIN Clientes c ON v.ClienteId = c.Id
    ORDER BY v.Fecha DESC;

    -- 4. Totales Generales
    SELECT
        (SELECT COUNT(*) FROM Productos WHERE Activo = 1) as TotalProductos,
        (SELECT SUM(Total) FROM Ventas) as TotalVentasHistorico,
        (SELECT COUNT(*) FROM Clientes) as TotalClientes;
END
GO
