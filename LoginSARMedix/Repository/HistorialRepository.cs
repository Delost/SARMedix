using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class HistorialRepository
    {
        private readonly string _conexion;


        public HistorialRepository(
            IConfiguration configuration)
        {
            _conexion =
                configuration
                    .GetConnectionString(
                        "ConexionMySQL"
                    )!;
        }



        // =========================================================
        // REGISTRAR HISTORIAL
        // =========================================================

        public async Task<bool> RegistrarHistorial(
            int idUsuarioResponsable,
            string usuarioResponsable,
            string descripcion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta =
                @"INSERT INTO historial
                  (
                      id_usuario_responsable,
                      usuario_responsable,
                      descripcion
                  )
                  VALUES
                  (
                      @idUsuarioResponsable,
                      @usuarioResponsable,
                      @descripcion
                  )";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUsuarioResponsable",
                idUsuarioResponsable
            );


            comando.Parameters.AddWithValue(
                "@usuarioResponsable",
                usuarioResponsable
            );


            comando.Parameters.AddWithValue(
                "@descripcion",
                descripcion
            );


            int resultado =
                await comando
                    .ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // =========================================================
        // HISTORIAL GENERAL
        //
        // TARJETA:
        // "Consultar todas las acciones registradas en SARMedix."
        //
        // POR ESO:
        // NO TIENE FILTRO.
        // MUESTRA TODO.
        // =========================================================

        public async Task<List<Historial>> ListadoHistorial()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
                      id_historial,
                      id_usuario_responsable,
                      usuario_responsable,
                      descripcion,
                      fecha_hora

                  FROM historial

                  ORDER BY
                      fecha_hora DESC,
                      id_historial DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando
                    .ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    CrearHistorialDesdeLector(
                        lector
                    )
                );
            }


            return historial;
        }



        // =========================================================
        // HISTORIAL DE USUARIOS
        //
        // TARJETA:
        // "Consultar cambios, creación, edición
        //  y gestión de usuarios."
        //
        // SOLO MOSTRAMOS ACCIONES QUE REALMENTE
        // MODIFICARON O GESTIONARON UN USUARIO.
        //
        // NO MOSTRAMOS:
        //
        // - Ingresó a Crear Usuario
        // - Ingresó al listado de usuarios
        // - Ingresó a editar al usuario...
        // - Ingresó a Usuarios y Permisos
        // - Roles y permisos
        //
        // ESAS ACCIONES SIGUEN EXISTIENDO,
        // PERO SE VEN EN HISTORIAL GENERAL.
        // =========================================================

        public async Task<List<Historial>>
            ListadoHistorialUsuarios()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
                      id_historial,
                      id_usuario_responsable,
                      usuario_responsable,
                      descripcion,
                      fecha_hora

                  FROM historial

                  WHERE

                      descripcion LIKE 'Creó al usuario %'

                      OR descripcion LIKE
                         'Actualizó al usuario %'

                      OR descripcion LIKE
                         'Desactivó al usuario %'

                      OR descripcion LIKE
                         'Reactivó al usuario %'

                      OR descripcion LIKE
                         'Eliminó al usuario %'

                  ORDER BY
                      fecha_hora DESC,
                      id_historial DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando
                    .ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    CrearHistorialDesdeLector(
                        lector
                    )
                );
            }


            return historial;
        }



        // =========================================================
        // HISTORIAL DE PRODUCTOS
        //
        // TARJETA:
        // "Consultar cambios realizados
        //  en productos y lotes."
        //
        // MOSTRAMOS:
        //
        // - Producto creado
        // - Producto actualizado
        // - Producto eliminado
        // - Producto desactivado/reactivado
        //
        // - Lote creado
        // - Lote actualizado
        // - Lote eliminado
        //
        // - Entradas de stock
        // - Salidas de stock
        //
        // NO MOSTRAMOS:
        //
        // - Ingresó al módulo Productos
        // - Ingresó a Crear Producto
        // - Ingresó al listado de productos
        // - Ingresó a editar producto
        // - Ingresó a Crear Lote
        // - Ingresó al listado de lotes
        // - Ingresó a editar lote
        //
        // TODO ESO QUEDA SOLAMENTE
        // EN HISTORIAL GENERAL.
        // =========================================================

        public async Task<List<Historial>>
            ListadoHistorialProductos()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
                      id_historial,
                      id_usuario_responsable,
                      usuario_responsable,
                      descripcion,
                      fecha_hora

                  FROM historial

                  WHERE

                      -- PRODUCTOS

                      descripcion LIKE
                          'Creó el producto %'

                      OR descripcion LIKE
                          'Actualizó el producto %'

                      OR descripcion LIKE
                          'Eliminó el producto %'

                      OR descripcion LIKE
                          'Desactivó el producto %'

                      OR descripcion LIKE
                          'Reactivó el producto %'


                      -- LOTES

                      OR descripcion LIKE
                          'Creó el lote %'

                      OR descripcion LIKE
                          'Actualizó el lote %'

                      OR descripcion LIKE
                          'Eliminó el lote %'


                      -- MOVIMIENTOS QUE CAMBIAN STOCK

                      OR descripcion LIKE
                          'Registró entrada %'

                      OR descripcion LIKE
                          'Registró salida %'

                  ORDER BY
                      fecha_hora DESC,
                      id_historial DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando
                    .ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    CrearHistorialDesdeLector(
                        lector
                    )
                );
            }


            return historial;
        }



        // =========================================================
        // CONVERTIR UNA FILA DE MYSQL EN HISTORIAL
        //
        // ANTES TENIAMOS ESTE MISMO CODIGO
        // REPETIDO EN LOS TRES METODOS.
        //
        // AHORA ESTA CENTRALIZADO AQUI.
        // =========================================================

        private Historial CrearHistorialDesdeLector(
            MySqlDataReader lector)
        {
            return new Historial()
            {
                id_historial =
                    lector.GetInt32(
                        "id_historial"
                    ),


                id_usuario_responsable =
                    lector.IsDBNull(
                        lector.GetOrdinal(
                            "id_usuario_responsable"
                        )
                    )
                    ? null
                    : lector.GetInt32(
                        "id_usuario_responsable"
                    ),


                usuario_responsable =
                    lector.GetString(
                        "usuario_responsable"
                    ),


                descripcion =
                    lector.GetString(
                        "descripcion"
                    ),


                fecha_hora =
                    lector.GetDateTime(
                        "fecha_hora"
                    )
            };
        }
    }
}