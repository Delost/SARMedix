using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LoginSARMedix.Controllers
{
    public class HomeController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly ProductoRepository _productoRepository;
        private readonly LoteRepository _loteRepository;

        public HomeController(
            UsuarioRepository usuarioRepository,
            ProductoRepository productoRepository,
            LoteRepository loteRepository)
        {
            _usuarioRepository = usuarioRepository;
            _productoRepository = productoRepository;
            _loteRepository = loteRepository;
        }



        //Iniciar sesion VISTA
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        //Iniciar sesion POST
        [HttpPost]
        public async Task<IActionResult> InicioSesion(Usuario reg)
        {
            var usuario = await _usuarioRepository.IniciarSesion(
                reg.nombre_usuario,
                reg.contrasena
            );

            if (usuario != null)
            {
                var permisos = await _usuarioRepository.ObtenerPermisos(usuario.id_rol);

                HttpContext.Session.SetString("Nombre", usuario.nombre ?? "");
                HttpContext.Session.SetString("Apellido", usuario.apellido ?? "");
                HttpContext.Session.SetString("Rol", usuario.rol ?? "");

                HttpContext.Session.SetString(
                    "Permisos",
                    string.Join("|", permisos)
                );

                return RedirectToAction("Modulos");
            }
            else
            {
                ViewBag.Mensaje = "Usuario o contraseña incorrectos";

                return View("Index");
            }
        }


        //MODULOS
        [HttpGet]
        public IActionResult Modulos()
        {
            string? nombre = HttpContext.Session.GetString("Nombre");

            if (nombre == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Nombre = nombre;

            ViewBag.Apellido =
                HttpContext.Session.GetString("Apellido");

            ViewBag.Rol =
                HttpContext.Session.GetString("Rol");

            string permisosTexto =
                HttpContext.Session.GetString("Permisos") ?? "";

            ViewBag.Permisos = permisosTexto
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            return View("Bienvenida");
        }


        //USUARIOS Y PERMISOS
        [HttpGet]
        public IActionResult UsuariosPermisos()
        {
            return View("~/Views/Home/Modulos/UsuariosPermisos.cshtml");
        }


        //PRODUCTOS
        [HttpGet]
        public IActionResult Productos()
        {
            return View("~/Views/Home/Modulos/Productos.cshtml");
        }


        //MOVIMIENTOS E HISTORIAL
        [HttpGet]
        public IActionResult MovimientoHistorial()
        {
            return View("~/Views/Home/Modulos/MovimientoHistorial.cshtml");
        }


        //MEDICAMENTOS CONTROLADOS
        [HttpGet]
        public IActionResult MedicamentosControlados()
        {
            return View("~/Views/Home/Modulos/MedicamentosControlados.cshtml");
        }


        //CREAR USUARIO VISTA
        [HttpGet]
        public IActionResult CrearUsuario()
        {
            return View();
        }


        //CREAR USUARIO POST
        [HttpPost]
        public async Task<IActionResult> NuevoUsuario(Usuario reg)
        {
            var resultado = await _usuarioRepository.CrearUsuario(reg);

            if (resultado)
            {
                ViewBag.Mensaje = "Usuario creado correctamente";

                return View("Index");
            }
            else
            {
                ViewBag.Mensaje = "No se pudo crear el usuario: nombre de usuario existente";

                return View("CrearUsuario", reg);
            }
        }


        //LISTADO DE USUARIOS
        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            var listado = await _usuarioRepository.ListadoUsuarios();

            return View(listado);
        }


        //DESACTIVAR USUARIO
        [HttpPost]
        public async Task<IActionResult> DesactivarUsuario(int idUsuario)
        {
            await _usuarioRepository.DesactivarUsuario(idUsuario);

            return RedirectToAction("Usuarios");
        }


        //EDITAR USUARIO VISTA
        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int idUsuario)
        {
            var usuario =
                await _usuarioRepository.ObtenerUsuarioPorId(idUsuario);

            if (usuario == null)
            {
                return RedirectToAction("Usuarios");
            }

            return View(usuario);
        }


        //ACTUALIZAR USUARIO POST
        [HttpPost]
        public async Task<IActionResult> ActualizarUsuario(Usuario reg)
        {
            var resultado =
                await _usuarioRepository.ActualizarUsuario(reg);

            if (resultado)
            {
                return RedirectToAction("Usuarios");
            }
            else
            {
                ViewBag.Mensaje = "No se pudo actualizar el usuario";

                return View("EditarUsuario", reg);
            }
        }


        //LISTADO DE USUARIOS INACTIVOS
        [HttpGet]
        public async Task<IActionResult> ListadoEliminarUsuarios()
        {
            var listado =
                await _usuarioRepository.ListadoEliminarUsuarios();

            return View(listado);
        }


        //ELIMINAR USUARIO
        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(int idUsuario)
        {
            await _usuarioRepository.EliminarUsuario(idUsuario);

            return RedirectToAction("ListadoEliminarUsuarios");
        }







        //PRODUCTOS






        //CREAR PRODUCTO VISTA
        [HttpGet]
        public IActionResult CrearProducto()
        {
            return View();
        }


        //CREAR PRODUCTO POST
        [HttpPost]
        public async Task<IActionResult> NuevoProducto(Producto reg)
        {
            var resultado =
                await _productoRepository.CrearProducto(reg);

            if (resultado)
            {
                ViewBag.Mensaje = "Producto registrado correctamente";

                return View("CrearProducto");
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo registrar el producto";

                return View("CrearProducto", reg);
            }
        }





        //LISTADO DE PRODUCTOS
        [HttpGet]
        public async Task<IActionResult> ListadoProductos2()
        {
            var listado =
                await _productoRepository.ListadoProductos();

            return View("Productos", listado);
        }


        //EDITAR PRODUCTO VISTA
        [HttpGet]
        public async Task<IActionResult> EditarProducto(int idProducto)
        {
            var producto =
                await _productoRepository.ObtenerProductoPorId(idProducto);

            if (producto == null)
            {
                return RedirectToAction("ListadoProductos2");
            }

            return View(producto);
        }

        //ACTUALIZAR PRODUCTO POST
        [HttpPost]
        public async Task<IActionResult> ActualizarProducto(Producto reg)
        {
            var resultado =
                await _productoRepository.ActualizarProducto(reg);

            if (resultado)
            {
                return RedirectToAction("ListadoProductos2");
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo actualizar el producto";

                return View("EditarProducto", reg);
            }
        }






        //LOTES


        //CREAR LOTE VISTA
        [HttpGet]
        public async Task<IActionResult> CrearLote()
        {
            ViewBag.Productos =
                await _loteRepository.ListadoProductos();

            ViewBag.Ubicaciones =
                await _loteRepository.ListadoUbicaciones();

            return View();
        }



        //CREAR LOTE POST
        [HttpPost]
        public async Task<IActionResult> NuevoLote(Lote reg)
        {
            var resultado =
                await _loteRepository.CrearLote(reg);

            if (resultado)
            {
                ViewBag.Mensaje =
                    "Lote registrado correctamente";

                ViewBag.Productos =
                    await _loteRepository.ListadoProductos();

                ViewBag.Ubicaciones =
                    await _loteRepository.ListadoUbicaciones();

                return View("CrearLote");
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo registrar el lote";

                ViewBag.Productos =
                    await _loteRepository.ListadoProductos();

                ViewBag.Ubicaciones =
                    await _loteRepository.ListadoUbicaciones();

                return View("CrearLote", reg);
            }
        }

        //LISTADO DE LOTES
        [HttpGet]
        public async Task<IActionResult> Lotes()
        {
            var listado =
                await _loteRepository.ListadoLotes();

            return View(listado);
        }




        //EDITAR LOTE VISTA
        [HttpGet]
        public async Task<IActionResult> EditarLote(int idLote)
        {
            var lote =
                await _loteRepository.ObtenerLotePorId(idLote);

            if (lote == null)
            {
                return RedirectToAction("Lotes");
            }

            ViewBag.Productos =
                await _loteRepository.ListadoProductos();

            ViewBag.Ubicaciones =
                await _loteRepository.ListadoUbicaciones();

            return View(lote);
        }



        //ACTUALIZAR LOTE POST
        [HttpPost]
        public async Task<IActionResult> ActualizarLote(Lote reg)
        {
            var resultado =
                await _loteRepository.ActualizarLote(reg);

            if (resultado)
            {
                return RedirectToAction("Lotes");
            }
            else
            {
                ViewBag.Mensaje =
                    "No se pudo actualizar el lote";

                ViewBag.Productos =
                    await _loteRepository.ListadoProductos();

                ViewBag.Ubicaciones =
                    await _loteRepository.ListadoUbicaciones();

                return View("EditarLote", reg);
            }
        }



    }
}