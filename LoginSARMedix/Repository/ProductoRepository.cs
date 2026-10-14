using LoginSARMedix.Models;
using MySqlConnector;

namespace LoginSARMedix.Repository
{
    public class ProductoRepository
    {
        private readonly string _conexion;

        public ProductoRepository(IConfiguration configuration)
        {
            _conexion = configuration.GetConnectionString("ConexionMySQL")!;
        }


        // =========================================================
        // CREAR PRODUCTO
        // El código interno se genera automáticamente.
        // =========================================================
        public async Task<bool> CrearProducto(Producto reg)
        {
            if (reg == null)
            {
                return false;
            }

            if (reg.tipo != "Medicamento" && reg.tipo != "Insumo")
            {
                return false;
            }

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            using MySqlTransaction transaccion =
                await conexion.BeginTransactionAsync();

            try
            {
                string? codigoBarras =
                    string.IsNullOrWhiteSpace(reg.codigo_barras)
                        ? null
                        : reg.codigo_barras.Trim();

                string? unidadMedida =
                    string.IsNullOrWhiteSpace(reg.unidad_medida)
                        ? null
                        : reg.unidad_medida.Trim();


                // VALIDAR CODIGO DE BARRAS SI VIENE INFORMADO
                if (codigoBarras != null)
                {
                    string consultaCodigoBarras = @"
                        SELECT COUNT(*)
                        FROM producto
                        WHERE codigo_barras = @codigoBarras";

                    using MySqlCommand comandoCodigoBarras =
                        new MySqlCommand(
                            consultaCodigoBarras,
                            conexion,
                            transaccion
                        );

                    comandoCodigoBarras.Parameters.AddWithValue(
                        "@codigoBarras",
                        codigoBarras
                    );

                    int cantidadCodigos =
                        Convert.ToInt32(
                            await comandoCodigoBarras.ExecuteScalarAsync()
                        );

                    if (cantidadCodigos > 0)
                    {
                        await transaccion.RollbackAsync();
                        return false;
                    }
                }


                // CODIGO TEMPORAL PARA CUMPLIR EL UNIQUE/NOT NULL
                string codigoTemporal =
                    "TMP-" + Guid.NewGuid().ToString("N");


                // INSERTAR DATOS GENERALES DEL PRODUCTO
                string consultaProducto = @"
                    INSERT INTO producto
                    (
                        codigo_interno,
                        codigo_barras,
                        nombre,
                        contenido_por_envase,
                        unidad_medida,
                        stock_minimo,
                        imagen_url
                    )
                    VALUES
                    (
                        @codigoInterno,
                        @codigoBarras,
                        @nombre,
                        @contenidoPorEnvase,
                        @unidadMedida,
                        @stockMinimo,
                        @imagenUrl
                    )";

                using MySqlCommand comandoProducto =
                    new MySqlCommand(
                        consultaProducto,
                        conexion,
                        transaccion
                    );

                comandoProducto.Parameters.AddWithValue(
                    "@codigoInterno",
                    codigoTemporal
                );

                comandoProducto.Parameters.AddWithValue(
                    "@codigoBarras",
                    codigoBarras == null
                        ? DBNull.Value
                        : codigoBarras
                );

                comandoProducto.Parameters.AddWithValue(
                    "@nombre",
                    reg.nombre
                );

                comandoProducto.Parameters.AddWithValue(
                    "@contenidoPorEnvase",
                    reg.contenido_por_envase <= 0
                        ? 1
                        : reg.contenido_por_envase
                );

                comandoProducto.Parameters.AddWithValue(
                    "@unidadMedida",
                    unidadMedida == null
                        ? DBNull.Value
                        : unidadMedida
                );

                comandoProducto.Parameters.AddWithValue(
                    "@stockMinimo",
                    reg.stock_minimo
                );

                comandoProducto.Parameters.AddWithValue(
                    "@imagenUrl",
                    string.IsNullOrWhiteSpace(reg.imagen_url)
                        ? DBNull.Value
                        : reg.imagen_url
                );

                await comandoProducto.ExecuteNonQueryAsync();


                // GENERAR CODIGO INTERNO CON EL ID REAL
                long idProducto =
                    comandoProducto.LastInsertedId;

                string prefijo =
                    reg.tipo == "Medicamento"
                        ? "MED"
                        : "INS";

                string codigoInterno =
                    $"{prefijo}-{idProducto:000000}";

                string consultaCodigoInterno = @"
                    UPDATE producto
                    SET codigo_interno = @codigoInterno
                    WHERE id_producto = @idProducto";

                using MySqlCommand comandoCodigoInterno =
                    new MySqlCommand(
                        consultaCodigoInterno,
                        conexion,
                        transaccion
                    );

                comandoCodigoInterno.Parameters.AddWithValue(
                    "@codigoInterno",
                    codigoInterno
                );

                comandoCodigoInterno.Parameters.AddWithValue(
                    "@idProducto",
                    idProducto
                );

                await comandoCodigoInterno.ExecuteNonQueryAsync();


                // MEDICAMENTO
                if (reg.tipo == "Medicamento")
                {
                    string consultaMedicamento = @"
                        INSERT INTO medicamento
                        (
                            id_producto,
                            forma_farmaceutica,
                            principio_activo,
                            concentracion,
                            requiere_control
                        )
                        VALUES
                        (
                            @idProducto,
                            @formaFarmaceutica,
                            @principioActivo,
                            @concentracion,
                            @requiereControl
                        )";

                    using MySqlCommand comandoMedicamento =
                        new MySqlCommand(
                            consultaMedicamento,
                            conexion,
                            transaccion
                        );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@formaFarmaceutica",
                        string.IsNullOrWhiteSpace(reg.forma_farmaceutica)
                            ? DBNull.Value
                            : reg.forma_farmaceutica.Trim()
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@principioActivo",
                        reg.principio_activo
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@concentracion",
                        reg.concentracion
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@requiereControl",
                        reg.requiere_control
                    );

                    await comandoMedicamento.ExecuteNonQueryAsync();
                }


                // INSUMO
                if (reg.tipo == "Insumo")
                {
                    string consultaInsumo = @"
                        INSERT INTO insumo
                        (
                            id_producto,
                            material,
                            es_esteril
                        )
                        VALUES
                        (
                            @idProducto,
                            @material,
                            @esEsteril
                        )";

                    using MySqlCommand comandoInsumo =
                        new MySqlCommand(
                            consultaInsumo,
                            conexion,
                            transaccion
                        );

                    comandoInsumo.Parameters.AddWithValue(
                        "@idProducto",
                        idProducto
                    );

                    comandoInsumo.Parameters.AddWithValue(
                        "@material",
                        reg.material
                    );

                    comandoInsumo.Parameters.AddWithValue(
                        "@esEsteril",
                        reg.es_esteril
                    );

                    await comandoInsumo.ExecuteNonQueryAsync();
                }


                await transaccion.CommitAsync();

                reg.id_producto = Convert.ToInt32(idProducto);
                reg.codigo_interno = codigoInterno;
                reg.codigo_barras = codigoBarras;
                reg.unidad_medida = unidadMedida;

                return true;
            }
            catch
            {
                await transaccion.RollbackAsync();
                return false;
            }
        }


