USE [arkdbsisventas]
GO

-- 1. SP para Modificar Compra (Reescritura completa segura)
CREATE OR ALTER PROCEDURE sp_ModificarCompra
    @CompraId INT,
    @ProveedorId INT,
    @UsuarioId INT,
    @Productos NVARCHAR(MAX), -- JSON con lista completa de productos
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
            THROW 51000, 'La compra no existe', 1;
        END

        -- A. Revertir Stock de la compra anterior
        UPDATE p
        SET p.Stock = p.Stock - cd.Cantidad
        FROM Productos p
        INNER JOIN ComprasDetalle cd ON p.Id = cd.ProductoId
        WHERE cd.CompraId = @CompraId;

        -- B. Eliminar detalles anteriores
        DELETE FROM ComprasDetalle WHERE CompraId = @CompraId;

        -- C. Procesar JSON de nuevos productos
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
            Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Cantidad,
            ISNULL(UnidadMedida, 'Unidad'), ISNULL(StockMinimo, 5)
        FROM OPENJSON(@Productos)
        WITH (
            Codigo NVARCHAR(50), Nombre NVARCHAR(150), CategoriaId INT, Talla NVARCHAR(20), Color NVARCHAR(30),
            PrecioCompra DECIMAL(10,2), PrecioVenta DECIMAL(10,2), Cantidad DECIMAL(10,2),
            UnidadMedida NVARCHAR(20), StockMinimo DECIMAL(10,2)
        );

        -- D. Identificar o Crear Productos (Igual que en Registrar)
        -- D.1 Vincular existentes
        UPDATE t
        SET t.ProductoExiste = 1, t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo;

        -- D.2 Insertar Nuevos
        INSERT INTO Productos (Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, Stock, UnidadMedida, StockMinimo)
        SELECT Codigo, Nombre, CategoriaId, Talla, Color, PrecioCompra, PrecioVenta, 0, UnidadMedida, StockMinimo
        FROM @TempProductos WHERE ProductoExiste = 0;

        -- D.3 Vincular los recien insertados
        UPDATE t
        SET t.ProductoId = p.Id
        FROM @TempProductos t
        INNER JOIN Productos p ON t.Codigo = p.Codigo
        WHERE t.ProductoId IS NULL;

        -- E. Actualizar Cabecera Compra
        DECLARE @Total DECIMAL(10,2) = (SELECT SUM(Cantidad * PrecioCompra) FROM @TempProductos);

        UPDATE Compras
        SET ProveedorId = @ProveedorId, Total = @Total, UsuarioId = @UsuarioId, Fecha = GETDATE()
        WHERE Id = @CompraId;

        -- F. Insertar Nuevos Detalles
        INSERT INTO ComprasDetalle (CompraId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT @CompraId, ProductoId, Cantidad, PrecioCompra, (Cantidad * PrecioCompra)
        FROM @TempProductos;

        -- G. Aplicar Nuevo Stock
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

-- 2. SP para Registrar Ajuste
CREATE OR ALTER PROCEDURE sp_RegistrarAjuste
    @UsuarioId INT,
    @ProductoId INT,
    @Cantidad DECIMAL(10,2), -- Positivo (Entrada) o Negativo (Salida)
    @Motivo NVARCHAR(300),
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

            -- Validaciones
            IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = @ProductoId) THROW 51000, 'Producto no encontrado', 1;
            IF @Cantidad = 0 THROW 51000, 'La cantidad no puede ser 0', 1;

            -- Insertar Ajuste (El Trigger TR_ActualizarStock_Ajuste se encargará del stock)
            INSERT INTO InventarioAjustes (UsuarioId, ProductoId, Cantidad, Motivo)
            VALUES (@UsuarioId, @ProductoId, @Cantidad, @Motivo);

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Ajuste registrado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

-- 3. SP Gestionar Cliente
CREATE OR ALTER PROCEDURE sp_GestionarCliente
    @Id INT = 0, -- 0 para Insertar
    @Nombre NVARCHAR(100),
    @Telefono NVARCHAR(20) = NULL,
    @CI NVARCHAR(20) = NULL,
    @Direccion NVARCHAR(200) = NULL,
    @Notas NVARCHAR(300) = NULL,
    @Accion NVARCHAR(10) = 'GUARDAR', -- GUARDAR, ELIMINAR
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @Accion = 'ELIMINAR'
        BEGIN
            DELETE FROM Clientes WHERE Id = @Id;
            SET @Mensaje = 'Cliente eliminado.';
        END
        ELSE
        BEGIN
            IF @Id = 0
            BEGIN
                INSERT INTO Clientes (Nombre, Telefono, CI, Direccion, Notas)
                VALUES (@Nombre, @Telefono, @CI, @Direccion, @Notas);
                SET @Mensaje = 'Cliente registrado.';
            END
            ELSE
            BEGIN
                UPDATE Clientes
                SET Nombre = @Nombre, Telefono = @Telefono, CI = @CI, Direccion = @Direccion, Notas = @Notas
                WHERE Id = @Id;
                SET @Mensaje = 'Cliente actualizado.';
            END
        END
        SET @Resultado = 1;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

-- 4. SP Gestionar Categoria
CREATE OR ALTER PROCEDURE sp_GestionarCategoria
    @Id INT = 0,
    @Nombre NVARCHAR(50),
    @Accion NVARCHAR(10) = 'GUARDAR',
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @Accion = 'ELIMINAR'
        BEGIN
            DELETE FROM Categorias WHERE Id = @Id;
            SET @Mensaje = 'Categoría eliminada.';
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM Categorias WHERE Nombre = @Nombre AND Id <> @Id)
                THROW 51000, 'Ya existe una categoría con ese nombre', 1;

            IF @Id = 0
            BEGIN
                INSERT INTO Categorias (Nombre) VALUES (@Nombre);
                SET @Mensaje = 'Categoría creada.';
            END
            ELSE
            BEGIN
                UPDATE Categorias SET Nombre = @Nombre WHERE Id = @Id;
                SET @Mensaje = 'Categoría actualizada.';
            END
        END
        SET @Resultado = 1;
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO
