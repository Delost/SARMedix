namespace LoginSARMedix.Models
{
    public class MovimientoControladoHistorial
    {
        public int id_movimiento { get; set; }

        public DateTime fecha_hora { get; set; }

        public string? tipo_movimiento { get; set; }

        public string? observacion { get; set; }


        // DETALLE DEL MOVIMIENTO

        public int cantidad { get; set; }

        public int stock_anterior { get; set; }

        public int stock_resultante { get; set; }


        // LOTE

        public int id_lote { get; set; }

        public string? numero_lote { get; set; }


        // PRODUCTO

        public int id_producto { get; set; }

        public string? nombre_producto { get; set; }


        // USUARIO RESPONSABLE

        public int id_usuario { get; set; }

        public string? usuario_responsable { get; set; }
    }
}