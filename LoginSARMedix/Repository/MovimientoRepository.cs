using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class MovimientoRepository
    {
        private readonly string _conexion;


        public MovimientoRepository(
            IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString(
                    "ConexionMySQL"
                )!;
        }



        // =========================================================
        // REGISTRAR MOVIMIENTO DE MEDICAMENTO CONTROLADO
        // =========================================================

        public async Task<(
            bool exito,
            string mensaje,
            string nombreProducto,
            string numeroLote,
            int stockAnterior,
            int stockResultante)>
            RegistrarMovimientoControlado(
                int idLote,
                string tipoMovimiento,
                int cantidad,
                string observacion,
                int idUsuario)
        {
            string tipo =
                (tipoMovimiento ?? "")
                    .Trim();


            // VALIDAR TIPO DE MOVIMIENTO

            if (!tipo.Equals(
                    "Entrada",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !tipo.Equals(
                    "Salida",
                    StringComparison.OrdinalIgnoreCase))
            {
                return (
                    false,
                    "El tipo de movimiento no es válido.",
                    "",
                    "",
                    0,
                    0
                );
            }


            // NORMALIZAR EL TEXTO QUE SE GUARDARA

            tipo =
                tipo.Equals(
                    "Entrada",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Entrada"
                    : "Salida";


            // VALIDAR CANTIDAD

            if (cantidad <= 0)
            {
                return (
                    false,
                    "La cantidad debe ser mayor a 0.",
                    "",
                    "",
                    0,
                    0
                );
            }


            // VALIDAR OBSERVACION

            observacion =
                (observacion ?? "")
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                    observacion))
            {
                return (
                    false,
                    "Debe indicar el motivo del movimiento.",
                    "",
                    "",
                    0,
                    0
                );
            }


            if (observacion.Length > 255)
            {
                return (
                    false,
                    "La observación no puede superar los 255 caracteres.",
                    "",
                    "",
                    0,
                    0
                );
            }


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            using MySqlTransaction transaccion =
                conexion.BeginTransaction();


            try
            {
                int stockAnterior = 0;

                int stockResultante = 0;


                string nombreProducto =
                    "";


                string numeroLote =
                    "";


                bool loteEncontrado =
                    false;


                bool requiereControl =
                    false;



                // =================================================
                // OBTENER EL LOTE Y BLOQUEARLO MIENTRAS
                // SE REALIZA LA TRANSACCION
                // =================================================

                string consultaLote = @"
                    SELECT
                        l.cantidad_actual,
                        l.numero_lote,
                        p.nombre,
                        m.requiere_control

                    FROM lote l

                    INNER JOIN producto p
                    ON l.id_producto = p.id_producto

                    INNER JOIN medicamento m
                    ON p.id_producto = m.id_producto

                    WHERE l.id_lote = @idLote

                    FOR UPDATE";


                using (
                    MySqlCommand comandoLote =
                        new MySqlCommand(
                            consultaLote,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoLote.Parameters.AddWithValue(
                        "@idLote",
                        idLote
                    );


                    using MySqlDataReader lector =
                        await comandoLote
                            .ExecuteReaderAsync();


                    if (await lector.ReadAsync())
                    {
                        loteEncontrado =
                            true;


                        stockAnterior =
                            Convert.ToInt32(
                                lector[
                                    "cantidad_actual"
                                ]
                            );


                        numeroLote =
                            lector[
                                "numero_lote"
                            ]
                                .ToString()
                            ?? "";


                        nombreProducto =
                            lector[
                                "nombre"
                            ]
                                .ToString()
                            ?? "";


                        requiereControl =
                            Convert.ToBoolean(
                                lector[
                                    "requiere_control"
                                ]
                            );
                    }
                }



                // =================================================
                // VALIDAR QUE EL LOTE EXISTA
                // =================================================

                if (!loteEncontrado)
                {
                    transaccion.Rollback();


                    return (
                        false,
                        "No se encontró el lote seleccionado.",
                        "",
                        "",
                        0,
                        0
                    );
                }



                // =================================================
                // VALIDAR QUE SEA MEDICAMENTO CONTROLADO
                // =================================================

                if (!requiereControl)
                {
                    transaccion.Rollback();


                    return (
                        false,
                        "El medicamento seleccionado no requiere control.",
                        nombreProducto,
                        numeroLote,
                        stockAnterior,
                        stockAnterior
                    );
                }



                // =================================================
                // CALCULAR STOCK RESULTANTE
                // =================================================

                if (tipo == "Entrada")
                {
                    if (cantidad >
                        int.MaxValue
                        -
                        stockAnterior)
                    {
                        transaccion.Rollback();


                        return (
                            false,
                            "La cantidad ingresada es demasiado alta.",
                            nombreProducto,
                            numeroLote,
                            stockAnterior,
                            stockAnterior
                        );
                    }


                    stockResultante =
                        stockAnterior
                        +
                        cantidad;
                }
                else
                {
                    if (cantidad >
                        stockAnterior)
                    {
                        transaccion.Rollback();


                        return (
                            false,
                            $"Stock insuficiente. El lote tiene {stockAnterior} unidades disponibles.",
                            nombreProducto,
                            numeroLote,
                            stockAnterior,
                            stockAnterior
                        );
                    }


                    stockResultante =
                        stockAnterior
                        -
                        cantidad;
                }



                // =================================================
                // CREAR MOVIMIENTO
                // =================================================

                string consultaMovimiento = @"
                    INSERT INTO movimiento
                    (
                        fecha_hora,
                        tipo_movimiento,
                        observacion,
                        id_usuario
                    )
                    VALUES
                    (
                        NOW(),
                        @tipoMovimiento,
                        @observacion,
                        @idUsuario
                    )";


                long idMovimiento;


                using (
                    MySqlCommand comandoMovimiento =
                        new MySqlCommand(
                            consultaMovimiento,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoMovimiento
                        .Parameters
                        .AddWithValue(
                            "@tipoMovimiento",
                            tipo
                        );


                    comandoMovimiento
                        .Parameters
                        .AddWithValue(
                            "@observacion",
                            observacion
                        );


                    comandoMovimiento
                        .Parameters
                        .AddWithValue(
                            "@idUsuario",
                            idUsuario
                        );


                    await comandoMovimiento
                        .ExecuteNonQueryAsync();


                    idMovimiento =
                        comandoMovimiento
                            .LastInsertedId;
                }



                // =================================================
                // CREAR DETALLE DEL MOVIMIENTO
                // =================================================

                string consultaDetalle = @"
                    INSERT INTO detalle_movimiento
                    (
                        cantidad,
                        stock_anterior,
                        stock_resultante,
                        id_movimiento,
                        id_lote
                    )
                    VALUES
                    (
                        @cantidad,
                        @stockAnterior,
                        @stockResultante,
                        @idMovimiento,
                        @idLote
                    )";


                using (
                    MySqlCommand comandoDetalle =
                        new MySqlCommand(
                            consultaDetalle,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoDetalle
                        .Parameters
                        .AddWithValue(
                            "@cantidad",
                            cantidad
                        );


                    comandoDetalle
                        .Parameters
                        .AddWithValue(
                            "@stockAnterior",
                            stockAnterior
                        );


                    comandoDetalle
                        .Parameters
                        .AddWithValue(
                            "@stockResultante",
                            stockResultante
                        );


                    comandoDetalle
                        .Parameters
                        .AddWithValue(
                            "@idMovimiento",
                            idMovimiento
                        );


                    comandoDetalle
                        .Parameters
                        .AddWithValue(
                            "@idLote",
                            idLote
                        );


                    await comandoDetalle
                        .ExecuteNonQueryAsync();
                }



                // =================================================
                // ACTUALIZAR STOCK DEL LOTE
                // =================================================

                string consultaActualizarStock = @"
                    UPDATE lote

                    SET cantidad_actual = @stockResultante

                    WHERE id_lote = @idLote";


                using (
                    MySqlCommand comandoStock =
                        new MySqlCommand(
                            consultaActualizarStock,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoStock
                        .Parameters
                        .AddWithValue(
                            "@stockResultante",
                            stockResultante
                        );


                    comandoStock
                        .Parameters
                        .AddWithValue(
                            "@idLote",
                            idLote
                        );


                    int filasActualizadas =
                        await comandoStock
                            .ExecuteNonQueryAsync();


                    if (filasActualizadas != 1)
                    {
                        transaccion.Rollback();


                        return (
                            false,
                            "No se pudo actualizar el stock del lote.",
                            nombreProducto,
                            numeroLote,
                            stockAnterior,
                            stockAnterior
                        );
                    }
                }



                // =================================================
                // CONFIRMAR TRANSACCION
                // =================================================

                transaccion.Commit();


                return (
                    true,
                    $"{tipo} registrada correctamente. Stock del lote: {stockAnterior} → {stockResultante}.",
                    nombreProducto,
                    numeroLote,
                    stockAnterior,
                    stockResultante
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
                    "Ocurrió un error al registrar el movimiento.",
                    "",
                    "",
                    0,
                    0
                );
            }
        }



        // =========================================================
        // HISTORIAL DE MOVIMIENTOS DE MEDICAMENTOS CONTROLADOS
        // =========================================================

        public async Task<List<MovimientoControladoHistorial>>
            ListadoMovimientosControlados()
        {
            List<MovimientoControladoHistorial> movimientos =
                new List<MovimientoControladoHistorial>();


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();



            /*
                ESTA CONSULTA UNE:

                movimiento
                    ↓
                detalle_movimiento
                    ↓
                lote
                    ↓
                producto
                    ↓
                medicamento

                TAMBIEN BUSCA EL USUARIO
                QUE REALIZO EL MOVIMIENTO.
            */

            string consulta = @"
                SELECT
                    mov.id_movimiento,
                    mov.fecha_hora,
                    mov.tipo_movimiento,
                    mov.observacion,

                    dm.cantidad,
                    dm.stock_anterior,
                    dm.stock_resultante,

                    l.id_lote,
                    l.numero_lote,

                    p.id_producto,
                    p.nombre AS nombre_producto,

                    mov.id_usuario,

                    COALESCE(
                        NULLIF(
                            TRIM(
                                CONCAT(
                                    COALESCE(
                                        u.nombre,
                                        ''
                                    ),
                                    ' ',
                                    COALESCE(
                                        u.apellido,
                                        ''
                                    )
                                )
                            ),
                            ''
                        ),
                        CONCAT(
                            'Usuario #',
                            mov.id_usuario
                        )
                    ) AS usuario_responsable

                FROM movimiento mov

                INNER JOIN detalle_movimiento dm
                ON mov.id_movimiento =
                   dm.id_movimiento

                INNER JOIN lote l
                ON dm.id_lote =
                   l.id_lote

                INNER JOIN producto p
                ON l.id_producto =
                   p.id_producto

                INNER JOIN medicamento med
                ON p.id_producto =
                   med.id_producto

                LEFT JOIN usuario u
                ON mov.id_usuario =
                   u.id_usuario

                WHERE med.requiere_control = 1

                ORDER BY
                    mov.fecha_hora DESC,
                    mov.id_movimiento DESC";


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
                MovimientoControladoHistorial movimiento =
                    new MovimientoControladoHistorial()
                    {
                        id_movimiento =
                            lector.GetInt32(
                                "id_movimiento"
                            ),


                        fecha_hora =
                            lector.GetDateTime(
                                "fecha_hora"
                            ),


                        tipo_movimiento =
                            lector.IsDBNull(
                                lector.GetOrdinal(
                                    "tipo_movimiento"
                                )
                            )
                                ? ""
                                : lector.GetString(
                                    "tipo_movimiento"
                                ),


                        observacion =
                            lector.IsDBNull(
                                lector.GetOrdinal(
                                    "observacion"
                                )
                            )
                                ? ""
                                : lector.GetString(
                                    "observacion"
                                ),


                        cantidad =
                            lector.GetInt32(
                                "cantidad"
                            ),


                        stock_anterior =
                            lector.GetInt32(
                                "stock_anterior"
                            ),


                        stock_resultante =
                            lector.GetInt32(
                                "stock_resultante"
                            ),


                        id_lote =
                            lector.GetInt32(
                                "id_lote"
                            ),


                        numero_lote =
                            lector.IsDBNull(
                                lector.GetOrdinal(
                                    "numero_lote"
                                )
                            )
                                ? ""
                                : lector.GetString(
                                    "numero_lote"
                                ),


                        id_producto =
                            lector.GetInt32(
                                "id_producto"
                            ),


                        nombre_producto =
                            lector.IsDBNull(
                                lector.GetOrdinal(
                                    "nombre_producto"
                                )
                            )
                                ? ""
                                : lector.GetString(
                                    "nombre_producto"
                                ),


                        id_usuario =
                            lector.GetInt32(
                                "id_usuario"
                            ),


                        usuario_responsable =
                            lector.IsDBNull(
                                lector.GetOrdinal(
                                    "usuario_responsable"
                                )
                            )
                                ? "Usuario no disponible"
                                : lector.GetString(
                                    "usuario_responsable"
                                )
                    };


                movimientos.Add(
                    movimiento
                );
            }


            return movimientos;
        }
    }
}