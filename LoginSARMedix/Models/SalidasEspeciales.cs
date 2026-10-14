namespace LoginSARMedix.Models
{
    // =========================================================
    // BAJA / MERMA DE INVENTARIO
    // =========================================================
    public class BajaInventario
    {
        public int id_lote { get; set; }

        public int cantidad { get; set; }

        public string? motivo { get; set; }
    }


    // =========================================================
    // SALIDA EXTRAORDINARIA A OTRO SERVICIO
    // =========================================================
    public class SalidaExtraordinaria
    {
        public int id_ubicacion { get; set; }

        public int id_producto { get; set; }

        public int cantidad { get; set; }

        public string? servicio_destino { get; set; }

        public string? motivo { get; set; }
    }


    // =========================================================
    // RESULTADO COMUN DE SALIDAS ESPECIALES
    // =========================================================
    public class ResultadoSalidaEspecial
    {
        public bool exito { get; set; }

        public string mensaje { get; set; } = "";

        public string nombreProducto { get; set; } = "";

        public string numeroLote { get; set; } = "";

        public string nombreSector { get; set; } = "";

        public string unidadMedida { get; set; } = "unidades";

        public string detalleLotes { get; set; } = "";

        public int stockAnterior { get; set; }

        public int stockResultante { get; set; }

        public int stockMinimo { get; set; }

        public long idMovimiento { get; set; }
    }


    // =========================================================
    // HISTORIAL OPERACIONAL DE UN SECTOR
    // =========================================================
    public class MovimientoSectorHistorial
    {
        public long id_movimiento { get; set; }

        public DateTime fecha_hora { get; set; }

        public string tipo_movimiento { get; set; } = "";

        public string observacion { get; set; } = "";

        public string responsable { get; set; } = "";

        public string producto { get; set; } = "";

        public string lote { get; set; } = "";

        public string sector { get; set; } = "";

        public string unidad_medida { get; set; } = "unidades";

        public int cantidad { get; set; }

        public int stock_anterior { get; set; }

        public int stock_resultante { get; set; }
    }


    // =========================================================
    // DETALLE DE UNA SALIDA DENTRO DE UN TURNO
    // =========================================================
    public class DetalleSalidaTurno
    {
        public long id_movimiento { get; set; }

        public DateTime fecha_hora { get; set; }

        public int id_producto { get; set; }

        public string nombre_producto { get; set; } = "";

        public string concentracion { get; set; } = "";

        public string unidad_medida { get; set; } = "unidades";

        public int cantidad { get; set; }

        public string tipo_salida { get; set; } = "";

        public bool es_controlado { get; set; }

        public string responsable { get; set; } = "";

        public string sector { get; set; } = "";

        public string lotes { get; set; } = "";

        public string observacion { get; set; } = "";
    }


    // =========================================================
    // RESUMEN AGRUPADO POR MEDICAMENTO
    // =========================================================
    public class ResumenSalidaTurno
    {
        public int id_producto { get; set; }

        public string nombre_producto { get; set; } = "";

        public string concentracion { get; set; } = "";

        public string unidad_medida { get; set; } = "unidades";

        public int cantidad_total { get; set; }

        public bool es_controlado { get; set; }

        public List<DetalleSalidaTurno> detalles { get; set; } =
            new List<DetalleSalidaTurno>();
    }


    // =========================================================
    // MODELO DE LA PANTALLA SALIDAS DEL TURNO
    // =========================================================
    public class HistorialSalidasTurnoViewModel
    {
        public DateTime fecha { get; set; }

        public string hora_inicio { get; set; } = "08:00";

        public string hora_fin { get; set; } = "00:00";

        public DateTime inicio_real { get; set; }

        public DateTime fin_real { get; set; }

        public bool puede_ver_controlados { get; set; }

        public List<ResumenSalidaTurno> resumen { get; set; } =
            new List<ResumenSalidaTurno>();
    }

}
