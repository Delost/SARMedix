namespace LoginSARMedix.Models
{
    public class EntregaPaciente
    {
        // Clínica desde donde se entregará el medicamento.
        public int id_ubicacion { get; set; }


        // Producto que se entregará.
        public int id_producto { get; set; }


        // Cantidad total que saldrá del inventario.
        public int cantidad { get; set; }
    }



    public class ResultadoEntregaPaciente
    {
        // Indica si toda la operación terminó correctamente.
        public bool exito { get; set; }


        // Mensaje que mostraremos en pantalla.
        public string mensaje { get; set; } = "";


        // Datos para mostrar y registrar en historial.
        public string nombreProducto { get; set; } = "";

        public string nombreClinica { get; set; } = "";

        public string unidadMedida { get; set; } = "unidades";


        // Stock total del producto en esa Clínica
        // antes y después de la entrega.
        public int stockAnterior { get; set; }

        public int stockResultante { get; set; }


        // Identificador del movimiento creado.
        public long idMovimiento { get; set; }


        // Texto con los lotes utilizados por FEFO.
        //
        // Ejemplo:
        // Lote A: 3 | Lote B: 4
        public string detalleLotes { get; set; } = "";
    }
}