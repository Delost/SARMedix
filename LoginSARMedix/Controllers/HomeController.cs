using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using LoginSARMedix.Services;
using Microsoft.AspNetCore.Mvc;

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



        public HomeController(
                             UsuarioRepository usuarioRepository,
                             ProductoRepository productoRepository,
                             LoteRepository loteRepository,
                             PermisoRepository permisoRepository,
                             EmailService emailService,
                             HistorialRepository historialRepository)
        {
            _usuarioRepository = usuarioRepository;
            _productoRepository = productoRepository;
            _loteRepository = loteRepository;
            _permisoRepository = permisoRepository;
            _emailService = emailService;
            _historialRepository = historialRepository;
        }


        // INICIO DE SESION

        [HttpGet]
        public IActionResult Index()
        {
            string? nombre =
                HttpContext.Session.GetString(
                    "Nombre"
                );


            // Si ya existe una sesion iniciada,
            // no permitir volver al login
            if (!string.IsNullOrEmpty(nombre))
            {
                return RedirectToAction(
                    "Modulos"
                );
            }


            return View();
        }


        // VERIFICAR CODIGO

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


        [HttpPost]
        public async Task<IActionResult> InicioSesion(
            Usuario reg)
        {
            var usuario =
                await _usuarioRepository.IniciarSesion(
                    reg.nombre_usuario,
                    reg.contrasena
                );


            if (usuario != null)
            {

                // GUARDAR ID DEL USUARIO LOGUEADO

                HttpContext.Session.SetInt32(
                    "IdUsuario",
                    usuario.id_usuario
                );




                var permisos =
                    await _usuarioRepository.ObtenerPermisos(
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


                HttpContext.Session.SetString(
                    "Permisos",
                    string.Join("|", permisos)
                );


                return RedirectToAction(
                    "Modulos"
                );
            }
            else
            {
                ViewBag.Mensaje =
                    "Usuario o contraseña incorrectos";


                return View(
                    "Index"
                );
            }
        }


        // RECUPERAR CONTRASEÑA

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
                    DateTime.Now.AddMinutes(10);


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
                    else
                    {
                        ViewBag.Mensaje =
                            "El código fue generado, pero no se pudo enviar el correo.";
                    }
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


        // NUEVA CONTRASEÑA

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


        // CAMBIAR CONTRASEÑA

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


        // CERRAR SESION

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



        // PANTALLA PRINCIPAL DE MODULOS

        [HttpGet]
        public IActionResult Modulos()
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


            return View(
                "Bienvenida"
            );
        }



        // ACCESO DENEGADO

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



        // MODULO USUARIOS Y PERMISOS

        [HttpGet]
        public IActionResult UsuariosPermisos()
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


            return View(
                "~/Views/Home/Modulos/UsuariosPermisos.cshtml"
            );
        }



        // MODULO PRODUCTOS

        [HttpGet]
        public IActionResult Productos()
        {
            if (!TieneAlgunPermiso(
                    "Consultar productos",
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            return View(
                "~/Views/Home/Modulos/Productos.cshtml"
            );
        }



        // MOVIMIENTOS E HISTORIAL

        [HttpGet]
        public IActionResult MovimientoHistorial()
        {
            if (!TieneAlgunPermiso(
                    "Registrar movimientos",
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }


            return View(
                "~/Views/Home/Modulos/MovimientoHistorial.cshtml"
            );

        }


        // HISTORIAL

        [HttpGet]
        public async Task<IActionResult> Historial()
        {
            if (!TienePermiso(
                    "Consultar historial"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _historialRepository
                    .ListadoHistorial();


            return View(
                listado
            );
        }


        // MEDICAMENTOS CONTROLADOS

        [HttpGet]
        public IActionResult MedicamentosControlados()
        {
            if (!TienePermiso(
                    "Gestionar medicamentos controlados"))
            {
                return AccesoNoAutorizado();
            }


            return View(
                "~/Views/Home/Modulos/MedicamentosControlados.cshtml"
            );
        }



        // CREAR USUARIO

        [HttpGet]
        public IActionResult CrearUsuario()
        {
            if (!TienePermiso(
                    "Crear usuario"))
            {
                return AccesoNoAutorizado();
            }


            return View();
        }



        // NUEVO USUARIO

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
                        HttpContext.Session.GetString("Nombre")
                        + " "
                        + HttpContext.Session.GetString("Apellido")
                    ).Trim();


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
                    "CrearUsuario"
                );
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo crear el usuario: RUT o nombre de usuario existente";


                return View(
                    "CrearUsuario",
                    reg
                );
            }
        }



        // LISTADO DE USUARIOS

        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _usuarioRepository
                    .ListadoUsuarios();


            await RegistrarAccionHistorial(
                   "Ingresó al listado de usuarios"
             );


            return View(
                listado
            );
        }



        // DESACTIVAR USUARIO

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



        // EDITAR USUARIO

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



        // ACTUALIZAR USUARIO

        [HttpPost]
        public async Task<IActionResult> ActualizarUsuario(
            Usuario reg)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _usuarioRepository
                    .ActualizarUsuario(
                        reg
                    );


            if (resultado)
            {
                return RedirectToAction(
                    "Usuarios"
                );
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo actualizar el usuario";


                return View(
                    "EditarUsuario",
                    reg
                );
            }
        }



        // USUARIOS INACTIVOS

        [HttpGet]
        public async Task<IActionResult>
            ListadoEliminarUsuarios()
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var listado =
                await _usuarioRepository
                    .ListadoEliminarUsuarios();


            return View(
                listado
            );
        }



        // REACTIVAR USUARIO

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






        // ELIMINAR USUARIO

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(
            int idUsuario)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            await _usuarioRepository
                .EliminarUsuario(
                    idUsuario
                );


            return RedirectToAction(
                "ListadoEliminarUsuarios"
            );
        }



        // ROLES Y PERMISOS

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


            return View();
        }



        // GUARDAR PERMISOS

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


            permisosSeleccionados ??=
                new List<int>();


            var resultado =
                await _permisoRepository
                    .ActualizarPermisosRol(
                        idRol,
                        permisosSeleccionados
                    );


            if (resultado)
            {
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
                    idRol = idRol
                }
            );
        }



        // CREAR ROL

        [HttpGet]
        public IActionResult CrearRol()
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


            return View();
        }



        // NUEVO ROL

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
                TempData["Mensaje"] =
                    "Rol creado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "RolesPermisos"
                );
            }
            else
            {
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
        }



        // EDITAR ROL

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



        // ACTUALIZAR ROL

        [HttpPost]
        public async Task<IActionResult> ActualizarRol(
            Rol reg)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _permisoRepository
                    .ActualizarRol(
                        reg
                    );


            if (resultado)
            {
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
            else
            {
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
        }



        // DESACTIVAR ROL

        [HttpPost]
        public async Task<IActionResult> DesactivarRol(
            int idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _permisoRepository
                    .DesactivarRol(
                        idRol
                    );


            if (resultado)
            {
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



        // ROLES INACTIVOS

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


            return View(
                listado
            );
        }



        // REACTIVAR ROL

        [HttpPost]
        public async Task<IActionResult> ReactivarRol(
            int idRol)
        {
            if (!TienePermiso(
                    "Gestionar usuarios y permisos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _permisoRepository
                    .ReactivarRol(
                        idRol
                    );


            if (resultado)
            {
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

        // REGISTRAR ACCION EN HISTORIAL

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
                    HttpContext.Session.GetString("Nombre")
                    + " "
                    + HttpContext.Session.GetString("Apellido")
                ).Trim();


            await _historialRepository
                .RegistrarHistorial(
                    idResponsable.Value,
                    nombreResponsable,
                    descripcion
                );
        }

        // VERIFICAR UN PERMISO

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
                p => p.Equals(
                    permiso,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }



        // VERIFICAR VARIOS PERMISOS

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



        // ACCESO NO AUTORIZADO

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



        // PRODUCTOS


        // CREAR PRODUCTO

        [HttpGet]
        public IActionResult CrearProducto()
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            return View();
        }



        // NUEVO PRODUCTO

        [HttpPost]
        public async Task<IActionResult> NuevoProducto(
            Producto reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
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


                ViewBag.Mensaje =
                    "Producto registrado correctamente";


                return View(
                    "CrearProducto"
                );
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo registrar el producto";


                return View(
                    "CrearProducto",
                    reg
                );
            }
        }



        // LISTADO DE PRODUCTOS

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


            return View(
                "Productos",
                listado
            );
        }



        // EDITAR PRODUCTO

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


            return View(
                producto
            );
        }



        // ACTUALIZAR PRODUCTO

        [HttpPost]
        public async Task<IActionResult> ActualizarProducto(
            Producto reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _productoRepository
                    .ActualizarProducto(
                        reg
                    );


            if (resultado)
            {
                TempData["Mensaje"] =
                    "Producto actualizado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "ListadoProductos2"
                );
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo actualizar el producto";


                return View(
                    "EditarProducto",
                    reg
                );
            }
        }



        // LOTES


        // CREAR LOTE

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


            return View();
        }



        // NUEVO LOTE

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
                ViewBag.Mensaje =
                    "Lote registrado correctamente";


                ViewBag.Productos =
                    await _loteRepository
                        .ListadoProductos();


                ViewBag.Ubicaciones =
                    await _loteRepository
                        .ListadoUbicaciones();


                return View(
                    "CrearLote"
                );
            }
            else
            {
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
        }



        // LISTADO DE LOTES

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


            return View(
                listado
            );
        }



        // EDITAR LOTE

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



        // ACTUALIZAR LOTE

        [HttpPost]
        public async Task<IActionResult> ActualizarLote(
            Lote reg)
        {
            if (!TienePermiso(
                    "Registrar productos"))
            {
                return AccesoNoAutorizado();
            }


            var resultado =
                await _loteRepository
                    .ActualizarLote(
                        reg
                    );


            if (resultado)
            {
                TempData["Mensaje"] =
                    "Lote actualizado correctamente";


                TempData["TipoMensaje"] =
                    "exito";


                return RedirectToAction(
                    "Lotes"
                );
            }
            else
            {
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
}