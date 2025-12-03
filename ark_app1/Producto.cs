using System;

namespace ark_app1
{
    public class Producto
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int? CategoriaId { get; set; }
        public string Talla { get; set; }
        public string Color { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Stock { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockMinimo { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Propiedad adicional para mostrar el nombre de la categoría en la UI
        public string CategoriaNombre { get; set; }
    }
}
