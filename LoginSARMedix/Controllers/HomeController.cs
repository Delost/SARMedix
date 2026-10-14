using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using LoginSARMedix.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Net;

namespace LoginSARMedix.Controllers
{
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true
    )]
    public class HomeController : Controller
    {
        private readonly EmailService _emailService;

        private readonly UsuarioRepository _usuarioRepository;
        private readonly ProductoRepository _productoRepository;
        private readonly LoteRepository _loteRepository;
        private readonly PermisoRepository _permisoRepository;

        private readonly HistorialRepository _historialRepository;
        private readonly MovimientoRepository _movimientoRepository;
        private readonly DosificacionRepository _dosificacionRepository;


        public HomeController(
            UsuarioRepository usuarioRepository,
            ProductoRepository productoRepository,
            LoteRepository loteRepository,
            PermisoRepository permisoRepository,
            EmailService emailService,
            HistorialRepository historialRepository,
            MovimientoRepository movimientoRepository,
            DosificacionRepository dosificacionRepository)
        {
            _usuarioRepository = usuarioRepository;
            _productoRepository = productoRepository;
            _loteRepository = loteRepository;
            _permisoRepository = permisoRepository;

            _emailService = emailService;

            _historialRepository = historialRepository;
            _movimientoRepository = movimientoRepository;
            _dosificacionRepository = dosificacionRepository;
        }


        // =========================================================
        // INICIO DE SESION
        // =========================================================

        [HttpGet]
        public IActionResult Index()
        {
            string? nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            if (!string.IsNullOrEmpty(nombre))
            {
                return RedirectToAction(
                    "Modulos"
                );
            }


            return View();
        }



        // =========================================================
        // VERIFICAR CODIGO
        // =========================================================

        [HttpGet]
        public IActionResult VerificarCodigo()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> VerificarCodigo(
            string codigo)
        {
            int? idUsuario =
                await _usuarioRepository
                    .ValidarCodigoRecuperacion(
                        codigo
                    );


            if (idUsuario == null)
            {
                ViewBag.Mensaje =
                    "El código es incorrecto o ha vencido.";

                return View();
            }


            HttpContext.Session.SetInt32(
                "IdUsuarioRecuperacion",
                idUsuario.Value
            );


            HttpContext.Session.SetString(
                "CodigoRecuperacion",
                codigo
            );


            return RedirectToAction(
                "NuevaContrasena"
            );
        }



        // =========================================================
        // INICIAR SESION
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> InicioSesion(
            Usuario reg)
        {
            var usuario =
                await _usuarioRepository
                    .IniciarSesion(
                        reg.nombre_usuario,
                        reg.contrasena
                    );


            if (usuario != null)
            {
                HttpContext.Session.SetInt32(
                    "IdUsuario",
                    usuario.id_usuario
                );


                var permisos =
                    await _usuarioRepository
                        .ObtenerPermisos(
                            usuario.id_rol
                        );


                HttpContext.Session.SetString(
                    "Nombre",
                    usuario.nombre ?? ""
                );


                HttpContext.Session.SetString(
                    "Apellido",
                    usuario.apellido ?? ""
                );


                HttpContext.Session.SetString(
                    "Rol",
                    usuario.rol ?? ""
                );


                HttpContext.Session.SetInt32(
                    "IdRol",
                    usuario.id_rol
                );


                HttpContext.Session.SetString(
                    "Permisos",
                    string.Join(
                        "|",
                        permisos
                    )
                );


                return RedirectToAction(
                    "Modulos"
                );
            }


            ViewBag.Mensaje =
                "Usuario o contraseña incorrectos";


            return View(
                "Index"
            );
        }



        // =========================================================
        // RECUPERAR CONTRASEÑA
        // =========================================================

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> RecuperarContrasena(
            string correo)
        {
            var usuario =
                await _usuarioRepository
                    .BuscarPorCorreo(
                        correo
                    );


            if (usuario != null)
            {
                Random random =
                    new Random();


                string codigo =
                    random.Next(
                        100000,
                        999999
                    ).ToString();


                DateTime fechaExpiracion =
                    DateTime.Now
                        .AddMinutes(
                            10
                        );


                var guardado =
                    await _usuarioRepository
                        .CrearCodigoRecuperacion(
                            usuario.id_usuario,
                            codigo,
                            fechaExpiracion
                        );


                if (guardado)
                {
                    bool correoEnviado =
                        await _emailService
                            .EnviarCodigoRecuperacion(
                                usuario.email!,
                                usuario.nombre ?? "Usuario",
                                codigo
                            );


                    if (correoEnviado)
                    {
                        return RedirectToAction(
                            "VerificarCodigo"
                        );
                    }


                    ViewBag.Mensaje =
                        "El código fue generado, pero no se pudo enviar el correo.";
                }
                else
                {
                    ViewBag.Mensaje =
                        "No se pudo generar el código.";
                }
            }
            else
            {
                ViewBag.Mensaje =
                    "Correo no registrado.";
            }


            return View();
        }



        // =========================================================
        // NUEVA CONTRASEÑA
        // =========================================================

        [HttpGet]
        public IActionResult NuevaContrasena()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuarioRecuperacion"
                );


            if (idUsuario == null)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            return View();
        }



        // =========================================================
        // CAMBIAR CONTRASEÑA
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(
            string nuevaContrasena,
            string confirmarContrasena)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuarioRecuperacion"
                );


            string? codigo =
                HttpContext.Session.GetString(
                    "CodigoRecuperacion"
                );


            if (idUsuario == null ||
                string.IsNullOrEmpty(codigo))
            {
                return RedirectToAction(
                    "Index"
                );
            }


            if (string.IsNullOrWhiteSpace(
                    nuevaContrasena))
            {
                ViewBag.Mensaje =
                    "Debe ingresar una nueva contraseña.";


                return View(
                    "NuevaContrasena"
                );
            }


            if (nuevaContrasena !=
                confirmarContrasena)
            {
                ViewBag.Mensaje =
                    "Las contraseñas no coinciden.";


                return View(
                    "NuevaContrasena"
                );
            }


            bool actualizado =
                await _usuarioRepository
                    .CambiarContrasena(
                        idUsuario.Value,
                        nuevaContrasena
                    );


            if (!actualizado)
            {
                ViewBag.Mensaje =
                    "No se pudo actualizar la contraseña.";


                return View(
                    "NuevaContrasena"
                );
            }


            await _usuarioRepository
                .MarcarCodigoComoUtilizado(
                    idUsuario.Value,
                    codigo
                );


            HttpContext.Session.Remove(
                "IdUsuarioRecuperacion"
            );


            HttpContext.Session.Remove(
                "CodigoRecuperacion"
            );


            TempData["Mensaje"] =
                "Contraseña actualizada correctamente.";


            return RedirectToAction(
                "Index"
            );
        }



        // =========================================================
        // CERRAR SESION
        // =========================================================

        [HttpPost]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();


            TempData["Mensaje"] =
                "Sesión cerrada correctamente";


            TempData["TipoMensaje"] =
                "exito";


            return RedirectToAction(
                "Index"
            );
        }



        // =========================================================
        // PANTALLA PRINCIPAL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Modulos()
        {
            string? nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            if (nombre == null)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            ViewBag.Nombre =
                nombre;


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            string permisosTexto =
                HttpContext.Session.GetString(
                    "Permisos"
                ) ?? "";


            ViewBag.Permisos =
                permisosTexto
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries
                    )
                    .ToList();


            await RegistrarAccionHistorial(
                "Ingresó a la pantalla principal de módulos"
            );


            return View(
                "Bienvenida"
            );
        }



        // =========================================================
        // ACCESO DENEGADO
        // =========================================================

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            string? nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            if (nombre == null)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            ViewBag.Nombre =
                nombre;


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            return View();
        }



        // =========================================================
        // MODULO USUARIOS Y PERMISOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> UsuariosPermisos()
        {
            if (!TieneAlgunPermiso(
                    "Crear usuario",
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            await RegistrarAccionHistorial(
                "Ingresó al módulo Usuarios y Permisos"
            );


            return View(
                "~/Views/Home/Modulos/UsuariosPermisos.cshtml"
            );
        }



        // =========================================================
        // MODULO PRODUCTOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Productos()
        {
            if (!TieneAlgunPermiso(
                    "Consultar productos",
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            await RegistrarAccionHistorial(
                "Ingresó al módulo Productos"
            );


            return View(
                "~/Views/Home/Modulos/Productos.cshtml"
            );
        }



        // =========================================================
        // MOVIMIENTOS E HISTORIAL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MovimientoHistorial()
        {
            if (!TieneAlgunPermiso(
                    "Registrar entradas de inventario",
                    "Trasladar stock",
                    "Preparar dosificaciones",
                    "Entregar dosificaciones",
                    "Registrar bajas de inventario",
                    "Registrar salidas extraordinarias",
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }


            await RegistrarAccionHistorial(
                "Ingresó al módulo Movimientos e Historial"
            );


            return View(
                "~/Views/Home/Modulos/MovimientoHistorial.cshtml"
            );
        }



        // =========================================================
        // TRASLADAR STOCK ENTRE BODEGA Y CLINICA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> TrasladarStock()
        {
            if (!TienePermiso(
                    "Trasladar stock"))
            {
                return AccesoNoAutorizado();
            }


            var ubicaciones =
                await _loteRepository
                    .ListadoUbicacionesActivas();


            var productos =
                await _productoRepository
                    .ListadoProductos();


            var lotes =
                await _loteRepository
                    .ListadoLotes();


            var idsUbicacionesActivas =
                ubicaciones
                    .Select(
                        u => u.id_ubicacion
                    )
                    .ToHashSet();


            var idsControlados =
                productos
                    .Where(
                        p => p.requiere_control
                    )
                    .Select(
                        p => p.id_producto
                    )
                    .ToHashSet();


            var lotesOrigen =
                lotes
                    .Where(
                        l =>
                            l.cantidad_actual > 0
                            &&
                            idsUbicacionesActivas
                                .Contains(
                                    l.id_ubicacion
                                )
                            &&
                            !idsControlados
                                .Contains(
                                    l.id_producto
                                )
                    )
                    .OrderBy(
                        l => l.nombre_ubicacion
                    )
                    .ThenBy(
                        l => l.nombre_producto
                    )
                    .ThenBy(
                        l => l.fecha_vencimiento
                    )
                    .ToList();


            ViewBag.Ubicaciones =
                ubicaciones;


            ViewBag.Productos =
                productos;


            ViewBag.LotesOrigen =
                lotesOrigen;


            await RegistrarAccionHistorial(
                "Ingresó a Trasladar Stock"
            );


            return View(
                "TrasladarStock",
                new TrasladoStock()
            );
        }



        // =========================================================
        // REGISTRAR TRASLADO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarTraslado(
            TrasladoStock reg)
        {
            if (!TienePermiso(
                    "Trasladar stock"))
            {
                return AccesoNoAutorizado();
            }


            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            var resultado =
                await _movimientoRepository
                    .RegistrarTrasladoInventario(
                        reg.id_lote_origen,
                        reg.id_ubicacion_destino,
                        reg.cantidad,
                        idUsuario.Value
                    );


            if (resultado.exito)
            {
                await RegistrarAccionHistorial(
                    $"Trasladó {reg.cantidad} {resultado.unidadMedida} de {resultado.nombreProducto}, lote {resultado.numeroLote}, desde {resultado.origen} hacia {resultado.destino}. Stock origen: {resultado.stockOrigenAnterior} → {resultado.stockOrigenResultante}. Stock destino: {resultado.stockDestinoAnterior} → {resultado.stockDestinoResultante}."
                );


                TempData["Mensaje"] =
                    resultado.mensaje;


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    resultado.mensaje;


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "TrasladarStock"
            );
        }




        // =========================================================
        // ENTREGA A PACIENTE - VISTA
        // =========================================================

        // =========================================================
        // PRODUCTOS DISPONIBLES EN LA CLINICA SELECCIONADA
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ProductosDisponiblesClinica(
            int idUbicacion)
        {
            if (!TieneAlgunPermiso(
                    "Preparar dosificaciones",
                    "Entregar dosificaciones",
                    "Registrar salidas extraordinarias"))
            {
                return Unauthorized();
            }

            if (idUbicacion <= 0)
            {
                return Json(
                    new
                    {
                        exito = true,
                        productos = Array.Empty<object>()
                    }
                );
            }

            var productos =
                await _loteRepository
                    .ListadoProductosDisponiblesClinica(
                        idUbicacion
                    );

            return Json(
                new
                {
                    exito = true,

                    productos =
                        productos.Select(
                            p => new
                            {
                                id_producto =
                                    p.id_producto,

                                nombre =
                                    p.nombre,

                                unidad_medida =
                                    p.unidad_medida,

                                forma_farmaceutica =
                                    p.forma_farmaceutica,

                                concentracion =
                                    p.concentracion,

                                tipo =
                                    p.tipo
                            }
                        )
                }
            );
        }



        // =========================================================
        // REGISTRAR ENTREGA A PACIENTE
        // =========================================================

        // =========================================================
        // DOSIFICACIONES PROGRAMADAS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Dosificaciones()
        {
            if (!TienePermiso(
                    "Preparar dosificaciones"))
            {
                return AccesoNoAutorizado();
            }


            var ubicaciones =
                await _loteRepository
                    .ListadoUbicacionesActivas();


            ViewBag.Clinicas =
                ubicaciones
                    .Where(
                        u =>
                            (u.tipo_sector ?? "")
                                .Equals(
                                    "Clinica",
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .OrderBy(
                        u => u.nombre_sector
                    )
                    .ToList();


            ViewBag.Programaciones =
                await _dosificacionRepository
                    .ListarProgramaciones();


            await RegistrarAccionHistorial(
                "Ingresó a Dosificaciones"
            );


            return View(
                "Dosificaciones"
            );
        }



        // =========================================================
        // GUARDAR / REUTILIZAR PROGRAMACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            GuardarDosificacionProgramada(
                int idUbicacion,
                int idProducto,
                int cantidad,
                int copias)
        {
            if (!TienePermiso(
                    "Preparar dosificaciones"))
            {
                return AccesoNoAutorizado();
            }


            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            copias =
                Math.Clamp(
                    copias,
                    1,
                    500
                );


            var resultado =
                await _dosificacionRepository
                    .CrearOObtenerProgramacion(
                        idUbicacion,
                        idProducto,
                        cantidad,
                        idUsuario.Value
                    );


            if (
                !resultado.exito
                ||
                resultado.dosificacion == null
            )
            {
                TempData["Mensaje"] =
                    resultado.mensaje;

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Dosificaciones"
                );
            }


            string accion =
                resultado.reutilizada
                    ? "Reutilizó"
                    : "Creó";


            await RegistrarAccionHistorial(
                $"{accion} programación "
                +
                $"{resultado.dosificacion.codigo}: "
                +
                $"{resultado.dosificacion.nombre_producto}, "
                +
                $"{resultado.dosificacion.cantidad} "
                +
                $"{resultado.dosificacion.unidad_medida}, "
                +
                $"Clínica {resultado.dosificacion.nombre_clinica}."
            );


            return RedirectToAction(
                "ImprimirDosificacion",
                new
                {
                    id =
                        resultado.dosificacion
                            .id_dosificacion,

                    copias
                }
            );
        }



        // =========================================================
        // IMPRIMIR / REIMPRIMIR PROGRAMACION
        // NO DESCUENTA STOCK
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            ImprimirDosificacion(
                int id,
                int copias = 1)
        {
            if (!TienePermiso(
                    "Preparar dosificaciones"))
            {
                return AccesoNoAutorizado();
            }


            var dosificacion =
                await _dosificacionRepository
                    .ObtenerPorId(
                        id
                    );


            if (
                dosificacion == null
                ||
                !dosificacion.activo
            )
            {
                TempData["Mensaje"] =
                    "No se encontró la programación solicitada.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Dosificaciones"
                );
            }


            copias =
                Math.Clamp(
                    copias,
                    1,
                    500
                );


            string nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                )
                ?? "";


            string apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                )
                ?? "";


            string rol =
                HttpContext.Session.GetString(
                    "Rol"
                )
                ?? "";


            string responsable =
                $"{rol} {nombre} {apellido}"
                    .Trim();


            ViewBag.Copias =
                copias;


            ViewBag.Responsable =
                responsable;


            await RegistrarAccionHistorial(
                $"Preparó impresión de {copias} etiqueta(s) "
                +
                $"para {dosificacion.codigo}: "
                +
                $"{dosificacion.nombre_producto}, "
                +
                $"{dosificacion.cantidad} "
                +
                $"{dosificacion.unidad_medida}. "
                +
                $"La impresión no modifica stock."
            );


            return View(
                "ImprimirDosificacion",
                dosificacion
            );
        }



        // =========================================================
        // ENTREGA MEDIANTE CODIGO DE DOSIFICACION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            EntregaDosificacion()
        {
            if (!TienePermiso(
                    "Entregar dosificaciones"))
            {
                return AccesoNoAutorizado();
            }


            await RegistrarAccionHistorial(
                "Ingresó a Entrega por escáner"
            );


            return View(
                "EntregaDosificacion"
            );
        }



        // =========================================================
        // SCAN -> FEFO -> DESCUENTO -> HISTORIAL
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ProcesarEntregaDosificacion(
                string codigo)
        {
            if (!TienePermiso(
                    "Entregar dosificaciones"))
            {
                return Json(
                    new
                    {
                        exito = false,
                        mensaje =
                            "No tiene permiso para entregar dosificaciones."
                    }
                );
            }


            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idUsuario.HasValue)
            {
                return Json(
                    new
                    {
                        exito = false,
                        mensaje =
                            "La sesión ha finalizado."
                    }
                );
            }


            string codigoNormalizado =
                NormalizarCodigoDosificacionController(
                    codigo
                );


            if (string.IsNullOrWhiteSpace(
                    codigoNormalizado))
            {
                return Json(
                    new
                    {
                        exito = false,
                        mensaje =
                            "Escanee o ingrese un código."
                    }
                );
            }



            // =====================================================
            // USAMOS EL MISMO LISTADO QUE MUESTRA LAS
            // PROGRAMACIONES GUARDADAS.
            //
            // Si DOS-000001 aparece en la pantalla Dosificaciones,
            // necesariamente podrá encontrarse aquí.
            // =====================================================

            var programaciones =
                await _dosificacionRepository
                    .ListarProgramaciones();


            int idDesdeCodigo =
                0;


            if (
                codigoNormalizado.StartsWith(
                    "DOS-",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                int.TryParse(
                    codigoNormalizado.Substring(4),
                    out idDesdeCodigo
                );
            }


            var dosificacion =
                programaciones
                    .FirstOrDefault(
                        d =>
                            d.activo
                            &&
                            (
                                string.Equals(
                                    NormalizarCodigoDosificacionController(
                                        d.codigo
                                    ),
                                    codigoNormalizado,
                                    StringComparison.OrdinalIgnoreCase
                                )
                                ||
                                (
                                    idDesdeCodigo > 0
                                    &&
                                    d.id_dosificacion
                                        ==
                                        idDesdeCodigo
                                )
                            )
                    );


            if (dosificacion == null)
            {
                return Json(
                    new
                    {
                        exito = false,
                        codigoRecibido =
                            codigoNormalizado,
                        mensaje =
                            $"No se encontró la programación {codigoNormalizado}. Revise que aparezca en Dosificaciones guardadas."
                    }
                );
            }



            // =====================================================
            // SCAN CONFIRMADO -> FEFO -> DESCUENTO
            // =====================================================

            var resultado =
                await _movimientoRepository
                    .RegistrarEntregaPaciente(
                        dosificacion.id_ubicacion,
                        dosificacion.id_producto,
                        dosificacion.cantidad,
                        idUsuario.Value
                    );


            if (!resultado.exito)
            {
                return Json(
                    new
                    {
                        exito = false,

                        codigo =
                            dosificacion.codigo,

                        producto =
                            dosificacion.nombre_producto,

                        cantidad =
                            dosificacion.cantidad,

                        unidad =
                            dosificacion.unidad_medida,

                        clinica =
                            dosificacion.nombre_clinica,

                        mensaje =
                            resultado.mensaje
                    }
                );
            }


            bool stockBajo =
                resultado.stockResultante
                <=
                dosificacion.stock_minimo;


            await RegistrarAccionHistorial(
                $"Entrega dosificada por escáner "
                +
                $"{dosificacion.codigo}: "
                +
                $"{dosificacion.cantidad} "
                +
                $"{dosificacion.unidad_medida} "
                +
                $"de {dosificacion.nombre_producto} "
                +
                $"desde {dosificacion.nombre_clinica}. "
                +
                $"Stock: "
                +
                $"{resultado.stockAnterior} "
                +
                $"→ "
                +
                $"{resultado.stockResultante}. "
                +
                $"FEFO: "
                +
                $"{resultado.detalleLotes}"
            );


            return Json(
                new
                {
                    exito = true,

                    codigo =
                        dosificacion.codigo,

                    producto =
                        dosificacion.nombre_producto,

                    concentracion =
                        dosificacion.concentracion,

                    cantidad =
                        dosificacion.cantidad,

                    unidad =
                        dosificacion.unidad_medida,

                    clinica =
                        dosificacion.nombre_clinica,

                    stockAnterior =
                        resultado.stockAnterior,

                    stockResultante =
                        resultado.stockResultante,

                    stockMinimo =
                        dosificacion.stock_minimo,

                    stockBajo,

                    detalleLotes =
                        resultado.detalleLotes,

                    mensaje =
                        "Entrega registrada correctamente."
                }
            );
        }




        // =========================================================
        // BAJA / MERMA DE INVENTARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> BajaInventario()
        {
            if (
                !EsEnfermeraOAdministrador()
                ||
                !TienePermiso(
                    "Registrar bajas de inventario"
                )
            )
            {
                return AccesoNoAutorizado();
            }

            var ubicaciones =
                await _loteRepository
                    .ListadoUbicacionesActivas();

            var idsBodegas =
                ubicaciones
                    .Where(
                        u =>
                            (u.tipo_sector ?? "")
                                .Equals(
                                    "Bodega",
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .Select(
                        u => u.id_ubicacion
                    )
                    .ToHashSet();

            var productos =
                await _productoRepository
                    .ListadoProductos();

            var idsControlados =
                productos
                    .Where(
                        p => p.requiere_control
                    )
                    .Select(
                        p => p.id_producto
                    )
                    .ToHashSet();

            var lotes =
                await _loteRepository
                    .ListadoLotes();

            ViewBag.LotesBodega =
                lotes
                    .Where(
                        l =>
                            l.cantidad_actual > 0
                            &&
                            idsBodegas.Contains(
                                l.id_ubicacion
                            )
                            &&
                            !idsControlados.Contains(
                                l.id_producto
                            )
                    )
                    .OrderBy(
                        l => l.nombre_ubicacion
                    )
                    .ThenBy(
                        l => l.nombre_producto
                    )
                    .ThenBy(
                        l => l.fecha_vencimiento
                    )
                    .ToList();

            ViewBag.UnidadesProductos =
                productos
                    .GroupBy(
                        p => p.id_producto
                    )
                    .ToDictionary(
                        g => g.Key,
                        g =>
                            string.IsNullOrWhiteSpace(
                                g.First().unidad_medida
                            )
                                ? "unidades"
                                : g.First().unidad_medida!
                    );

            return View(
                "BajaInventario",
                new BajaInventario()
            );
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarBajaInventario(
            BajaInventario reg)
        {
            if (
                !EsEnfermeraOAdministrador()
                ||
                !TienePermiso(
                    "Registrar bajas de inventario"
                )
            )
            {
                return AccesoNoAutorizado();
            }

            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }

            var resultado =
                await _movimientoRepository
                    .RegistrarBajaInventario(
                        reg.id_lote,
                        reg.cantidad,
                        reg.motivo ?? "",
                        idUsuario.Value
                    );

            TempData["Mensaje"] =
                resultado.mensaje;

            TempData["TipoMensaje"] =
                resultado.exito
                    ? "exito"
                    : "error";

            if (resultado.exito)
            {
                await RegistrarAccionHistorial(
                    $"Registró baja de {reg.cantidad} {resultado.unidadMedida} de {resultado.nombreProducto}, lote {resultado.numeroLote}, desde {resultado.nombreSector}. Stock: {resultado.stockAnterior} → {resultado.stockResultante}. Motivo: {(reg.motivo ?? "").Trim()}"
                );

                if (
                    resultado.stockResultante
                    <=
                    resultado.stockMinimo
                )
                {
                    TempData["Mensaje"] =
                        resultado.mensaje
                        +
                        " ⚠ El producto quedó en nivel de stock bajo.";
                }
            }

            return RedirectToAction(
                "BajaInventario"
            );
        }



        // =========================================================
        // SALIDA EXTRAORDINARIA A OTRO SERVICIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SalidaExtraordinaria()
        {
            if (
                !EsEnfermeraOAdministrador()
                ||
                !TienePermiso(
                    "Registrar salidas extraordinarias"
                )
            )
            {
                return AccesoNoAutorizado();
            }

            var ubicaciones =
                await _loteRepository
                    .ListadoUbicacionesActivas();

            ViewBag.Clinicas =
                ubicaciones
                    .Where(
                        u =>
                            (u.tipo_sector ?? "")
                                .Equals(
                                    "Clinica",
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .OrderBy(
                        u => u.nombre_sector
                    )
                    .ToList();

            return View(
                "SalidaExtraordinaria",
                new SalidaExtraordinaria()
            );
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarSalidaExtraordinaria(
            SalidaExtraordinaria reg)
        {
            if (
                !EsEnfermeraOAdministrador()
                ||
                !TienePermiso(
                    "Registrar salidas extraordinarias"
                )
            )
            {
                return AccesoNoAutorizado();
            }

            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }

            var resultado =
                await _movimientoRepository
                    .RegistrarSalidaExtraordinaria(
                        reg.id_ubicacion,
                        reg.id_producto,
                        reg.cantidad,
                        reg.servicio_destino ?? "",
                        reg.motivo ?? "",
                        idUsuario.Value
                    );

            TempData["Mensaje"] =
                resultado.mensaje;

            TempData["TipoMensaje"] =
                resultado.exito
                    ? "exito"
                    : "error";

            if (resultado.exito)
            {
                await RegistrarAccionHistorial(
                    $"Registró salida extraordinaria de {reg.cantidad} {resultado.unidadMedida} de {resultado.nombreProducto} desde {resultado.nombreSector} hacia {(reg.servicio_destino ?? "").Trim()}. Stock: {resultado.stockAnterior} → {resultado.stockResultante}. FEFO: {resultado.detalleLotes}. Motivo: {(reg.motivo ?? "").Trim()}"
                );

                if (
                    resultado.stockResultante
                    <=
                    resultado.stockMinimo
                )
                {
                    TempData["Mensaje"] =
                        resultado.mensaje
                        +
                        " ⚠ El producto quedó en nivel de stock bajo.";
                }
            }

            return RedirectToAction(
                "SalidaExtraordinaria"
            );
        }



        // =========================================================
        // HISTORIAL OPERACIONAL POR SECTOR
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> HistorialSectores(
            int? idUbicacion = null)
        {
            if (!TienePermiso(
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }

            var ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();

            ViewBag.Ubicaciones =
                ubicaciones;

            ViewBag.IdUbicacion =
                idUbicacion;

            List<MovimientoSectorHistorial> movimientos =
                new List<MovimientoSectorHistorial>();

            if (
                idUbicacion.HasValue
                &&
                idUbicacion.Value > 0
            )
            {
                movimientos =
                    await _movimientoRepository
                        .ListadoMovimientosSector(
                            idUbicacion.Value
                        );

                string nombreSector =
                    ubicaciones
                        .FirstOrDefault(
                            u =>
                                u.id_ubicacion
                                ==
                                idUbicacion.Value
                        )
                        ?.nombre_sector
                    ?? "sector";

                await RegistrarAccionHistorial(
                    $"Consultó el historial operacional del sector {nombreSector}"
                );
            }

            return View(
                "HistorialSectores",
                movimientos
            );
        }



        // =========================================================
        // SALIDAS DEL TURNO / RANGO HORARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> HistorialSalidasTurno(
            DateTime? fecha = null,
            string horaInicio = "08:00",
            string horaFin = "00:00")
        {
            bool puedeVer =
                TienePermiso(
                    "Entregar dosificaciones"
                )
                ||
                TienePermiso(
                    "Gestionar medicamentos controlados"
                )
                ||
                TienePermiso(
                    "Registrar salidas extraordinarias"
                );


            if (!puedeVer)
            {
                return AccesoNoAutorizado();
            }


            HistorialSalidasTurnoViewModel modelo =
                await ConstruirModeloSalidasTurno(
                    fecha,
                    horaInicio,
                    horaFin
                );


            await RegistrarAccionHistorial(
                $"Consultó salidas del turno desde "
                +
                $"{modelo.inicio_real:dd/MM/yyyy HH:mm} "
                +
                $"hasta "
                +
                $"{modelo.fin_real:dd/MM/yyyy HH:mm}"
            );


            return View(
                "HistorialSalidasTurno",
                modelo
            );
        }



        // =========================================================
        // INFORME PRESENTACION DE SALIDAS DEL TURNO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> InformeSalidasTurno(
            DateTime? fecha = null,
            string horaInicio = "08:00",
            string horaFin = "00:00")
        {
            bool puedeVer =
                TienePermiso(
                    "Entregar dosificaciones"
                )
                ||
                TienePermiso(
                    "Gestionar medicamentos controlados"
                )
                ||
                TienePermiso(
                    "Registrar salidas extraordinarias"
                );


            if (!puedeVer)
            {
                return AccesoNoAutorizado();
            }


            HistorialSalidasTurnoViewModel modelo =
                await ConstruirModeloSalidasTurno(
                    fecha,
                    horaInicio,
                    horaFin
                );


            string nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                )
                ?? "";


            string apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                )
                ?? "";


            string rol =
                HttpContext.Session.GetString(
                    "Rol"
                )
                ?? "";


            ViewBag.GeneradoPor =
                $"{nombre} {apellido}"
                    .Trim();


            ViewBag.RolGenerador =
                rol;


            ViewBag.FechaGeneracion =
                DateTime.Now;


            await RegistrarAccionHistorial(
                $"Generó informe de salidas del turno desde "
                +
                $"{modelo.inicio_real:dd/MM/yyyy HH:mm} "
                +
                $"hasta "
                +
                $"{modelo.fin_real:dd/MM/yyyy HH:mm}"
            );


            return View(
                "InformeSalidasTurno",
                modelo
            );
        }



        // =========================================================
        // DESCARGAR INFORME DE SALIDAS DEL TURNO
        // Genera el archivo HTML directamente desde el servidor.
        // No depende de JavaScript del navegador.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DescargarInformeSalidasTurno(
            DateTime? fecha = null,
            string horaInicio = "08:00",
            string horaFin = "00:00")
        {
            bool puedeVer =
                TienePermiso(
                    "Entregar dosificaciones"
                )
                ||
                TienePermiso(
                    "Gestionar medicamentos controlados"
                )
                ||
                TienePermiso(
                    "Registrar salidas extraordinarias"
                );


            if (!puedeVer)
            {
                return AccesoNoAutorizado();
            }


            HistorialSalidasTurnoViewModel modelo =
                await ConstruirModeloSalidasTurno(
                    fecha,
                    horaInicio,
                    horaFin
                );


            string nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                )
                ?? "";


            string apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                )
                ?? "";


            string rol =
                HttpContext.Session.GetString(
                    "Rol"
                )
                ?? "";


            string responsable =
                $"{nombre} {apellido}"
                    .Trim();


            string E(
                string? valor)
            {
                return WebUtility.HtmlEncode(
                    valor ?? ""
                );
            }


            var detalles =
                modelo.resumen
                    .SelectMany(
                        r => r.detalles
                    )
                    .OrderBy(
                        d => d.fecha_hora
                    )
                    .ThenBy(
                        d => d.nombre_producto
                    )
                    .ToList();


            int totalMovimientos =
                detalles
                    .Select(
                        d => d.id_movimiento
                    )
                    .Distinct()
                    .Count();


            StringBuilder html =
                new StringBuilder();


            html.Append(
                """
                <!DOCTYPE html>
                <html lang="es">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Informe de salidas por turno</title>
                    <style>
                        *{box-sizing:border-box}
                        body{margin:0;padding:28px;background:#eef3f6;color:#20384a;font-family:Arial,Helvetica,sans-serif}
                        .documento{width:min(1050px,100%);margin:0 auto;padding:38px 42px;background:#fff}
                        .cabecera{display:flex;justify-content:space-between;gap:25px;padding-bottom:20px;border-bottom:3px solid #0c7898}
                        .marca{color:#087bc2;font-size:15px;font-weight:900;letter-spacing:.5px}
                        h1{margin:7px 0 4px;color:#17324d;font-size:28px}
                        .subtitulo{margin:0;color:#667d8d;font-size:13px}
                        .folio{min-width:210px;text-align:right;color:#5e7484;font-size:12px;line-height:1.6}
                        .datos{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin:22px 0}
                        .dato{padding:12px 13px;border:1px solid #d9e5ec;border-radius:9px;background:#f8fbfd}
                        .dato small{display:block;color:#738897;font-size:10px;font-weight:800;text-transform:uppercase}
                        .dato strong{display:block;margin-top:4px;color:#17324d;font-size:13px}
                        .seccion{margin-top:28px}
                        .seccion h2{margin:0 0 4px;color:#17324d;font-size:19px}
                        .seccion p{margin:0 0 10px;color:#718493;font-size:11px}
                        table{width:100%;border-collapse:collapse}
                        th{padding:10px 9px;background:#0d6d8e;color:#fff;text-align:left;font-size:10px;text-transform:uppercase}
                        td{padding:10px 9px;border-bottom:1px solid #e4ebef;color:#314c5f;font-size:11px;vertical-align:top}
                        tbody tr:nth-child(even){background:#f8fbfc}
                        .med{font-weight:900;color:#17324d}
                        .conc{display:block;margin-top:2px;color:#788d9a;font-size:10px}
                        .total{font-size:15px;font-weight:900;color:#0d7189}
                        .controlado{font-weight:800;color:#ae3a49}
                        .normal{font-weight:800;color:#3c714a}
                        .nota{margin-top:10px;padding:10px 12px;border-left:4px solid #0b8098;background:#eff8fb;color:#4d6879;font-size:10px;line-height:1.45}
                        .pie{display:flex;justify-content:space-between;gap:20px;margin-top:34px;padding-top:15px;border-top:1px solid #dce6eb;color:#71828e;font-size:9px}
                        @media(max-width:800px){body{padding:12px}.documento{padding:22px}.cabecera{flex-direction:column}.folio{text-align:left}.datos{grid-template-columns:repeat(2,minmax(0,1fr))}}
                        @media print{body{padding:0;background:#fff}.documento{width:100%;padding:20px}.seccion{break-inside:avoid}tr{page-break-inside:avoid}}
                    </style>
                </head>
                <body>
                    <main class="documento">
                        <header class="cabecera">
                            <div>
                                <div class="marca">SARMedix · SAR de Chiguay</div>
                                <h1>Informe de salidas por turno</h1>
                                <p class="subtitulo">Consolidado de medicamentos entregados durante el periodo seleccionado.</p>
                            </div>
                            <div class="folio">
                """
            );


            html.Append(
                $"<div><strong>Generado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</div>"
            );

            html.Append(
                $"<div><strong>Responsable:</strong> {E(string.IsNullOrWhiteSpace(responsable) ? "-" : responsable)}</div>"
            );

            html.Append(
                $"<div><strong>Perfil:</strong> {E(string.IsNullOrWhiteSpace(rol) ? "-" : rol)}</div>"
            );


            html.Append(
                """
                            </div>
                        </header>
                        <section class="datos">
                """
            );


            html.Append(
                $"<div class=\"dato\"><small>Inicio del corte</small><strong>{modelo.inicio_real:dd/MM/yyyy HH:mm}</strong></div>"
            );

            html.Append(
                $"<div class=\"dato\"><small>Fin del corte</small><strong>{modelo.fin_real:dd/MM/yyyy HH:mm}</strong></div>"
            );

            html.Append(
                $"<div class=\"dato\"><small>Medicamentos distintos</small><strong>{modelo.resumen.Count}</strong></div>"
            );

            html.Append(
                $"<div class=\"dato\"><small>Registros de salida</small><strong>{totalMovimientos}</strong></div>"
            );


            html.Append(
                """
                        </section>
                        <section class="seccion">
                            <h2>1. Resumen total de salidas</h2>
                            <p>Totales consolidados por medicamento.</p>
                            <table>
                                <thead>
                                    <tr>
                                        <th>#</th>
                                        <th>Medicamento</th>
                                        <th>Total</th>
                                        <th>Unidad</th>
                                        <th>Tipo</th>
                                        <th>Registros</th>
                                    </tr>
                                </thead>
                                <tbody>
                """
            );


            int numero =
                1;


            foreach (
                var item in modelo.resumen
            )
            {
                string concentracion =
                    string.IsNullOrWhiteSpace(
                        item.concentracion
                    )
                        ? ""
                        : $"<span class=\"conc\">{E(item.concentracion)}</span>";


                string tipo =
                    item.es_controlado
                        ? "<span class=\"controlado\">Controlado</span>"
                        : "<span class=\"normal\">Salida clínica</span>";


                html.Append(
                    $"""
                    <tr>
                        <td>{numero}</td>
                        <td>
                            <span class="med">{E(item.nombre_producto)}</span>
                            {concentracion}
                        </td>
                        <td><span class="total">{item.cantidad_total}</span></td>
                        <td>{E(item.unidad_medida)}</td>
                        <td>{tipo}</td>
                        <td>{item.detalles.Count}</td>
                    </tr>
                    """
                );


                numero++;
            }


            if (modelo.resumen.Count == 0)
            {
                html.Append(
                    """
                    <tr>
                        <td colspan="6">
                            No existen salidas para el periodo consultado.
                        </td>
                    </tr>
                    """
                );
            }


            html.Append(
                """
                                </tbody>
                            </table>
                            <div class="nota">
                                Los totales representan la suma de las cantidades entregadas dentro del horario seleccionado.
                            </div>
                        </section>

                        <section class="seccion">
                            <h2>2. Detalle cronológico por hora</h2>
                            <p>Respaldo de cada salida registrada durante el corte.</p>
                            <table>
                                <thead>
                                    <tr>
                                        <th>Hora</th>
                                        <th>Medicamento</th>
                                        <th>Cantidad</th>
                                        <th>Tipo</th>
                                        <th>Sector</th>
                                        <th>Responsable</th>
                                        <th>Lote(s)</th>
                                    </tr>
                                </thead>
                                <tbody>
                """
            );


            foreach (
                var detalle in detalles
            )
            {
                string concentracion =
                    string.IsNullOrWhiteSpace(
                        detalle.concentracion
                    )
                        ? ""
                        : $"<span class=\"conc\">{E(detalle.concentracion)}</span>";


                html.Append(
                    $"""
                    <tr>
                        <td>{detalle.fecha_hora:dd/MM HH:mm}</td>
                        <td>
                            <span class="med">{E(detalle.nombre_producto)}</span>
                            {concentracion}
                        </td>
                        <td>{detalle.cantidad} {E(detalle.unidad_medida)}</td>
                        <td>{E(detalle.tipo_salida)}</td>
                        <td>{E(detalle.sector)}</td>
                        <td>{E(detalle.responsable)}</td>
                        <td>{E(detalle.lotes)}</td>
                    </tr>
                    """
                );
            }


            if (detalles.Count == 0)
            {
                html.Append(
                    """
                    <tr>
                        <td colspan="7">
                            No existen movimientos para mostrar.
                        </td>
                    </tr>
                    """
                );
            }


            html.Append(
                $"""
                                </tbody>
                            </table>
                        </section>

                        <footer class="pie">
                            <span>Informe generado automáticamente por SARMedix.</span>
                            <span>
                                Periodo:
                                {modelo.inicio_real:dd/MM/yyyy HH:mm}
                                →
                                {modelo.fin_real:dd/MM/yyyy HH:mm}
                            </span>
                        </footer>
                    </main>
                </body>
                </html>
                """
            );


            string nombreArchivo =
                $"Informe_Salidas_{modelo.fecha:yyyy-MM-dd}_"
                +
                $"{modelo.hora_inicio.Replace(":", "")}-"
                +
                $"{modelo.hora_fin.Replace(":", "")}.html";


            await RegistrarAccionHistorial(
                $"Descargó informe de salidas del turno desde "
                +
                $"{modelo.inicio_real:dd/MM/yyyy HH:mm} "
                +
                $"hasta "
                +
                $"{modelo.fin_real:dd/MM/yyyy HH:mm}"
            );


            return File(
                Encoding.UTF8.GetBytes(
                    html.ToString()
                ),
                "text/html; charset=utf-8",
                nombreArchivo
            );
        }



        // =========================================================
        // CONSTRUIR MODELO COMUN PARA PANTALLA E INFORME
        // =========================================================

        private async Task<HistorialSalidasTurnoViewModel>
            ConstruirModeloSalidasTurno(
                DateTime? fecha,
                string horaInicio,
                string horaFin)
        {
            DateTime dia =
                (fecha ?? DateTime.Today)
                    .Date;


            if (!TimeSpan.TryParse(
                    horaInicio,
                    out TimeSpan horaInicioValor))
            {
                horaInicioValor =
                    new TimeSpan(
                        8,
                        0,
                        0
                    );

                horaInicio =
                    "08:00";
            }


            if (!TimeSpan.TryParse(
                    horaFin,
                    out TimeSpan horaFinValor))
            {
                horaFinValor =
                    TimeSpan.Zero;

                horaFin =
                    "00:00";
            }


            DateTime inicio =
                dia.Add(
                    horaInicioValor
                );


            DateTime fin =
                dia.Add(
                    horaFinValor
                );


            // Ejemplo:
            // 08:00 -> 00:00 significa medianoche del día siguiente.
            if (fin <= inicio)
            {
                fin =
                    fin.AddDays(
                        1
                    );
            }


            bool puedeVerControlados =
                TienePermiso(
                    "Gestionar medicamentos controlados"
                );


            var detalles =
                await _movimientoRepository
                    .ListadoSalidasTurno(
                        inicio,
                        fin,
                        puedeVerControlados
                    );


            var resumen =
                detalles
                    .GroupBy(
                        d => new
                        {
                            d.id_producto,
                            d.nombre_producto,
                            d.concentracion,
                            d.unidad_medida,
                            d.es_controlado
                        }
                    )
                    .Select(
                        grupo =>
                            new ResumenSalidaTurno()
                            {
                                id_producto =
                                    grupo.Key.id_producto,

                                nombre_producto =
                                    grupo.Key.nombre_producto,

                                concentracion =
                                    grupo.Key.concentracion,

                                unidad_medida =
                                    grupo.Key.unidad_medida,

                                es_controlado =
                                    grupo.Key.es_controlado,

                                cantidad_total =
                                    grupo.Sum(
                                        d => d.cantidad
                                    ),

                                detalles =
                                    grupo
                                        .OrderBy(
                                            d => d.fecha_hora
                                        )
                                        .ToList()
                            }
                    )
                    .OrderByDescending(
                        r => r.cantidad_total
                    )
                    .ThenBy(
                        r => r.nombre_producto
                    )
                    .ToList();


            return new HistorialSalidasTurnoViewModel()
            {
                fecha =
                    dia,

                hora_inicio =
                    horaInicio,

                hora_fin =
                    horaFin,

                inicio_real =
                    inicio,

                fin_real =
                    fin,

                puede_ver_controlados =
                    puedeVerControlados,

                resumen =
                    resumen
            };
        }



        // =========================================================
        // HISTORIAL GENERAL
        // BUSQUEDA + FILTROS + PAGINACION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Historial(
            string? buscar = null,
            string? filtro = "todos",
            string? periodo = "todos",
            int pagina = 1)
        {
            if (!TienePermiso(
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _historialRepository
                    .ListadoHistorial();


            string buscarActual =
                (buscar ?? "")
                    .Trim();


            string filtroActual =
                (filtro ?? "todos")
                    .Trim()
                    .ToLowerInvariant();


            string periodoActual =
                (periodo ?? "todos")
                    .Trim()
                    .ToLowerInvariant();



            // BUSQUEDA

            if (!string.IsNullOrWhiteSpace(
                    buscarActual))
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.usuario_responsable ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ||
                                (h.descripcion ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // FILTRO POR CATEGORIA

            if (filtroActual == "usuarios")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .Contains(
                                        "usuario",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "productos")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .Contains(
                                        "producto",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "lotes")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .Contains(
                                        "lote",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "medicamentos")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .Contains(
                                        "medicamento",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ||
                                (h.descripcion ?? "")
                                    .Contains(
                                        "controlado",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "roles")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .Contains(
                                        "rol",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ||
                                (h.descripcion ?? "")
                                    .Contains(
                                        "permis",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "sistema")
            {
                listado =
                    listado
                        .Where(
                            h =>
                            {
                                string descripcion =
                                    h.descripcion ?? "";


                                bool esUsuario =
                                    descripcion.Contains(
                                        "usuario",
                                        StringComparison.OrdinalIgnoreCase
                                    );


                                bool esProducto =
                                    descripcion.Contains(
                                        "producto",
                                        StringComparison.OrdinalIgnoreCase
                                    );


                                bool esLote =
                                    descripcion.Contains(
                                        "lote",
                                        StringComparison.OrdinalIgnoreCase
                                    );


                                bool esMedicamento =
                                    descripcion.Contains(
                                        "medicamento",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.Contains(
                                        "controlado",
                                        StringComparison.OrdinalIgnoreCase
                                    );


                                bool esRol =
                                    descripcion.Contains(
                                        "rol",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.Contains(
                                        "permis",
                                        StringComparison.OrdinalIgnoreCase
                                    );


                                return
                                    !esUsuario
                                    &&
                                    !esProducto
                                    &&
                                    !esLote
                                    &&
                                    !esMedicamento
                                    &&
                                    !esRol;
                            }
                        )
                        .ToList();
            }



            // FILTRO POR PERIODO

            DateTime hoy =
                DateTime.Today;


            if (periodoActual == "hoy")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                ==
                                hoy
                        )
                        .ToList();
            }


            if (periodoActual == "7")
            {
                DateTime desde =
                    hoy.AddDays(
                        -6
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }


            if (periodoActual == "30")
            {
                DateTime desde =
                    hoy.AddDays(
                        -29
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }



            // PAGINACION

            int registrosPorPagina =
                10;


            int totalRegistros =
                listado.Count;


            int totalPaginas =
                totalRegistros == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalRegistros
                        /
                        (double)registrosPorPagina
                    );


            if (pagina < 1)
            {
                pagina = 1;
            }


            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina =
                    totalPaginas;
            }


            var listadoPagina =
                listado
                    .Skip(
                        (pagina - 1)
                        *
                        registrosPorPagina
                    )
                    .Take(
                        registrosPorPagina
                    )
                    .ToList();


            ViewBag.PaginaActual =
                pagina;


            ViewBag.TotalPaginas =
                totalPaginas;


            ViewBag.TotalRegistros =
                totalRegistros;


            ViewBag.Buscar =
                buscarActual;


            ViewBag.Filtro =
                filtroActual;


            ViewBag.Periodo =
                periodoActual;


            return View(
                listadoPagina
            );
        }



        // =========================================================
        // HISTORIAL DE USUARIOS
        // BUSQUEDA + FILTROS + PAGINACION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> HistorialUsuarios(
            string? buscar = null,
            string? filtro = "todos",
            string? periodo = "todos",
            int pagina = 1)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _historialRepository
                    .ListadoHistorialUsuarios();


            string buscarActual =
                (buscar ?? "")
                    .Trim();


            string filtroActual =
                (filtro ?? "todos")
                    .Trim()
                    .ToLowerInvariant();


            string periodoActual =
                (periodo ?? "todos")
                    .Trim()
                    .ToLowerInvariant();



            // BUSQUEDA

            if (!string.IsNullOrWhiteSpace(
                    buscarActual))
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.usuario_responsable ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ||
                                (h.descripcion ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // FILTRO POR ACCION

            if (filtroActual == "crear")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Creó al usuario ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "actualizar")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Actualizó al usuario ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "desactivar")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Desactivó al usuario ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "reactivar")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Reactivó al usuario ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }


            if (filtroActual == "eliminar")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Eliminó al usuario ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // PERIODO

            DateTime hoy =
                DateTime.Today;


            if (periodoActual == "hoy")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                ==
                                hoy
                        )
                        .ToList();
            }


            if (periodoActual == "7")
            {
                DateTime desde =
                    hoy.AddDays(
                        -6
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }


            if (periodoActual == "30")
            {
                DateTime desde =
                    hoy.AddDays(
                        -29
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }



            // PAGINACION

            int registrosPorPagina =
                10;


            int totalRegistros =
                listado.Count;


            int totalPaginas =
                totalRegistros == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalRegistros
                        /
                        (double)registrosPorPagina
                    );


            if (pagina < 1)
            {
                pagina = 1;
            }


            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina =
                    totalPaginas;
            }


            var listadoPagina =
                listado
                    .Skip(
                        (pagina - 1)
                        *
                        registrosPorPagina
                    )
                    .Take(
                        registrosPorPagina
                    )
                    .ToList();


            ViewBag.PaginaActual =
                pagina;


            ViewBag.TotalPaginas =
                totalPaginas;


            ViewBag.TotalRegistros =
                totalRegistros;


            ViewBag.Buscar =
                buscarActual;


            ViewBag.Filtro =
                filtroActual;


            ViewBag.Periodo =
                periodoActual;


            return View(
                listadoPagina
            );
        }



        // =========================================================
        // HISTORIAL DE PRODUCTOS
        // BUSQUEDA + FILTROS + PAGINACION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> HistorialProductos(
            string? buscar = null,
            string? filtro = "todos",
            string? periodo = "todos",
            int pagina = 1)
        {
            if (!TienePermiso(
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _historialRepository
                    .ListadoHistorialProductos();


            string buscarActual =
                (buscar ?? "")
                    .Trim();


            string filtroActual =
                (filtro ?? "todos")
                    .Trim()
                    .ToLowerInvariant();


            string periodoActual =
                (periodo ?? "todos")
                    .Trim()
                    .ToLowerInvariant();



            // BUSQUEDA

            if (!string.IsNullOrWhiteSpace(
                    buscarActual))
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.usuario_responsable ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ||
                                (h.descripcion ?? "")
                                    .Contains(
                                        buscarActual,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // FILTRO PRODUCTOS

            if (filtroActual == "productos")
            {
                listado =
                    listado
                        .Where(
                            h =>
                            {
                                string descripcion =
                                    h.descripcion ?? "";


                                return
                                    descripcion.StartsWith(
                                        "Creó el producto ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Actualizó el producto ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Eliminó el producto ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Desactivó el producto ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Reactivó el producto ",
                                        StringComparison.OrdinalIgnoreCase
                                    );
                            }
                        )
                        .ToList();
            }



            // FILTRO LOTES

            if (filtroActual == "lotes")
            {
                listado =
                    listado
                        .Where(
                            h =>
                            {
                                string descripcion =
                                    h.descripcion ?? "";


                                return
                                    descripcion.StartsWith(
                                        "Creó el lote ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Actualizó el lote ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    ||
                                    descripcion.StartsWith(
                                        "Eliminó el lote ",
                                        StringComparison.OrdinalIgnoreCase
                                    );
                            }
                        )
                        .ToList();
            }



            // FILTRO ENTRADAS

            if (filtroActual == "entradas")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Registró entrada ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // FILTRO SALIDAS

            if (filtroActual == "salidas")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                (h.descripcion ?? "")
                                    .StartsWith(
                                        "Registró salida ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                        .ToList();
            }



            // PERIODO

            DateTime hoy =
                DateTime.Today;


            if (periodoActual == "hoy")
            {
                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                ==
                                hoy
                        )
                        .ToList();
            }


            if (periodoActual == "7")
            {
                DateTime desde =
                    hoy.AddDays(
                        -6
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }


            if (periodoActual == "30")
            {
                DateTime desde =
                    hoy.AddDays(
                        -29
                    );


                listado =
                    listado
                        .Where(
                            h =>
                                h.fecha_hora.Date
                                >=
                                desde
                        )
                        .ToList();
            }



            // PAGINACION

            int registrosPorPagina =
                10;


            int totalRegistros =
                listado.Count;


            int totalPaginas =
                totalRegistros == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalRegistros
                        /
                        (double)registrosPorPagina
                    );


            if (pagina < 1)
            {
                pagina = 1;
            }


            if (totalPaginas > 0 &&
                pagina > totalPaginas)
            {
                pagina =
                    totalPaginas;
            }


            var listadoPagina =
                listado
                    .Skip(
                        (pagina - 1)
                        *
                        registrosPorPagina
                    )
                    .Take(
                        registrosPorPagina
                    )
                    .ToList();


            ViewBag.PaginaActual =
                pagina;


            ViewBag.TotalPaginas =
                totalPaginas;


            ViewBag.TotalRegistros =
                totalRegistros;


            ViewBag.Buscar =
                buscarActual;


            ViewBag.Filtro =
                filtroActual;


            ViewBag.Periodo =
                periodoActual;


            return View(
                listadoPagina
            );
        }



        // =========================================================
        // MEDICAMENTOS CONTROLADOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MedicamentosControlados()
        {
            if (!TienePermiso(
                    "Gestionar medicamentos controlados"))
            {
                return AccesoNoAutorizado();
            }


            var productos =
                await _productoRepository
                    .ListadoProductos();


            var medicamentosControlados =
                productos
                    .Where(
                        p =>
                            string.Equals(
                                p.tipo,
                                "Medicamento",
                                StringComparison.OrdinalIgnoreCase
                            )
                            &&
                            p.requiere_control
                    )
                    .OrderBy(
                        p => p.nombre
                    )
                    .ToList();


            var lotes =
                await _loteRepository
                    .ListadoLotes();


            var idsMedicamentosControlados =
                medicamentosControlados
                    .Select(
                        p => p.id_producto
                    )
                    .ToHashSet();


            ViewBag.LotesControlados =
                lotes
                    .Where(
                        l =>
                            idsMedicamentosControlados
                                .Contains(
                                    l.id_producto
                                )
                    )
                    .OrderBy(
                        l => l.fecha_vencimiento
                    )
                    .ToList();


            // CARGAR HISTORIAL REAL DE MOVIMIENTOS
            // DE MEDICAMENTOS CONTROLADOS

            ViewBag.MovimientosControlados =
                await _movimientoRepository
                    .ListadoMovimientosControlados();


            await RegistrarAccionHistorial(
                "Ingresó al módulo Medicamentos Controlados"
            );


            return View(
                "~/Views/Home/Modulos/MedicamentosControlados.cshtml",
                medicamentosControlados
            );
        }


        // =========================================================
        // UBICACIONES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Ubicaciones()
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";

            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            var ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            var lotes =
                await _loteRepository
                    .ListadoLotes();


            var productos =
                await _productoRepository
                    .ListadoProductos();


            ViewBag.LotesUbicaciones =
                lotes;


            ViewBag.ProductosUbicaciones =
                productos;


            await RegistrarAccionHistorial(
                "Ingresó al módulo Ubicaciones"
            );


            return View(
                "~/Views/Home/Ubicaciones.cshtml",
                ubicaciones
            );
        }



        // =========================================================
        // REGISTRAR MOVIMIENTO CONTROLADO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMovimientoControlado(
        int idLote,
        string tipoMovimiento,
        int cantidad,
        string observacion)
        {
            if (!TienePermiso(
                    "Gestionar medicamentos controlados"))
            {
                return AccesoNoAutorizado();
            }


            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            var resultado =
                await _movimientoRepository
                    .RegistrarMovimientoControlado(
                        idLote,
                        tipoMovimiento,
                        cantidad,
                        observacion,
                        idUsuario.Value
                    );


            if (resultado.exito)
            {
                string tipoTexto =
                    (tipoMovimiento ?? "movimiento")
                        .Trim()
                        .ToLowerInvariant();


                await RegistrarAccionHistorial(
                    $"Registró {tipoTexto} de {cantidad} unidad(es) del medicamento {resultado.nombreProducto}, lote {resultado.numeroLote}. Stock: {resultado.stockAnterior} → {resultado.stockResultante}. Motivo: {observacion.Trim()}"
                );


                TempData["Mensaje"] =
                    resultado.mensaje;


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    resultado.mensaje;


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "MedicamentosControlados"
            );
        }
        // =========================================================
        // CREAR UBICACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUbicacion(
            string nombreSector,
            string tipoSector)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            nombreSector =
                (nombreSector ?? "")
                    .Trim();


            tipoSector =
                (tipoSector ?? "")
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                    nombreSector))
            {
                TempData["Mensaje"] =
                    "Debe ingresar un nombre para el sector.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            bool tipoValido =
                tipoSector.Equals(
                    "Bodega",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                tipoSector.Equals(
                    "Clinica",
                    StringComparison.OrdinalIgnoreCase
                );


            if (!tipoValido)
            {
                TempData["Mensaje"] =
                    "Debe seleccionar un tipo de sector válido.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            tipoSector =
                tipoSector.Equals(
                    "Bodega",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Bodega"
                    : "Clinica";


            bool existe =
                await _loteRepository
                    .ExisteUbicacion(
                        nombreSector
                    );


            if (existe)
            {
                TempData["Mensaje"] =
                    "Ya existe un sector con ese nombre.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            bool creada =
                await _loteRepository
                    .CrearUbicacion(
                        nombreSector,
                        tipoSector
                    );


            if (creada)
            {
                string tipoVisible =
                    tipoSector == "Clinica"
                        ? "Clínica"
                        : "Bodega";


                await RegistrarAccionHistorial(
                    $"Creó el sector {nombreSector} de tipo {tipoVisible}"
                );


                TempData["Mensaje"] =
                    "Sector creado correctamente.";

                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo crear el sector.";

                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Ubicaciones"
            );
        }


        // =========================================================
        // EDITAR UBICACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUbicacion(
            int idUbicacion,
            string nombreSector,
            string tipoSector)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            nombreSector =
                (nombreSector ?? "")
                    .Trim();


            tipoSector =
                (tipoSector ?? "")
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                    nombreSector))
            {
                TempData["Mensaje"] =
                    "Debe ingresar un nombre para el sector.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            bool tipoValido =
                tipoSector.Equals(
                    "Bodega",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                tipoSector.Equals(
                    "Clinica",
                    StringComparison.OrdinalIgnoreCase
                );


            if (!tipoValido)
            {
                TempData["Mensaje"] =
                    "Debe seleccionar un tipo de sector válido.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            tipoSector =
                tipoSector.Equals(
                    "Bodega",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Bodega"
                    : "Clinica";


            var ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            var ubicacionActual =
                ubicaciones.FirstOrDefault(
                    u =>
                        u.id_ubicacion == idUbicacion
                );


            if (ubicacionActual == null)
            {
                TempData["Mensaje"] =
                    "El sector seleccionado no existe.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            string tipoActual =
                (ubicacionActual.tipo_sector ?? "")
                    .Trim();


            bool cambiaTipo =
                !tipoActual.Equals(
                    tipoSector,
                    StringComparison.OrdinalIgnoreCase
                );


            if (cambiaTipo)
            {
                bool tieneLotes =
                    await _loteRepository
                        .TieneLotesUbicacion(
                            idUbicacion
                        );


                if (tieneLotes)
                {
                    TempData["Mensaje"] =
                        "No se puede cambiar el tipo del sector porque tiene lotes asociados. Esto protege la trazabilidad histórica.";

                    TempData["TipoMensaje"] =
                        "error";


                    return RedirectToAction(
                        "Ubicaciones"
                    );
                }
            }


            bool existe =
                await _loteRepository
                    .ExisteUbicacionEditar(
                        idUbicacion,
                        nombreSector
                    );


            if (existe)
            {
                TempData["Mensaje"] =
                    "Ya existe otro sector con ese nombre.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            bool actualizado =
                await _loteRepository
                    .ActualizarUbicacion(
                        idUbicacion,
                        nombreSector,
                        tipoSector
                    );


            if (actualizado)
            {
                string tipoVisible =
                    tipoSector == "Clinica"
                        ? "Clínica"
                        : "Bodega";


                await RegistrarAccionHistorial(
                    $"Actualizó el sector {nombreSector}. Tipo: {tipoVisible}"
                );


                TempData["Mensaje"] =
                    "Sector actualizado correctamente.";

                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo actualizar el sector.";

                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Ubicaciones"
            );
        }


        // =========================================================
        // INACTIVAR UBICACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InactivarUbicacion(
            int idUbicacion,
            string nombreUbicacion)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            int stock =
                await _loteRepository
                    .ObtenerStockUbicacion(
                        idUbicacion
                    );


            if (stock > 0)
            {
                TempData["Mensaje"] =
                    $"No se puede inactivar {nombreUbicacion} porque todavía contiene {stock} unidad(es).";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            bool resultado =
                await _loteRepository
                    .InactivarUbicacion(
                        idUbicacion
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Inactivó la ubicación {nombreUbicacion}"
                );


                TempData["Mensaje"] =
                    "Ubicación inactivada correctamente.";

                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo inactivar la ubicación.";

                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Ubicaciones"
            );
        }

        // =========================================================
        // REACTIVAR UBICACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivarUbicacion(
            int idUbicacion,
            string nombreUbicacion)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            bool resultado =
                await _loteRepository
                    .ReactivarUbicacion(
                        idUbicacion
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Reactivó la ubicación {nombreUbicacion}"
                );


                TempData["Mensaje"] =
                    "Ubicación reactivada correctamente.";

                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo reactivar la ubicación.";

                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Ubicaciones"
            );
        }

        // =========================================================
        // INVENTARIO DE UNA UBICACION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> InventarioUbicacion(
            int idUbicacion)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            var ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            var ubicacion =
                ubicaciones.FirstOrDefault(
                    u =>
                        u.id_ubicacion == idUbicacion
                );


            if (ubicacion == null)
            {
                TempData["Mensaje"] =
                    "La ubicación seleccionada no existe.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            var lotes =
                await _loteRepository
                    .ListadoLotes();


            var lotesUbicacion =
                lotes
                    .Where(
                        l =>
                            l.id_ubicacion == idUbicacion
                    )
                    .OrderBy(
                        l =>
                            l.fecha_vencimiento
                    )
                    .ToList();


            ViewBag.NombreUbicacion =
                ubicacion.nombre_sector;

            ViewBag.UbicacionActiva =
                ubicacion.activo;


            await RegistrarAccionHistorial(
                $"Consultó el inventario de la ubicación {ubicacion.nombre_sector}"
            );


            return View(
                "~/Views/Home/InventarioUbicacion.cshtml",
                lotesUbicacion
            );
        }





        // =========================================================
        // ELIMINAR UBICACION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUbicacion(
            int idUbicacion,
            string nombreUbicacion)
        {
            string rolActual =
                HttpContext.Session.GetString("Rol") ?? "";


            if (!rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AccesoNoAutorizado();
            }


            // =====================================================
            // VERIFICAR STOCK ACTUAL
            // =====================================================

            int stock =
                await _loteRepository
                    .ObtenerStockUbicacion(
                        idUbicacion
                    );


            if (stock > 0)
            {
                TempData["Mensaje"] =
                    $"No se puede eliminar {nombreUbicacion} porque todavía contiene {stock} unidad(es).";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            // =====================================================
            // VERIFICAR SI EXISTEN LOTES HISTORICOS
            // =====================================================

            bool tieneLotes =
                await _loteRepository
                    .TieneLotesUbicacion(
                        idUbicacion
                    );


            if (tieneLotes)
            {
                TempData["Mensaje"] =
                    $"No se puede eliminar {nombreUbicacion} porque tiene lotes asociados. Debe mantenerse inactiva para conservar la trazabilidad.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Ubicaciones"
                );
            }


            // =====================================================
            // ELIMINAR
            // =====================================================

            bool resultado =
                await _loteRepository
                    .EliminarUbicacion(
                        idUbicacion
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Eliminó la ubicación {nombreUbicacion}"
                );


                TempData["Mensaje"] =
                    "Ubicación eliminada correctamente.";

                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo eliminar la ubicación.";

                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Ubicaciones"
            );
        }



        // =========================================================
        // CREAR USUARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CrearUsuario()
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return AccesoNoAutorizado();
            }


            await RegistrarAccionHistorial(
                "Ingresó a Crear Usuario"
            );


            return View();
        }



        // =========================================================
        // VERIFICAR RUT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> VerificarRut(
            string rut)
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return Unauthorized();
            }


            if (string.IsNullOrWhiteSpace(
                    rut))
            {
                return Json(
                    new
                    {
                        existe = false
                    }
                );
            }


            bool existe =
                await _usuarioRepository
                    .ExisteRut(
                        rut
                    );


            return Json(
                new
                {
                    existe
                }
            );
        }



        // =========================================================
        // VERIFICAR CORREO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> VerificarCorreo(
            string correo)
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return Unauthorized();
            }


            if (string.IsNullOrWhiteSpace(
                    correo))
            {
                return Json(
                    new
                    {
                        existe = false
                    }
                );
            }


            bool existe =
                await _usuarioRepository
                    .ExisteCorreo(
                        correo
                    );


            return Json(
                new
                {
                    existe
                }
            );
        }



        // =========================================================
        // VERIFICAR NOMBRE USUARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> VerificarNombreUsuario(
            string nombreUsuario)
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return Unauthorized();
            }


            if (string.IsNullOrWhiteSpace(
                    nombreUsuario))
            {
                return Json(
                    new
                    {
                        existe = false
                    }
                );
            }


            bool existe =
                await _usuarioRepository
                    .ExisteNombreUsuario(
                        nombreUsuario
                    );


            return Json(
                new
                {
                    existe
                }
            );
        }



        // =========================================================
        // NUEVO USUARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> NuevoUsuario(
            Usuario reg)
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _usuarioRepository
                    .CrearUsuario(
                        reg
                    );


            if (resultado)
            {
                int? idResponsable =
                    HttpContext.Session.GetInt32(
                        "IdUsuario"
                    );


                string nombreResponsable =
                    (
                        HttpContext.Session.GetString(
                            "Nombre"
                        )
                        +
                        " "
                        +
                        HttpContext.Session.GetString(
                            "Apellido"
                        )
                    )
                    .Trim();


                if (idResponsable.HasValue)
                {
                    await _historialRepository
                        .RegistrarHistorial(
                            idResponsable.Value,
                            nombreResponsable,
                            $"Creó al usuario {reg.nombre} {reg.apellido} ({reg.nombre_usuario})"
                        );
                }


                TempData["Mensaje"] =
                    "Usuario creado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "Usuarios"
                );
            }


            ViewBag.Mensaje =
                "No se pudo crear el usuario: RUT, correo o nombre de usuario existente";


            return View(
                "CrearUsuario",
                reg
            );
        }



        // =========================================================
        // LISTADO DE USUARIOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Usuarios(
            int pagina = 1)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _usuarioRepository
                    .ListadoUsuarios();


            int registrosPorPagina =
                10;


            int totalRegistros =
                listado.Count;


            int totalPaginas =
                (int)Math.Ceiling(
                    totalRegistros
                    /
                    (double)registrosPorPagina
                );


            var listadoPagina =
                listado
                    .Skip(
                        (pagina - 1)
                        *
                        registrosPorPagina
                    )
                    .Take(
                        registrosPorPagina
                    )
                    .ToList();


            ViewBag.PaginaActual =
                pagina;


            ViewBag.TotalPaginas =
                totalPaginas;


            await RegistrarAccionHistorial(
                "Ingresó al listado de usuarios"
            );


            return View(
                listadoPagina
            );
        }



        // =========================================================
        // DESACTIVAR USUARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> DesactivarUsuario(
            int idUsuario)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var usuario =
                await _usuarioRepository
                    .ObtenerUsuarioPorId(
                        idUsuario
                    );


            var resultado =
                await _usuarioRepository
                    .DesactivarUsuario(
                        idUsuario
                    );


            if (resultado)
            {
                if (usuario != null)
                {
                    await RegistrarAccionHistorial(
                        $"Desactivó al usuario {usuario.nombre} {usuario.apellido} ({usuario.nombre_usuario})"
                    );
                }


                TempData["Mensaje"] =
                    "Usuario desactivado correctamente";


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo desactivar el usuario";


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "Usuarios"
            );
        }



        // =========================================================
        // EDITAR USUARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(
            int idUsuario)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var usuario =
                await _usuarioRepository
                    .ObtenerUsuarioPorId(
                        idUsuario
                    );


            if (usuario == null)
            {
                return RedirectToAction(
                    "Usuarios"
                );
            }


            await RegistrarAccionHistorial(
                $"Ingresó a editar al usuario {usuario.nombre} {usuario.apellido} ({usuario.nombre_usuario})"
            );


            return View(
                usuario
            );
        }



        // =========================================================
        // ACTUALIZAR USUARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ActualizarUsuario(
            Usuario reg)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var usuarioAnterior =
                await _usuarioRepository
                    .ObtenerUsuarioPorId(
                        reg.id_usuario
                    );


            List<string> cambios =
                new List<string>();


            if (usuarioAnterior != null)
            {
                if (usuarioAnterior.nombre != reg.nombre)
                {
                    cambios.Add(
                        $"Nombre: '{usuarioAnterior.nombre}' → '{reg.nombre}'"
                    );
                }


                if (usuarioAnterior.apellido != reg.apellido)
                {
                    cambios.Add(
                        $"Apellido: '{usuarioAnterior.apellido}' → '{reg.apellido}'"
                    );
                }


                if (usuarioAnterior.nombre_usuario != reg.nombre_usuario)
                {
                    cambios.Add(
                        $"Nombre de usuario: '{usuarioAnterior.nombre_usuario}' → '{reg.nombre_usuario}'"
                    );
                }


                if (usuarioAnterior.email != reg.email)
                {
                    cambios.Add(
                        $"Correo: '{usuarioAnterior.email}' → '{reg.email}'"
                    );
                }


                if (usuarioAnterior.id_rol != reg.id_rol)
                {
                    cambios.Add(
                        $"Rol: '{usuarioAnterior.id_rol}' → '{reg.id_rol}'"
                    );
                }
            }


            var resultado =
                await _usuarioRepository
                    .ActualizarUsuario(
                        reg
                    );


            if (resultado)
            {
                if (cambios.Count > 0)
                {
                    string detalleCambios =
                        string.Join(
                            " | ",
                            cambios
                        );


                    await RegistrarAccionHistorial(
                        $"Actualizó al usuario {reg.nombre} {reg.apellido} ({reg.nombre_usuario}). {detalleCambios}"
                    );
                }


                return RedirectToAction(
                    "Usuarios"
                );
            }


            ViewBag.Mensaje =
                "No se pudo actualizar el usuario";


            return View(
                "EditarUsuario",
                reg
            );
        }



        // =========================================================
        // USUARIOS INACTIVOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ListadoEliminarUsuarios()
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _usuarioRepository
                    .ListadoEliminarUsuarios();


            await RegistrarAccionHistorial(
                "Ingresó al listado de usuarios inactivos"
            );


            return View(
                listado
            );
        }



        // =========================================================
        // REACTIVAR USUARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ReactivarUsuario(
            int idUsuario)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var usuario =
                await _usuarioRepository
                    .ObtenerUsuarioPorId(
                        idUsuario
                    );


            var resultado =
                await _usuarioRepository
                    .ReactivarUsuario(
                        idUsuario
                    );


            if (resultado)
            {
                if (usuario != null)
                {
                    await RegistrarAccionHistorial(
                        $"Reactivó al usuario {usuario.nombre} {usuario.apellido} ({usuario.nombre_usuario})"
                    );
                }


                TempData["Mensaje"] =
                    "Usuario reactivado correctamente";


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo reactivar el usuario";


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "ListadoEliminarUsuarios"
            );
        }



        // =========================================================
        // ELIMINAR USUARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(
            int idUsuario)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var usuario =
                await _usuarioRepository
                    .ObtenerUsuarioPorId(
                        idUsuario
                    );


            var resultado =
                await _usuarioRepository
                    .EliminarUsuario(
                        idUsuario
                    );


            if (resultado &&
                usuario != null)
            {
                await RegistrarAccionHistorial(
                    $"Eliminó al usuario {usuario.nombre} {usuario.apellido} ({usuario.nombre_usuario})"
                );
            }


            return RedirectToAction(
                "ListadoEliminarUsuarios"
            );
        }



        // =========================================================
        // ROLES Y PERMISOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> RolesPermisos(
            int? idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            var roles =
                await _permisoRepository
                    .ListadoRoles();


            var permisos =
                await _permisoRepository
                    .ListadoPermisos();


            List<int> permisosRol =
                new List<int>();


            if (idRol.HasValue)
            {
                permisosRol =
                    await _permisoRepository
                        .PermisosPorRol(
                            idRol.Value
                        );
            }


            ViewBag.Roles =
                roles;


            ViewBag.Permisos =
                permisos;


            ViewBag.PermisosRol =
                permisosRol;


            ViewBag.IdRol =
                idRol;


            await RegistrarAccionHistorial(
                "Ingresó a Roles y Permisos"
            );


            return View();
        }



        // =========================================================
        // GUARDAR PERMISOS
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> GuardarPermisos(
            int idRol,
            List<int>? permisosSeleccionados)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var rol =
                await _permisoRepository
                    .ObtenerRolPorId(
                        idRol
                    );


            permisosSeleccionados ??=
                new List<int>();


            var permisosAnteriores =
                await _permisoRepository
                    .PermisosPorRol(
                        idRol
                    );


            var todosPermisos =
                await _permisoRepository
                    .ListadoPermisos();


            var permisosAgregadosIds =
                permisosSeleccionados
                    .Except(
                        permisosAnteriores
                    )
                    .ToList();


            var permisosQuitadosIds =
                permisosAnteriores
                    .Except(
                        permisosSeleccionados
                    )
                    .ToList();


            var permisosAgregados =
                todosPermisos
                    .Where(
                        p =>
                            permisosAgregadosIds
                                .Contains(
                                    p.id_permiso
                                )
                    )
                    .Select(
                        p => p.nombre
                    )
                    .ToList();


            var permisosQuitados =
                todosPermisos
                    .Where(
                        p =>
                            permisosQuitadosIds
                                .Contains(
                                    p.id_permiso
                                )
                    )
                    .Select(
                        p => p.nombre
                    )
                    .ToList();


            var resultado =
                await _permisoRepository
                    .ActualizarPermisosRol(
                        idRol,
                        permisosSeleccionados
                    );


            if (resultado)
            {
                if (rol != null)
                {
                    List<string> cambiosPermisos =
                        new List<string>();


                    if (permisosAgregados.Count > 0)
                    {
                        cambiosPermisos.Add(
                            $"Agregó: {string.Join(", ", permisosAgregados)}"
                        );
                    }


                    if (permisosQuitados.Count > 0)
                    {
                        cambiosPermisos.Add(
                            $"Quitó: {string.Join(", ", permisosQuitados)}"
                        );
                    }


                    if (cambiosPermisos.Count > 0)
                    {
                        string detalleCambios =
                            string.Join(
                                " | ",
                                cambiosPermisos
                            );


                        await RegistrarAccionHistorial(
                            $"Actualizó los permisos del rol {rol.nombre}. {detalleCambios}"
                        );
                    }
                }


                int? idRolSesion =
                    HttpContext.Session.GetInt32(
                        "IdRol"
                    );


                if (idRolSesion.HasValue &&
                    idRolSesion.Value == idRol)
                {
                    var permisosActualizados =
                        await _usuarioRepository
                            .ObtenerPermisos(
                                idRol
                            );


                    HttpContext.Session.SetString(
                        "Permisos",
                        string.Join(
                            "|",
                            permisosActualizados
                        )
                    );
                }


                TempData["Mensaje"] =
                    "Permisos actualizados correctamente";


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudieron actualizar los permisos";


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "RolesPermisos",
                new
                {
                    idRol
                }
            );
        }



        // =========================================================
        // CREAR ROL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CrearRol()
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            await RegistrarAccionHistorial(
                "Ingresó a Crear Rol"
            );


            return View();
        }



        // =========================================================
        // NUEVO ROL
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> NuevoRol(
            Rol reg)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _permisoRepository
                    .CrearRol(
                        reg
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Creó el rol {reg.nombre}"
                );


                TempData["Mensaje"] =
                    "Rol creado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "RolesPermisos"
                );
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            ViewBag.Mensaje =
                "No se pudo crear el rol. El nombre ya existe.";


            return View(
                "CrearRol",
                reg
            );
        }



        // =========================================================
        // EDITAR ROL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditarRol(
            int idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var rol =
                await _permisoRepository
                    .ObtenerRolPorId(
                        idRol
                    );


            if (rol == null)
            {
                TempData["Mensaje"] =
                    "No se encontró el rol seleccionado";


                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "RolesPermisos"
                );
            }


            await RegistrarAccionHistorial(
                $"Ingresó a editar el rol {rol.nombre}"
            );


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            return View(
                rol
            );
        }



        // =========================================================
        // ACTUALIZAR ROL
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ActualizarRol(
            Rol reg)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var rolAnterior =
                await _permisoRepository
                    .ObtenerRolPorId(
                        reg.id_rol
                    );


            List<string> cambios =
                new List<string>();


            if (rolAnterior != null)
            {
                if (rolAnterior.nombre != reg.nombre)
                {
                    cambios.Add(
                        $"Nombre: '{rolAnterior.nombre}' → '{reg.nombre}'"
                    );
                }


                if (rolAnterior.descripcion != reg.descripcion)
                {
                    cambios.Add(
                        $"Descripción: '{rolAnterior.descripcion}' → '{reg.descripcion}'"
                    );
                }
            }


            var resultado =
                await _permisoRepository
                    .ActualizarRol(
                        reg
                    );


            if (resultado)
            {
                if (cambios.Count > 0)
                {
                    string detalleCambios =
                        string.Join(
                            " | ",
                            cambios
                        );


                    await RegistrarAccionHistorial(
                        $"Actualizó el rol {reg.nombre}. {detalleCambios}"
                    );
                }


                TempData["Mensaje"] =
                    "Rol actualizado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "RolesPermisos",
                    new
                    {
                        idRol = reg.id_rol
                    }
                );
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            ViewBag.Mensaje =
                "No se pudo actualizar el rol. El nombre ya existe.";


            return View(
                "EditarRol",
                reg
            );
        }



        // =========================================================
        // DESACTIVAR ROL
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> DesactivarRol(
            int idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var rol =
                await _permisoRepository
                    .ObtenerRolPorId(
                        idRol
                    );


            var resultado =
                await _permisoRepository
                    .DesactivarRol(
                        idRol
                    );


            if (resultado)
            {
                if (rol != null)
                {
                    await RegistrarAccionHistorial(
                        $"Desactivó el rol {rol.nombre}"
                    );
                }


                TempData["Mensaje"] =
                    "Rol desactivado correctamente";


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo desactivar el rol";


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "RolesPermisos"
            );
        }



        // =========================================================
        // ROLES INACTIVOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> RolesInactivos()
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            ViewBag.Apellido =
                HttpContext.Session.GetString(
                    "Apellido"
                );


            ViewBag.Rol =
                HttpContext.Session.GetString(
                    "Rol"
                );


            var listado =
                await _permisoRepository
                    .ListadoRolesInactivos();


            await RegistrarAccionHistorial(
                "Ingresó al listado de roles inactivos"
            );


            return View(
                listado
            );
        }



        // =========================================================
        // REACTIVAR ROL
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ReactivarRol(
            int idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var rol =
                await _permisoRepository
                    .ObtenerRolPorId(
                        idRol
                    );


            var resultado =
                await _permisoRepository
                    .ReactivarRol(
                        idRol
                    );


            if (resultado)
            {
                if (rol != null)
                {
                    await RegistrarAccionHistorial(
                        $"Reactivó el rol {rol.nombre}"
                    );
                }


                TempData["Mensaje"] =
                    "Rol reactivado correctamente";


                TempData["TipoMensaje"] =
                    "exito";
            }
            else
            {
                TempData["Mensaje"] =
                    "No se pudo reactivar el rol";


                TempData["TipoMensaje"] =
                    "error";
            }


            return RedirectToAction(
                "RolesInactivos"
            );
        }



        // =========================================================
        // REGISTRAR ACCION EN HISTORIAL
        // =========================================================

        private async Task RegistrarAccionHistorial(
            string descripcion)
        {
            int? idResponsable =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idResponsable.HasValue)
            {
                return;
            }


            string nombreResponsable =
                (
                    HttpContext.Session.GetString(
                        "Nombre"
                    )
                    +
                    " "
                    +
                    HttpContext.Session.GetString(
                        "Apellido"
                    )
                )
                .Trim();


            await _historialRepository
                .RegistrarHistorial(
                    idResponsable.Value,
                    nombreResponsable,
                    descripcion
                );
        }



        // =========================================================
        // VERIFICAR PERMISO
        // =========================================================

        private bool TienePermiso(
            string permiso)
        {
            string permisosTexto =
                HttpContext.Session.GetString(
                    "Permisos"
                ) ?? "";


            var permisos =
                permisosTexto
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries
                    );


            return permisos.Any(
                p =>
                    p.Equals(
                        permiso,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }



        // =========================================================
        // VERIFICAR VARIOS PERMISOS
        // =========================================================

        private bool TieneAlgunPermiso(
            params string[] permisosRequeridos)
        {
            foreach (
                string permiso
                in permisosRequeridos)
            {
                if (TienePermiso(
                        permiso
                    ))
                {
                    return true;
                }
            }


            return false;
        }



        // =========================================================
        // AUTORIZACION CLINICA SUPERIOR
        // SOLO ENFERMERA O ADMINISTRADOR
        // =========================================================

        private bool EsEnfermeraOAdministrador()
        {
            string rolActual =
                HttpContext.Session.GetString(
                    "Rol"
                )
                ?? "";

            return
                rolActual.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                rolActual.Equals(
                    "Enfermera",
                    StringComparison.OrdinalIgnoreCase
                );
        }



        // =========================================================
        // ACCESO NO AUTORIZADO
        // =========================================================

        private IActionResult AccesoNoAutorizado()
        {
            string? nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            if (nombre == null)
            {
                return RedirectToAction(
                    "Index"
                );
            }


            return RedirectToAction(
                "AccesoDenegado"
            );
        }



        // =========================================================
        // CREAR PRODUCTO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CrearProducto(
            string? codigoBarras)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            Producto producto =
                new Producto();


            if (!string.IsNullOrWhiteSpace(
                    codigoBarras))
            {
                producto.codigo_barras =
                    codigoBarras.Trim();
            }


            await RegistrarAccionHistorial(
                "Ingresó a Crear Producto"
            );


            return View(
                producto
            );
        }


        // =========================================================
        // NUEVO PRODUCTO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> NuevoProducto(
            Producto reg,
            IFormFile? imagen)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            if (imagen != null &&
                imagen.Length > 0)
            {
                string extension =
                    Path.GetExtension(
                        imagen.FileName
                    )
                    .ToLower();


                string[] extensionesPermitidas =
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };


                if (!extensionesPermitidas.Contains(
                        extension))
                {
                    ViewBag.Mensaje =
                        "La imagen debe ser JPG, PNG o WEBP";


                    return View(
                        "CrearProducto",
                        reg
                    );
                }


                if (imagen.Length >
                    5 * 1024 * 1024)
                {
                    ViewBag.Mensaje =
                        "La imagen no puede superar los 5 MB";


                    return View(
                        "CrearProducto",
                        reg
                    );
                }


                string carpeta =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "productos"
                    );


                Directory.CreateDirectory(
                    carpeta
                );


                string nombreArchivo =
                    Guid.NewGuid()
                    .ToString()
                    +
                    extension;


                string rutaArchivo =
                    Path.Combine(
                        carpeta,
                        nombreArchivo
                    );


                using (
                    var stream =
                        new FileStream(
                            rutaArchivo,
                            FileMode.Create
                        )
                )
                {
                    await imagen.CopyToAsync(
                        stream
                    );
                }


                reg.imagen_url =
                    "/images/productos/"
                    +
                    nombreArchivo;
            }


            var resultado =
                await _productoRepository
                    .CrearProducto(
                        reg
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Creó el producto {reg.nombre} ({reg.codigo_interno})"
                );


                TempData["Mensaje"] =
                    "Producto registrado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "ListadoProductos2"
                );
            }


            ViewBag.Mensaje =
                "No se pudo registrar el producto";


            return View(
                "CrearProducto",
                reg
            );
        }


        // =========================================================
        // BUSCAR PRODUCTO POR CODIGO DE BARRAS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> BuscarProductoPorCodigoBarras(
            string codigo)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return Unauthorized();
            }


            codigo =
                (codigo ?? "")
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                    codigo))
            {
                return Json(
                    new
                    {
                        existe = false
                    }
                );
            }


            var producto =
                await _productoRepository
                    .ObtenerProductoPorCodigoBarras(
                        codigo
                    );


            if (producto == null)
            {
                return Json(
                    new
                    {
                        existe = false,
                        codigo
                    }
                );
            }


            return Json(
                new
                {
                    existe = true,

                    producto = new
                    {
                        producto.id_producto,
                        producto.codigo_interno,
                        producto.codigo_barras,
                        producto.nombre,
                        producto.stock_minimo,
                        producto.imagen_url,
                        producto.tipo,
                        producto.principio_activo,
                        producto.concentracion,
                        producto.requiere_control,
                        producto.material,
                        producto.es_esteril
                    }
                }
            );
        }



        // =========================================================
        // LISTADO PRODUCTOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ListadoProductos2()
        {
            if (!TienePermiso(
                    "Consultar productos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _productoRepository
                    .ListadoProductos();


            await RegistrarAccionHistorial(
                "Ingresó al listado de productos"
            );


            return View(
                "Productos",
                listado
            );
        }



        // =========================================================
        // EDITAR PRODUCTO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditarProducto(
            int idProducto)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var producto =
                await _productoRepository
                    .ObtenerProductoPorId(
                        idProducto
                    );


            if (producto == null)
            {
                return RedirectToAction(
                    "ListadoProductos2"
                );
            }


            await RegistrarAccionHistorial(
                $"Ingresó a editar el producto {producto.nombre} ({producto.codigo_interno})"
            );


            return View(
                producto
            );
        }



        // =========================================================
        // ACTUALIZAR PRODUCTO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ActualizarProducto(
            Producto reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var productoAnterior =
                await _productoRepository
                    .ObtenerProductoPorId(
                        reg.id_producto
                    );


            List<string> cambios =
                new List<string>();


            if (productoAnterior != null)
            {
                if (productoAnterior.codigo_interno !=
                    reg.codigo_interno)
                {
                    cambios.Add(
                        $"Código interno: '{productoAnterior.codigo_interno}' → '{reg.codigo_interno}'"
                    );
                }


                if (productoAnterior.nombre !=
                    reg.nombre)
                {
                    cambios.Add(
                        $"Nombre: '{productoAnterior.nombre}' → '{reg.nombre}'"
                    );
                }


                if (productoAnterior.stock_minimo !=
                    reg.stock_minimo)
                {
                    cambios.Add(
                        $"Stock mínimo: '{productoAnterior.stock_minimo}' → '{reg.stock_minimo}'"
                    );
                }
            }


            var resultado =
                await _productoRepository
                    .ActualizarProducto(
                        reg
                    );


            if (resultado)
            {
                if (cambios.Count > 0)
                {
                    string detalleCambios =
                        string.Join(
                            " | ",
                            cambios
                        );


                    await RegistrarAccionHistorial(
                        $"Actualizó el producto {reg.nombre} ({reg.codigo_interno}). {detalleCambios}"
                    );
                }


                TempData["Mensaje"] =
                    "Producto actualizado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "ListadoProductos2"
                );
            }


            ViewBag.Mensaje =
                "No se pudo actualizar el producto";


            return View(
                "EditarProducto",
                reg
            );
        }



        // =========================================================
        // CREAR LOTE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CrearLote()
        {
            if (!TienePermiso(
                    "Registrar entradas de inventario"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Productos =
                await _loteRepository
                    .ListadoProductos();


            ViewBag.Ubicaciones =
     await _loteRepository
         .ListadoUbicacionesActivas();


            await RegistrarAccionHistorial(
                "Ingresó a Crear Lote"
            );


            return View();
        }



        // =========================================================
        // REGISTRAR ENTRADA DE INVENTARIO
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> NuevoLote(
            Lote reg)
        {
            if (!TienePermiso(
                    "Registrar entradas de inventario"))
            {
                return AccesoNoAutorizado();
            }


            int? idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario"
                );


            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Index"
                );
            }



            // =====================================================
            // VALIDACIONES
            // =====================================================

            if (reg.id_producto <= 0)
            {
                ViewBag.Mensaje =
                    "Debe seleccionar un producto.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                return View(
                    "CrearLote",
                    reg
                );
            }


            if (string.IsNullOrWhiteSpace(
                    reg.numero_lote))
            {
                ViewBag.Mensaje =
                    "Debe ingresar el número de lote.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                return View(
                    "CrearLote",
                    reg
                );
            }


            if (!reg.fecha_vencimiento.HasValue)
            {
                ViewBag.Mensaje =
                    "Debe ingresar la fecha de vencimiento.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                return View(
                    "CrearLote",
                    reg
                );
            }


            if (reg.cantidad_actual <= 0)
            {
                ViewBag.Mensaje =
                    "La cantidad de entrada debe ser mayor a 0.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                return View(
                    "CrearLote",
                    reg
                );
            }



            // =====================================================
            // REGISTRAR ENTRADA COMPLETA EN UNA SOLA TRANSACCION
            // =====================================================

            int cantidadEntrada =
                reg.cantidad_actual;

            var resultado =
                await _movimientoRepository
                    .RegistrarEntradaInventarioCompleta(
                        reg,
                        idUsuario.Value
                    );

            if (!resultado.exito)
            {
                ViewBag.Mensaje =
                    resultado.mensaje;

                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();

                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();

                return View(
                    "CrearLote",
                    reg
                );
            }



            // =====================================================
            // REGISTRAR HISTORIAL GENERAL
            // =====================================================

            await RegistrarAccionHistorial(
                $"Registró entrada de {cantidadEntrada} unidad(es) del producto {resultado.nombreProducto}, lote {resultado.numeroLote}. Stock: {resultado.stockAnterior} → {resultado.stockResultante}"
            );



            TempData["Mensaje"] =
                resultado.mensaje;


            TempData["TipoMensaje"] =
                "exito";


            return RedirectToAction(
                "Lotes"
            );
        }


        // =========================================================
        // LISTADO LOTES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Lotes()
        {
            if (!TienePermiso(
                    "Consultar productos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _loteRepository
                    .ListadoLotes();


            await RegistrarAccionHistorial(
                "Ingresó al listado de lotes"
            );


            return View(
                listado
            );
        }



        // =========================================================
        // EDITAR LOTE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditarLote(
            int idLote)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var lote =
                await _loteRepository
                    .ObtenerLotePorId(
                        idLote
                    );


            if (lote == null)
            {
                return RedirectToAction(
                    "Lotes"
                );
            }


            await RegistrarAccionHistorial(
                $"Ingresó a editar el lote {lote.numero_lote}"
            );


            ViewBag.Productos =
                await _loteRepository
                    .ListadoProductos();


            // =====================================================
            // SOLO UBICACIONES ACTIVAS
            // =====================================================

            var ubicaciones =
                await _loteRepository
                    .ListadoUbicacionesActivas();


            // =====================================================
            // SI EL LOTE YA PERTENECE A UNA UBICACION INACTIVA,
            // LA MOSTRAMOS PARA NO PERDER SU UBICACION ACTUAL.
            // =====================================================

            bool existeUbicacionActual =
                ubicaciones.Any(
                    u =>
                        u.id_ubicacion
                        ==
                        lote.id_ubicacion
                );


            if (!existeUbicacionActual)
            {
                var todasUbicaciones =
                    await _loteRepository
                        .ListadoUbicaciones();


                var ubicacionActual =
                    todasUbicaciones
                        .FirstOrDefault(
                            u =>
                                u.id_ubicacion
                                ==
                                lote.id_ubicacion
                        );


                if (ubicacionActual != null)
                {
                    ubicaciones.Add(
                        ubicacionActual
                    );
                }
            }


            ViewBag.Ubicaciones =
                ubicaciones
                    .OrderBy(
                        u =>
                            u.nombre_sector
                    )
                    .ToList();


            return View(
                lote
            );
        }



        // =========================================================
        // ACTUALIZAR LOTE
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ActualizarLote(
            Lote reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var loteAnterior =
                await _loteRepository
                    .ObtenerLotePorId(
                        reg.id_lote
                    );

            // =====================================================
            // VALIDAR UBICACION DE DESTINO
            // =====================================================

            if (loteAnterior == null)
            {
                TempData["Mensaje"] =
                    "El lote seleccionado no existe.";

                TempData["TipoMensaje"] =
                    "error";


                return RedirectToAction(
                    "Lotes"
                );
            }


            var todasUbicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            var ubicacionDestino =
                todasUbicaciones
                    .FirstOrDefault(
                        u =>
                            u.id_ubicacion
                            ==
                            reg.id_ubicacion
                    );


            if (ubicacionDestino == null)
            {
                ViewBag.Mensaje =
                    "La ubicación seleccionada no existe.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                return View(
                    "EditarLote",
                    reg
                );
            }


            // =====================================================
            // NO PERMITIR MOVER EL LOTE A UNA UBICACION INACTIVA
            // =====================================================

            if (
                !ubicacionDestino.activo
                &&
                loteAnterior.id_ubicacion
                    !=
                    reg.id_ubicacion
            )
            {
                ViewBag.Mensaje =
                    "No se puede mover el lote a una ubicación inactiva.";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                var ubicacionesActivas =
                    await _loteRepository
                        .ListadoUbicacionesActivas();


                var ubicacionAnterior =
                    todasUbicaciones
                        .FirstOrDefault(
                            u =>
                                u.id_ubicacion
                                ==
                                loteAnterior.id_ubicacion
                        );


                if (
                    ubicacionAnterior != null
                    &&
                    !ubicacionesActivas.Any(
                        u =>
                            u.id_ubicacion
                            ==
                            ubicacionAnterior.id_ubicacion
                    )
                )
                {
                    ubicacionesActivas.Add(
                        ubicacionAnterior
                    );
                }


                ViewBag.Ubicaciones =
                    ubicacionesActivas
                        .OrderBy(
                            u =>
                                u.nombre_sector
                        )
                        .ToList();


                return View(
                    "EditarLote",
                    reg
                );
            }


            List<string> cambios =
                new List<string>();


            if (loteAnterior != null)
            {
                if (loteAnterior.numero_lote !=
                    reg.numero_lote)
                {
                    cambios.Add(
                        $"Número de lote: '{loteAnterior.numero_lote}' → '{reg.numero_lote}'"
                    );
                }


                if (loteAnterior.fecha_vencimiento !=
                    reg.fecha_vencimiento)
                {
                    cambios.Add(
                        $"Fecha de vencimiento: '{loteAnterior.fecha_vencimiento}' → '{reg.fecha_vencimiento}'"
                    );
                }


                if (loteAnterior.cantidad_actual !=
                    reg.cantidad_actual)
                {
                    cambios.Add(
                        $"Cantidad: '{loteAnterior.cantidad_actual}' → '{reg.cantidad_actual}'"
                    );
                }


                if (loteAnterior.id_producto !=
                    reg.id_producto)
                {
                    cambios.Add(
                        $"Producto: '{loteAnterior.id_producto}' → '{reg.id_producto}'"
                    );
                }


                if (loteAnterior.id_ubicacion !=
                    reg.id_ubicacion)
                {
                    cambios.Add(
                        $"Ubicación: '{loteAnterior.id_ubicacion}' → '{reg.id_ubicacion}'"
                    );
                }
            }


            var resultado =
                await _loteRepository
                    .ActualizarLote(
                        reg
                    );


            if (resultado)
            {
                if (cambios.Count > 0)
                {
                    string detalleCambios =
                        string.Join(
                            " | ",
                            cambios
                        );


                    await RegistrarAccionHistorial(
                        $"Actualizó el lote {reg.numero_lote}. {detalleCambios}"
                    );
                }


                TempData["Mensaje"] =
                    "Lote actualizado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "Lotes"
                );
            }


            ViewBag.Mensaje =
                "No se pudo actualizar el lote";


            ViewBag.Productos =
                await _loteRepository
                    .ListadoProductos();


            ViewBag.Ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            return View(
                "EditarLote",
                reg
            );
        }

        // =========================================================
        // NORMALIZAR CODIGO DE DOSIFICACION
        // =========================================================

        private string NormalizarCodigoDosificacionController(
            string? codigo)
        {
            string limpio =
                (codigo ?? "")
                    .Trim()
                    .ToUpperInvariant();


            int posicionDos =
                limpio.IndexOf(
                    "DOS",
                    StringComparison.OrdinalIgnoreCase
                );


            // Prefijos AIM que algunos lectores agregan.
            if (
                posicionDos > 0
                &&
                limpio.StartsWith(
                    "]",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                limpio =
                    limpio.Substring(
                        posicionDos
                    );
            }


            limpio =
                limpio
                    .Replace("*", "")
                    .Replace(" ", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Replace("–", "-")
                    .Replace("—", "-")
                    .Replace("−", "-")
                    .Replace("'", "-")
                    .Replace("’", "-")
                    .Replace("´", "-")
                    .Replace("`", "-");


            // DOS000001 -> DOS-000001
            if (
                limpio.StartsWith(
                    "DOS",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !limpio.StartsWith(
                    "DOS-",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string resto =
                    limpio.Substring(3)
                        .TrimStart('-');


                if (
                    !string.IsNullOrWhiteSpace(
                        resto
                    )
                    &&
                    resto.All(
                        char.IsDigit
                    )
                )
                {
                    limpio =
                        "DOS-"
                        +
                        resto;
                }
            }


            return limpio;
        }

    }
}