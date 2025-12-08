
-- Procedimiento para Historial de Ajustes
CREATE OR ALTER PROCEDURE sp_ObtenerHistorialAjustes
    @Filtro NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        a.Id,
        a.Fecha,
        p.Codigo,
        p.Nombre AS Producto,
        u.NombreCompleto AS Usuario,
        a.Cantidad,
        a.Motivo,
        COUNT(*) OVER() AS TotalRegistros
    FROM InventarioAjustes a
    INNER JOIN Productos p ON a.ProductoId = p.Id
    INNER JOIN Usuarios u ON a.UsuarioId = u.Id
    WHERE (@Filtro IS NULL OR p.Nombre LIKE '%' + @Filtro + '%' OR p.Codigo LIKE '%' + @Filtro + '%' OR u.NombreCompleto LIKE '%' + @Filtro + '%')
    ORDER BY a.Fecha DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Procedimiento para Historial de Cambios de Precio (Basado en Compras)
-- Nota: Como no existe una tabla especifica de logs de precios, mostramos el historial de precios de compra
-- registrados en ComprasDetalle, comparados con el precio de venta actual (referencial).
CREATE OR ALTER PROCEDURE sp_ObtenerHistorialPrecios
    @Filtro NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        cd.Id,
        c.Fecha,
        p.Codigo,
        p.Nombre AS Producto,
        u.NombreCompleto AS Usuario, -- Quien registró la compra
        cd.PrecioUnitario AS PrecioCompraRegistrado, -- El precio en ese momento
        p.PrecioVenta AS PrecioVentaActual,
        cd.Cantidad,
        c.Id AS CompraId,
        COUNT(*) OVER() AS TotalRegistros
    FROM ComprasDetalle cd
    INNER JOIN Compras c ON cd.CompraId = c.Id
    INNER JOIN Productos p ON cd.ProductoId = p.Id
    INNER JOIN Usuarios u ON c.UsuarioId = u.Id
    WHERE (@Filtro IS NULL OR p.Nombre LIKE '%' + @Filtro + '%' OR p.Codigo LIKE '%' + @Filtro + '%')
    ORDER BY c.Fecha DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
