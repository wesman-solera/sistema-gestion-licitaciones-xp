/*
 * Guiones de la interfaz del Sistema de Gestion de Licitaciones.
 *
 * Se escribio en JavaScript sin dependencias externas por el requisito 9 del enunciado: la
 * interfaz no puede quedar inutilizable por falta de acceso a una CDN. Ademas, todo lo que
 * ocurre aqui es una mejora sobre una pagina que ya funciona sin guiones: los formularios se
 * envian al servidor, la validacion real vive en el servidor y los alternadores de tema y de
 * moneda son formularios POST. Si JavaScript falla, el sistema sigue siendo usable.
 */
(function () {
    "use strict";

    /**
     * Despliega y oculta el menu de navegacion en pantallas angostas.
     */
    function prepararMenu() {
        var boton = document.querySelector("[data-menu-boton]");
        var navegacion = document.querySelector("[data-menu]");

        if (!boton || !navegacion) {
            return;
        }

        boton.addEventListener("click", function () {
            var abierto = navegacion.classList.toggle("abierta");
            boton.setAttribute("aria-expanded", abierto ? "true" : "false");
        });
    }

    /**
     * Pide confirmacion antes de cualquier eliminacion.
     *
     * El enunciado (seccion 8.9) exige confirmar antes de eliminar. La confirmacion real vive
     * en una pagina propia del servidor; esta es una segunda barrera para los botones que
     * eliminan directamente desde un listado.
     */
    function prepararConfirmaciones() {
        var formularios = document.querySelectorAll("form[data-confirmar]");

        Array.prototype.forEach.call(formularios, function (formulario) {
            formulario.addEventListener("submit", function (evento) {
                var mensaje = formulario.getAttribute("data-confirmar");

                if (!window.confirm(mensaje)) {
                    evento.preventDefault();
                }
            });
        });
    }

    /**
     * Marca visualmente los campos numericos que quedan fuera de rango.
     *
     * No sustituye la validacion del servidor: es una ayuda inmediata para el usuario mientras
     * escribe. El servidor vuelve a validar todo y la base de datos tiene sus propias
     * restricciones CHECK.
     */
    function prepararValidacionInmediata() {
        var campos = document.querySelectorAll("input[type=number][data-minimo]");

        Array.prototype.forEach.call(campos, function (campo) {
            var contenedor = campo.closest(".campo");
            var minimo = parseFloat(campo.getAttribute("data-minimo"));

            function revisar() {
                var valor = parseFloat(campo.value);
                var invalido = campo.value !== "" && (isNaN(valor) || valor < minimo);

                campo.classList.toggle("entrada-invalida", invalido);

                if (!contenedor) {
                    return;
                }

                var aviso = contenedor.querySelector("[data-aviso-inmediato]");

                if (invalido && !aviso) {
                    aviso = document.createElement("span");
                    aviso.className = "campo__error";
                    aviso.setAttribute("data-aviso-inmediato", "");
                    aviso.textContent = "El valor debe ser mayor o igual que " + minimo + ".";
                    contenedor.appendChild(aviso);
                } else if (!invalido && aviso) {
                    aviso.remove();
                }
            }

            campo.addEventListener("input", revisar);
            campo.addEventListener("blur", revisar);
        });
    }

    /**
     * Evita el doble envio de un formulario por doble clic.
     */
    function prepararAntiDobleEnvio() {
        var formularios = document.querySelectorAll("form[data-una-vez]");

        Array.prototype.forEach.call(formularios, function (formulario) {
            formulario.addEventListener("submit", function () {
                var botones = formulario.querySelectorAll("button[type=submit]");

                Array.prototype.forEach.call(botones, function (boton) {
                    // El atributo disabled impediria que el valor del boton viaje en el envio,
                    // asi que se bloquea visualmente sin deshabilitarlo.
                    boton.setAttribute("aria-disabled", "true");
                    boton.style.pointerEvents = "none";
                });
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        prepararMenu();
        prepararConfirmaciones();
        prepararValidacionInmediata();
        prepararAntiDobleEnvio();
    });
})();
