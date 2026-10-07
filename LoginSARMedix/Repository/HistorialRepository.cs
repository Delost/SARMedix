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


        //==================================================
        // REGISTRAR HISTORIAL
        //==================================================

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
    }
}