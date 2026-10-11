using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using LoginSARMedix.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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


        public HomeController(
            UsuarioRepository usuarioRepository,
            ProductoRepository productoRepository,
            LoteRepository loteRepository,
            PermisoRepository permisoRepository,
            EmailService emailService,
            HistorialRepository historialRepository,
            MovimientoRepository movimientoRepository)
        {
            _usuarioRepository = usuarioRepository;
            _productoRepository = productoRepository;
            _loteRepository = loteRepository;
            _permisoRepository = permisoRepository;

            _emailService = emailService;

            _historialRepository = historialRepository;
            _movimientoRepository = movimientoRepository;
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
                    "Registrar movimientos",
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
                    "Consultar historial"))
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
        public async Task<IActionResult> CrearProducto()
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            await RegistrarAccionHistorial(
                "Ingresó a Crear Producto"
            );


            return View();
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
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            ViewBag.Productos =
                await _loteRepository
                    .ListadoProductos();


            ViewBag.Ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            await RegistrarAccionHistorial(
                "Ingresó a Crear Lote"
            );


            return View();
        }



        // =========================================================
        // NUEVO LOTE
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> NuevoLote(
            Lote reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _loteRepository
                    .CrearLote(
                        reg
                    );


            if (resultado)
            {
                await RegistrarAccionHistorial(
                    $"Creó el lote {reg.numero_lote}"
                );


                return RedirectToAction(
                    "Lotes"
                );
            }


            ViewBag.Mensaje =
                "No se pudo registrar el lote";


            ViewBag.Productos =
                await _loteRepository
                    .ListadoProductos();


            ViewBag.Ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


            return View(
                "CrearLote",
                reg
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


            ViewBag.Ubicaciones =
                await _loteRepository
                    .ListadoUbicaciones();


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
    }
}