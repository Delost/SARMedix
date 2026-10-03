using LoginSARMedix.Models;
using LoginSARMedix.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LoginSARMedix.Controllers
{
    public class HomeController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;

        public HomeController(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
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
                ViewBag.Mensaje = "Inicio de sesión exitoso";
                ViewBag.Nombre = usuario.nombre;
                ViewBag.Apellido = usuario.apellido;
                ViewBag.Rol = usuario.rol;
                
                ViewBag.Permisos = permisos;

                return View("Bienvenida");
            }
            else
            {
                ViewBag.Mensaje = "Usuario o contraseña incorrectos";
                return View("Index");
            }

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



    }
}