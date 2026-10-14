namespace LoginSARMedix.Models
{
    public class TrasladoStock
    {
        public int id_lote_origen { get; set; }

        public int id_ubicacion_destino { get; set; }

        public int cantidad { get; set; }
    }


    public class ResultadoTrasladoInventario
    {
        public bool exito { get; set; }

        public string mensaje { get; set; } = "";

        public string nombreProducto { get; set; } = "";

        public string numeroLote { get; set; } = "";

        public string origen { get; set; } = "";

        public string destino { get; set; } = "";

        public string unidadMedida { get; set; } = "unidades";

        public int stockOrigenAnterior { get; set; }

        public int stockOrigenResultante { get; set; }

        public int stockDestinoAnterior { get; set; }

        public int stockDestinoResultante { get; set; }
    }
}
