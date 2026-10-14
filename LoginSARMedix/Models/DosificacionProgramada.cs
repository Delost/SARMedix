namespace LoginSARMedix.Models
{
    public class DosificacionProgramada
    {
        public int id_dosificacion { get; set; }

        public string codigo { get; set; } = "";

        public int id_ubicacion { get; set; }

        public string nombre_clinica { get; set; } = "";

        public int id_producto { get; set; }

        public string nombre_producto { get; set; } = "";

        public string? concentracion { get; set; }

        public string? forma_farmaceutica { get; set; }

        public int cantidad { get; set; }

        public string unidad_medida { get; set; } = "Unidades";

        public int stock_minimo { get; set; }

        public string indicacion_base { get; set; } =
            "Tomar ____ comprimido(s) cada ____ horas por ____ días";

        public bool activo { get; set; }

        public DateTime fecha_creacion { get; set; }

        public int id_usuario_creador { get; set; }

        public string nombre_creador { get; set; } = "";
    }


    public class ResultadoProgramacionDosificacion
    {
        public bool exito { get; set; }

        public string mensaje { get; set; } = "";

        public bool reutilizada { get; set; }

        public DosificacionProgramada? dosificacion { get; set; }
    }
}
