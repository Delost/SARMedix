namespace LoginSARMedix.Models
{
    public class Producto
    {
     

        public int id_producto { get; set; }

        public string? codigo_interno { get; set; }

        public string? nombre { get; set; }

        public int stock_minimo { get; set; }


        //PARA SABER SI ES MEDICAMENTO O INSUMO
        public string? tipo { get; set; }

        //DATOS DE MEDICAMENTO
        public string? principio_activo { get; set; }

        public string? concentracion { get; set; }

        public bool requiere_control { get; set; }

        //DATOS DE INSUMO

        public string? material { get; set; }

        public bool es_esteril { get; set; }
    }
}