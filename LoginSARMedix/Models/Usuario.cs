namespace LoginSARMedix.Models
{
    public class Usuario
    {
        public int id_usuario { get; set; }

        public string? nombre { get; set; }

        public string? apellido { get; set; }

        public string? rut { get; set; }

        public int id_rol { get; set; }

        public string? nombre_usuario { get; set; }

        public string? contrasena { get; set; }

        public bool activo { get; set; }

        public string? rol { get; set; }

        public List<string> permisos { get; set; } = new List<string>();

        public string? email { get; set; }
    }
}