using LoginSARMedix.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class HistorialRepository
    {
        private readonly string _conexion;

        public HistorialRepository(IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString("ConexionMySQL")!;
        }


        // REGISTRAR HISTORIAL

        public async Task<bool> RegistrarHistorial(
            int idUsuarioResponsable,
            string usuarioResponsable,
            string descripcion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

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
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // LISTAR HISTORIAL

        public async Task<List<Historial>> ListadoHistorial()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
                      id_historial,
                      id_usuario_responsable,
                      usuario_responsable,
                      descripcion,
                      fecha_hora
                  FROM historial
                  ORDER BY fecha_hora DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    new Historial()
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
                    }
                );
            }


            return historial;
        }







        //HISTORIAL DE USUARIOS
        public async Task<List<Historial>> ListadoHistorialUsuarios()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
              id_historial,
              id_usuario_responsable,
              usuario_responsable,
              descripcion,
              fecha_hora
          FROM historial
          WHERE descripcion LIKE '%usuario%'
             OR descripcion LIKE '%rol%'
             OR descripcion LIKE '%permiso%'
          ORDER BY fecha_hora DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    new Historial()
                    {
                        id_historial =
                            lector.GetInt32("id_historial"),

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
                    }
                );
            }


            return historial;
        }





        // HISTORIAL DE PRODUCTOS

        public async Task<List<Historial>> ListadoHistorialProductos()
        {
            List<Historial> historial =
                new List<Historial>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
              id_historial,
              id_usuario_responsable,
              usuario_responsable,
              descripcion,
              fecha_hora
          FROM historial
          WHERE descripcion LIKE '%producto%'
             OR descripcion LIKE '%lote%'
          ORDER BY fecha_hora DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                historial.Add(
                    new Historial()
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
                    }
                );
            }


            return historial;
        }



    }
}