namespace LoginSARMedix.Models
{
    public class Ubicacion
    {
        public int id_ubicacion { get; set; }

        public string? nombre_sector { get; set; }

        // Define si el sector corresponde
        // a una Bodega o una Clínica.
        public string? tipo_sector { get; set; }

        public bool activo { get; set; }
    }
}