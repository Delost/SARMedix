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


        //CREAR PRODUCTO
        public async Task<bool> CrearProducto(Producto reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();


            //VER CODIGO INTERNO
            string consultaExiste = @"SELECT COUNT(*)
                                      FROM producto
                                      WHERE codigo_interno = @codigoInterno";

            using MySqlCommand comandoExiste =
                new MySqlCommand(consultaExiste, conexion);

            comandoExiste.Parameters.AddWithValue(
                "@codigoInterno",
                reg.codigo_interno
            );

            int cantidad = Convert.ToInt32(
                await comandoExiste.ExecuteScalarAsync()
            );

            if (cantidad > 0)
            {
                return false;
            }



            using MySqlTransaction transaccion =
                conexion.BeginTransaction();


            //INSERTAR PRODUCTO GENERAL
            string consultaProducto = @"INSERT INTO producto
                                        (codigo_interno,
                                         nombre,
                                         stock_minimo,
                                         imagen_url)
                                        VALUES
                                        (@codigoInterno,
                                         @nombre,
                                         @stockMinimo,
                                         @imagenUrl)";

            using MySqlCommand comandoProducto =
                new MySqlCommand(
                    consultaProducto,
                    conexion,
                    transaccion
                );

            comandoProducto.Parameters.AddWithValue(
                "@codigoInterno",
                reg.codigo_interno
            );

            comandoProducto.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
            );

            comandoProducto.Parameters.AddWithValue(
                "@stockMinimo",
                reg.stock_minimo
            );

            comandoProducto.Parameters.AddWithValue(
                "@imagenUrl",
                (object?)reg.imagen_url
                ?? DBNull.Value
            );

            await comandoProducto.ExecuteNonQueryAsync();


            //OBTENER ID DEL PRODUCTO CREADO
            long idProducto =
                comandoProducto.LastInsertedId;


            //SI ES MEDICAMENTO
            if (reg.tipo == "Medicamento")
            {
                string consultaMedicamento =
                    @"INSERT INTO medicamento
                      (id_producto,
                       principio_activo,
                       concentracion,
                       requiere_control)
                      VALUES
                      (@idProducto,
                       @principioActivo,
                       @concentracion,
                       @requiereControl)";

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


            //SI ES INSUMO
            else if (reg.tipo == "Insumo")
            {
                string consultaInsumo =
                    @"INSERT INTO insumo
                      (id_producto,
                       material,
                       es_esteril)
                      VALUES
                      (@idProducto,
                       @material,
                       @esEsteril)";

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


            //SI NO SE SELECCIONÓ TIPO
            else
            {
                transaccion.Rollback();

                return false;
            }


            //GUARDAR TODO
            transaccion.Commit();

            return true;
        }









        //LISTADO DE PRODUCTOS
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
                                    p.nombre,
                                    p.stock_minimo,
                                    p.imagen_url,

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
                productos.Add(new Producto()
                {
                    id_producto =
                        Convert.ToInt32(lector["id_producto"]),

                    codigo_interno =
                        lector["codigo_interno"].ToString(),

                    nombre =
                        lector["nombre"].ToString(),

                    stock_minimo =
                        Convert.ToInt32(lector["stock_minimo"]),

                    imagen_url =
                        lector["imagen_url"] == DBNull.Value
                        ? null
                        : lector["imagen_url"].ToString(),

                    tipo =
                        lector["tipo"].ToString(),

                    principio_activo =
                        lector["principio_activo"] == DBNull.Value
                        ? null
                        : lector["principio_activo"].ToString(),

                    concentracion =
                        lector["concentracion"] == DBNull.Value
                        ? null
                        : lector["concentracion"].ToString(),

                    requiere_control =
                        lector["requiere_control"] != DBNull.Value &&
                        Convert.ToBoolean(lector["requiere_control"]),

                    material =
                        lector["material"] == DBNull.Value
                        ? null
                        : lector["material"].ToString(),

                    es_esteril =
                        lector["es_esteril"] != DBNull.Value &&
                        Convert.ToBoolean(lector["es_esteril"])
                });
            }

            return productos;
        }





        //OBTENER PRODUCTO POR ID
        public async Task<Producto?> ObtenerProductoPorId(int idProducto)
        {
            Producto? producto = null;

            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            string consulta = @"
                                SELECT
                                    p.id_producto,
                                    p.codigo_interno,
                                    p.nombre,
                                    p.stock_minimo,
                                    p.imagen_url,

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
                producto = new Producto()
                {
                    id_producto =
                        Convert.ToInt32(lector["id_producto"]),

                    codigo_interno =
                        lector["codigo_interno"].ToString(),

                    nombre =
                        lector["nombre"].ToString(),

                    stock_minimo =
                        Convert.ToInt32(lector["stock_minimo"]),

                    imagen_url =
                        lector["imagen_url"] == DBNull.Value
                        ? null
                        : lector["imagen_url"].ToString(),

                    tipo =
                        lector["tipo"].ToString(),

                    principio_activo =
                        lector["principio_activo"] == DBNull.Value
                        ? null
                        : lector["principio_activo"].ToString(),

                    concentracion =
                        lector["concentracion"] == DBNull.Value
                        ? null
                        : lector["concentracion"].ToString(),

                    requiere_control =
                        lector["requiere_control"] != DBNull.Value &&
                        Convert.ToBoolean(lector["requiere_control"]),

                    material =
                        lector["material"] == DBNull.Value
                        ? null
                        : lector["material"].ToString(),

                    es_esteril =
                        lector["es_esteril"] != DBNull.Value &&
                        Convert.ToBoolean(lector["es_esteril"])
                };
            }

            return producto;
        }


        //ACTUALIZAR PRODUCTO
        public async Task<bool> ActualizarProducto(Producto reg)
        {
            using MySqlConnection conexion =
                new MySqlConnection(_conexion);

            await conexion.OpenAsync();

            using MySqlTransaction transaccion =
                conexion.BeginTransaction();


            //ACTUALIZAR DATOS GENERALES
            string consultaProducto = @"
        UPDATE producto
        SET codigo_interno = @codigoInterno,
            nombre = @nombre,
            stock_minimo = @stockMinimo
        WHERE id_producto = @idProducto";

            using MySqlCommand comandoProducto =
                new MySqlCommand(
                    consultaProducto,
                    conexion,
                    transaccion
                );

            comandoProducto.Parameters.AddWithValue(
                "@codigoInterno",
                reg.codigo_interno
            );

            comandoProducto.Parameters.AddWithValue(
                "@nombre",
                reg.nombre
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


            //SI ES MEDICAMENTO
            if (reg.tipo == "Medicamento")
            {
                string consultaMedicamento = @"
            UPDATE medicamento
            SET principio_activo = @principioActivo,
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


            //SI ES INSUMO
            else if (reg.tipo == "Insumo")
            {
                string consultaInsumo = @"
            UPDATE insumo
            SET material = @material,
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


            transaccion.Commit();

            return true;
        }
    }
}