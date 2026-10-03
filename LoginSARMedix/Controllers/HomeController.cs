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

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

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
    }
}