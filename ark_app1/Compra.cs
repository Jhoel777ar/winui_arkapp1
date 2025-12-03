using System;

namespace ark_app1
{
    public class Compra
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Proveedor { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; }
        public string Estado { get; set; }
    }
}
