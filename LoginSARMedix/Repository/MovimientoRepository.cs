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
        // REGISTRAR ENTRADA NORMAL DE INVENTARIO
        // =========================================================

        public async Task<(
            bool exito,
            string mensaje,
            string nombreProducto,
            string numeroLote,
            int stockAnterior,
            int stockResultante)>
            RegistrarEntradaInventario(
                int idLote,
                int cantidad,
                string observacion,
                int idUsuario)
        {
            if (idLote <= 0)
            {
                return (
                    false,
                    "Debe seleccionar un lote válido.",
                    "",
                    "",
                    0,
                    0
                );
            }

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

            observacion =
                (observacion ?? "")
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    observacion))
            {
                observacion =
                    "Entrada de inventario";
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

                string nombreProducto = "";
                string numeroLote = "";

                bool loteEncontrado = false;

                string consultaLote = @"
                    SELECT
                        l.cantidad_actual,
                        l.numero_lote,
                        p.nombre

                    FROM lote l

                    INNER JOIN producto p
                    ON l.id_producto = p.id_producto

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
                        loteEncontrado = true;

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
                    }
                }

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

                if (
                    cantidad
                    >
                    int.MaxValue
                    -
                    stockAnterior
                )
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
                        'Entrada',
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

                transaccion.Commit();

                return (
                    true,
                    $"Entrada registrada correctamente. Stock del lote: {stockAnterior} → {stockResultante}.",
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
                    "Ocurrió un error al registrar la entrada de inventario.",
                    "",
                    "",
                    0,
                    0
                );
            }
        }




        // =========================================================
        // REGISTRAR TRASLADO ENTRE BODEGA Y CLINICA
        // =========================================================
        //
        // REGLAS:
        // - EL ORIGEN Y DESTINO DEBEN SER DISTINTOS.
        // - SOLO SE PERMITE BODEGA <-> CLINICA.
        // - BODEGA -> CLINICA NO ADMITE LOTES VENCIDOS.
        // - LOS MEDICAMENTOS CONTROLADOS NO PASAN POR ESTE FLUJO.
        // - TODO SE REALIZA EN UNA SOLA TRANSACCION.
        // =========================================================

        public async Task<ResultadoTrasladoInventario>
            RegistrarTrasladoInventario(
                int idLoteOrigen,
                int idUbicacionDestino,
                int cantidad,
                int idUsuario)
        {
            ResultadoTrasladoInventario resultado =
                new ResultadoTrasladoInventario();


            if (idLoteOrigen <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar un lote de origen.";

                return resultado;
            }


            if (idUbicacionDestino <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar un sector de destino.";

                return resultado;
            }


            if (cantidad <= 0)
            {
                resultado.mensaje =
                    "La cantidad a trasladar debe ser mayor a 0.";

                return resultado;
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
                int idProducto = 0;
                int idUbicacionOrigen = 0;

                int stockOrigenAnterior = 0;
                int stockDestinoAnterior = 0;

                string nombreProducto = "";
                string numeroLote = "";

                string nombreOrigen = "";
                string nombreDestino = "";

                string tipoOrigen = "";
                string tipoDestino = "";

                string unidadMedida = "unidades";

                bool origenActivo = false;
                bool destinoActivo = false;

                bool requiereControl = false;

                DateTime? fechaVencimientoOrigen =
                    null;



                // =================================================
                // OBTENER Y BLOQUEAR LOTE DE ORIGEN
                // =================================================

                string consultaOrigen = @"
                    SELECT
                        l.id_producto,
                        l.id_ubicacion,
                        l.numero_lote,
                        l.fecha_vencimiento,
                        l.cantidad_actual,

                        p.nombre,
                        p.unidad_medida,

                        u.nombre_sector,
                        u.tipo_sector,
                        u.activo,

                        COALESCE(
                            m.requiere_control,
                            0
                        ) AS requiere_control

                    FROM lote l

                    INNER JOIN producto p
                    ON l.id_producto = p.id_producto

                    INNER JOIN ubicacion u
                    ON l.id_ubicacion = u.id_ubicacion

                    LEFT JOIN medicamento m
                    ON p.id_producto = m.id_producto

                    WHERE l.id_lote = @idLoteOrigen

                    FOR UPDATE";


                using (
                    MySqlCommand comandoOrigen =
                        new MySqlCommand(
                            consultaOrigen,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoOrigen.Parameters.AddWithValue(
                        "@idLoteOrigen",
                        idLoteOrigen
                    );


                    using MySqlDataReader lector =
                        await comandoOrigen
                            .ExecuteReaderAsync();


                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se encontró el lote de origen.";

                        return resultado;
                    }


                    idProducto =
                        Convert.ToInt32(
                            lector["id_producto"]
                        );


                    idUbicacionOrigen =
                        Convert.ToInt32(
                            lector["id_ubicacion"]
                        );


                    numeroLote =
                        lector["numero_lote"]
                            .ToString()
                        ?? "";


                    if (
                        lector["fecha_vencimiento"]
                        !=
                        DBNull.Value
                    )
                    {
                        fechaVencimientoOrigen =
                            Convert.ToDateTime(
                                lector[
                                    "fecha_vencimiento"
                                ]
                            );
                    }


                    stockOrigenAnterior =
                        Convert.ToInt32(
                            lector["cantidad_actual"]
                        );


                    nombreProducto =
                        lector["nombre"]
                            .ToString()
                        ?? "";


                    if (
                        lector["unidad_medida"]
                        !=
                        DBNull.Value
                    )
                    {
                        unidadMedida =
                            lector["unidad_medida"]
                                .ToString()
                            ?? "unidades";
                    }


                    nombreOrigen =
                        lector["nombre_sector"]
                            .ToString()
                        ?? "";


                    tipoOrigen =
                        lector["tipo_sector"]
                            .ToString()
                        ?? "";


                    origenActivo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );


                    requiereControl =
                        Convert.ToBoolean(
                            lector["requiere_control"]
                        );
                }



                // =================================================
                // VALIDACIONES DEL ORIGEN
                // =================================================

                if (!origenActivo)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "El sector de origen está inactivo.";

                    return resultado;
                }


                if (
                    idUbicacionOrigen
                    ==
                    idUbicacionDestino
                )
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "El sector de origen y destino no pueden ser el mismo.";

                    return resultado;
                }


                if (
                    stockOrigenAnterior
                    <
                    cantidad
                )
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        $"Stock insuficiente. El lote tiene {stockOrigenAnterior} {unidadMedida} disponibles.";

                    return resultado;
                }


                if (requiereControl)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Los medicamentos controlados deben gestionarse desde su módulo restringido.";

                    return resultado;
                }



                // =================================================
                // OBTENER Y BLOQUEAR DESTINO
                // =================================================

                string consultaDestino = @"
                    SELECT
                        nombre_sector,
                        tipo_sector,
                        activo

                    FROM ubicacion

                    WHERE id_ubicacion = @idUbicacionDestino

                    FOR UPDATE";


                using (
                    MySqlCommand comandoDestino =
                        new MySqlCommand(
                            consultaDestino,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoDestino.Parameters.AddWithValue(
                        "@idUbicacionDestino",
                        idUbicacionDestino
                    );


                    using MySqlDataReader lector =
                        await comandoDestino
                            .ExecuteReaderAsync();


                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se encontró el sector de destino.";

                        return resultado;
                    }


                    nombreDestino =
                        lector["nombre_sector"]
                            .ToString()
                        ?? "";


                    tipoDestino =
                        lector["tipo_sector"]
                            .ToString()
                        ?? "";


                    destinoActivo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );
                }



                // =================================================
                // VALIDACIONES DE TIPO DE SECTOR
                // =================================================

                if (!destinoActivo)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "El sector de destino está inactivo.";

                    return resultado;
                }


                bool origenEsBodega =
                    tipoOrigen.Equals(
                        "Bodega",
                        StringComparison.OrdinalIgnoreCase
                    );


                bool origenEsClinica =
                    tipoOrigen.Equals(
                        "Clinica",
                        StringComparison.OrdinalIgnoreCase
                    );


                bool destinoEsBodega =
                    tipoDestino.Equals(
                        "Bodega",
                        StringComparison.OrdinalIgnoreCase
                    );


                bool destinoEsClinica =
                    tipoDestino.Equals(
                        "Clinica",
                        StringComparison.OrdinalIgnoreCase
                    );


                bool combinacionValida =
                    (
                        origenEsBodega
                        &&
                        destinoEsClinica
                    )
                    ||
                    (
                        origenEsClinica
                        &&
                        destinoEsBodega
                    );


                if (!combinacionValida)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Los traslados permitidos son entre Bodega y Clínica.";

                    return resultado;
                }


                // Un lote vencido puede volver desde Clínica a Bodega
                // para su posterior baja, pero nunca ir desde Bodega
                // hacia una Clínica para uso asistencial.

                if (
                    origenEsBodega
                    &&
                    destinoEsClinica
                    &&
                    fechaVencimientoOrigen.HasValue
                    &&
                    fechaVencimientoOrigen.Value.Date
                    <
                    DateTime.Today
                )
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "No se puede trasladar un lote vencido desde Bodega hacia una Clínica.";

                    return resultado;
                }



                // =================================================
                // BUSCAR MISMO LOTE EN EL DESTINO
                // =================================================

                int idLoteDestino = 0;

                DateTime? fechaVencimientoDestino =
                    null;


                string consultaLoteDestino = @"
                    SELECT
                        id_lote,
                        fecha_vencimiento,
                        cantidad_actual

                    FROM lote

                    WHERE id_producto = @idProducto
                      AND numero_lote = @numeroLote
                      AND id_ubicacion = @idUbicacionDestino

                    LIMIT 1

                    FOR UPDATE";


                using (
                    MySqlCommand comandoLoteDestino =
                        new MySqlCommand(
                            consultaLoteDestino,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoLoteDestino.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );


                    comandoLoteDestino.Parameters.AddWithValue(
                        "@numeroLote",
                        numeroLote
                    );


                    comandoLoteDestino.Parameters.AddWithValue(
                        "@idUbicacionDestino",
                        idUbicacionDestino
                    );


                    using MySqlDataReader lector =
                        await comandoLoteDestino
                            .ExecuteReaderAsync();


                    if (await lector.ReadAsync())
                    {
                        idLoteDestino =
                            Convert.ToInt32(
                                lector["id_lote"]
                            );


                        stockDestinoAnterior =
                            Convert.ToInt32(
                                lector["cantidad_actual"]
                            );


                        if (
                            lector["fecha_vencimiento"]
                            !=
                            DBNull.Value
                        )
                        {
                            fechaVencimientoDestino =
                                Convert.ToDateTime(
                                    lector[
                                        "fecha_vencimiento"
                                    ]
                                );
                        }
                    }
                }



                // =================================================
                // VALIDAR FECHA DEL LOTE SI YA EXISTE EN DESTINO
                // =================================================

                if (idLoteDestino > 0)
                {
                    bool fechasCoinciden =
                        (
                            !fechaVencimientoOrigen.HasValue
                            &&
                            !fechaVencimientoDestino.HasValue
                        )
                        ||
                        (
                            fechaVencimientoOrigen.HasValue
                            &&
                            fechaVencimientoDestino.HasValue
                            &&
                            fechaVencimientoOrigen.Value.Date
                            ==
                            fechaVencimientoDestino.Value.Date
                        );


                    if (!fechasCoinciden)
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "El mismo número de lote ya existe en el destino, pero su fecha de vencimiento no coincide.";

                        return resultado;
                    }
                }



                // =================================================
                // SI NO EXISTE EN DESTINO, CREARLO CON STOCK 0
                // =================================================

                if (idLoteDestino <= 0)
                {
                    string consultaCrearLote = @"
                        INSERT INTO lote
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
                            @idUbicacionDestino
                        )";


                    using MySqlCommand comandoCrearLote =
                        new MySqlCommand(
                            consultaCrearLote,
                            conexion,
                            transaccion
                        );


                    comandoCrearLote.Parameters.AddWithValue(
                        "@numeroLote",
                        numeroLote
                    );


                    comandoCrearLote.Parameters.AddWithValue(
                        "@fechaVencimiento",
                        fechaVencimientoOrigen.HasValue
                            ? fechaVencimientoOrigen.Value
                            : DBNull.Value
                    );


                    comandoCrearLote.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );


                    comandoCrearLote.Parameters.AddWithValue(
                        "@idUbicacionDestino",
                        idUbicacionDestino
                    );


                    await comandoCrearLote
                        .ExecuteNonQueryAsync();


                    idLoteDestino =
                        Convert.ToInt32(
                            comandoCrearLote.LastInsertedId
                        );


                    stockDestinoAnterior =
                        0;
                }



                int stockOrigenResultante =
                    stockOrigenAnterior
                    -
                    cantidad;


                int stockDestinoResultante =
                    stockDestinoAnterior
                    +
                    cantidad;



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
                        'Traslado',
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
                    comandoMovimiento.Parameters.AddWithValue(
                        "@observacion",
                        $"Traslado de {nombreOrigen} a {nombreDestino}"
                    );


                    comandoMovimiento.Parameters.AddWithValue(
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
                // DETALLE DEL LOTE DE ORIGEN
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
                    MySqlCommand comandoDetalleOrigen =
                        new MySqlCommand(
                            consultaDetalle,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoDetalleOrigen.Parameters.AddWithValue(
                        "@cantidad",
                        cantidad
                    );


                    comandoDetalleOrigen.Parameters.AddWithValue(
                        "@stockAnterior",
                        stockOrigenAnterior
                    );


                    comandoDetalleOrigen.Parameters.AddWithValue(
                        "@stockResultante",
                        stockOrigenResultante
                    );


                    comandoDetalleOrigen.Parameters.AddWithValue(
                        "@idMovimiento",
                        idMovimiento
                    );


                    comandoDetalleOrigen.Parameters.AddWithValue(
                        "@idLote",
                        idLoteOrigen
                    );


                    await comandoDetalleOrigen
                        .ExecuteNonQueryAsync();
                }



                // =================================================
                // DETALLE DEL LOTE DE DESTINO
                // =================================================

                using (
                    MySqlCommand comandoDetalleDestino =
                        new MySqlCommand(
                            consultaDetalle,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoDetalleDestino.Parameters.AddWithValue(
                        "@cantidad",
                        cantidad
                    );


                    comandoDetalleDestino.Parameters.AddWithValue(
                        "@stockAnterior",
                        stockDestinoAnterior
                    );


                    comandoDetalleDestino.Parameters.AddWithValue(
                        "@stockResultante",
                        stockDestinoResultante
                    );


                    comandoDetalleDestino.Parameters.AddWithValue(
                        "@idMovimiento",
                        idMovimiento
                    );


                    comandoDetalleDestino.Parameters.AddWithValue(
                        "@idLote",
                        idLoteDestino
                    );


                    await comandoDetalleDestino
                        .ExecuteNonQueryAsync();
                }



                // =================================================
                // ACTUALIZAR STOCK DE ORIGEN
                // =================================================

                string consultaActualizarStock = @"
                    UPDATE lote
                    SET cantidad_actual = @stockResultante
                    WHERE id_lote = @idLote";


                using (
                    MySqlCommand comandoStockOrigen =
                        new MySqlCommand(
                            consultaActualizarStock,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoStockOrigen.Parameters.AddWithValue(
                        "@stockResultante",
                        stockOrigenResultante
                    );


                    comandoStockOrigen.Parameters.AddWithValue(
                        "@idLote",
                        idLoteOrigen
                    );


                    int filas =
                        await comandoStockOrigen
                            .ExecuteNonQueryAsync();


                    if (filas != 1)
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se pudo actualizar el stock del sector de origen.";

                        return resultado;
                    }
                }



                // =================================================
                // ACTUALIZAR STOCK DE DESTINO
                // =================================================

                using (
                    MySqlCommand comandoStockDestino =
                        new MySqlCommand(
                            consultaActualizarStock,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoStockDestino.Parameters.AddWithValue(
                        "@stockResultante",
                        stockDestinoResultante
                    );


                    comandoStockDestino.Parameters.AddWithValue(
                        "@idLote",
                        idLoteDestino
                    );


                    int filas =
                        await comandoStockDestino
                            .ExecuteNonQueryAsync();


                    if (filas != 1)
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se pudo actualizar el stock del sector de destino.";

                        return resultado;
                    }
                }



                // =================================================
                // CONFIRMAR TODO EL TRASLADO
                // =================================================

                transaccion.Commit();


                resultado.exito =
                    true;


                resultado.mensaje =
                    $"Traslado registrado correctamente. {nombreOrigen}: {stockOrigenAnterior} → {stockOrigenResultante} | {nombreDestino}: {stockDestinoAnterior} → {stockDestinoResultante}.";


                resultado.nombreProducto =
                    nombreProducto;


                resultado.numeroLote =
                    numeroLote;


                resultado.origen =
                    nombreOrigen;


                resultado.destino =
                    nombreDestino;


                resultado.unidadMedida =
                    unidadMedida;


                resultado.stockOrigenAnterior =
                    stockOrigenAnterior;


                resultado.stockOrigenResultante =
                    stockOrigenResultante;


                resultado.stockDestinoAnterior =
                    stockDestinoAnterior;


                resultado.stockDestinoResultante =
                    stockDestinoResultante;


                return resultado;
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


                resultado.mensaje =
                    "Ocurrió un error al registrar el traslado.";

                return resultado;
            }
        }




        // =========================================================
        // ENTRADA COMPLETA Y TRANSACCIONAL
        // =========================================================
        //
        // BUSCA O CREA EL LOTE, REGISTRA MOVIMIENTO, DETALLE Y
        // ACTUALIZA STOCK DENTRO DE UNA SOLA TRANSACCION.
        // TAMBIEN BLOQUEA FECHAS DE VENCIMIENTO YA PASADAS.
        // =========================================================

        public async Task<(
            bool exito,
            string mensaje,
            string nombreProducto,
            string numeroLote,
            int stockAnterior,
            int stockResultante)>
            RegistrarEntradaInventarioCompleta(
                Lote reg,
                int idUsuario)
        {
            if (reg == null)
            {
                return (
                    false,
                    "No se recibieron datos de la entrada.",
                    "",
                    "",
                    0,
                    0
                );
            }

            if (reg.id_producto <= 0)
            {
                return (
                    false,
                    "Debe seleccionar un producto.",
                    "",
                    "",
                    0,
                    0
                );
            }

            if (reg.id_ubicacion <= 0)
            {
                return (
                    false,
                    "Debe seleccionar un sector.",
                    "",
                    "",
                    0,
                    0
                );
            }

            string numeroLote =
                (reg.numero_lote ?? "")
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    numeroLote))
            {
                return (
                    false,
                    "Debe ingresar el número de lote.",
                    "",
                    "",
                    0,
                    0
                );
            }

            if (!reg.fecha_vencimiento.HasValue)
            {
                return (
                    false,
                    "Debe ingresar la fecha de vencimiento.",
                    "",
                    numeroLote,
                    0,
                    0
                );
            }

            DateTime fechaVencimiento =
                reg.fecha_vencimiento.Value.Date;

            if (fechaVencimiento < DateTime.Today)
            {
                return (
                    false,
                    "No se puede registrar como entrada utilizable un lote que ya está vencido.",
                    "",
                    numeroLote,
                    0,
                    0
                );
            }

            if (reg.cantidad_actual <= 0)
            {
                return (
                    false,
                    "La cantidad de entrada debe ser mayor a 0.",
                    "",
                    numeroLote,
                    0,
                    0
                );
            }

            if (idUsuario <= 0)
            {
                return (
                    false,
                    "No se pudo identificar al usuario responsable.",
                    "",
                    numeroLote,
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
                string nombreProducto = "";
                string nombreSector = "";
                bool sectorActivo = false;

                string consultaDatos = @"
                    SELECT
                        p.nombre AS nombre_producto,
                        u.nombre_sector,
                        u.activo
                    FROM producto p
                    INNER JOIN ubicacion u
                        ON u.id_ubicacion = @idUbicacion
                    WHERE p.id_producto = @idProducto
                    LIMIT 1";

                using (
                    MySqlCommand comandoDatos =
                        new MySqlCommand(
                            consultaDatos,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoDatos.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );

                    comandoDatos.Parameters.AddWithValue(
                        "@idUbicacion",
                        reg.id_ubicacion
                    );

                    using MySqlDataReader lector =
                        await comandoDatos
                            .ExecuteReaderAsync();

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        return (
                            false,
                            "El producto o sector seleccionado no existe.",
                            "",
                            numeroLote,
                            0,
                            0
                        );
                    }

                    nombreProducto =
                        lector["nombre_producto"]?.ToString()
                        ?? "";

                    nombreSector =
                        lector["nombre_sector"]?.ToString()
                        ?? "";

                    sectorActivo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );
                }

                if (!sectorActivo)
                {
                    transaccion.Rollback();

                    return (
                        false,
                        "No se pueden registrar entradas en un sector inactivo.",
                        nombreProducto,
                        numeroLote,
                        0,
                        0
                    );
                }

                int idLote = 0;
                int stockAnterior = 0;
                DateTime? vencimientoExistente = null;

                string consultaLote = @"
                    SELECT
                        id_lote,
                        fecha_vencimiento,
                        cantidad_actual
                    FROM lote
                    WHERE id_producto = @idProducto
                      AND id_ubicacion = @idUbicacion
                      AND LOWER(TRIM(numero_lote))
                          =
                          LOWER(TRIM(@numeroLote))
                    LIMIT 1
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
                        "@idProducto",
                        reg.id_producto
                    );

                    comandoLote.Parameters.AddWithValue(
                        "@idUbicacion",
                        reg.id_ubicacion
                    );

                    comandoLote.Parameters.AddWithValue(
                        "@numeroLote",
                        numeroLote
                    );

                    using MySqlDataReader lector =
                        await comandoLote
                            .ExecuteReaderAsync();

                    if (await lector.ReadAsync())
                    {
                        idLote =
                            Convert.ToInt32(
                                lector["id_lote"]
                            );

                        stockAnterior =
                            Convert.ToInt32(
                                lector["cantidad_actual"]
                            );

                        if (
                            lector["fecha_vencimiento"]
                            != DBNull.Value
                        )
                        {
                            vencimientoExistente =
                                Convert.ToDateTime(
                                    lector["fecha_vencimiento"]
                                )
                                .Date;
                        }
                    }
                }

                if (
                    idLote > 0
                    &&
                    vencimientoExistente.HasValue
                    &&
                    vencimientoExistente.Value
                        !=
                        fechaVencimiento
                )
                {
                    transaccion.Rollback();

                    return (
                        false,
                        $"El lote {numeroLote} ya existe en {nombreSector} con otra fecha de vencimiento.",
                        nombreProducto,
                        numeroLote,
                        stockAnterior,
                        stockAnterior
                    );
                }

                if (idLote <= 0)
                {
                    string consultaCrearLote = @"
                        INSERT INTO lote
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

                    using MySqlCommand comandoCrearLote =
                        new MySqlCommand(
                            consultaCrearLote,
                            conexion,
                            transaccion
                        );

                    comandoCrearLote.Parameters.AddWithValue(
                        "@numeroLote",
                        numeroLote
                    );

                    comandoCrearLote.Parameters.AddWithValue(
                        "@fechaVencimiento",
                        fechaVencimiento
                    );

                    comandoCrearLote.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );

                    comandoCrearLote.Parameters.AddWithValue(
                        "@idUbicacion",
                        reg.id_ubicacion
                    );

                    await comandoCrearLote
                        .ExecuteNonQueryAsync();

                    idLote =
                        Convert.ToInt32(
                            comandoCrearLote.LastInsertedId
                        );

                    stockAnterior = 0;
                }

                if (
                    reg.cantidad_actual
                    >
                    int.MaxValue
                    -
                    stockAnterior
                )
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

                int stockResultante =
                    stockAnterior
                    +
                    reg.cantidad_actual;

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
                        'Entrada',
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
                    comandoMovimiento.Parameters.AddWithValue(
                        "@observacion",
                        $"Entrada de inventario en {nombreSector}"
                    );

                    comandoMovimiento.Parameters.AddWithValue(
                        "@idUsuario",
                        idUsuario
                    );

                    await comandoMovimiento
                        .ExecuteNonQueryAsync();

                    idMovimiento =
                        comandoMovimiento.LastInsertedId;
                }

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
                    comandoDetalle.Parameters.AddWithValue(
                        "@cantidad",
                        reg.cantidad_actual
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@stockAnterior",
                        stockAnterior
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@stockResultante",
                        stockResultante
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@idMovimiento",
                        idMovimiento
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@idLote",
                        idLote
                    );

                    await comandoDetalle
                        .ExecuteNonQueryAsync();
                }

                string consultaStock = @"
                    UPDATE lote
                    SET cantidad_actual = @stockResultante
                    WHERE id_lote = @idLote";

                using (
                    MySqlCommand comandoStock =
                        new MySqlCommand(
                            consultaStock,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoStock.Parameters.AddWithValue(
                        "@stockResultante",
                        stockResultante
                    );

                    comandoStock.Parameters.AddWithValue(
                        "@idLote",
                        idLote
                    );

                    int filas =
                        await comandoStock
                            .ExecuteNonQueryAsync();

                    if (filas != 1)
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

                transaccion.Commit();

                return (
                    true,
                    $"Entrada registrada correctamente. Stock del lote: {stockAnterior} → {stockResultante}.",
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
                    "Ocurrió un error al registrar la entrada de inventario.",
                    "",
                    numeroLote,
                    0,
                    0
                );
            }
        }




        // =========================================================
        // ENTREGA FEFO DESDE CLINICA
        // MOTOR REUTILIZADO POR LA ENTREGA POR ESCANER
        // =========================================================

        public async Task<ResultadoEntregaPaciente>
            RegistrarEntregaPaciente(
                int idUbicacionClinica,
                int idProducto,
                int cantidad,
                int idUsuario)
        {
            ResultadoEntregaPaciente resultado =
                new ResultadoEntregaPaciente();

            if (idUbicacionClinica <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar una Clínica.";

                return resultado;
            }

            if (idProducto <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar un producto.";

                return resultado;
            }

            if (cantidad <= 0)
            {
                resultado.mensaje =
                    "La cantidad a entregar debe ser mayor a 0.";

                return resultado;
            }

            if (idUsuario <= 0)
            {
                resultado.mensaje =
                    "No se pudo identificar al usuario responsable.";

                return resultado;
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
                string nombreClinica = "";
                string nombreProducto = "";
                string unidadMedida =
                    "unidades";
                bool requiereControl =
                    false;

                string consultaClinica = @"
                    SELECT
                        nombre_sector,
                        tipo_sector,
                        activo
                    FROM ubicacion
                    WHERE id_ubicacion = @idUbicacion
                    FOR UPDATE";

                using (
                    MySqlCommand comandoClinica =
                        new MySqlCommand(
                            consultaClinica,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoClinica.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacionClinica
                    );

                    using MySqlDataReader lector =
                        await comandoClinica
                            .ExecuteReaderAsync();

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "La Clínica seleccionada no existe.";

                        return resultado;
                    }

                    nombreClinica =
                        lector["nombre_sector"]?.ToString()
                        ?? "";

                    string tipoSector =
                        lector["tipo_sector"]?.ToString()
                        ?? "";

                    bool activo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );

                    if (!activo)
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "La Clínica seleccionada se encuentra inactiva.";

                        return resultado;
                    }

                    if (
                        !tipoSector.Equals(
                            "Clinica",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "Las entregas solo pueden realizarse desde una Clínica.";

                        return resultado;
                    }
                }

                string consultaProducto = @"
                    SELECT
                        p.nombre,
                        p.unidad_medida,
                        COALESCE(
                            m.requiere_control,
                            0
                        ) AS requiere_control
                    FROM producto p
                    LEFT JOIN medicamento m
                        ON p.id_producto = m.id_producto
                    WHERE p.id_producto = @idProducto";

                using (
                    MySqlCommand comandoProducto =
                        new MySqlCommand(
                            consultaProducto,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoProducto.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    using MySqlDataReader lector =
                        await comandoProducto
                            .ExecuteReaderAsync();

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "El producto seleccionado no existe.";

                        return resultado;
                    }

                    nombreProducto =
                        lector["nombre"]?.ToString()
                        ?? "";

                    if (
                        lector["unidad_medida"]
                        != DBNull.Value
                    )
                    {
                        unidadMedida =
                            lector["unidad_medida"]?.ToString()
                            ?? "unidades";
                    }

                    requiereControl =
                        Convert.ToBoolean(
                            lector["requiere_control"]
                        );
                }

                if (requiereControl)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Los medicamentos controlados deben salir desde su módulo restringido.";

                    return resultado;
                }

                List<(
                    int idLote,
                    string numeroLote,
                    int stockActual
                )> lotes =
                    new List<(
                        int,
                        string,
                        int
                    )>();

                string consultaLotes = @"
                    SELECT
                        id_lote,
                        numero_lote,
                        cantidad_actual
                    FROM lote
                    WHERE id_producto = @idProducto
                      AND id_ubicacion = @idUbicacion
                      AND cantidad_actual > 0
                      AND
                      (
                          fecha_vencimiento IS NULL
                          OR DATE(fecha_vencimiento) >= CURDATE()
                      )
                    ORDER BY
                        CASE
                            WHEN fecha_vencimiento IS NULL
                            THEN 1
                            ELSE 0
                        END,
                        fecha_vencimiento ASC,
                        id_lote ASC
                    FOR UPDATE";

                using (
                    MySqlCommand comandoLotes =
                        new MySqlCommand(
                            consultaLotes,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoLotes.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    comandoLotes.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacionClinica
                    );

                    using MySqlDataReader lector =
                        await comandoLotes
                            .ExecuteReaderAsync();

                    while (await lector.ReadAsync())
                    {
                        lotes.Add(
                            (
                                Convert.ToInt32(
                                    lector["id_lote"]
                                ),
                                lector["numero_lote"]?.ToString()
                                    ?? "",
                                Convert.ToInt32(
                                    lector["cantidad_actual"]
                                )
                            )
                        );
                    }
                }

                int stockDisponible =
                    lotes.Sum(
                        lote =>
                            lote.stockActual
                    );

                resultado.stockAnterior =
                    stockDisponible;

                resultado.nombreProducto =
                    nombreProducto;

                resultado.nombreClinica =
                    nombreClinica;

                resultado.unidadMedida =
                    unidadMedida;

                if (stockDisponible < cantidad)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        $"Stock insuficiente en {nombreClinica}. Disponible: {stockDisponible} {unidadMedida}.";

                    return resultado;
                }

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
                        'Salida paciente',
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
                    comandoMovimiento.Parameters.AddWithValue(
                        "@observacion",
                        $"Entrega dosificada desde {nombreClinica}"
                    );

                    comandoMovimiento.Parameters.AddWithValue(
                        "@idUsuario",
                        idUsuario
                    );

                    await comandoMovimiento
                        .ExecuteNonQueryAsync();

                    idMovimiento =
                        comandoMovimiento.LastInsertedId;
                }

                int cantidadPendiente =
                    cantidad;

                List<string> detalleLotes =
                    new List<string>();

                foreach (var lote in lotes)
                {
                    if (cantidadPendiente <= 0)
                    {
                        break;
                    }

                    int cantidadDesdeEsteLote =
                        Math.Min(
                            cantidadPendiente,
                            lote.stockActual
                        );

                    int stockResultanteLote =
                        lote.stockActual
                        -
                        cantidadDesdeEsteLote;

                    string consultaActualizarLote = @"
                        UPDATE lote
                        SET cantidad_actual = @stockResultante
                        WHERE id_lote = @idLote";

                    using (
                        MySqlCommand comandoActualizarLote =
                            new MySqlCommand(
                                consultaActualizarLote,
                                conexion,
                                transaccion
                            )
                    )
                    {
                        comandoActualizarLote.Parameters.AddWithValue(
                            "@stockResultante",
                            stockResultanteLote
                        );

                        comandoActualizarLote.Parameters.AddWithValue(
                            "@idLote",
                            lote.idLote
                        );

                        int filas =
                            await comandoActualizarLote
                                .ExecuteNonQueryAsync();

                        if (filas != 1)
                        {
                            transaccion.Rollback();

                            resultado.mensaje =
                                "No se pudo actualizar uno de los lotes utilizados.";

                            return resultado;
                        }
                    }

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
                        comandoDetalle.Parameters.AddWithValue(
                            "@cantidad",
                            cantidadDesdeEsteLote
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@stockAnterior",
                            lote.stockActual
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@stockResultante",
                            stockResultanteLote
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@idMovimiento",
                            idMovimiento
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@idLote",
                            lote.idLote
                        );

                        await comandoDetalle
                            .ExecuteNonQueryAsync();
                    }

                    detalleLotes.Add(
                        $"Lote {lote.numeroLote}: "
                        +
                        $"{cantidadDesdeEsteLote} {unidadMedida}"
                    );

                    cantidadPendiente -=
                        cantidadDesdeEsteLote;
                }

                if (cantidadPendiente != 0)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "No fue posible completar la entrega.";

                    return resultado;
                }

                int stockResultante =
                    stockDisponible
                    -
                    cantidad;

                transaccion.Commit();

                resultado.exito =
                    true;

                resultado.idMovimiento =
                    idMovimiento;

                resultado.stockResultante =
                    stockResultante;

                resultado.detalleLotes =
                    string.Join(
                        " | ",
                        detalleLotes
                    );

                resultado.mensaje =
                    $"Entrega registrada correctamente. Stock disponible en {nombreClinica}: {stockDisponible} → {stockResultante} {unidadMedida}.";

                return resultado;
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

                resultado.mensaje =
                    "Ocurrió un error al registrar la entrega.";

                return resultado;
            }
        }




        // =========================================================
        // BAJA / MERMA DESDE BODEGA
        // =========================================================

        public async Task<ResultadoSalidaEspecial>
            RegistrarBajaInventario(
                int idLote,
                int cantidad,
                string motivo,
                int idUsuario)
        {
            ResultadoSalidaEspecial resultado =
                new ResultadoSalidaEspecial();

            motivo =
                (motivo ?? "")
                    .Trim();

            if (idLote <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar un lote.";

                return resultado;
            }

            if (cantidad <= 0)
            {
                resultado.mensaje =
                    "La cantidad de baja debe ser mayor a 0.";

                return resultado;
            }

            if (string.IsNullOrWhiteSpace(
                    motivo))
            {
                resultado.mensaje =
                    "Debe indicar el motivo de la baja.";

                return resultado;
            }

            if (motivo.Length > 150)
            {
                resultado.mensaje =
                    "El motivo no puede superar 150 caracteres.";

                return resultado;
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
                int stockMinimo = 0;

                string nombreProducto = "";
                string numeroLote = "";
                string nombreSector = "";
                string tipoSector = "";
                string unidadMedida = "unidades";

                bool requiereControl = false;

                string consultaLote = @"
                    SELECT
                        l.numero_lote,
                        l.cantidad_actual,
                        p.nombre,
                        p.unidad_medida,
                        p.stock_minimo,
                        u.nombre_sector,
                        u.tipo_sector,
                        COALESCE(
                            m.requiere_control,
                            0
                        ) AS requiere_control
                    FROM lote l
                    INNER JOIN producto p
                        ON p.id_producto = l.id_producto
                    INNER JOIN ubicacion u
                        ON u.id_ubicacion = l.id_ubicacion
                    LEFT JOIN medicamento m
                        ON m.id_producto = p.id_producto
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

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se encontró el lote seleccionado.";

                        return resultado;
                    }

                    numeroLote =
                        lector["numero_lote"]?.ToString()
                        ?? "";

                    stockAnterior =
                        Convert.ToInt32(
                            lector["cantidad_actual"]
                        );

                    nombreProducto =
                        lector["nombre"]?.ToString()
                        ?? "";

                    unidadMedida =
                        lector["unidad_medida"] == DBNull.Value
                            ? "unidades"
                            : lector["unidad_medida"]?.ToString()
                                ?? "unidades";

                    stockMinimo =
                        Convert.ToInt32(
                            lector["stock_minimo"]
                        );

                    nombreSector =
                        lector["nombre_sector"]?.ToString()
                        ?? "";

                    tipoSector =
                        lector["tipo_sector"]?.ToString()
                        ?? "";

                    requiereControl =
                        Convert.ToBoolean(
                            lector["requiere_control"]
                        );
                }

                if (
                    !tipoSector.Equals(
                        "Bodega",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Las bajas de inventario solo pueden registrarse desde una Bodega.";

                    return resultado;
                }

                if (requiereControl)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Los medicamentos controlados deben manejarse desde su módulo restringido.";

                    return resultado;
                }

                if (cantidad > stockAnterior)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        $"Stock insuficiente. El lote tiene {stockAnterior} {unidadMedida}.";

                    return resultado;
                }

                int stockResultante =
                    stockAnterior
                    -
                    cantidad;

                string observacion =
                    $"Baja en {nombreSector}. Motivo: {motivo}";

                if (observacion.Length > 255)
                {
                    observacion =
                        observacion.Substring(
                            0,
                            255
                        );
                }

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
                        'Baja',
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
                    comandoMovimiento.Parameters.AddWithValue(
                        "@observacion",
                        observacion
                    );

                    comandoMovimiento.Parameters.AddWithValue(
                        "@idUsuario",
                        idUsuario
                    );

                    await comandoMovimiento
                        .ExecuteNonQueryAsync();

                    idMovimiento =
                        comandoMovimiento.LastInsertedId;
                }

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
                    comandoDetalle.Parameters.AddWithValue(
                        "@cantidad",
                        cantidad
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@stockAnterior",
                        stockAnterior
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@stockResultante",
                        stockResultante
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@idMovimiento",
                        idMovimiento
                    );

                    comandoDetalle.Parameters.AddWithValue(
                        "@idLote",
                        idLote
                    );

                    await comandoDetalle
                        .ExecuteNonQueryAsync();
                }

                string consultaActualizar = @"
                    UPDATE lote
                    SET cantidad_actual = @stockResultante
                    WHERE id_lote = @idLote";

                using (
                    MySqlCommand comandoActualizar =
                        new MySqlCommand(
                            consultaActualizar,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoActualizar.Parameters.AddWithValue(
                        "@stockResultante",
                        stockResultante
                    );

                    comandoActualizar.Parameters.AddWithValue(
                        "@idLote",
                        idLote
                    );

                    int filas =
                        await comandoActualizar
                            .ExecuteNonQueryAsync();

                    if (filas != 1)
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "No se pudo actualizar el stock del lote.";

                        return resultado;
                    }
                }

                transaccion.Commit();

                resultado.exito =
                    true;

                resultado.idMovimiento =
                    idMovimiento;

                resultado.nombreProducto =
                    nombreProducto;

                resultado.numeroLote =
                    numeroLote;

                resultado.nombreSector =
                    nombreSector;

                resultado.unidadMedida =
                    unidadMedida;

                resultado.stockAnterior =
                    stockAnterior;

                resultado.stockResultante =
                    stockResultante;

                resultado.stockMinimo =
                    stockMinimo;

                resultado.mensaje =
                    $"Baja registrada correctamente. Stock: {stockAnterior} → {stockResultante} {unidadMedida}.";

                return resultado;
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

                resultado.mensaje =
                    "Ocurrió un error al registrar la baja.";

                return resultado;
            }
        }




        // =========================================================
        // SALIDA EXTRAORDINARIA A OTRO SERVICIO
        // FEFO DESDE CLINICA
        // =========================================================

        public async Task<ResultadoSalidaEspecial>
            RegistrarSalidaExtraordinaria(
                int idUbicacionClinica,
                int idProducto,
                int cantidad,
                string servicioDestino,
                string motivo,
                int idUsuario)
        {
            ResultadoSalidaEspecial resultado =
                new ResultadoSalidaEspecial();

            servicioDestino =
                (servicioDestino ?? "")
                    .Trim();

            motivo =
                (motivo ?? "")
                    .Trim();

            if (idUbicacionClinica <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar una Clínica.";

                return resultado;
            }

            if (idProducto <= 0)
            {
                resultado.mensaje =
                    "Debe seleccionar un medicamento.";

                return resultado;
            }

            if (cantidad <= 0)
            {
                resultado.mensaje =
                    "La cantidad debe ser mayor a 0.";

                return resultado;
            }

            if (string.IsNullOrWhiteSpace(
                    servicioDestino))
            {
                resultado.mensaje =
                    "Debe indicar el servicio de destino.";

                return resultado;
            }

            if (servicioDestino.Length > 80)
            {
                resultado.mensaje =
                    "El servicio de destino no puede superar 80 caracteres.";

                return resultado;
            }

            if (string.IsNullOrWhiteSpace(
                    motivo))
            {
                resultado.mensaje =
                    "Debe indicar el motivo de la salida.";

                return resultado;
            }

            if (motivo.Length > 120)
            {
                resultado.mensaje =
                    "El motivo no puede superar 120 caracteres.";

                return resultado;
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
                string nombreClinica = "";
                string nombreProducto = "";
                string unidadMedida = "unidades";

                int stockMinimo = 0;

                bool requiereControl = false;

                string consultaClinica = @"
                    SELECT
                        nombre_sector,
                        tipo_sector,
                        activo
                    FROM ubicacion
                    WHERE id_ubicacion = @idUbicacion
                    FOR UPDATE";

                using (
                    MySqlCommand comandoClinica =
                        new MySqlCommand(
                            consultaClinica,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoClinica.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacionClinica
                    );

                    using MySqlDataReader lector =
                        await comandoClinica
                            .ExecuteReaderAsync();

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "La Clínica seleccionada no existe.";

                        return resultado;
                    }

                    nombreClinica =
                        lector["nombre_sector"]?.ToString()
                        ?? "";

                    string tipoSector =
                        lector["tipo_sector"]?.ToString()
                        ?? "";

                    bool activo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );

                    if (
                        !activo
                        ||
                        !tipoSector.Equals(
                            "Clinica",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "La salida extraordinaria debe realizarse desde una Clínica activa.";

                        return resultado;
                    }
                }

                string consultaProducto = @"
                    SELECT
                        p.nombre,
                        p.unidad_medida,
                        p.stock_minimo,
                        COALESCE(
                            m.requiere_control,
                            0
                        ) AS requiere_control
                    FROM producto p
                    INNER JOIN medicamento m
                        ON m.id_producto = p.id_producto
                    WHERE p.id_producto = @idProducto";

                using (
                    MySqlCommand comandoProducto =
                        new MySqlCommand(
                            consultaProducto,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoProducto.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    using MySqlDataReader lector =
                        await comandoProducto
                            .ExecuteReaderAsync();

                    if (!await lector.ReadAsync())
                    {
                        transaccion.Rollback();

                        resultado.mensaje =
                            "El medicamento seleccionado no existe.";

                        return resultado;
                    }

                    nombreProducto =
                        lector["nombre"]?.ToString()
                        ?? "";

                    unidadMedida =
                        lector["unidad_medida"] == DBNull.Value
                            ? "unidades"
                            : lector["unidad_medida"]?.ToString()
                                ?? "unidades";

                    stockMinimo =
                        Convert.ToInt32(
                            lector["stock_minimo"]
                        );

                    requiereControl =
                        Convert.ToBoolean(
                            lector["requiere_control"]
                        );
                }

                if (requiereControl)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "Los medicamentos controlados deben salir desde su módulo restringido.";

                    return resultado;
                }

                List<(
                    int idLote,
                    string numeroLote,
                    int stockActual
                )> lotes =
                    new List<(
                        int,
                        string,
                        int
                    )>();

                string consultaLotes = @"
                    SELECT
                        id_lote,
                        numero_lote,
                        cantidad_actual
                    FROM lote
                    WHERE id_producto = @idProducto
                      AND id_ubicacion = @idUbicacion
                      AND cantidad_actual > 0
                      AND
                      (
                          fecha_vencimiento IS NULL
                          OR DATE(fecha_vencimiento) >= CURDATE()
                      )
                    ORDER BY
                        CASE
                            WHEN fecha_vencimiento IS NULL
                            THEN 1
                            ELSE 0
                        END,
                        fecha_vencimiento ASC,
                        id_lote ASC
                    FOR UPDATE";

                using (
                    MySqlCommand comandoLotes =
                        new MySqlCommand(
                            consultaLotes,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoLotes.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    comandoLotes.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacionClinica
                    );

                    using MySqlDataReader lector =
                        await comandoLotes
                            .ExecuteReaderAsync();

                    while (await lector.ReadAsync())
                    {
                        lotes.Add(
                            (
                                Convert.ToInt32(
                                    lector["id_lote"]
                                ),
                                lector["numero_lote"]?.ToString()
                                    ?? "",
                                Convert.ToInt32(
                                    lector["cantidad_actual"]
                                )
                            )
                        );
                    }
                }

                int stockDisponible =
                    lotes.Sum(
                        lote =>
                            lote.stockActual
                    );

                if (stockDisponible < cantidad)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        $"Stock insuficiente en {nombreClinica}. Disponible: {stockDisponible} {unidadMedida}.";

                    return resultado;
                }

                string observacion =
                    $"Salida extraordinaria a {servicioDestino}. Motivo: {motivo}";

                if (observacion.Length > 255)
                {
                    observacion =
                        observacion.Substring(
                            0,
                            255
                        );
                }

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
                        'Salida extraordinaria',
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
                    comandoMovimiento.Parameters.AddWithValue(
                        "@observacion",
                        observacion
                    );

                    comandoMovimiento.Parameters.AddWithValue(
                        "@idUsuario",
                        idUsuario
                    );

                    await comandoMovimiento
                        .ExecuteNonQueryAsync();

                    idMovimiento =
                        comandoMovimiento.LastInsertedId;
                }

                int cantidadPendiente =
                    cantidad;

                List<string> detalleLotes =
                    new List<string>();

                foreach (var lote in lotes)
                {
                    if (cantidadPendiente <= 0)
                    {
                        break;
                    }

                    int cantidadDesdeEsteLote =
                        Math.Min(
                            cantidadPendiente,
                            lote.stockActual
                        );

                    int stockResultanteLote =
                        lote.stockActual
                        -
                        cantidadDesdeEsteLote;

                    string consultaActualizar = @"
                        UPDATE lote
                        SET cantidad_actual = @stockResultante
                        WHERE id_lote = @idLote";

                    using (
                        MySqlCommand comandoActualizar =
                            new MySqlCommand(
                                consultaActualizar,
                                conexion,
                                transaccion
                            )
                    )
                    {
                        comandoActualizar.Parameters.AddWithValue(
                            "@stockResultante",
                            stockResultanteLote
                        );

                        comandoActualizar.Parameters.AddWithValue(
                            "@idLote",
                            lote.idLote
                        );

                        int filas =
                            await comandoActualizar
                                .ExecuteNonQueryAsync();

                        if (filas != 1)
                        {
                            transaccion.Rollback();

                            resultado.mensaje =
                                "No se pudo actualizar uno de los lotes.";

                            return resultado;
                        }
                    }

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
                        comandoDetalle.Parameters.AddWithValue(
                            "@cantidad",
                            cantidadDesdeEsteLote
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@stockAnterior",
                            lote.stockActual
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@stockResultante",
                            stockResultanteLote
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@idMovimiento",
                            idMovimiento
                        );

                        comandoDetalle.Parameters.AddWithValue(
                            "@idLote",
                            lote.idLote
                        );

                        await comandoDetalle
                            .ExecuteNonQueryAsync();
                    }

                    detalleLotes.Add(
                        $"Lote {lote.numeroLote}: "
                        +
                        $"{cantidadDesdeEsteLote} {unidadMedida}"
                    );

                    cantidadPendiente -=
                        cantidadDesdeEsteLote;
                }

                if (cantidadPendiente != 0)
                {
                    transaccion.Rollback();

                    resultado.mensaje =
                        "No fue posible completar la salida extraordinaria.";

                    return resultado;
                }

                int stockResultante =
                    stockDisponible
                    -
                    cantidad;

                transaccion.Commit();

                resultado.exito =
                    true;

                resultado.idMovimiento =
                    idMovimiento;

                resultado.nombreProducto =
                    nombreProducto;

                resultado.nombreSector =
                    nombreClinica;

                resultado.unidadMedida =
                    unidadMedida;

                resultado.stockAnterior =
                    stockDisponible;

                resultado.stockResultante =
                    stockResultante;

                resultado.stockMinimo =
                    stockMinimo;

                resultado.detalleLotes =
                    string.Join(
                        " | ",
                        detalleLotes
                    );

                resultado.mensaje =
                    $"Salida extraordinaria registrada. Stock en {nombreClinica}: {stockDisponible} → {stockResultante} {unidadMedida}.";

                return resultado;
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

                resultado.mensaje =
                    "Ocurrió un error al registrar la salida extraordinaria.";

                return resultado;
            }
        }




        // =========================================================
        // HISTORIAL OPERACIONAL POR SECTOR
        // =========================================================

        public async Task<List<MovimientoSectorHistorial>>
            ListadoMovimientosSector(
                int idUbicacion)
        {
            List<MovimientoSectorHistorial> listado =
                new List<MovimientoSectorHistorial>();

            if (idUbicacion <= 0)
            {
                return listado;
            }

            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );

            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    mov.id_movimiento,
                    mov.fecha_hora,
                    mov.tipo_movimiento,
                    COALESCE(
                        mov.observacion,
                        ''
                    ) AS observacion,

                    dm.cantidad,
                    dm.stock_anterior,
                    dm.stock_resultante,

                    COALESCE(
                        CONCAT(
                            us.nombre,
                            ' ',
                            us.apellido
                        ),
                        us.nombre_usuario,
                        'Usuario'
                    ) AS responsable,

                    p.nombre AS producto,
                    COALESCE(
                        p.unidad_medida,
                        'unidades'
                    ) AS unidad_medida,

                    l.numero_lote,

                    u.nombre_sector

                FROM detalle_movimiento dm

                INNER JOIN movimiento mov
                    ON mov.id_movimiento = dm.id_movimiento

                INNER JOIN lote l
                    ON l.id_lote = dm.id_lote

                INNER JOIN producto p
                    ON p.id_producto = l.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = l.id_ubicacion

                LEFT JOIN usuario us
                    ON us.id_usuario = mov.id_usuario

                WHERE u.id_ubicacion = @idUbicacion

                ORDER BY
                    mov.fecha_hora DESC,
                    dm.id_detalle DESC";

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
                listado.Add(
                    new MovimientoSectorHistorial()
                    {
                        id_movimiento =
                            Convert.ToInt64(
                                lector["id_movimiento"]
                            ),

                        fecha_hora =
                            Convert.ToDateTime(
                                lector["fecha_hora"]
                            ),

                        tipo_movimiento =
                            lector["tipo_movimiento"]?.ToString()
                            ?? "",

                        observacion =
                            lector["observacion"]?.ToString()
                            ?? "",

                        responsable =
                            lector["responsable"]?.ToString()
                            ?? "",

                        producto =
                            lector["producto"]?.ToString()
                            ?? "",

                        lote =
                            lector["numero_lote"]?.ToString()
                            ?? "",

                        sector =
                            lector["nombre_sector"]?.ToString()
                            ?? "",

                        unidad_medida =
                            lector["unidad_medida"]?.ToString()
                            ?? "unidades",

                        cantidad =
                            Convert.ToInt32(
                                lector["cantidad"]
                            ),

                        stock_anterior =
                            Convert.ToInt32(
                                lector["stock_anterior"]
                            ),

                        stock_resultante =
                            Convert.ToInt32(
                                lector["stock_resultante"]
                            )
                    }
                );
            }

            return listado;
        }




        // =========================================================
        // SALIDAS DE UN TURNO / RANGO HORARIO
        // =========================================================
        //
        // INCLUYE:
        // - Entregas realizadas por codigo DOS (Salida paciente)
        // - Salidas extraordinarias
        // - Salidas de medicamentos controlados SOLO cuando el
        //   usuario tiene permiso para ver controlados.
        //
        // NO INCLUYE:
        // - Bajas / mermas
        // - Traslados
        // - Entradas
        // =========================================================

        public async Task<List<DetalleSalidaTurno>>
            ListadoSalidasTurno(
                DateTime inicio,
                DateTime fin,
                bool incluirControlados)
        {
            List<DetalleSalidaTurno> listado =
                new List<DetalleSalidaTurno>();


            if (fin <= inicio)
            {
                return listado;
            }


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta = @"
                SELECT
                    mov.id_movimiento,
                    mov.fecha_hora,
                    mov.tipo_movimiento,

                    p.id_producto,
                    p.nombre AS nombre_producto,

                    MAX(
                        COALESCE(
                            med.concentracion,
                            ''
                        )
                    ) AS concentracion,

                    MAX(
                        COALESCE(
                            p.unidad_medida,
                            'unidades'
                        )
                    ) AS unidad_medida,

                    SUM(dm.cantidad) AS cantidad,

                    MAX(
                        COALESCE(
                            med.requiere_control,
                            0
                        )
                    ) AS requiere_control,

                    MAX(
                        COALESCE(
                            NULLIF(
                                TRIM(
                                    CONCAT(
                                        COALESCE(us.nombre, ''),
                                        ' ',
                                        COALESCE(us.apellido, '')
                                    )
                                ),
                                ''
                            ),
                            us.nombre_usuario,
                            CONCAT(
                                'Usuario #',
                                mov.id_usuario
                            )
                        )
                    ) AS responsable,

                    GROUP_CONCAT(
                        DISTINCT u.nombre_sector
                        ORDER BY u.nombre_sector
                        SEPARATOR ', '
                    ) AS sector,

                    GROUP_CONCAT(
                        CONCAT(
                            COALESCE(l.numero_lote, '-'),
                            ': ',
                            dm.cantidad,
                            ' ',
                            COALESCE(
                                p.unidad_medida,
                                'unidades'
                            )
                        )
                        ORDER BY l.id_lote
                        SEPARATOR ' | '
                    ) AS lotes,

                    MAX(
                        COALESCE(
                            mov.observacion,
                            ''
                        )
                    ) AS observacion

                FROM movimiento mov

                INNER JOIN detalle_movimiento dm
                    ON dm.id_movimiento = mov.id_movimiento

                INNER JOIN lote l
                    ON l.id_lote = dm.id_lote

                INNER JOIN producto p
                    ON p.id_producto = l.id_producto

                LEFT JOIN medicamento med
                    ON med.id_producto = p.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = l.id_ubicacion

                LEFT JOIN usuario us
                    ON us.id_usuario = mov.id_usuario

                WHERE
                    mov.fecha_hora >= @inicio
                    AND mov.fecha_hora < @fin
                    AND
                    (
                        mov.tipo_movimiento = 'Salida paciente'
                        OR
                        mov.tipo_movimiento = 'Salida extraordinaria'
                        OR
                        (
                            @incluirControlados = 1
                            AND mov.tipo_movimiento = 'Salida'
                            AND COALESCE(
                                med.requiere_control,
                                0
                            ) = 1
                        )
                    )

                GROUP BY
                    mov.id_movimiento,
                    mov.fecha_hora,
                    mov.tipo_movimiento,
                    p.id_producto,
                    p.nombre

                ORDER BY
                    mov.fecha_hora DESC,
                    mov.id_movimiento DESC";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@inicio",
                inicio
            );


            comando.Parameters.AddWithValue(
                "@fin",
                fin
            );


            comando.Parameters.AddWithValue(
                "@incluirControlados",
                incluirControlados
                    ? 1
                    : 0
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                string tipoMovimiento =
                    lector["tipo_movimiento"]?.ToString()
                    ?? "";


                bool esControlado =
                    Convert.ToBoolean(
                        lector["requiere_control"]
                    );


                string tipoSalida =
                    tipoMovimiento.Equals(
                        "Salida paciente",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "Entrega por escáner"
                        : tipoMovimiento.Equals(
                            "Salida extraordinaria",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? "Salida extraordinaria"
                            : esControlado
                                ? "Medicamento controlado"
                                : "Salida";


                listado.Add(
                    new DetalleSalidaTurno()
                    {
                        id_movimiento =
                            Convert.ToInt64(
                                lector["id_movimiento"]
                            ),

                        fecha_hora =
                            Convert.ToDateTime(
                                lector["fecha_hora"]
                            ),

                        id_producto =
                            Convert.ToInt32(
                                lector["id_producto"]
                            ),

                        nombre_producto =
                            lector["nombre_producto"]?.ToString()
                            ?? "",

                        concentracion =
                            lector["concentracion"]?.ToString()
                            ?? "",

                        unidad_medida =
                            lector["unidad_medida"]?.ToString()
                            ?? "unidades",

                        cantidad =
                            Convert.ToInt32(
                                lector["cantidad"]
                            ),

                        tipo_salida =
                            tipoSalida,

                        es_controlado =
                            esControlado,

                        responsable =
                            lector["responsable"]?.ToString()
                            ?? "",

                        sector =
                            lector["sector"] == DBNull.Value
                                ? ""
                                : lector["sector"]?.ToString()
                                    ?? "",

                        lotes =
                            lector["lotes"] == DBNull.Value
                                ? ""
                                : lector["lotes"]?.ToString()
                                    ?? "",

                        observacion =
                            lector["observacion"]?.ToString()
                            ?? ""
                    }
                );
            }


            return listado;
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