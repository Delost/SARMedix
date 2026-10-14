namespace LoginSARMedix.Models
{
    public class Producto
    {
        public int id_producto { get; set; }

        // Identificador interno generado automáticamente por SARMedix.
        // No se solicita al usuario en los formularios.
        public string? codigo_interno { get; set; }

        public string? codigo_barras { get; set; }

        public string? nombre { get; set; }

        // Cantidad que contiene el producto.
        // Ejemplos: 20 unidades, 120 mL, 500 g.
        public int contenido_por_envase { get; set; } = 1;

        // Unidad asociada al contenido y al stock.
        // Ejemplos: Unidades, mL, L, mg, g, kg o una unidad personalizada.
        public string? unidad_medida { get; set; }

        // Umbral para mostrar alerta de stock bajo.
        public int stock_minimo { get; set; }

        public string? imagen_url { get; set; }


        // PARA SABER SI ES MEDICAMENTO O INSUMO
        public string? tipo { get; set; }


        // DATOS DE MEDICAMENTO
        public string? forma_farmaceutica { get; set; }

        public string? principio_activo { get; set; }

        public string? concentracion { get; set; }

        public bool requiere_control { get; set; }


        // DATOS DE INSUMO
        public string? material { get; set; }

        public bool es_esteril { get; set; }
    }
}
