using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class DosificacionRepository
    {
        private readonly string _conexion;


        public DosificacionRepository(
            IConfiguration configuration)
        {
            _conexion =
                configuration.GetConnectionString(
                    "ConexionMySQL"
                )!;
        }



        // =========================================================
        // LISTAR PROGRAMACIONES ACTIVAS
        // =========================================================

        public async Task<List<DosificacionProgramada>>
            ListarProgramaciones()
        {
            List<DosificacionProgramada> lista =
                new List<DosificacionProgramada>();


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta = @"
                SELECT
                    d.id_dosificacion,
                    d.codigo,
                    d.id_ubicacion,
                    u.nombre_sector AS nombre_clinica,
                    d.id_producto,
                    p.nombre AS nombre_producto,
                    p.unidad_medida,
                    p.stock_minimo,
                    m.concentracion,
                    m.forma_farmaceutica,
                    d.cantidad,
                    d.indicacion_base,
                    d.activo,
                    d.fecha_creacion,
                    d.id_usuario_creador,
                    CONCAT(
                        COALESCE(us.nombre, ''),
                        ' ',
                        COALESCE(us.apellido, '')
                    ) AS nombre_creador

                FROM dosificacion_programada d

                INNER JOIN producto p
                    ON p.id_producto = d.id_producto

                INNER JOIN medicamento m
                    ON m.id_producto = p.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = d.id_ubicacion

                INNER JOIN usuario us
                    ON us.id_usuario = d.id_usuario_creador

                WHERE d.activo = 1

                ORDER BY
                    p.nombre,
                    d.cantidad,
                    u.nombre_sector,
                    d.id_dosificacion";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            while (await lector.ReadAsync())
            {
                lista.Add(
                    CrearDesdeLector(
                        lector
                    )
                );
            }


            return lista;
        }



        // =========================================================
        // OBTENER PROGRAMACION POR ID
        // =========================================================

        public async Task<DosificacionProgramada?>
            ObtenerPorId(
                int idDosificacion)
        {
            if (idDosificacion <= 0)
            {
                return null;
            }


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta = @"
                SELECT
                    d.id_dosificacion,
                    d.codigo,
                    d.id_ubicacion,
                    u.nombre_sector AS nombre_clinica,
                    d.id_producto,
                    p.nombre AS nombre_producto,
                    p.unidad_medida,
                    p.stock_minimo,
                    m.concentracion,
                    m.forma_farmaceutica,
                    d.cantidad,
                    d.indicacion_base,
                    d.activo,
                    d.fecha_creacion,
                    d.id_usuario_creador,
                    CONCAT(
                        COALESCE(us.nombre, ''),
                        ' ',
                        COALESCE(us.apellido, '')
                    ) AS nombre_creador

                FROM dosificacion_programada d

                INNER JOIN producto p
                    ON p.id_producto = d.id_producto

                INNER JOIN medicamento m
                    ON m.id_producto = p.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = d.id_ubicacion

                INNER JOIN usuario us
                    ON us.id_usuario = d.id_usuario_creador

                WHERE d.id_dosificacion = @idDosificacion

                LIMIT 1";


            using MySqlCommand comando =
                new MySqlCommand(
                    consulta,
                    conexion
                );


            comando.Parameters.AddWithValue(
                "@idDosificacion",
                idDosificacion
            );


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            if (await lector.ReadAsync())
            {
                return CrearDesdeLector(
                    lector
                );
            }


            return null;
        }



        // =========================================================
        // OBTENER PROGRAMACION POR CODIGO
        // Acepta texto manual y lecturas de scanner Code 39.
        // =========================================================

        public async Task<DosificacionProgramada?>
            ObtenerPorCodigo(
                string codigo)
        {
            codigo =
                NormalizarCodigoDosificacion(
                    codigo
                );


            if (string.IsNullOrWhiteSpace(
                    codigo))
            {
                return null;
            }



            // =====================================================
            // LOS CODIGOS SARMedix TIENEN FORMATO DOS-000001.
            // COMO EL NUMERO ES EL MISMO ID DE LA PROGRAMACION,
            // PRIORIZAMOS LA BUSQUEDA POR ID.
            // ESTO EVITA PROBLEMAS CON PREFIJOS/SUFIJOS DEL SCANNER.
            // =====================================================

            if (
                codigo.StartsWith(
                    "DOS-",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string parteNumerica =
                    codigo.Substring(4);


                if (
                    int.TryParse(
                        parteNumerica,
                        out int idDosificacion
                    )
                    &&
                    idDosificacion > 0
                )
                {
                    var porId =
                        await ObtenerPorId(
                            idDosificacion
                        );


                    if (
                        porId != null
                        &&
                        porId.activo
                    )
                    {
                        return porId;
                    }
                }
            }



            // =====================================================
            // RESPALDO: BUSQUEDA DIRECTA POR CODIGO
            // =====================================================

            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            string consulta = @"
                SELECT
                    d.id_dosificacion,
                    d.codigo,
                    d.id_ubicacion,
                    u.nombre_sector AS nombre_clinica,
                    d.id_producto,
                    p.nombre AS nombre_producto,
                    p.unidad_medida,
                    p.stock_minimo,
                    m.concentracion,
                    m.forma_farmaceutica,
                    d.cantidad,
                    d.indicacion_base,
                    d.activo,
                    d.fecha_creacion,
                    d.id_usuario_creador,
                    CONCAT(
                        COALESCE(us.nombre, ''),
                        ' ',
                        COALESCE(us.apellido, '')
                    ) AS nombre_creador

                FROM dosificacion_programada d

                INNER JOIN producto p
                    ON p.id_producto = d.id_producto

                INNER JOIN medicamento m
                    ON m.id_producto = p.id_producto

                INNER JOIN ubicacion u
                    ON u.id_ubicacion = d.id_ubicacion

                INNER JOIN usuario us
                    ON us.id_usuario = d.id_usuario_creador

                WHERE d.codigo = @codigo
                  AND d.activo = 1

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


            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();


            if (await lector.ReadAsync())
            {
                return CrearDesdeLector(
                    lector
                );
            }


            return null;
        }



        // =========================================================
        // NORMALIZAR CODIGO LEIDO POR SCANNER
        // =========================================================

        private string NormalizarCodigoDosificacion(
            string codigo)
        {
            string limpio =
                (codigo ?? "")
                    .Trim()
                    .ToUpperInvariant();


            // Algunos scanners pueden enviar el identificador
            // AIM de Code 39, por ejemplo ]A0DOS-000001.
            if (
                limpio.StartsWith(
                    "]A",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                int posicionDos =
                    limpio.IndexOf(
                        "DOS",
                        StringComparison.OrdinalIgnoreCase
                    );


                if (posicionDos >= 0)
                {
                    limpio =
                        limpio.Substring(
                            posicionDos
                        );
                }
            }


            limpio =
                limpio
                    .Replace("*", "")
                    .Replace(" ", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Replace("–", "-")
                    .Replace("—", "-")
                    .Replace("−", "-");


            // Si llega DOS000001, lo convertimos a DOS-000001.
            if (
                limpio.StartsWith(
                    "DOS",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !limpio.StartsWith(
                    "DOS-",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string resto =
                    limpio.Substring(3)
                        .TrimStart('-');


                if (
                    !string.IsNullOrWhiteSpace(
                        resto
                    )
                    &&
                    resto.All(
                        char.IsDigit
                    )
                )
                {
                    limpio =
                        "DOS-"
                        +
                        resto;
                }
            }


            return limpio;
        }



        // =========================================================
        // CREAR O REUTILIZAR PROGRAMACION
        // =========================================================

        public async Task<ResultadoProgramacionDosificacion>
            CrearOObtenerProgramacion(
                int idUbicacion,
                int idProducto,
                int cantidad,
                int idUsuario)
        {
            ResultadoProgramacionDosificacion resultado =
                new ResultadoProgramacionDosificacion();


            if (idUbicacion <= 0)
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
                    "La cantidad por paquete debe ser mayor a 0.";

                return resultado;
            }


            if (idUsuario <= 0)
            {
                resultado.mensaje =
                    "No se pudo identificar al usuario.";

                return resultado;
            }


            using MySqlConnection conexion =
                new MySqlConnection(
                    _conexion
                );


            await conexion.OpenAsync();


            using MySqlTransaction transaccion =
                await conexion.BeginTransactionAsync();


            try
            {
                // =================================================
                // VALIDAR CLINICA
                // =================================================

                string nombreClinica = "";


                string consultaClinica = @"
                    SELECT
                        nombre_sector,
                        tipo_sector,
                        activo

                    FROM ubicacion

                    WHERE id_ubicacion = @idUbicacion

                    LIMIT 1";


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
                        idUbicacion
                    );


                    using MySqlDataReader lector =
                        await comandoClinica
                            .ExecuteReaderAsync();


                    if (!await lector.ReadAsync())
                    {
                        await transaccion.RollbackAsync();

                        resultado.mensaje =
                            "La Clínica seleccionada no existe.";

                        return resultado;
                    }


                    bool activo =
                        Convert.ToBoolean(
                            lector["activo"]
                        );


                    string tipo =
                        lector["tipo_sector"]
                            ?.ToString()
                        ?? "";


                    nombreClinica =
                        lector["nombre_sector"]
                            ?.ToString()
                        ?? "";


                    if (
                        !activo
                        ||
                        !tipo.Equals(
                            "Clinica",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        await transaccion.RollbackAsync();

                        resultado.mensaje =
                            "La ubicación seleccionada no corresponde a una Clínica activa.";

                        return resultado;
                    }
                }



                // =================================================
                // VALIDAR MEDICAMENTO Y STOCK UTIL
                // =================================================

                string nombreProducto = "";

                string unidadMedida =
                    "Unidades";

                string? concentracion =
                    null;

                string? formaFarmaceutica =
                    null;

                int stockMinimo =
                    0;

                bool requiereControl =
                    false;

                int stockDisponible =
                    0;


                string consultaProducto = @"
                    SELECT
                        p.nombre,
                        p.unidad_medida,
                        p.stock_minimo,
                        m.concentracion,
                        m.forma_farmaceutica,
                        m.requiere_control,

                        COALESCE(
                            SUM(
                                CASE
                                    WHEN
                                        l.id_ubicacion = @idUbicacion
                                        AND l.cantidad_actual > 0
                                        AND
                                        (
                                            l.fecha_vencimiento IS NULL
                                            OR DATE(l.fecha_vencimiento) >= CURDATE()
                                        )
                                    THEN l.cantidad_actual
                                    ELSE 0
                                END
                            ),
                            0
                        ) AS stock_disponible

                    FROM producto p

                    INNER JOIN medicamento m
                        ON m.id_producto = p.id_producto

                    LEFT JOIN lote l
                        ON l.id_producto = p.id_producto

                    WHERE p.id_producto = @idProducto

                    GROUP BY
                        p.id_producto,
                        p.nombre,
                        p.unidad_medida,
                        p.stock_minimo,
                        m.concentracion,
                        m.forma_farmaceutica,
                        m.requiere_control";


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


                    comandoProducto.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacion
                    );


                    using MySqlDataReader lector =
                        await comandoProducto
                            .ExecuteReaderAsync();


                    if (!await lector.ReadAsync())
                    {
                        await transaccion.RollbackAsync();

                        resultado.mensaje =
                            "El medicamento seleccionado no existe.";

                        return resultado;
                    }


                    nombreProducto =
                        lector["nombre"]
                            ?.ToString()
                        ?? "";


                    unidadMedida =
                        lector["unidad_medida"] == DBNull.Value
                            ? "Unidades"
                            : lector["unidad_medida"]
                                ?.ToString()
                                ?? "Unidades";


                    stockMinimo =
                        Convert.ToInt32(
                            lector["stock_minimo"]
                        );


                    concentracion =
                        lector["concentracion"] == DBNull.Value
                            ? null
                            : lector["concentracion"]
                                ?.ToString();


                    formaFarmaceutica =
                        lector["forma_farmaceutica"] == DBNull.Value
                            ? null
                            : lector["forma_farmaceutica"]
                                ?.ToString();


                    requiereControl =
                        Convert.ToBoolean(
                            lector["requiere_control"]
                        );


                    stockDisponible =
                        Convert.ToInt32(
                            lector["stock_disponible"]
                        );
                }


                if (requiereControl)
                {
                    await transaccion.RollbackAsync();

                    resultado.mensaje =
                        "Los medicamentos controlados deben usar su módulo restringido.";

                    return resultado;
                }


                if (stockDisponible <= 0)
                {
                    await transaccion.RollbackAsync();

                    resultado.mensaje =
                        $"No existe stock utilizable de {nombreProducto} en {nombreClinica}.";

                    return resultado;
                }



                // =================================================
                // REUTILIZAR SI YA EXISTE
                // =================================================

                int idExistente =
                    0;


                string consultaExistente = @"
                    SELECT id_dosificacion

                    FROM dosificacion_programada

                    WHERE id_ubicacion = @idUbicacion

                      AND id_producto = @idProducto

                      AND cantidad = @cantidad

                    LIMIT 1";


                using (
                    MySqlCommand comandoExistente =
                        new MySqlCommand(
                            consultaExistente,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoExistente.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacion
                    );


                    comandoExistente.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );


                    comandoExistente.Parameters.AddWithValue(
                        "@cantidad",
                        cantidad
                    );


                    object? valor =
                        await comandoExistente
                            .ExecuteScalarAsync();


                    if (
                        valor != null
                        &&
                        valor != DBNull.Value
                    )
                    {
                        idExistente =
                            Convert.ToInt32(
                                valor
                            );
                    }
                }


                if (idExistente > 0)
                {
                    string activar = @"
                        UPDATE dosificacion_programada

                        SET activo = 1

                        WHERE id_dosificacion = @idDosificacion";


                    using (
                        MySqlCommand comandoActivar =
                            new MySqlCommand(
                                activar,
                                conexion,
                                transaccion
                            )
                    )
                    {
                        comandoActivar.Parameters.AddWithValue(
                            "@idDosificacion",
                            idExistente
                        );


                        await comandoActivar
                            .ExecuteNonQueryAsync();
                    }


                    await transaccion.CommitAsync();


                    resultado.exito =
                        true;

                    resultado.reutilizada =
                        true;

                    resultado.dosificacion =
                        await ObtenerPorId(
                            idExistente
                        );

                    resultado.mensaje =
                        "La programación ya existía y será reutilizada.";

                    return resultado;
                }



                // =================================================
                // CREAR NUEVA PROGRAMACION
                // =================================================

                string indicacion =
                    ObtenerIndicacionBase(
                        formaFarmaceutica
                    );


                string codigoTemporal =
                    "TMP-"
                    +
                    Guid.NewGuid()
                        .ToString("N");


                string insertar = @"
                    INSERT INTO dosificacion_programada
                    (
                        codigo,
                        id_ubicacion,
                        id_producto,
                        cantidad,
                        indicacion_base,
                        activo,
                        fecha_creacion,
                        id_usuario_creador
                    )
                    VALUES
                    (
                        @codigo,
                        @idUbicacion,
                        @idProducto,
                        @cantidad,
                        @indicacion,
                        1,
                        NOW(),
                        @idUsuario
                    )";


                long idDosificacion;


                using (
                    MySqlCommand comandoInsertar =
                        new MySqlCommand(
                            insertar,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoInsertar.Parameters.AddWithValue(
                        "@codigo",
                        codigoTemporal
                    );


                    comandoInsertar.Parameters.AddWithValue(
                        "@idUbicacion",
                        idUbicacion
                    );


                    comandoInsertar.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );


                    comandoInsertar.Parameters.AddWithValue(
                        "@cantidad",
                        cantidad
                    );


                    comandoInsertar.Parameters.AddWithValue(
                        "@indicacion",
                        indicacion
                    );


                    comandoInsertar.Parameters.AddWithValue(
                        "@idUsuario",
                        idUsuario
                    );


                    await comandoInsertar
                        .ExecuteNonQueryAsync();


                    idDosificacion =
                        comandoInsertar
                            .LastInsertedId;
                }


                string codigoFinal =
                    $"DOS-{idDosificacion:000000}";


                string actualizarCodigo = @"
                    UPDATE dosificacion_programada

                    SET codigo = @codigo

                    WHERE id_dosificacion = @idDosificacion";


                using (
                    MySqlCommand comandoCodigo =
                        new MySqlCommand(
                            actualizarCodigo,
                            conexion,
                            transaccion
                        )
                )
                {
                    comandoCodigo.Parameters.AddWithValue(
                        "@codigo",
                        codigoFinal
                    );


                    comandoCodigo.Parameters.AddWithValue(
                        "@idDosificacion",
                        idDosificacion
                    );


                    await comandoCodigo
                        .ExecuteNonQueryAsync();
                }


                await transaccion.CommitAsync();


                resultado.exito =
                    true;

                resultado.reutilizada =
                    false;

                resultado.dosificacion =
                    await ObtenerPorId(
                        Convert.ToInt32(
                            idDosificacion
                        )
                    );

                resultado.mensaje =
                    "Programación creada correctamente.";

                return resultado;
            }
            catch
            {
                try
                {
                    await transaccion.RollbackAsync();
                }
                catch
                {
                }


                resultado.mensaje =
                    "Ocurrió un error al guardar la programación.";

                return resultado;
            }
        }



        // =========================================================
        // INDICACION BASE
        // =========================================================

        private string ObtenerIndicacionBase(
            string? formaFarmaceutica)
        {
            string forma =
                (formaFarmaceutica ?? "")
                    .Trim();


            if (
                forma.Equals(
                    "Cápsula",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return
                    "Tomar ____ cápsula(s) cada ____ horas por ____ días";
            }


            if (
                forma.Equals(
                    "Gotas",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return
                    "Administrar ____ gotas cada ____ horas por ____ días";
            }


            if (
                forma.Equals(
                    "Jarabe",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                forma.Equals(
                    "Solución oral",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return
                    "Administrar ____ mL cada ____ horas por ____ días";
            }


            return
                "Tomar ____ comprimido(s) cada ____ horas por ____ días";
        }



        // =========================================================
        // MAPEAR LECTOR
        // =========================================================

        private DosificacionProgramada CrearDesdeLector(
            MySqlDataReader lector)
        {
            return new DosificacionProgramada()
            {
                id_dosificacion =
                    Convert.ToInt32(
                        lector["id_dosificacion"]
                    ),

                codigo =
                    lector["codigo"]
                        ?.ToString()
                    ?? "",

                id_ubicacion =
                    Convert.ToInt32(
                        lector["id_ubicacion"]
                    ),

                nombre_clinica =
                    lector["nombre_clinica"]
                        ?.ToString()
                    ?? "",

                id_producto =
                    Convert.ToInt32(
                        lector["id_producto"]
                    ),

                nombre_producto =
                    lector["nombre_producto"]
                        ?.ToString()
                    ?? "",

                unidad_medida =
                    lector["unidad_medida"] == DBNull.Value
                        ? "Unidades"
                        : lector["unidad_medida"]
                            ?.ToString()
                            ?? "Unidades",

                stock_minimo =
                    Convert.ToInt32(
                        lector["stock_minimo"]
                    ),

                concentracion =
                    lector["concentracion"] == DBNull.Value
                        ? null
                        : lector["concentracion"]
                            ?.ToString(),

                forma_farmaceutica =
                    lector["forma_farmaceutica"] == DBNull.Value
                        ? null
                        : lector["forma_farmaceutica"]
                            ?.ToString(),

                cantidad =
                    Convert.ToInt32(
                        lector["cantidad"]
                    ),

                indicacion_base =
                    lector["indicacion_base"]
                        ?.ToString()
                    ?? "",

                activo =
                    Convert.ToBoolean(
                        lector["activo"]
                    ),

                fecha_creacion =
                    Convert.ToDateTime(
                        lector["fecha_creacion"]
                    ),

                id_usuario_creador =
                    Convert.ToInt32(
                        lector["id_usuario_creador"]
                    ),

                nombre_creador =
                    lector["nombre_creador"]
                        ?.ToString()
                        ?.Trim()
                    ?? ""
            };
        }
    }
}
