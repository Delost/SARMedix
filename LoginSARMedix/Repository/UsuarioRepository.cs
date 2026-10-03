using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class UsuarioRepository
    {
        private readonly string _conexion;

        public UsuarioRepository(IConfiguration configuration)
        {
            _conexion = configuration.GetConnectionString("ConexionMySQL")!;
        }

        public async Task<Usuario?> IniciarSesion(string nombreUsuario, string contrasena)
        {
            Usuario? usuario = null;

            using MySqlConnection conexion = new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"SELECT u.id_usuario,
                                       u.nombre,
                                       u.apellido,
                                       u.rut,
                                       u.id_rol,
                                       u.nombre_usuario,
                                       u.contrasena,
                                       u.activo,
                                       r.nombre AS rol
                                FROM usuario u
                                INNER JOIN rol r
                                ON u.id_rol = r.id_rol
                                WHERE u.nombre_usuario = @nombreUsuario
                                AND u.contrasena = @contrasena
                                AND u.activo = 1";

            using MySqlCommand comando = new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
            comando.Parameters.AddWithValue("@contrasena", contrasena);

            using MySqlDataReader lector = await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                usuario = new Usuario()
                {
                    id_usuario = lector.GetInt32("id_usuario"),
                    nombre = lector.GetString("nombre"),
                    apellido = lector.GetString("apellido"),
                    rut = lector.GetString("rut"),
                    id_rol = lector.GetInt32("id_rol"),
                    nombre_usuario = lector.GetString("nombre_usuario"),
                    contrasena = lector.GetString("contrasena"),
                    activo = lector.GetBoolean("activo"),
                    rol = lector.GetString("rol")
                };
            }

            return usuario;
        }
        public async Task<List<string>> ObtenerPermisos(int idRol)
        {
            List<string> permisos = new List<string>();

            using MySqlConnection conexion = new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"SELECT p.nombre
                        FROM rol_permiso rp
                        INNER JOIN permiso p
                        ON rp.id_permiso = p.id_permiso
                        WHERE rp.id_rol = @idRol";

            using MySqlCommand comando = new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue("@idRol", idRol);

            using MySqlDataReader lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                permisos.Add(lector.GetString("nombre"));
            }

            return permisos;
        }
    }
}