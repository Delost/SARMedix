
using Microsoft.AspNetCore.Mvc;

namespace LoginSARMedix.Models
{
    public class Historial
    {
        public int id_historial { get; set; }

        public int? id_usuario_responsable { get; set; }

        public string? usuario_responsable { get; set; }

        public string? descripcion { get; set; }

        public DateTime fecha_hora { get; set; }
    }
}