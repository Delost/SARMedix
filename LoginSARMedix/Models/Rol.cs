namespace LoginSARMedix.Models
{
    public class Rol
    {
        public int id_rol { get; set; }

        public string? nombre { get; set; }

        public string? descripcion { get; set; }

        public bool activo { get; set; }
    }
}