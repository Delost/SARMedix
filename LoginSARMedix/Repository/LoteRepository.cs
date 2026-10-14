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


        // =========================================================
        // LISTADO DE PRODUCTOS PARA REGISTRAR ENTRADAS
        // Incluye contenido y unidad para convertir envases a stock real.
        // =========================================================
        public async Task<List<Producto>> ListadoProductos()
        {
            List<Producto> productos =
                new List<Producto>();

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    p.id_producto,
                    p.codigo_interno,
                    p.nombre,
                    p.contenido_por_envase,
                    p.unidad_medida,
                    p.stock_minimo,
                    m.forma_farmaceutica,
                    CASE
                        WHEN m.id_producto IS NOT NULL THEN 'Medicamento'
                        WHEN i.id_producto IS NOT NULL THEN 'Insumo'
                        ELSE ''
                    END AS tipo
                FROM producto p
                LEFT JOIN medicamento m
                    ON p.id_producto = m.id_producto
                LEFT JOIN insumo i
                    ON p.id_producto = i.id_producto
                ORDER BY p.nombre";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                productos.Add(new Producto()
                {
                    id_producto =
                        Convert.ToInt32(lector["id_producto"]),

                    codigo_interno =
                        lector["codigo_interno"].ToString(),

                    nombre =
                        lector["nombre"].ToString(),

                    contenido_por_envase =
                        lector["contenido_por_envase"] == DBNull.Value
                            ? 1
                            : Convert.ToInt32(
                                lector["contenido_por_envase"]
                            ),

                    unidad_medida =
                        lector["unidad_medida"] == DBNull.Value
                            ? null
                            : lector["unidad_medida"].ToString(),

                    stock_minimo =
                        Convert.ToInt32(lector["stock_minimo"]),

                    forma_farmaceutica =
                        lector["forma_farmaceutica"] == DBNull.Value
                            ? null
                            : lector["forma_farmaceutica"].ToString(),

                    tipo =
                        lector["tipo"].ToString()
                });
            }

            return productos;
        }


        // =========================================================
        // PRODUCTOS DISPONIBLES EN UNA CLINICA
        // Solo medicamentos con stock útil en la clínica seleccionada.
        // Excluye vencidos y medicamentos controlados.
        // =========================================================
        public async Task<List<Producto>> ListadoProductosDisponiblesClinica(
            int idUbicacion)
        {
            List<Producto> productos =
                new List<Producto>();

            if (idUbicacion <= 0)
            {
                return productos;
            }

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                SELECT DISTINCT
                    p.id_producto,
                    p.codigo_interno,
                    p.nombre,
                    p.unidad_medida,
                    p.stock_minimo,
                    m.forma_farmaceutica,
                    m.concentracion

                FROM lote l

                INNER JOIN producto p
                    ON p.id_producto = l.id_producto

                INNER JOIN medicamento m
                    ON m.id_producto = p.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = l.id_ubicacion

                WHERE
                    l.id_ubicacion = @idUbicacion

                    AND u.activo = 1

                    AND LOWER(TRIM(u.tipo_sector)) = 'clinica'

                    AND l.cantidad_actual > 0

                    AND
                    (
                        l.fecha_vencimiento IS NULL
                        OR DATE(l.fecha_vencimiento) >= CURDATE()
                    )

                    AND COALESCE(m.requiere_control, 0) = 0

                ORDER BY p.nombre";

            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );

            comando.Parameters.AddWithValue(
                "@idUbicacion",
                idUbicacion
            );

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                productos.Add(
                    new Producto()
                    {
                        id_producto =
                            Convert.ToInt32(
                                lector["id_producto"]
                            ),

                        codigo_interno =
                            lector["codigo_interno"]?.ToString(),

                        nombre =
                            lector["nombre"]?.ToString(),

                        unidad_medida =
                            lector["unidad_medida"] == DBNull.Value
                                ? null
                                : lector["unidad_medida"].ToString(),

                        stock_minimo =
                            Convert.ToInt32(
                                lector["stock_minimo"]
                            ),

                        forma_farmaceutica =
                            lector["forma_farmaceutica"] == DBNull.Value
                                ? null
                                : lector["forma_farmaceutica"].ToString(),

                        concentracion =
                            lector["concentracion"] == DBNull.Value
                                ? null
                                : lector["concentracion"].ToString(),

                        tipo = "Medicamento"
                    }
                );
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
              tipo_sector,
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

                        tipo_sector =
                            lector["tipo_sector"]
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
              tipo_sector,
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

                        tipo_sector =
                            lector["tipo_sector"]
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
            string nombreSector,
            string tipoSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"INSERT INTO ubicacion
          (nombre_sector, tipo_sector)

          VALUES
          (@nombreSector, @tipoSector)";


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
                "@tipoSector",
                tipoSector
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
            string nombreSector,
            string tipoSector)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            string consulta =
                @"UPDATE ubicacion
          SET nombre_sector = @nombreSector,
              tipo_sector = @tipoSector
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
                "@tipoSector",
                tipoSector
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

        // =========================================================
        // PREPARAR LOTE PARA UNA ENTRADA
        //
        // SI YA EXISTE:
        // DEVUELVE SU ID SIN MODIFICAR EL STOCK.
        //
        // SI NO EXISTE:
        // CREA EL LOTE CON STOCK 0 Y DEVUELVE SU ID.
        //
        // EL STOCK LO MODIFICA MOVIMIENTO REPOSITORY.
        // =========================================================

        public async Task<(
            bool exito,
            string mensaje,
            int idLote,
            bool loteExistente)>
            PrepararLoteEntrada(
                Lote reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            using MySqlTransaction transaccion =
                conexion.BeginTransaction();


            try
            {
                int idLoteExistente = 0;

                DateTime? fechaVencimientoExistente =
                    null;



                // =====================================================
                // BUSCAR MISMO PRODUCTO + LOTE + UBICACION
                // =====================================================

                string consultaBuscar =
                    @"SELECT
                  id_lote,
                  fecha_vencimiento

              FROM lote

              WHERE id_producto = @idProducto
                AND numero_lote = @numeroLote
                AND id_ubicacion = @idUbicacion

              LIMIT 1

              FOR UPDATE";


                using (
                    MySqlCommand comandoBuscar =
                        new MySqlCommand(
                            consultaBuscar,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoBuscar.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );


                    comandoBuscar.Parameters.AddWithValue(
                        "@numeroLote",
                        reg.numero_lote
                    );


                    comandoBuscar.Parameters.AddWithValue(
                        "@idUbicacion",
                        reg.id_ubicacion
                    );


                    using MySqlDataReader lector =
                        await comandoBuscar
                            .ExecuteReaderAsync();


                    if (await lector.ReadAsync())
                    {
                        idLoteExistente =
                            Convert.ToInt32(
                                lector["id_lote"]
                            );


                        fechaVencimientoExistente =
                            Convert.ToDateTime(
                                lector["fecha_vencimiento"]
                            );
                    }
                }



                // =====================================================
                // SI YA EXISTE
                // =====================================================

                if (idLoteExistente > 0)
                {
                    if (
                        fechaVencimientoExistente.HasValue
                        &&
                        reg.fecha_vencimiento.HasValue
                        &&
                        fechaVencimientoExistente.Value.Date
                        !=
                        reg.fecha_vencimiento.Value.Date
                    )
                    {
                        transaccion.Rollback();


                        return (
                            false,
                            "El lote ya existe, pero la fecha de vencimiento no coincide.",
                            0,
                            true
                        );
                    }


                    transaccion.Commit();


                    return (
                        true,
                        "Lote existente.",
                        idLoteExistente,
                        true
                    );
                }



                // =====================================================
                // SI NO EXISTE, CREARLO CON STOCK 0
                // =====================================================

                string consultaCrear =
                    @"INSERT INTO lote
              (
                  numero_lote,
                  fecha_vencimiento,
                  cantidad_actual,
                  id_producto,
                  id_ubicacion
              )
              VALUES
              (
                  @numeroLote,
                  @fechaVencimiento,
                  0,
                  @idProducto,
                  @idUbicacion
              )";


                using MySqlCommand comandoCrear =
                    new MySqlCommand(
                        consultaCrear,
                        conexion,
                        transaccion
                    );


                comandoCrear.Parameters.AddWithValue(
                    "@numeroLote",
                    reg.numero_lote
                );


                comandoCrear.Parameters.AddWithValue(
                    "@fechaVencimiento",
                    reg.fecha_vencimiento
                );


                comandoCrear.Parameters.AddWithValue(
                    "@idProducto",
                    reg.id_producto
                );


                comandoCrear.Parameters.AddWithValue(
                    "@idUbicacion",
                    reg.id_ubicacion
                );


                await comandoCrear
                    .ExecuteNonQueryAsync();


                int idNuevoLote =
                    Convert.ToInt32(
                        comandoCrear.LastInsertedId
                    );


                transaccion.Commit();


                return (
                    true,
                    "Nuevo lote preparado.",
                    idNuevoLote,
                    false
                );
            }
            catch
            {
                try
                {
                    transaccion.Rollback();
                }
                catch
                {
                }


                return (
                    false,
                    "Ocurrió un error al preparar el lote.",
                    0,
                    false
                );
            }
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