namespace LoginSARMedix.Models
{
    public class Lote
    {
        public int id_lote { get; set; }

        public string? numero_lote { get; set; }

        public DateTime? fecha_vencimiento { get; set; }

        public int cantidad_actual { get; set; }

        public int id_producto { get; set; }

        public int id_ubicacion { get; set; }

        public string? nombre_producto { get; set; }

        public string? nombre_ubicacion { get; set; }
    }
}