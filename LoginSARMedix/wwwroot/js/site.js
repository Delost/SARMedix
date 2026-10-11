// =============================================================
// SARMedix - Validación global de formularios
// =============================================================
// Regla general:
// - Se muestra un solo error a la vez.
// - Solo el campo correspondiente queda con borde rojo.
// - Al comenzar a corregirlo, el error desaparece.
// - Las confirmaciones propias de cada vista siguen funcionando.
// - En Crear Usuario se valida en caliente RUT, correo y usuario.
// =============================================================

(function () {
    "use strict";

    const CLASE_ERROR = "campo-error";
    const ATRIBUTO_MENSAJE_CLIENTE = "data-validacion-cliente";

    // formulario -> campo que actualmente tiene el error visible
    const erroresActuales = new WeakMap();

    // formulario -> temporizadores para consultas en caliente
    const temporizadoresUnicidad = new WeakMap();

    // =========================================================
    // FUNCIONES BÁSICAS
    // =========================================================

    function textoCampo(campo) {
        if (!campo) return "";
        return String(campo.value ?? "").trim();
    }

    function campoTieneTexto(campo) {
        return textoCampo(campo) !== "";
    }

    function correoValido(valor) {
        const correo = String(valor ?? "").trim();
        const expresion = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return expresion.test(correo);
    }

    function numeroValidoMayorOIgualA(campo, minimo) {
        if (!campo || textoCampo(campo) === "") return false;
        const numero = Number(campo.value);
        return !Number.isNaN(numero) && numero >= minimo;
    }

    function numeroValidoMayorA(campo, minimo) {
        if (!campo || textoCampo(campo) === "") return false;
        const numero = Number(campo.value);
        return !Number.isNaN(numero) && numero > minimo;
    }

    function obtenerAccionFormulario(formulario) {
        try {
            const url = new URL(formulario.action, window.location.href);
            return url.pathname.replace(/\/+$/, "").toLowerCase();
        } catch {
            return "";
        }
    }

    // =========================================================
    // REGLAS REUTILIZABLES
    // =========================================================

    function reglaTexto(selector, mensaje) {
        return {
            selector,
            mensaje,
            validar: campo => campoTieneTexto(campo)
        };
    }

    function reglaNumeroMinimo(selector, mensaje, minimo) {
        return {
            selector,
            mensaje,
            validar: campo => numeroValidoMayorOIgualA(campo, minimo)
        };
    }

    function reglaDuplicado(selector, mensaje) {
        return {
            selector,
            mensaje,
            validar: campo => campo.dataset.duplicado !== "true"
        };
    }

    // =========================================================
    // CONFIGURACIÓN DE FORMULARIOS
    // =========================================================

    const configuraciones = [
        // ---------------------------------------------------------
        // INICIO DE SESIÓN
        // ---------------------------------------------------------
        {
            accion: "/home/iniciosesion",
            tipoMensaje: "espacio",
            campos: [
                reglaTexto(
                    '[name="nombre_usuario"]',
                    "Debe ingresar el nombre de usuario."
                ),
                reglaTexto(
                    '[name="contrasena"]',
                    "Debe ingresar la contraseña."
                )
            ]
        },

        // ---------------------------------------------------------
        // CREAR USUARIO
        // ---------------------------------------------------------
        {
            accion: "/home/nuevousuario",
            tipoMensaje: "espacio",
            campos: [
                reglaTexto(
                    "#nombre",
                    "Debe ingresar el nombre."
                ),

                reglaTexto(
                    "#apellido",
                    "Debe ingresar el apellido."
                ),

                reglaTexto(
                    "#email",
                    "Debe ingresar el correo electrónico."
                ),

                {
                    selector: "#email",
                    mensaje:
                        "Debe ingresar un correo electrónico válido.",

                    validar: campo =>
                        correoValido(
                            campo.value
                        )
                },

                reglaDuplicado(
                    "#email",
                    "El correo electrónico ingresado ya está registrado."
                ),

                reglaTexto(
                    "#rut",
                    "Debe ingresar el RUT."
                ),

                reglaDuplicado(
                    "#rut",
                    "El RUT ingresado ya está registrado."
                ),

                {
                    selector: "#id_rol",
                    mensaje:
                        "Debe seleccionar un rol.",

                    validar: campo =>
                        textoCampo(
                            campo
                        )
                        !==
                        ""
                },

                reglaTexto(
                    "#nombre_usuario",
                    "Debe ingresar el nombre de usuario."
                ),

                reglaDuplicado(
                    "#nombre_usuario",
                    "El nombre de usuario ingresado ya está en uso."
                ),

                reglaTexto(
                    "#contrasena",
                    "Debe ingresar una contraseña."
                )
            ]
        },

        // ---------------------------------------------------------
        // EDITAR USUARIO
        // ---------------------------------------------------------
        {
            accion: "/home/actualizarusuario",
            tipoMensaje: "formulario",
            campos: [
                reglaTexto(
                    '[name="nombre"]',
                    "Debe ingresar el nombre."
                ),

                reglaTexto(
                    '[name="apellido"]',
                    "Debe ingresar el apellido."
                ),

                reglaTexto(
                    '[name="email"]',
                    "Debe ingresar el correo electrónico."
                ),

                {
                    selector:
                        '[name="email"]',

                    mensaje:
                        "Debe ingresar un correo electrónico válido.",

                    validar: campo =>
                        correoValido(
                            campo.value
                        )
                },

                reglaTexto(
                    '[name="rut"]',
                    "Debe ingresar el RUT."
                ),

                {
                    selector:
                        '[name="id_rol"]',

                    mensaje:
                        "Debe seleccionar un rol.",

                    validar: campo =>
                        Number(
                            campo.value
                        )
                        >
                        0
                },

                reglaTexto(
                    '[name="nombre_usuario"]',
                    "Debe ingresar el nombre de usuario."
                )
            ]
        },

        // ---------------------------------------------------------
        // CREAR PRODUCTO
        // ---------------------------------------------------------
        {
            accion: "/home/nuevoproducto",
            tipoMensaje: "espacio",
            campos: [
                {
                    selector:
                        "#tipoProducto",

                    mensaje:
                        "Debe seleccionar el tipo de producto.",

                    validar: campo =>
                        textoCampo(
                            campo
                        )
                        !==
                        ""
                },

                reglaTexto(
                    "#codigoInterno",
                    "Debe ingresar el código interno."
                ),

                reglaTexto(
                    "#nombreProducto",
                    "Debe ingresar el nombre del producto."
                ),

                reglaNumeroMinimo(
                    "#stockMinimo",
                    "Debe ingresar un stock mínimo válido.",
                    0
                ),

                {
                    selector:
                        "#principioActivo",

                    mensaje:
                        "Debe ingresar el principio activo.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                "#tipoProducto"
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Medicamento"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                },

                {
                    selector:
                        "#concentracion",

                    mensaje:
                        "Debe ingresar la concentración.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                "#tipoProducto"
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Medicamento"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                },

                {
                    selector:
                        "#material",

                    mensaje:
                        "Debe ingresar el material.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                "#tipoProducto"
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Insumo"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                }
            ]
        },

        // ---------------------------------------------------------
        // EDITAR PRODUCTO
        // ---------------------------------------------------------
        {
            accion: "/home/actualizarproducto",
            tipoMensaje: "espacio",
            campos: [
                reglaTexto(
                    "#codigoInterno",
                    "Debe ingresar el código interno."
                ),

                reglaTexto(
                    "#nombreProducto",
                    "Debe ingresar el nombre del producto."
                ),

                reglaNumeroMinimo(
                    "#stockMinimo",
                    "Debe ingresar un stock mínimo válido.",
                    0
                ),

                {
                    selector:
                        "#principioActivo",

                    mensaje:
                        "Debe ingresar el principio activo.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                '[name="tipo"]'
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Medicamento"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                },

                {
                    selector:
                        "#concentracion",

                    mensaje:
                        "Debe ingresar la concentración.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                '[name="tipo"]'
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Medicamento"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                },

                {
                    selector:
                        "#material",

                    mensaje:
                        "Debe ingresar el material.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                '[name="tipo"]'
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Insumo"
                        );
                    },

                    validar: campo =>
                        campoTieneTexto(
                            campo
                        )
                }
            ]
        },

        // ---------------------------------------------------------
        // CREAR LOTE
        // ---------------------------------------------------------
        {
            accion: "/home/nuevolote",
            tipoMensaje: "espacio",
            campos: [
                {
                    selector:
                        "#producto",

                    mensaje:
                        "Debe seleccionar un producto.",

                    validar: campo =>
                        textoCampo(
                            campo
                        )
                        !==
                        ""
                },

                reglaTexto(
                    "#numeroLote",
                    "Debe ingresar el número de lote."
                ),

                reglaTexto(
                    "#fechaVencimiento",
                    "Debe ingresar la fecha de vencimiento."
                ),

                reglaNumeroMinimo(
                    "#cantidadInicial",
                    "Debe ingresar una cantidad inicial válida.",
                    0
                ),

                {
                    selector:
                        "#ubicacion",

                    mensaje:
                        "Debe seleccionar una ubicación.",

                    validar: campo =>
                        textoCampo(
                            campo
                        )
                        !==
                        ""
                }
            ]
        },

        // ---------------------------------------------------------
        // EDITAR LOTE
        // ---------------------------------------------------------
        {
            accion: "/home/actualizarlote",
            tipoMensaje: "espacio",
            campos: [
                {
                    selector:
                        "#producto",

                    mensaje:
                        "Debe seleccionar un producto.",

                    validar: campo =>
                        Number(
                            campo.value
                        )
                        >
                        0
                },

                reglaTexto(
                    "#numeroLote",
                    "Debe ingresar el número de lote."
                ),

                reglaTexto(
                    "#fechaVencimiento",
                    "Debe ingresar la fecha de vencimiento."
                ),

                reglaNumeroMinimo(
                    "#cantidadActual",
                    "Debe ingresar una cantidad actual válida.",
                    0
                ),

                {
                    selector:
                        "#ubicacion",

                    mensaje:
                        "Debe seleccionar una ubicación.",

                    validar: campo =>
                        Number(
                            campo.value
                        )
                        >
                        0
                }
            ]
        },

        // ---------------------------------------------------------
        // CREAR ROL
        // ---------------------------------------------------------
        {
            accion: "/home/nuevorol",
            tipoMensaje: "rol",
            campos: [
                reglaTexto(
                    "#nombreRol",
                    "Debe ingresar el nombre del rol."
                ),

                reglaTexto(
                    "#descripcionRol",
                    "Debe ingresar una descripción."
                )
            ]
        },

        // ---------------------------------------------------------
        // EDITAR ROL
        // ---------------------------------------------------------
        {
            accion: "/home/actualizarrol",
            tipoMensaje: "formulario",
            campos: [
                reglaTexto(
                    '[name="nombre"]',
                    "Debe ingresar el nombre del rol."
                ),

                reglaTexto(
                    '[name="descripcion"]',
                    "Debe ingresar una descripción."
                )
            ]
        },

        // ---------------------------------------------------------
        // RECUPERAR CONTRASEÑA
        // ---------------------------------------------------------
        {
            accion: "/home/recuperarcontrasena",
            tipoMensaje: "formulario",
            campos: [
                reglaTexto(
                    '[name="correo"]',
                    "Debe ingresar el correo electrónico."
                ),

                {
                    selector:
                        '[name="correo"]',

                    mensaje:
                        "Debe ingresar un correo electrónico válido.",

                    validar: campo =>
                        correoValido(
                            campo.value
                        )
                }
            ]
        },

        // ---------------------------------------------------------
        // VERIFICAR CÓDIGO
        // ---------------------------------------------------------
        {
            accion: "/home/verificarcodigo",
            tipoMensaje: "formulario",
            campos: [
                reglaTexto(
                    '[name="codigo"]',
                    "Debe ingresar el código de recuperación."
                ),

                {
                    selector:
                        '[name="codigo"]',

                    mensaje:
                        "Debe ingresar un código válido de 6 dígitos.",

                    validar: campo =>
                        /^\d{6}$/.test(
                            textoCampo(
                                campo
                            )
                        )
                }
            ]
        },

        // ---------------------------------------------------------
        // NUEVA CONTRASEÑA
        // ---------------------------------------------------------
        {
            accion: "/home/cambiarcontrasena",
            tipoMensaje: "formulario",
            campos: [
                reglaTexto(
                    "#nuevaContrasena",
                    "Debe ingresar una nueva contraseña."
                ),

                reglaTexto(
                    "#confirmarContrasena",
                    "Debe confirmar la nueva contraseña."
                ),

                {
                    selector:
                        "#confirmarContrasena",

                    mensaje:
                        "Las contraseñas no coinciden.",

                    validar: (
                        campo,
                        formulario
                    ) => {
                        const nueva =
                            formulario.querySelector(
                                "#nuevaContrasena"
                            );

                        return (
                            nueva
                            &&
                            campo.value
                            ===
                            nueva.value
                        );
                    }
                }
            ]
        },

        // ---------------------------------------------------------
        // MEDICAMENTOS CONTROLADOS - ENTRADA / SALIDA
        // ---------------------------------------------------------
        {
            accion: "/home/registrarmovimientocontrolado",
            tipoMensaje: "movimiento",
            campos: [
                {
                    selector:
                        "#movimientoCantidad",

                    mensaje:
                        "Debe ingresar una cantidad válida.",

                    validar: campo =>
                        numeroValidoMayorA(
                            campo,
                            0
                        )
                },

                {
                    selector:
                        "#movimientoCantidad",

                    mensaje:
                        "La cantidad no puede ser mayor al stock disponible.",

                    cuando: formulario => {
                        const tipo =
                            formulario.querySelector(
                                "#movimientoTipo"
                            );

                        return (
                            tipo
                            &&
                            tipo.value
                            ===
                            "Salida"
                        );
                    },

                    validar: (
                        campo,
                        formulario
                    ) => {
                        const stock =
                            formulario.querySelector(
                                "#movimientoStockActual"
                            );

                        return (
                            Number(
                                campo.value
                            )
                            <=
                            Number(
                                stock?.value
                                ??
                                0
                            )
                        );
                    }
                },

                reglaTexto(
                    "#movimientoObservacion",
                    "Debe ingresar el motivo u observación."
                )
            ]
        }
    ];

    // =========================================================
    // BUSCAR CONFIGURACIÓN
    // =========================================================

    function obtenerConfiguracion(
        formulario
    ) {
        const accion =
            obtenerAccionFormulario(
                formulario
            );

        return configuraciones.find(
            configuracion =>
                accion.endsWith(
                    configuracion.accion
                )
        )
        ??
        null;
    }

    // =========================================================
    // BORDES DE ERROR
    // =========================================================

    function limpiarBordesFormulario(
        formulario
    ) {
        formulario
            .querySelectorAll(
                "." + CLASE_ERROR
            )
            .forEach(
                campo =>
                    campo.classList.remove(
                        CLASE_ERROR
                    )
            );
    }

    // =========================================================
    // MENSAJE GENÉRICO
    // =========================================================

    function crearMensajeFormulario(
        formulario
    ) {
        let mensaje =
            formulario.querySelector(
                "["
                +
                ATRIBUTO_MENSAJE_CLIENTE
                +
                '="true"]'
            );

        if (mensaje) {
            return mensaje;
        }

        mensaje =
            document.createElement(
                "div"
            );

        mensaje.setAttribute(
            ATRIBUTO_MENSAJE_CLIENTE,
            "true"
        );

        mensaje.className =
            "mensaje-validacion-campo";

        mensaje.style.display =
            "none";

        formulario.insertBefore(
            mensaje,
            formulario.firstChild
        );

        return mensaje;
    }

    function crearMensajeEnEspacio(
        formulario
    ) {
        const espacio =
            formulario.querySelector(
                ".espacio-mensaje"
            );

        if (!espacio) {
            return crearMensajeFormulario(
                formulario
            );
        }

        let mensaje =
            espacio.querySelector(
                "["
                +
                ATRIBUTO_MENSAJE_CLIENTE
                +
                '="true"]'
            );

        if (!mensaje) {
            mensaje =
                document.createElement(
                    "div"
                );

            mensaje.setAttribute(
                ATRIBUTO_MENSAJE_CLIENTE,
                "true"
            );

            mensaje.className =
                "mensaje-validacion-campo";

            mensaje.style.display =
                "none";

            espacio.appendChild(
                mensaje
            );
        }

        return mensaje;
    }

    function obtenerMensajeRol() {
        const mensaje =
            document.getElementById(
                "mensajeRol"
            );

        const texto =
            document.getElementById(
                "textoMensajeRol"
            );

        if (
            !mensaje
            ||
            !texto
        ) {
            return null;
        }

        return {
            contenedor:
                mensaje,

            texto:
                texto
        };
    }

    // =========================================================
    // MOSTRAR MENSAJE
    // =========================================================

    function mostrarMensaje(
        formulario,
        configuracion,
        texto
    ) {
        // Medicamentos controlados
        if (
            configuracion.tipoMensaje
            ===
            "movimiento"
        ) {
            const mensaje =
                document.getElementById(
                    "mensajeErrorMovimiento"
                );

            if (mensaje) {
                mensaje.textContent =
                    texto;

                mensaje.style.display =
                    "block";

                mensaje.dataset.validacionCliente =
                    "true";
            }

            return;
        }

        // Crear rol
        if (
            configuracion.tipoMensaje
            ===
            "rol"
        ) {
            const elementos =
                obtenerMensajeRol();

            if (elementos) {
                elementos.texto.innerText =
                    texto;

                elementos.contenedor.className =
                    "mensaje-error";

                elementos.contenedor.style.visibility =
                    "visible";

                elementos.contenedor.style.opacity =
                    "1";

                elementos.contenedor.dataset.validacionCliente =
                    "true";
            }

            return;
        }

        let mensaje;

        // Formularios que ya reservan un espacio superior
        if (
            configuracion.tipoMensaje
            ===
            "espacio"
        ) {
            const espacio =
                formulario.querySelector(
                    ".espacio-mensaje"
                );

            if (espacio) {
                Array.from(
                    espacio.children
                )
                .forEach(
                    hijo => {
                        if (
                            hijo.getAttribute(
                                ATRIBUTO_MENSAJE_CLIENTE
                            )
                            !==
                            "true"
                        ) {
                            hijo.style.display =
                                "none";
                        }
                    }
                );
            }

            mensaje =
                crearMensajeEnEspacio(
                    formulario
                );
        }
        else {
            mensaje =
                crearMensajeFormulario(
                    formulario
                );
        }

        mensaje.textContent =
            texto;

        mensaje.style.display =
            "flex";

        mensaje.dataset.validacionCliente =
            "true";
    }

    // =========================================================
    // OCULTAR MENSAJE
    // =========================================================

    function ocultarMensaje(
        formulario,
        configuracion
    ) {
        if (
            configuracion.tipoMensaje
            ===
            "movimiento"
        ) {
            const mensaje =
                document.getElementById(
                    "mensajeErrorMovimiento"
                );

            if (
                mensaje
                &&
                mensaje.dataset.validacionCliente
                ===
                "true"
            ) {
                mensaje.style.display =
                    "none";

                mensaje.textContent =
                    "";

                delete mensaje.dataset.validacionCliente;
            }

            return;
        }

        if (
            configuracion.tipoMensaje
            ===
            "rol"
        ) {
            const elementos =
                obtenerMensajeRol();

            if (
                elementos
                &&
                elementos.contenedor.dataset.validacionCliente
                ===
                "true"
            ) {
                elementos.texto.innerText =
                    "";

                elementos.contenedor.className =
                    "mensaje-error mensaje-oculto";

                elementos.contenedor.style.visibility =
                    "hidden";

                delete elementos.contenedor.dataset.validacionCliente;
            }

            return;
        }

        const mensaje =
            formulario.querySelector(
                "["
                +
                ATRIBUTO_MENSAJE_CLIENTE
                +
                '="true"]'
            );

        if (mensaje) {
            mensaje.style.display =
                "none";

            mensaje.textContent =
                "";
        }
    }

    // =========================================================
    // BUSCAR PRIMER ERROR
    // =========================================================

    function encontrarPrimerError(
        formulario,
        configuracion
    ) {
        for (
            const regla
            of
            configuracion.campos
        ) {
            if (
                regla.cuando
                &&
                !regla.cuando(
                    formulario
                )
            ) {
                continue;
            }

            const campo =
                formulario.querySelector(
                    regla.selector
                );

            if (!campo) {
                continue;
            }

            let esValido =
                true;

            try {
                esValido =
                    regla.validar(
                        campo,
                        formulario
                    );
            }
            catch {
                esValido =
                    false;
            }

            if (!esValido) {
                return {
                    campo:
                        campo,

                    mensaje:
                        regla.mensaje
                };
            }
        }

        return null;
    }

    // =========================================================
    // VALIDACIÓN EN CALIENTE - CREAR USUARIO
    // =========================================================

    function obtenerTemporizadoresUnicidad(
        formulario
    ) {
        let temporizadores =
            temporizadoresUnicidad.get(
                formulario
            );

        if (!temporizadores) {
            temporizadores = {
                rut: null,
                email: null,
                nombreUsuario: null
            };

            temporizadoresUnicidad.set(
                formulario,
                temporizadores
            );
        }

        return temporizadores;
    }

    function esFormularioCrearUsuario(
        formulario
    ) {
        return obtenerAccionFormulario(
            formulario
        ).endsWith(
            "/home/nuevousuario"
        );
    }

    function crearUrlAccionHome(
        formulario,
        accion,
        nombreParametro,
        valor
    ) {
        const url =
            new URL(
                formulario.action,
                window.location.href
            );

        /*
            NuevoUsuario pasa a:

            VerificarRut
            VerificarCorreo
            VerificarNombreUsuario
        */

        url.pathname =
            url.pathname.replace(
                /\/[^/]+\/?$/,
                "/" + accion
            );

        url.search =
            "";

        url.hash =
            "";

        url.searchParams.set(
            nombreParametro,
            valor
        );

        return url.toString();
    }

    function mostrarDuplicadoEnCaliente(
        formulario,
        campo,
        mensajeTexto
    ) {
        const configuracion =
            obtenerConfiguracion(
                formulario
            );

        if (!configuracion) {
            return;
        }

        // Mantener un solo error visible.
        limpiarBordesFormulario(
            formulario
        );

        ocultarMensaje(
            formulario,
            configuracion
        );

        campo.classList.add(
            CLASE_ERROR
        );

        erroresActuales.set(
            formulario,
            campo
        );

        mostrarMensaje(
            formulario,
            configuracion,
            mensajeTexto
        );
    }

    function limpiarDuplicadoEnCaliente(
        formulario,
        campo
    ) {
        campo.classList.remove(
            CLASE_ERROR
        );

        const campoActual =
            erroresActuales.get(
                formulario
            );

        if (
            campoActual
            !==
            campo
        ) {
            return;
        }

        const configuracion =
            obtenerConfiguracion(
                formulario
            );

        if (configuracion) {
            ocultarMensaje(
                formulario,
                configuracion
            );
        }

        erroresActuales.delete(
            formulario
        );
    }

    async function consultarDuplicado(
        formulario,
        campo,
        accion,
        nombreParametro,
        valorConsultado,
        mensajeDuplicado
    ) {
        try {
            const url =
                crearUrlAccionHome(
                    formulario,
                    accion,
                    nombreParametro,
                    valorConsultado
                );

            const respuesta =
                await fetch(
                    url,
                    {
                        method:
                            "GET",

                        headers:
                            {
                                "X-Requested-With":
                                    "XMLHttpRequest"
                            }
                    }
                );

            if (!respuesta.ok) {
                return;
            }

            const datos =
                await respuesta.json();

            /*
                Si el usuario siguió escribiendo mientras
                esperábamos la respuesta, esta respuesta
                antigua ya no sirve.
            */

            if (
                textoCampo(
                    campo
                )
                !==
                valorConsultado
            ) {
                return;
            }

            if (
                datos.existe
                ===
                true
            ) {
                campo.dataset.duplicado =
                    "true";

                mostrarDuplicadoEnCaliente(
                    formulario,
                    campo,
                    mensajeDuplicado
                );
            }
            else {
                campo.dataset.duplicado =
                    "false";

                limpiarDuplicadoEnCaliente(
                    formulario,
                    campo
                );
            }
        }
        catch (error) {
            /*
                Si la consulta en caliente falla por red,
                no bloqueamos el formulario.

                El servidor sigue siendo la protección
                definitiva antes de insertar.
            */

            console.error(
                "No se pudo realizar la validación en caliente:",
                error
            );
        }
    }

    function programarValidacionUnicidad(
        formulario,
        campo
    ) {
        if (
            !esFormularioCrearUsuario(
                formulario
            )
        ) {
            return;
        }

        const temporizadores =
            obtenerTemporizadoresUnicidad(
                formulario
            );

        const idCampo =
            campo.id;

        /*
            Apenas el usuario cambia el valor, el valor anterior
            deja de considerarse duplicado.
        */

        campo.dataset.duplicado =
            "false";

        // ---------------------------------------------------------
        // RUT
        // ---------------------------------------------------------

        if (
            idCampo
            ===
            "rut"
        ) {
            clearTimeout(
                temporizadores.rut
            );

            const valor =
                textoCampo(
                    campo
                );

            /*
                Evita consultar cuando el RUT todavía está
                demasiado incompleto.
            */

            if (
                valor.length
                <
                9
            ) {
                return;
            }

            temporizadores.rut =
                setTimeout(
                    () => {
                        consultarDuplicado(
                            formulario,
                            campo,
                            "VerificarRut",
                            "rut",
                            valor,
                            "El RUT ingresado ya está registrado."
                        );
                    },
                    450
                );

            return;
        }

        // ---------------------------------------------------------
        // CORREO
        // ---------------------------------------------------------

        if (
            idCampo
            ===
            "email"
        ) {
            clearTimeout(
                temporizadores.email
            );

            const valor =
                textoCampo(
                    campo
                );

            if (
                !correoValido(
                    valor
                )
            ) {
                return;
            }

            temporizadores.email =
                setTimeout(
                    () => {
                        consultarDuplicado(
                            formulario,
                            campo,
                            "VerificarCorreo",
                            "correo",
                            valor,
                            "El correo electrónico ingresado ya está registrado."
                        );
                    },
                    450
                );

            return;
        }

        // ---------------------------------------------------------
        // NOMBRE DE USUARIO
        // ---------------------------------------------------------

        if (
            idCampo
            ===
            "nombre_usuario"
        ) {
            clearTimeout(
                temporizadores.nombreUsuario
            );

            const valor =
                textoCampo(
                    campo
                );

            if (
                valor.length
                <
                3
            ) {
                return;
            }

            temporizadores.nombreUsuario =
                setTimeout(
                    () => {
                        consultarDuplicado(
                            formulario,
                            campo,
                            "VerificarNombreUsuario",
                            "nombreUsuario",
                            valor,
                            "El nombre de usuario ingresado ya está en uso."
                        );
                    },
                    450
                );
        }
    }

    function validarUnicidadCrearUsuarioEnCaliente(
        evento
    ) {
        const campo =
            evento.target;

        if (
            !(
                campo
                instanceof
                HTMLInputElement
            )
        ) {
            return;
        }

        if (
            campo.id
            !==
            "rut"
            &&
            campo.id
            !==
            "email"
            &&
            campo.id
            !==
            "nombre_usuario"
        ) {
            return;
        }

        const formulario =
            campo.form;

        if (!formulario) {
            return;
        }

        programarValidacionUnicidad(
            formulario,
            campo
        );
    }

    // =========================================================
    // SUBMIT GLOBAL
    // =========================================================
    //
    // Capture = true permite validar antes de los onsubmit que
    // ya existen en Crear Usuario, Producto, Lote, Rol, etc.
    //
    // =========================================================

    document.addEventListener(
        "submit",
        function (
            evento
        ) {
            const formulario =
                evento.target;

            if (
                !(
                    formulario
                    instanceof
                    HTMLFormElement
                )
            ) {
                return;
            }

            const configuracion =
                obtenerConfiguracion(
                    formulario
                );

            if (!configuracion) {
                return;
            }

            limpiarBordesFormulario(
                formulario
            );

            ocultarMensaje(
                formulario,
                configuracion
            );

            const error =
                encontrarPrimerError(
                    formulario,
                    configuracion
                );

            if (!error) {
                erroresActuales.delete(
                    formulario
                );

                return;
            }

            evento.preventDefault();

            evento.stopImmediatePropagation();

            error.campo.classList.add(
                CLASE_ERROR
            );

            erroresActuales.set(
                formulario,
                error.campo
            );

            mostrarMensaje(
                formulario,
                configuracion,
                error.mensaje
            );

            setTimeout(
                () => {
                    error.campo.focus();
                },
                10
            );
        },
        true
    );

    // =========================================================
    // QUITAR ERROR CUANDO EL USUARIO EMPIEZA A CORREGIR
    // =========================================================

    function corregirCampo(
        evento
    ) {
        const campo =
            evento.target;

        if (
            !(
                campo
                instanceof
                HTMLInputElement
            )
            &&
            !(
                campo
                instanceof
                HTMLSelectElement
            )
            &&
            !(
                campo
                instanceof
                HTMLTextAreaElement
            )
        ) {
            return;
        }

        const formulario =
            campo.form;

        if (!formulario) {
            return;
        }

        const configuracion =
            obtenerConfiguracion(
                formulario
            );

        if (!configuracion) {
            return;
        }

        const campoActual =
            erroresActuales.get(
                formulario
            );

        if (
            campoActual
            ===
            campo
        ) {
            campo.classList.remove(
                CLASE_ERROR
            );

            ocultarMensaje(
                formulario,
                configuracion
            );

            erroresActuales.delete(
                formulario
            );
        }
    }

    // =========================================================
    // EVENTOS GLOBALES
    // =========================================================

    document.addEventListener(
        "input",
        corregirCampo,
        true
    );

    /*
        Esta escucha va en fase normal (bubble), no en capture.

        Así, en el campo RUT primero alcanza a ejecutarse el
        oninput de CrearUsuario.cshtml que agrega el guion y
        recién después consultamos el RUT ya formateado.
    */

    document.addEventListener(
        "input",
        validarUnicidadCrearUsuarioEnCaliente
    );

    document.addEventListener(
        "change",
        corregirCampo,
        true
    );
})();