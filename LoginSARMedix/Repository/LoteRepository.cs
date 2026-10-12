using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class LoteRepository
    {
        private readonly string _conexion;

        public LoteRepository(IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString("ConexionMySQL")!;
        }


        //LISTADO DE PRODUCTOS
        public async Task<List<Producto>> ListadoProductos()
        {
            List<Producto> productos =
                new List<Producto>();

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta =
                @"SELECT id_producto,
                         codigo_interno,
                         nombre,
                         stock_minimo
                  FROM producto
                  ORDER BY nombre";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                productos.Add(new Producto()
                {
                    id_producto =
                        Convert.ToInt32(
                            lector["id_producto"]),

                    codigo_interno =
                        lector["codigo_interno"].ToString(),

                    nombre =
                        lector["nombre"].ToString(),

                    stock_minimo =
                        Convert.ToInt32(
                            lector["stock_minimo"])
                });
            }

            return productos;
        }


        // =========================================================
        // LISTADO DE UBICACIONES
        // =========================================================

        public async Task<List<Ubicacion>> ListadoUbicaciones()
        {
            List<Ubicacion> ubicaciones =
                new List<Ubicacion>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
              id_ubicacion,
              nombre_sector,
              activo
          FROM ubicacion
          ORDER BY nombre_sector";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                ubicaciones.Add(
                    new Ubicacion()
                    {
                        id_ubicacion =
                            Convert.ToInt32(
                                lector["id_ubicacion"]
                            ),

                        nombre_sector =
                            lector["nombre_sector"]
                                .ToString(),

                        activo =
                            Convert.ToBoolean(
                                lector["activo"]
                            )
                    }
                );
            }


            return ubicaciones;
        }


        // =========================================================
        // LISTADO DE UBICACIONES ACTIVAS
        // =========================================================

        public async Task<List<Ubicacion>> ListadoUbicacionesActivas()
        {
            List<Ubicacion> ubicaciones =
                new List<Ubicacion>();


            using MySqlConnection conexion =
                new MySqlConnection(_conexion);


            await conexion.OpenAsync();


            string consulta =
                @"SELECT
              id_ubicacion,
              nombre_sector,
              activo
          FROM ubicacion
          WHERE activo = 1
          ORDER BY nombre_sector";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                ubicaciones.Add(
                    new Ubicacion()
                    {
                        id_ubicacion =
                            Convert.ToInt32(
                                lector["id_ubicacion"]
                            ),

                        nombre_sector =
                            lector["nombre_sector"]
                                .ToString(),

                        activo =
                            Convert.ToBoolean(
                                lector["activo"]
                            )
                    }
                );
            }


            return ubicaciones;
        }


        // =========================================================
        // VERIFICAR SI EXISTE UNA UBICACION
        // =========================================================

        public async Task<bool> ExisteUbicacion(
            string nombreSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"SELECT COUNT(*)
          FROM ubicacion
          WHERE LOWER(TRIM(nombre_sector))
                =
                LOWER(TRIM(@nombreSector))";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombreSector",
                nombreSector
            );


            int cantidad =
                Convert.ToInt32(
                    await comando.ExecuteScalarAsync()
                );


            return cantidad > 0;
        }


        // =========================================================
        // CREAR UBICACION
        // =========================================================

        public async Task<bool> CrearUbicacion(
            string nombreSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"INSERT INTO ubicacion
          (nombre_sector)

          VALUES
          (@nombreSector)";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombreSector",
                nombreSector
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        // =========================================================
        // OBTENER STOCK DE UNA UBICACION
        // =========================================================

        public async Task<int> ObtenerStockUbicacion(
            int idUbicacion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"SELECT COALESCE(SUM(cantidad_actual), 0)
          FROM lote
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int stock =
                Convert.ToInt32(
                    await comando.ExecuteScalarAsync()
                );


            return stock;
        }


        // =========================================================
        // INACTIVAR UBICACION
        // =========================================================

        public async Task<bool> InactivarUbicacion(
            int idUbicacion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"UPDATE ubicacion
          SET activo = 0
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        // =========================================================
        // REACTIVAR UBICACION
        // =========================================================

        public async Task<bool> ReactivarUbicacion(
            int idUbicacion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"UPDATE ubicacion
          SET activo = 1
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        // =========================================================
        // VERIFICAR SI LA UBICACION TIENE LOTES ASOCIADOS
        // =========================================================

        public async Task<bool> TieneLotesUbicacion(
            int idUbicacion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"SELECT COUNT(*)
          FROM lote
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int cantidad =
                Convert.ToInt32(
                    await comando.ExecuteScalarAsync()
                );


            return cantidad > 0;
        }


        // =========================================================
        // ELIMINAR UBICACION
        // =========================================================

        public async Task<bool> EliminarUbicacion(
            int idUbicacion)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"DELETE FROM ubicacion
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }



        // =========================================================
        // VERIFICAR NOMBRE DE UBICACION AL EDITAR
        // =========================================================

        public async Task<bool> ExisteUbicacionEditar(
            int idUbicacion,
            string nombreSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"SELECT COUNT(*)
          FROM ubicacion
          WHERE LOWER(TRIM(nombre_sector))
                =
                LOWER(TRIM(@nombreSector))
          AND id_ubicacion <> @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombreSector",
                nombreSector
            );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int cantidad =
                Convert.ToInt32(
                    await comando.ExecuteScalarAsync()
                );


            return cantidad > 0;
        }


        // =========================================================
        // ACTUALIZAR UBICACION
        // =========================================================

        public async Task<bool> ActualizarUbicacion(
            int idUbicacion,
            string nombreSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"UPDATE ubicacion
          SET nombre_sector = @nombreSector
          WHERE id_ubicacion = @idUbicacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@nombreSector",
                nombreSector
            );


            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );


            int resultado =
                await comando.ExecuteNonQueryAsync();


            return resultado > 0;
        }

        //CREAR LOTE
        public async Task<bool> CrearLote(Lote reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta =
                @"INSERT INTO lote
                  (numero_lote,
                   fecha_vencimiento,
                   cantidad_actual,
                   id_producto,
                   id_ubicacion)

                  VALUES
                  (@numeroLote,
                   @fechaVencimiento,
                   @cantidadActual,
                   @idProducto,
                   @idUbicacion)";

            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );

            comando.Parameters.AddWithValue(
                "@numeroLote",
                reg.numero_lote
            );

            comando.Parameters.AddWithValue(
                "@fechaVencimiento",
                reg.fecha_vencimiento
            );

            comando.Parameters.AddWithValue(
                "@cantidadActual",
                reg.cantidad_actual
            );

            comando.Parameters.AddWithValue(
                "@idProducto",
                reg.id_producto
            );

            comando.Parameters.AddWithValue(
                "@idUbicacion",
                reg.id_ubicacion
            );

            int resultado =
                await comando.ExecuteNonQueryAsync();

            return resultado > 0;
        }






        //LISTADO DE LOTES
        public async Task<List<Lote>> ListadoLotes()
        {
            List<Lote> lotes = new List<Lote>();

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                                SELECT
                                    l.id_lote,
                                    l.numero_lote,
                                    l.fecha_vencimiento,
                                    l.cantidad_actual,
                                    l.id_producto,
                                    l.id_ubicacion,
                                    p.nombre AS nombre_producto,
                                    u.nombre_sector AS nombre_ubicacion

                                FROM lote l

                                INNER JOIN producto p
                                ON l.id_producto = p.id_producto

                                INNER JOIN ubicacion u
                                ON l.id_ubicacion = u.id_ubicacion

                                ORDER BY l.fecha_vencimiento ASC";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                lotes.Add(new Lote()
                {
                    id_lote =
                        Convert.ToInt32(lector["id_lote"]),

                    numero_lote =
                        lector["numero_lote"].ToString(),

                    fecha_vencimiento =
                        Convert.ToDateTime(
                            lector["fecha_vencimiento"]),

                    cantidad_actual =
                        Convert.ToInt32(
                            lector["cantidad_actual"]),

                    id_producto =
                        Convert.ToInt32(
                            lector["id_producto"]),

                    id_ubicacion =
                        Convert.ToInt32(
                            lector["id_ubicacion"]),

                    nombre_producto =
                        lector["nombre_producto"].ToString(),

                    nombre_ubicacion =
                        lector["nombre_ubicacion"].ToString()
                });
            }

            return lotes;
        }






        //OBTENER LOTE POR ID
        public async Task<Lote?> ObtenerLotePorId(int idLote)
        {
            Lote? lote = null;

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"SELECT id_lote,
                               numero_lote,
                               fecha_vencimiento,
                               cantidad_actual,
                               id_producto,
                               id_ubicacion
                        FROM lote
                        WHERE id_lote = @idLote";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue(
                "@idLote",
                idLote
            );

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                lote = new Lote()
                {
                    id_lote =
                        Convert.ToInt32(lector["id_lote"]),

                    numero_lote =
                        lector["numero_lote"].ToString(),

                    fecha_vencimiento =
                        Convert.ToDateTime(
                            lector["fecha_vencimiento"]),

                    cantidad_actual =
                        Convert.ToInt32(
                            lector["cantidad_actual"]),

                    id_producto =
                        Convert.ToInt32(
                            lector["id_producto"]),

                    id_ubicacion =
                        Convert.ToInt32(
                            lector["id_ubicacion"])
                };
            }

            return lote;
        }







        //ACTUALIZAR LOTE
        public async Task<bool> ActualizarLote(Lote reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"UPDATE lote
                                SET numero_lote = @numeroLote,
                                    fecha_vencimiento = @fechaVencimiento,
                                    cantidad_actual = @cantidadActual,
                                    id_producto = @idProducto,
                                    id_ubicacion = @idUbicacion
                                WHERE id_lote = @idLote";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue(
                "@numeroLote",
                reg.numero_lote
            );

            comando.Parameters.AddWithValue(
                "@fechaVencimiento",
                reg.fecha_vencimiento
            );

            comando.Parameters.AddWithValue(
                "@cantidadActual",
                reg.cantidad_actual
            );

            comando.Parameters.AddWithValue(
                "@idProducto",
                reg.id_producto
            );

            comando.Parameters.AddWithValue(
                "@idUbicacion",
                reg.id_ubicacion
            );

            comando.Parameters.AddWithValue(
                "@idLote",
                reg.id_lote
            );

            int resultado =
                await comando.ExecuteNonQueryAsync();

            return resultado > 0;
        }






    }



}