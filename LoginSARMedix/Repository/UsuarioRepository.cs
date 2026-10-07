using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class UsuarioRepository
    {
        private readonly string _conexion;


        public UsuarioRepository(IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString("ConexionMySQL")!;
        }



        // INICIAR SESION

        public async Task<Usuario?> IniciarSesion(
            string nombreUsuario,
            string contrasena)
        {
            Usuario? usuario = null;


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT u.id_usuario,
                         u.nombre,
                         u.apellido,
                         u.email,
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


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombreUsuario",
                nombreUsuario
            );


            comando.Parameters.AddWithValue(
                "@contrasena",
                contrasena
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            if (await lector.ReadAsync())
            {
                usuario =
                    new Usuario()
                    {
                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),

                        nombre =
                            lector.GetString(
                                "nombre"
                            ),

                        apellido =
                            lector.GetString(
                                "apellido"
                            ),

                        email =
                            lector.IsDBNull(
                                lector.GetOrdinal("email")
                            )
                            ? null
                            : lector.GetString("email"),

                        rut =
                            lector.GetString(
                                "rut"
                            ),

                        id_rol =
                            lector.GetInt32(
                                "id_rol"
                            ),

                        nombre_usuario =
                            lector.GetString(
                                "nombre_usuario"
                            ),

                        contrasena =
                            lector.GetString(
                                "contrasena"
                            ),

                        activo =
                            lector.GetBoolean(
                                "activo"
                            ),

                        rol =
                            lector.GetString(
                                "rol"
                            )
                    };
            }


            return usuario;
        }



        // OBTENER PERMISOS

        public async Task<List<string>> ObtenerPermisos(
            int idRol)
        {
            List<string> permisos =
                new List<string>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT p.nombre
                  FROM rol_permiso rp
                  INNER JOIN permiso p
                  ON rp.id_permiso = p.id_permiso
                  WHERE rp.id_rol = @idRol";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idRol",
                idRol
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                permisos.Add(
                    lector.GetString(
                        "nombre"
                    )
                );
            }


            return permisos;
        }



        // CREAR USUARIO

        public async Task<bool> CrearUsuario(
            Usuario reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();



            // COMPROBAR SI YA EXISTE RUT, USUARIO O EMAIL

            string consultaExiste =
                @"SELECT COUNT(*)
                  FROM usuario
                  WHERE rut = @rut
                  OR nombre_usuario = @nombreUsuario
                  OR email = @email";


            using MySqlCommand comandoExiste =
                new MySqlCommand(
                    consultaExiste,
                    conexion
                );


            comandoExiste.Parameters.AddWithValue(
                "@rut",
                reg.rut
            );


            comandoExiste.Parameters.AddWithValue(
                "@nombreUsuario",
                reg.nombre_usuario
            );


            comandoExiste.Parameters.AddWithValue(
                "@email",
                reg.email
            );


            int cantidad =
                Convert.ToInt32(
                    await comandoExiste
                        .ExecuteScalarAsync()
                );


            if (cantidad > 0)
            {
                return false;
            }



            // INSERTAR NUEVO USUARIO

            string consulta =
                @"INSERT INTO usuario
                  (
                      nombre,
                      apellido,
                      email,
                      rut,
                      id_rol,
                      nombre_usuario,
                      contrasena,
                      activo
                  )
                  VALUES
                  (
                      @nombre,
                      @apellido,
                      @email,
                      @rut,
                      @idRol,
                      @nombreUsuario,
                      @contrasena,
                      1
                  )";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );


            comando.Parameters.AddWithValue(
                "@apellido",
                reg.apellido
            );


            comando.Parameters.AddWithValue(
                "@email",
                reg.email
            );


            comando.Parameters.AddWithValue(
                "@rut",
                reg.rut
            );


            comando.Parameters.AddWithValue(
                "@idRol",
                reg.id_rol
            );


            comando.Parameters.AddWithValue(
                "@nombreUsuario",
                reg.nombre_usuario
            );


            comando.Parameters.AddWithValue(
                "@contrasena",
                reg.contrasena
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // LISTADO DE USUARIOS

        public async Task<List<Usuario>>
            ListadoUsuarios()
        {
            List<Usuario> usuarios =
                new List<Usuario>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT u.id_usuario,
                         u.nombre,
                         u.apellido,
                         u.email,
                         u.rut,
                         u.id_rol,
                         u.nombre_usuario,
                         u.activo,
                         r.nombre AS rol
                  FROM usuario u
                  INNER JOIN rol r
                  ON u.id_rol = r.id_rol";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                usuarios.Add(
                    new Usuario()
                    {
                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),

                        nombre =
                            lector.GetString(
                                "nombre"
                            ),

                        apellido =
                            lector.GetString(
                                "apellido"
                            ),

                        email =
                            lector.IsDBNull(
                                lector.GetOrdinal("email")
                            )
                            ? null
                            : lector.GetString("email"),

                        rut =
                            lector.GetString(
                                "rut"
                            ),

                        id_rol =
                            lector.GetInt32(
                                "id_rol"
                            ),

                        nombre_usuario =
                            lector.GetString(
                                "nombre_usuario"
                            ),

                        activo =
                            lector.GetBoolean(
                                "activo"
                            ),

                        rol =
                            lector.GetString(
                                "rol"
                            )
                    }
                );
            }


            return usuarios;
        }



        // DESACTIVAR USUARIO

        public async Task<bool> DesactivarUsuario(
            int idUsuario)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE usuario
                  SET activo = 0
                  WHERE id_usuario = @idUsuario";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // OBTENER USUARIO POR ID

        public async Task<Usuario?> ObtenerUsuarioPorId(
            int idUsuario)
        {
            Usuario? usuario = null;


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_usuario,
                         nombre,
                         apellido,
                         email,
                         rut,
                         id_rol,
                         nombre_usuario,
                         activo
                  FROM usuario
                  WHERE id_usuario = @idUsuario";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            if (await lector.ReadAsync())
            {
                usuario =
                    new Usuario()
                    {
                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),

                        nombre =
                            lector.GetString(
                                "nombre"
                            ),

                        apellido =
                            lector.GetString(
                                "apellido"
                            ),

                        email =
                            lector.IsDBNull(
                                lector.GetOrdinal("email")
                            )
                            ? null
                            : lector.GetString("email"),

                        rut =
                            lector.GetString(
                                "rut"
                            ),

                        id_rol =
                            lector.GetInt32(
                                "id_rol"
                            ),

                        nombre_usuario =
                            lector.GetString(
                                "nombre_usuario"
                            ),

                        activo =
                            lector.GetBoolean(
                                "activo"
                            )
                    };
            }


            return usuario;
        }



        // ACTUALIZAR USUARIO

        public async Task<bool> ActualizarUsuario(
            Usuario reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE usuario
                  SET nombre = @nombre,
                      apellido = @apellido,
                      email = @email,
                      rut = @rut,
                      id_rol = @idRol,
                      nombre_usuario = @nombreUsuario
                  WHERE id_usuario = @idUsuario";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );


            comando.Parameters.AddWithValue(
                "@apellido",
                reg.apellido
            );


            comando.Parameters.AddWithValue(
                "@email",
                reg.email
            );


            comando.Parameters.AddWithValue(
                "@rut",
                reg.rut
            );


            comando.Parameters.AddWithValue(
                "@idRol",
                reg.id_rol
            );


            comando.Parameters.AddWithValue(
                "@nombreUsuario",
                reg.nombre_usuario
            );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                reg.id_usuario
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // LISTADO DE USUARIOS INACTIVOS

        public async Task<List<Usuario>>
            ListadoEliminarUsuarios()
        {
            List<Usuario> usuarios =
                new List<Usuario>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT u.id_usuario,
                         u.nombre,
                         u.apellido,
                         u.email,
                         u.rut,
                         u.id_rol,
                         u.nombre_usuario,
                         u.activo,
                         r.nombre AS rol
                  FROM usuario u
                  INNER JOIN rol r
                  ON u.id_rol = r.id_rol
                  WHERE u.activo = 0";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                usuarios.Add(
                    new Usuario()
                    {
                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),

                        nombre =
                            lector.GetString(
                                "nombre"
                            ),

                        apellido =
                            lector.GetString(
                                "apellido"
                            ),

                        email =
                            lector.IsDBNull(
                                lector.GetOrdinal("email")
                            )
                            ? null
                            : lector.GetString("email"),

                        rut =
                            lector.GetString(
                                "rut"
                            ),

                        id_rol =
                            lector.GetInt32(
                                "id_rol"
                            ),

                        nombre_usuario =
                            lector.GetString(
                                "nombre_usuario"
                            ),

                        activo =
                            lector.GetBoolean(
                                "activo"
                            ),

                        rol =
                            lector.GetString(
                                "rol"
                            )
                    }
                );
            }


            return usuarios;
        }



        // REACTIVAR USUARIO

        public async Task<bool> ReactivarUsuario(
            int idUsuario)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE usuario
                  SET activo = 1
                  WHERE id_usuario = @idUsuario
                  AND activo = 0";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // ELIMINAR USUARIO

        public async Task<bool> EliminarUsuario(
            int idUsuario)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"DELETE FROM usuario
                  WHERE id_usuario = @idUsuario
                  AND activo = 0";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // BUSCAR USUARIO POR CORREO

        public async Task<Usuario?> BuscarPorCorreo(
            string correo)
        {
            Usuario? usuario = null;


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_usuario,
                         nombre,
                         apellido,
                         email
                  FROM usuario
                  WHERE email = @correo
                  AND activo = 1
                  LIMIT 1";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@correo",
                correo
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            if (await lector.ReadAsync())
            {
                usuario =
                    new Usuario()
                    {
                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),

                        nombre =
                            lector.GetString(
                                "nombre"
                            ),

                        apellido =
                            lector.GetString(
                                "apellido"
                            ),

                        email =
                            lector.IsDBNull(
                                lector.GetOrdinal("email")
                            )
                            ? null
                            : lector.GetString("email")
                    };
            }


            return usuario;
        }

        // CREAR CODIGO DE RECUPERACION

        public async Task<bool> CrearCodigoRecuperacion(
            int idUsuario,
            string codigo,
            DateTime fechaExpiracion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"INSERT INTO recuperacion_contrasena
          (
              id_usuario,
              codigo,
              fecha_expiracion,
              utilizado
          )
          VALUES
          (
              @idUsuario,
              @codigo,
              @fechaExpiracion,
              0
          )";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            comando.Parameters.AddWithValue(
                "@codigo",
                codigo
            );


            comando.Parameters.AddWithValue(
                "@fechaExpiracion",
                fechaExpiracion
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        // VALIDAR CODIGO DE RECUPERACION

        public async Task<int?> ValidarCodigoRecuperacion(
            string codigo)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_usuario
          FROM recuperacion_contrasena
          WHERE codigo = @codigo
          AND utilizado = 0
          AND fecha_expiracion >= NOW()
          ORDER BY id_recuperacion DESC
          LIMIT 1";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@codigo",
                codigo
            );


            object? resultado =
                await comando.ExecuteScalarAsync();


            if (resultado == null)
            {
                return null;
            }


            return Convert.ToInt32(
                resultado
            );
        }

        // CAMBIAR CONTRASEÑA

        public async Task<bool> CambiarContrasena(
            int idUsuario,
            string nuevaContrasena)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE usuario
          SET contrasena = @contrasena
          WHERE id_usuario = @idUsuario";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@contrasena",
                nuevaContrasena
            );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        // MARCAR CODIGO COMO UTILIZADO

        public async Task<bool> MarcarCodigoComoUtilizado(
            int idUsuario,
            string codigo)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE recuperacion_contrasena
          SET utilizado = 1
          WHERE id_usuario = @idUsuario
          AND codigo = @codigo
          AND utilizado = 0";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuario",
                idUsuario
            );


            comando.Parameters.AddWithValue(
                "@codigo",
                codigo
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

    }
}