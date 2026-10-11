using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoginSARMedix.ViewComponents
{
    public class AlertasViewComponent : ViewComponent
    {
        private readonly ProductoRepository _productoRepository;
        private readonly LoteRepository _loteRepository;


        public AlertasViewComponent(
            ProductoRepository productoRepository,
            LoteRepository loteRepository)
        {
            _productoRepository =
                productoRepository;

            _loteRepository =
                loteRepository;
        }



        // =====================================================
        // GENERAR ALERTAS
        // =====================================================

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // =================================================
            // VERIFICAR SESION
            // =================================================
            //
            // Si no existe IdUsuario, significa que no hay
            // un usuario autenticado.
            //
            // En ese caso no mostramos la campana.
            // =================================================

            int? idUsuario =
                ViewContext
                    .HttpContext
                    .Session
                    .GetInt32(
                        "IdUsuario"
                    );


            if (!idUsuario.HasValue)
            {
                return Content(
                    string.Empty
                );
            }



            // =================================================
            // LEER PERMISOS DEL USUARIO
            // =================================================

            string permisosTexto =
                ViewContext
                    .HttpContext
                    .Session
                    .GetString(
                        "Permisos"
                    )
                ??
                "";


            var permisos =
                permisosTexto
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries
                    )
                    .ToList();



            // =================================================
            // FUNCION LOCAL PARA CONSULTAR PERMISOS
            // =================================================

            bool TienePermiso(
                string permiso)
            {
                return permisos.Any(
                    p =>
                        p.Equals(
                            permiso,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
            }



            // =================================================
            // PERMISOS IMPORTANTES PARA LAS ALERTAS
            // =================================================

            bool puedeConsultarProductos =
                TienePermiso(
                    "Consultar productos"
                );


            bool puedeRegistrarProductos =
                TienePermiso(
                    "Registrar productos"
                );


            bool puedeGestionarControlados =
                TienePermiso(
                    "Gestionar medicamentos controlados"
                );



            // =================================================
            // SI EL ROL NO TIENE NINGUN ACCESO DE INVENTARIO,
            // NO MOSTRAMOS LA CAMPANA.
            // =================================================

            if (
                !puedeConsultarProductos
                &&
                !puedeRegistrarProductos
                &&
                !puedeGestionarControlados
            )
            {
                return Content(
                    string.Empty
                );
            }



            // =================================================
            // OBTENER INFORMACION REAL DE MYSQL
            // =================================================

            var productos =
                await _productoRepository
                    .ListadoProductos();


            var lotes =
                await _loteRepository
                    .ListadoLotes();



            // =================================================
            // DICCIONARIO DE PRODUCTOS
            // =================================================
            //
            // Esto permite encontrar rápidamente el producto
            // correspondiente a cada lote.
            // =================================================

            var productosPorId =
                productos
                    .ToDictionary(
                        producto =>
                            producto.id_producto
                    );



            DateTime hoy =
                DateTime.Today;


            DateTime limiteVencimiento =
                hoy.AddDays(
                    30
                );



            List<AlertaItemViewModel> alertas =
                new List<AlertaItemViewModel>();



            // =================================================
            // FUNCION PARA SABER SI EL USUARIO PUEDE VER
            // UNA ALERTA DE UN PRODUCTO
            // =================================================

            bool PuedeVerProducto(
                Producto producto)
            {
                /*
                    Si puede consultar o registrar productos,
                    puede ver cualquier producto del inventario.
                */

                if (
                    puedeConsultarProductos
                    ||
                    puedeRegistrarProductos
                )
                {
                    return true;
                }


                /*
                    Si solamente tiene acceso al módulo de
                    medicamentos controlados, solo podrá ver
                    alertas de medicamentos que efectivamente
                    requieren control.
                */

                if (
                    puedeGestionarControlados
                    &&
                    producto.requiere_control
                )
                {
                    return true;
                }


                return false;
            }



            // =================================================
            // CREAR URL PARA UNA ALERTA DE STOCK
            // =================================================
            //
            // OBJETIVO:
            //
            // Cuando el usuario haga clic en una alerta de
            // stock bajo, lo enviamos al LISTADO DE LOTES.
            //
            // Allí Lotes.cshtml recibirá:
            //
            // idProducto
            // tipoAlerta = stock
            //
            // y resaltará todos los lotes relacionados con
            // ese producto.
            //
            // Si el usuario no tiene permiso para consultar
            // productos, respetamos sus permisos y usamos
            // una ruta alternativa segura.
            // =================================================

            string? CrearUrlProducto(
                Producto producto)
            {
                // =============================================
                // PUEDE CONSULTAR LOTES
                // =============================================

                if (puedeConsultarProductos)
                {
                    return Url.Action(
                        "Lotes",
                        "Home",
                        new
                        {
                            idProducto =
                                producto.id_producto,

                            tipoAlerta =
                                "stock"
                        }
                    );
                }


                // =============================================
                // SOLO MEDICAMENTOS CONTROLADOS
                // =============================================
                //
                // No abrimos el listado general de lotes porque
                // el controlador protege ese módulo con permisos.
                //
                // En ese caso conservamos el acceso seguro al
                // módulo de medicamentos controlados.
                // =============================================

                if (
                    puedeGestionarControlados
                    &&
                    producto.requiere_control
                )
                {
                    return Url.Action(
                        "MedicamentosControlados",
                        "Home",
                        new
                        {
                            idProducto =
                                producto.id_producto,

                            tipoAlerta =
                                "stock"
                        }
                    );
                }


                // =============================================
                // PUEDE REGISTRAR, PERO NO CONSULTAR
                // =============================================
                //
                // Es un caso poco habitual, pero evitamos enviar
                // al usuario a una acción para la que no posee
                // permiso de consulta.
                // =============================================

                if (puedeRegistrarProductos)
                {
                    return Url.Action(
                        "EditarProducto",
                        "Home",
                        new
                        {
                            idProducto =
                                producto.id_producto
                        }
                    );
                }


                return null;
            }



            // =================================================
            // TEXTO DEL DESTINO DE STOCK
            // =================================================

            string CrearTextoDestinoProducto(
                Producto producto)
            {
                if (puedeConsultarProductos)
                {
                    return
                        "Ubicar en lotes";
                }


                if (
                    puedeGestionarControlados
                    &&
                    producto.requiere_control
                )
                {
                    return
                        "Ver medicamento controlado";
                }


                if (puedeRegistrarProductos)
                {
                    return
                        "Abrir producto";
                }


                return
                    "";
            }



            // =================================================
            // CREAR URL PARA UNA ALERTA DE LOTE
            // =================================================
            //
            // Para alertas de vencimiento mandamos:
            //
            // idLote
            // idProducto
            // tipoAlerta
            //
            // De esa forma Lotes.cshtml puede encontrar
            // exactamente la fila que originó la alerta.
            // =================================================

            string? CrearUrlLote(
                Lote lote,
                Producto producto,
                string tipoAlerta)
            {
                // =============================================
                // PUEDE CONSULTAR LOTES
                // =============================================

                if (puedeConsultarProductos)
                {
                    return Url.Action(
                        "Lotes",
                        "Home",
                        new
                        {
                            idLote =
                                lote.id_lote,

                            idProducto =
                                lote.id_producto,

                            tipoAlerta =
                                tipoAlerta
                        }
                    );
                }


                // =============================================
                // SOLO MEDICAMENTOS CONTROLADOS
                // =============================================

                if (
                    puedeGestionarControlados
                    &&
                    producto.requiere_control
                )
                {
                    return Url.Action(
                        "MedicamentosControlados",
                        "Home",
                        new
                        {
                            idLote =
                                lote.id_lote,

                            idProducto =
                                lote.id_producto,

                            tipoAlerta =
                                tipoAlerta
                        }
                    );
                }


                // =============================================
                // PUEDE REGISTRAR, PERO NO CONSULTAR
                // =============================================

                if (puedeRegistrarProductos)
                {
                    return Url.Action(
                        "EditarLote",
                        "Home",
                        new
                        {
                            idLote =
                                lote.id_lote
                        }
                    );
                }


                return null;
            }



            // =================================================
            // TEXTO DEL DESTINO DE LOTE
            // =================================================

            string CrearTextoDestinoLote(
                Producto producto)
            {
                if (puedeConsultarProductos)
                {
                    return
                        "Ubicar en lotes";
                }


                if (
                    puedeGestionarControlados
                    &&
                    producto.requiere_control
                )
                {
                    return
                        "Ver medicamento controlado";
                }


                if (puedeRegistrarProductos)
                {
                    return
                        "Abrir lote";
                }


                return
                    "";
            }



            // =================================================
            // ALERTAS DE STOCK BAJO
            // =================================================

            foreach (var producto in productos)
            {
                /*
                    Antes de calcular y mostrar una alerta,
                    comprobamos si el rol tiene permiso
                    para ver ese producto.
                */

                if (!PuedeVerProducto(producto))
                {
                    continue;
                }



                int stockActual =
                    lotes
                        .Where(
                            lote =>
                                lote.id_producto
                                ==
                                producto.id_producto
                        )
                        .Sum(
                            lote =>
                                lote.cantidad_actual
                        );



                // =============================================
                // STOCK BAJO
                // =============================================
                //
                // La alerta se activa cuando:
                //
                // stock actual <= stock mínimo
                //
                // Ejemplo:
                //
                // mínimo = 5
                // stock = 5
                //
                // => STOCK BAJO
                // =============================================

                if (
                    stockActual
                    <=
                    producto.stock_minimo
                )
                {
                    alertas.Add(
                        new AlertaItemViewModel
                        {
                            Tipo =
                                "stock",

                            Titulo =
                                producto.nombre
                                ??
                                "Producto",

                            Detalle =
                                $"Stock actual: {stockActual} · Mínimo: {producto.stock_minimo}",

                            Prioridad =
                                2,

                            IdProducto =
                                producto.id_producto,

                            UrlDestino =
                                CrearUrlProducto(
                                    producto
                                ),

                            TextoDestino =
                                CrearTextoDestinoProducto(
                                    producto
                                )
                        }
                    );
                }
            }



            // =================================================
            // ALERTAS DE VENCIMIENTO
            // =================================================

            foreach (var lote in lotes)
            {
                // =============================================
                // BUSCAR PRODUCTO DEL LOTE
                // =============================================

                if (
                    !productosPorId.TryGetValue(
                        lote.id_producto,
                        out Producto? producto
                    )
                )
                {
                    continue;
                }



                // =============================================
                // COMPROBAR PERMISO
                // =============================================

                if (!PuedeVerProducto(producto))
                {
                    continue;
                }



                /*
                    Un lote con cantidad 0 no tiene unidades
                    físicas disponibles.

                    Por lo tanto no generamos una alerta
                    de vencimiento para un lote vacío.
                */

                if (
                    lote.cantidad_actual
                    <=
                    0
                )
                {
                    continue;
                }



                if (
                    !lote.fecha_vencimiento
                        .HasValue
                )
                {
                    continue;
                }



                DateTime fechaVencimiento =
                    lote.fecha_vencimiento
                        .Value
                        .Date;



                // =============================================
                // LOTE VENCIDO
                // =============================================

                if (
                    fechaVencimiento
                    <
                    hoy
                )
                {
                    int diasVencido =
                        (
                            hoy
                            -
                            fechaVencimiento
                        )
                        .Days;


                    alertas.Add(
                        new AlertaItemViewModel
                        {
                            Tipo =
                                "vencido",

                            Titulo =
                                lote.nombre_producto
                                ??
                                producto.nombre
                                ??
                                "Producto",

                            Detalle =
                                $"Lote {lote.numero_lote} · Venció el {fechaVencimiento:dd/MM/yyyy} · {diasVencido} {(diasVencido == 1 ? "día" : "días")} vencido · Stock: {lote.cantidad_actual}",

                            Fecha =
                                fechaVencimiento,

                            Prioridad =
                                1,

                            IdProducto =
                                lote.id_producto,

                            IdLote =
                                lote.id_lote,

                            UrlDestino =
                                CrearUrlLote(
                                    lote,
                                    producto,
                                    "vencido"
                                ),

                            TextoDestino =
                                CrearTextoDestinoLote(
                                    producto
                                )
                        }
                    );


                    continue;
                }



                // =============================================
                // PROXIMO A VENCER
                // =============================================

                if (
                    fechaVencimiento
                    <=
                    limiteVencimiento
                )
                {
                    int diasRestantes =
                        (
                            fechaVencimiento
                            -
                            hoy
                        )
                        .Days;



                    string textoDias;


                    if (diasRestantes == 0)
                    {
                        textoDias =
                            "Vence hoy";
                    }
                    else if (diasRestantes == 1)
                    {
                        textoDias =
                            "Vence mañana";
                    }
                    else
                    {
                        textoDias =
                            $"Vence en {diasRestantes} días";
                    }



                    alertas.Add(
                        new AlertaItemViewModel
                        {
                            Tipo =
                                "proximo",

                            Titulo =
                                lote.nombre_producto
                                ??
                                producto.nombre
                                ??
                                "Producto",

                            Detalle =
                                $"Lote {lote.numero_lote} · {fechaVencimiento:dd/MM/yyyy} · {textoDias} · Stock: {lote.cantidad_actual}",

                            Fecha =
                                fechaVencimiento,

                            Prioridad =
                                3,

                            IdProducto =
                                lote.id_producto,

                            IdLote =
                                lote.id_lote,

                            UrlDestino =
                                CrearUrlLote(
                                    lote,
                                    producto,
                                    "proximo"
                                ),

                            TextoDestino =
                                CrearTextoDestinoLote(
                                    producto
                                )
                        }
                    );
                }
            }



            // =================================================
            // ORDENAR ALERTAS
            // =================================================
            //
            // 1 = Vencidos
            // 2 = Stock bajo
            // 3 = Próximos a vencer
            //
            // =================================================

            alertas =
                alertas
                    .OrderBy(
                        alerta =>
                            alerta.Prioridad
                    )
                    .ThenBy(
                        alerta =>
                            alerta.Fecha
                            ??
                            DateTime.MaxValue
                    )
                    .ThenBy(
                        alerta =>
                            alerta.Titulo
                    )
                    .ToList();



            // =================================================
            // MODELO FINAL
            // =================================================

            AlertasViewModel modelo =
                new AlertasViewModel
                {
                    Alertas =
                        alertas,

                    Total =
                        alertas.Count,

                    TotalStockBajo =
                        alertas.Count(
                            alerta =>
                                alerta.Tipo
                                ==
                                "stock"
                        ),

                    TotalProximos =
                        alertas.Count(
                            alerta =>
                                alerta.Tipo
                                ==
                                "proximo"
                        ),

                    TotalVencidos =
                        alertas.Count(
                            alerta =>
                                alerta.Tipo
                                ==
                                "vencido"
                        )
                };


            return View(
                modelo
            );
        }
    }



    // =========================================================
    // MODELO DE LA CAMPANA
    // =========================================================

    public class AlertasViewModel
    {
        public List<AlertaItemViewModel> Alertas { get; set; }
            =
            new List<AlertaItemViewModel>();


        public int Total { get; set; }


        public int TotalStockBajo { get; set; }


        public int TotalProximos { get; set; }


        public int TotalVencidos { get; set; }
    }



    // =========================================================
    // MODELO DE UNA ALERTA
    // =========================================================

    public class AlertaItemViewModel
    {
        public string Tipo { get; set; }
            =
            "";


        public string Titulo { get; set; }
            =
            "";


        public string Detalle { get; set; }
            =
            "";


        public DateTime? Fecha { get; set; }


        public int Prioridad { get; set; }


        public int? IdProducto { get; set; }


        public int? IdLote { get; set; }



        // =====================================================
        // NAVEGACION
        // =====================================================

        public string? UrlDestino { get; set; }


        public string TextoDestino { get; set; }
            =
            "";
    }
}