        // =========================================================
        // LISTADO DE PRODUCTOS
        // =========================================================
        public async Task<List<Producto>> ListadoProductos()
        {
            List<Producto> productos = new List<Producto>();

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    p.id_producto,
                    p.codigo_interno,
                    p.codigo_barras,
                    p.nombre,
                    p.contenido_por_envase,
                    p.unidad_medida,
                    p.stock_minimo,
                    p.imagen_url,

                    m.forma_farmaceutica,
                    m.principio_activo,
                    m.concentracion,
                    m.requiere_control,

                    i.material,
                    i.es_esteril,

                    CASE
                        WHEN m.id_producto IS NOT NULL THEN 'Medicamento'
                        WHEN i.id_producto IS NOT NULL THEN 'Insumo'
                    END AS tipo

                FROM producto p

                LEFT JOIN medicamento m
                    ON p.id_producto = m.id_producto

                LEFT JOIN insumo i
                    ON p.id_producto = i.id_producto

                ORDER BY tipo, p.nombre";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                productos.Add(
                    CrearProductoDesdeLector(lector)
                );
            }

            return productos;
        }


        // =========================================================
        // OBTENER PRODUCTO POR ID
        // =========================================================
        public async Task<Producto?> ObtenerProductoPorId(
            int idProducto)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    p.id_producto,
                    p.codigo_interno,
                    p.codigo_barras,
                    p.nombre,
                    p.contenido_por_envase,
                    p.unidad_medida,
                    p.stock_minimo,
                    p.imagen_url,

                    m.forma_farmaceutica,
                    m.principio_activo,
                    m.concentracion,
                    m.requiere_control,

                    i.material,
                    i.es_esteril,

                    CASE
                        WHEN m.id_producto IS NOT NULL THEN 'Medicamento'
                        WHEN i.id_producto IS NOT NULL THEN 'Insumo'
                    END AS tipo

                FROM producto p

                LEFT JOIN medicamento m
                    ON p.id_producto = m.id_producto

                LEFT JOIN insumo i
                    ON p.id_producto = i.id_producto

                WHERE p.id_producto = @idProducto";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue(
                "@idProducto",
                idProducto
            );

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                return CrearProductoDesdeLector(lector);
            }

            return null;
        }


        // =========================================================
        // BUSCAR PRODUCTO POR CODIGO DE BARRAS / GTIN
        // =========================================================
        public async Task<Producto?> ObtenerProductoPorCodigoBarras(
            string codigoBarras)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                SELECT
                    p.id_producto,
                    p.codigo_interno,
                    p.codigo_barras,
                    p.nombre,
                    p.contenido_por_envase,
                    p.unidad_medida,
                    p.stock_minimo,
                    p.imagen_url,

                    m.forma_farmaceutica,
                    m.principio_activo,
                    m.concentracion,
                    m.requiere_control,

                    i.material,
                    i.es_esteril,

                    CASE
                        WHEN m.id_producto IS NOT NULL THEN 'Medicamento'
                        WHEN i.id_producto IS NOT NULL THEN 'Insumo'
                    END AS tipo

                FROM producto p

                LEFT JOIN medicamento m
                    ON p.id_producto = m.id_producto

                LEFT JOIN insumo i
                    ON p.id_producto = i.id_producto

                WHERE p.codigo_barras = @codigoBarras
                LIMIT 1";

            using MySqlCommand comando =
                new MySqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue(
                "@codigoBarras",
                codigoBarras.Trim()
            );

            using MySqlDataReader lector =
                await comando.ExecuteReaderAsync();

            if (await lector.ReadAsync())
            {
                return CrearProductoDesdeLector(lector);
            }

            return null;
        }


        // =========================================================
        // ACTUALIZAR PRODUCTO
        // El código interno se mantiene estable.
        // =========================================================
        public async Task<bool> ActualizarProducto(Producto reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            using MySqlTransaction transaccion =
                await conexion.BeginTransactionAsync();

            try
            {
                string? codigoBarras =
                    string.IsNullOrWhiteSpace(reg.codigo_barras)
                        ? null
                        : reg.codigo_barras.Trim();

                string? unidadMedida =
                    string.IsNullOrWhiteSpace(reg.unidad_medida)
                        ? null
                        : reg.unidad_medida.Trim();


                if (codigoBarras != null)
                {
                    string consultaExiste = @"
                        SELECT COUNT(*)
                        FROM producto
                        WHERE codigo_barras = @codigoBarras
                          AND id_producto <> @idProducto";

                    using MySqlCommand comandoExiste =
                        new MySqlCommand(
                            consultaExiste,
                            conexion,
                            transaccion
                        );

                    comandoExiste.Parameters.AddWithValue(
                        "@codigoBarras",
                        codigoBarras
                    );

                    comandoExiste.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );

                    int cantidad =
                        Convert.ToInt32(
                            await comandoExiste.ExecuteScalarAsync()
                        );

                    if (cantidad > 0)
                    {
                        await transaccion.RollbackAsync();
                        return false;
                    }
                }


                string consultaProducto = @"
                    UPDATE producto
                    SET
                        codigo_barras = @codigoBarras,
                        nombre = @nombre,
                        contenido_por_envase = @contenidoPorEnvase,
                        unidad_medida = @unidadMedida,
                        stock_minimo = @stockMinimo
                    WHERE id_producto = @idProducto";

                using MySqlCommand comandoProducto =
                    new MySqlCommand(
                        consultaProducto,
                        conexion,
                        transaccion
                    );

                comandoProducto.Parameters.AddWithValue(
                    "@codigoBarras",
                    codigoBarras == null
                        ? DBNull.Value
                        : codigoBarras
                );

                comandoProducto.Parameters.AddWithValue(
                    "@nombre",
                    reg.nombre
                );

                comandoProducto.Parameters.AddWithValue(
                    "@contenidoPorEnvase",
                    reg.contenido_por_envase <= 0
                        ? 1
                        : reg.contenido_por_envase
                );

                comandoProducto.Parameters.AddWithValue(
                    "@unidadMedida",
                    unidadMedida == null
                        ? DBNull.Value
                        : unidadMedida
                );

                comandoProducto.Parameters.AddWithValue(
                    "@stockMinimo",
                    reg.stock_minimo
                );

                comandoProducto.Parameters.AddWithValue(
                    "@idProducto",
                    reg.id_producto
                );

                await comandoProducto.ExecuteNonQueryAsync();


                if (reg.tipo == "Medicamento")
                {
                    string consultaMedicamento = @"
                        UPDATE medicamento
                        SET
                            forma_farmaceutica = @formaFarmaceutica,
                            principio_activo = @principioActivo,
                            concentracion = @concentracion,
                            requiere_control = @requiereControl
                        WHERE id_producto = @idProducto";

                    using MySqlCommand comandoMedicamento =
                        new MySqlCommand(
                            consultaMedicamento,
                            conexion,
                            transaccion
                        );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@formaFarmaceutica",
                        string.IsNullOrWhiteSpace(reg.forma_farmaceutica)
                            ? DBNull.Value
                            : reg.forma_farmaceutica.Trim()
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@principioActivo",
                        reg.principio_activo
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@concentracion",
                        reg.concentracion
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@requiereControl",
                        reg.requiere_control
                    );

                    comandoMedicamento.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );

                    await comandoMedicamento.ExecuteNonQueryAsync();
                }
                else if (reg.tipo == "Insumo")
                {
                    string consultaInsumo = @"
                        UPDATE insumo
                        SET
                            material = @material,
                            es_esteril = @esEsteril
                        WHERE id_producto = @idProducto";

                    using MySqlCommand comandoInsumo =
                        new MySqlCommand(
                            consultaInsumo,
                            conexion,
                            transaccion
                        );

                    comandoInsumo.Parameters.AddWithValue(
                        "@material",
                        reg.material
                    );

                    comandoInsumo.Parameters.AddWithValue(
                        "@esEsteril",
                        reg.es_esteril
                    );

                    comandoInsumo.Parameters.AddWithValue(
                        "@idProducto",
                        reg.id_producto
                    );

                    await comandoInsumo.ExecuteNonQueryAsync();
                }
                else
                {
                    await transaccion.RollbackAsync();
                    return false;
                }


                await transaccion.CommitAsync();

                return true;
            }
            catch
            {
                await transaccion.RollbackAsync();
                return false;
            }
        }


        // =========================================================
        // CONVERTIR FILA MYSQL A PRODUCTO
        // =========================================================
        private Producto CrearProductoDesdeLector(
            MySqlDataReader lector)
        {
            return new Producto()
            {
                id_producto =
                    Convert.ToInt32(
                        lector["id_producto"]
                    ),

                codigo_interno =
                    lector["codigo_interno"]
                        .ToString(),

                codigo_barras =
                    lector["codigo_barras"] == DBNull.Value
                        ? null
                        : lector["codigo_barras"].ToString(),

                nombre =
                    lector["nombre"]
                        .ToString(),

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
                    Convert.ToInt32(
                        lector["stock_minimo"]
                    ),

                imagen_url =
                    lector["imagen_url"] == DBNull.Value
                        ? null
                        : lector["imagen_url"].ToString(),

                tipo =
                    lector["tipo"] == DBNull.Value
                        ? null
                        : lector["tipo"].ToString(),

                forma_farmaceutica =
                    lector["forma_farmaceutica"] == DBNull.Value
                        ? null
                        : lector["forma_farmaceutica"].ToString(),

                principio_activo =
                    lector["principio_activo"] == DBNull.Value
                        ? null
                        : lector["principio_activo"].ToString(),

                concentracion =
                    lector["concentracion"] == DBNull.Value
                        ? null
                        : lector["concentracion"].ToString(),

                requiere_control =
                    lector["requiere_control"] != DBNull.Value
                    && Convert.ToBoolean(
                        lector["requiere_control"]
                    ),

                material =
                    lector["material"] == DBNull.Value
                        ? null
                        : lector["material"].ToString(),

                es_esteril =
                    lector["es_esteril"] != DBNull.Value
                    && Convert.ToBoolean(
                        lector["es_esteril"]
                    )
            };
        }
    }
}
