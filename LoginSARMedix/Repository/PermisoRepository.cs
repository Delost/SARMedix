using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class PermisoRepository
    {
        private readonly string _conexion;


        public PermisoRepository(IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString("ConexionMySQL")!;
        }



        //==================================================
        // LISTAR ROLES ACTIVOS
        //==================================================

        public async Task<List<Rol>> ListadoRoles()
        {
            List<Rol> roles =
                new List<Rol>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_rol,
                         nombre,
                         descripcion,
                         activo
                  FROM rol
                  WHERE activo = 1";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                roles.Add(
                    new Rol()
                    {
                        id_rol =
                            Convert.ToInt32(
                                lector["id_rol"]
                            ),

                        nombre =
                            lector["nombre"].ToString(),

                        descripcion =
                            lector["descripcion"].ToString(),

                        activo =
                            Convert.ToBoolean(
                                lector["activo"]
                            )
                    }
                );
            }


            return roles;
        }



        //==================================================
        // LISTAR TODOS LOS PERMISOS
        //==================================================

        public async Task<List<Permiso>> ListadoPermisos()
        {
            List<Permiso> permisos =
                new List<Permiso>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_permiso,
                         codigo,
                         nombre,
                         descripcion
                  FROM permiso";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                permisos.Add(
                    new Permiso()
                    {
                        id_permiso =
                            Convert.ToInt32(
                                lector["id_permiso"]
                            ),

                        codigo =
                            lector["codigo"].ToString(),

                        nombre =
                            lector["nombre"].ToString(),

                        descripcion =
                            lector["descripcion"].ToString()
                    }
                );
            }


            return permisos;
        }



        //==================================================
        // OBTENER PERMISOS DE UN ROL
        //==================================================

        public async Task<List<int>> PermisosPorRol(
            int idRol)
        {
            List<int> permisos =
                new List<int>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_permiso
                  FROM rol_permiso
                  WHERE id_rol = @idRol";


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
                    Convert.ToInt32(
                        lector["id_permiso"]
                    )
                );
            }


            return permisos;
        }



        //==================================================
        // ACTUALIZAR PERMISOS DE UN ROL
        //==================================================

        public async Task<bool> ActualizarPermisosRol(
            int idRol,
            List<int> permisosSeleccionados)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            using MySqlTransaction transaccion =
                conexion.BeginTransaction();


            try
            {
                string consultaEliminar =
                    @"DELETE FROM rol_permiso
                      WHERE id_rol = @idRol";


                using MySqlCommand comandoEliminar =
                    new MySqlCommand(
                        consultaEliminar,
                        conexion,
                        transaccion
                    );


                comandoEliminar.Parameters.AddWithValue(
                    "@idRol",
                    idRol
                );


                await comandoEliminar
                    .ExecuteNonQueryAsync();



                foreach (
                    int idPermiso
                    in permisosSeleccionados)
                {
                    string consultaInsertar =
                        @"INSERT INTO rol_permiso
                          (id_rol, id_permiso)
                          VALUES
                          (@idRol, @idPermiso)";


                    using MySqlCommand comandoInsertar =
                        new MySqlCommand(
                            consultaInsertar,
                            conexion,
                            transaccion
                        );


                    comandoInsertar.Parameters
                        .AddWithValue(
                            "@idRol",
                            idRol
                        );


                    comandoInsertar.Parameters
                        .AddWithValue(
                            "@idPermiso",
                            idPermiso
                        );


                    await comandoInsertar
                        .ExecuteNonQueryAsync();
                }


                transaccion.Commit();


                return true;
            }
            catch
            {
                transaccion.Rollback();


                return false;
            }
        }



        //==================================================
        // CREAR NUEVO ROL
        //==================================================

        public async Task<bool> CrearRol(Rol reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();



            //VERIFICAR SI EL ROL YA EXISTE

            string consultaExiste =
                @"SELECT COUNT(*)
                  FROM rol
                  WHERE nombre = @nombre";


            using MySqlCommand comandoExiste =
                new MySqlCommand(
                    consultaExiste,
                    conexion
                );


            comandoExiste.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
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



            //INSERTAR NUEVO ROL

            string consultaInsertar =
                @"INSERT INTO rol
                  (nombre, descripcion, activo)
                  VALUES
                  (@nombre, @descripcion, 1)";


            using MySqlCommand comandoInsertar =
                new MySqlCommand(
                    consultaInsertar,
                    conexion
                );


            comandoInsertar.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );


            comandoInsertar.Parameters.AddWithValue(
                "@descripcion",
                reg.descripcion
            );


            int resultado =
                await comandoInsertar
                    .ExecuteNonQueryAsync();


            return resultado > 0;
        }



        //==================================================
        // OBTENER ROL POR ID
        //==================================================

        public async Task<Rol?> ObtenerRolPorId(
            int idRol)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_rol,
                         nombre,
                         descripcion,
                         activo
                  FROM rol
                  WHERE id_rol = @idRol";


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


            if (await lector.ReadAsync())
            {
                return new Rol()
                {
                    id_rol =
                        Convert.ToInt32(
                            lector["id_rol"]
                        ),

                    nombre =
                        lector["nombre"].ToString(),

                    descripcion =
                        lector["descripcion"].ToString(),

                    activo =
                        Convert.ToBoolean(
                            lector["activo"]
                        )
                };
            }


            return null;
        }



        //==================================================
        // ACTUALIZAR ROL
        //==================================================

        public async Task<bool> ActualizarRol(
            Rol reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();



            //VERIFICAR SI OTRO ROL TIENE EL MISMO NOMBRE

            string consultaExiste =
                @"SELECT COUNT(*)
                  FROM rol
                  WHERE nombre = @nombre
                  AND id_rol <> @idRol";


            using MySqlCommand comandoExiste =
                new MySqlCommand(
                    consultaExiste,
                    conexion
                );


            comandoExiste.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );


            comandoExiste.Parameters.AddWithValue(
                "@idRol",
                reg.id_rol
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



            //ACTUALIZAR NOMBRE Y DESCRIPCION

            string consultaActualizar =
                @"UPDATE rol
                  SET nombre = @nombre,
                      descripcion = @descripcion
                  WHERE id_rol = @idRol";


            using MySqlCommand comandoActualizar =
                new MySqlCommand(
                    consultaActualizar,
                    conexion
                );


            comandoActualizar.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );


            comandoActualizar.Parameters.AddWithValue(
                "@descripcion",
                reg.descripcion
            );


            comandoActualizar.Parameters.AddWithValue(
                "@idRol",
                reg.id_rol
            );


            int resultado =
                await comandoActualizar
                    .ExecuteNonQueryAsync();


            return resultado > 0;
        }



        //==================================================
        // DESACTIVAR ROL
        //==================================================

        public async Task<bool> DesactivarRol(
            int idRol)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE rol
                  SET activo = 0
                  WHERE id_rol = @idRol";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idRol",
                idRol
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        //==================================================
        // LISTAR ROLES INACTIVOS
        //==================================================

        public async Task<List<Rol>>
            ListadoRolesInactivos()
        {
            List<Rol> roles =
                new List<Rol>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT id_rol,
                         nombre,
                         descripcion,
                         activo
                  FROM rol
                  WHERE activo = 0";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                roles.Add(
                    new Rol()
                    {
                        id_rol =
                            Convert.ToInt32(
                                lector["id_rol"]
                            ),

                        nombre =
                            lector["nombre"].ToString(),

                        descripcion =
                            lector["descripcion"].ToString(),

                        activo =
                            Convert.ToBoolean(
                                lector["activo"]
                            )
                    }
                );
            }


            return roles;
        }



        //==================================================
        // REACTIVAR ROL
        //==================================================

        public async Task<bool> ReactivarRol(
            int idRol)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"UPDATE rol
                  SET activo = 1
                  WHERE id_rol = @idRol";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idRol",
                idRol
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }
    }
}