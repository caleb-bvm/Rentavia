// Confirmación antes de enviar formularios sensibles (eliminar, cancelar, etc.)
document.querySelectorAll('form[data-confirm]').forEach(form => {
    form.addEventListener('submit', event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});

// Color de los chips de estado según su texto
const tonos = {
    success: ['activo', 'activa', 'disponible', 'habilitado', 'confirmada', 'cerrado', 'finalizado'],
    info: ['abierto', 'en curso', 'programado', 'convertida'],
    warning: ['fueradeservicio', 'pendiente'],
    danger: ['atrasado', 'cancelada', 'bloqueado', 'inactivo', 'inactiva', 'retirado']
};

document.querySelectorAll('.state').forEach(chip => {
    const texto = chip.textContent.trim().toLowerCase();
    const tono = Object.keys(tonos).find(t => tonos[t].includes(texto));
    if (tono) chip.classList.add('tone-' + tono);
});

// Snackbar: los mensajes se ocultan solos después de unos segundos
document.querySelectorAll('.alert').forEach(alerta => {
    setTimeout(() => {
        alerta.classList.add('is-hiding');
        setTimeout(() => alerta.remove(), 300);
    }, 5000);
});

// Tema claro u oscuro: sigue al sistema hasta que el usuario elige uno.
// Igual que en site.css, el oscuro es el predeterminado si el sistema no prefiere claro.
const raiz = document.documentElement;
const esquemaClaro = window.matchMedia('(prefers-color-scheme: light)');
const temaActual = () => raiz.dataset.theme || (esquemaClaro.matches ? 'light' : 'dark');

function pintarTema() {
    const oscuro = temaActual() === 'dark';
    document.querySelectorAll('[data-theme-toggle]').forEach(boton => {
        // El ícono anuncia el esquema al que se cambia
        boton.querySelector('[data-icono="sol"]').hidden = !oscuro;
        boton.querySelector('[data-icono="luna"]').hidden = oscuro;
        boton.setAttribute('aria-label', oscuro ? 'Activar tema claro' : 'Activar tema oscuro');
        boton.title = boton.getAttribute('aria-label');
    });
    document.querySelector('meta[name="theme-color"]')
        ?.setAttribute('content', getComputedStyle(raiz).getPropertyValue('--surface').trim());
}

document.querySelectorAll('[data-theme-toggle]').forEach(boton => {
    boton.addEventListener('click', () => {
        raiz.dataset.theme = temaActual() === 'dark' ? 'light' : 'dark';
        try { localStorage.setItem('rentavia-tema', raiz.dataset.theme); } catch (e) { }
        pintarTema();
    });
});

esquemaClaro.addEventListener('change', pintarTema);
pintarTema();

// Paneles de filtros: resumen del rango de fechas, corrección del fin y atajos
const DIA = 24 * 60 * 60 * 1000;
const dosDigitos = n => String(n).padStart(2, '0');
const plural = (n, uno, varios) => `${n} ${n === 1 ? uno : varios}`;

// date ("yyyy-MM-dd") y datetime-local ("yyyy-MM-ddTHH:mm") se leen como hora local
function leerFecha(input) {
    if (!input.value) return null;
    const [fecha, hora = '00:00'] = input.value.split('T');
    const [anio, mes, dia] = fecha.split('-').map(Number);
    const [h, min] = hora.split(':').map(Number);
    return new Date(anio, mes - 1, dia, h, min);
}

function formatearFecha(fecha, soloFecha) {
    const dia = `${fecha.getFullYear()}-${dosDigitos(fecha.getMonth() + 1)}-${dosDigitos(fecha.getDate())}`;
    return soloFecha ? dia : `${dia}T${dosDigitos(fecha.getHours())}:${dosDigitos(fecha.getMinutes())}`;
}

document.querySelectorAll('form[data-rango-fechas]').forEach(form => {
    const inicio = form.querySelector('[data-rango-inicio]');
    const fin = form.querySelector('[data-rango-fin]');
    const resumen = form.querySelector('[data-duracion]');
    const soloFecha = inicio.type === 'date';
    const maxDias = Number(form.dataset.rangoMaxDias) || 0;
    // Con fechas, el fin puede coincidir con el inicio; con fecha y hora debe ser posterior
    const invertido = (a, b) => soloFecha ? b < a : b <= a;

    function actualizar() {
        fin.min = inicio.value;
        marcarAtajo();
        if (!resumen) return;
        const a = leerFecha(inicio), b = leerFecha(fin);
        let texto, error = true;
        if (!a || !b) texto = 'Completa ambas fechas.';
        else if (invertido(a, b)) texto = soloFecha ? 'La fecha final no puede ser anterior a la inicial.' : 'La devolución debe ser posterior a la recogida.';
        else if (soloFecha) {
            const dias = Math.round((b - a) / DIA) + 1;
            error = maxDias > 0 && dias > maxDias;
            texto = error ? `El período no puede superar ${maxDias} días.` : `Período de <strong>${plural(dias, 'día', 'días')}</strong>`;
        } else {
            // La tarifa se cobra por períodos de 24 horas redondeados hacia arriba
            error = false;
            const minutos = Math.round((b - a) / 60000);
            const horas = Math.floor(minutos / 60), resto = minutos % 60;
            const duracion = [horas && `${horas} h`, resto && `${resto} min`].filter(Boolean).join(' ');
            texto = `Se cobran <strong>${plural(Math.ceil((b - a) / DIA), 'día', 'días')}</strong> de tarifa (${duracion})`;
        }
        resumen.classList.toggle('is-error', error);
        resumen.innerHTML = `<span class="icon" aria-hidden="true">${error ? 'error' : 'schedule'}</span><span>${texto}</span>`;
    }

    // Si la recogida pasa la devolución, la devolución se mueve en vez de dejar el rango inválido
    inicio.addEventListener('change', () => {
        const a = leerFecha(inicio), b = leerFecha(fin);
        if (a && b && invertido(a, b)) fin.value = formatearFecha(new Date(a.getTime() + (soloFecha ? 0 : DIA)), soloFecha);
        actualizar();
    });
    fin.addEventListener('change', actualizar);
    inicio.addEventListener('input', actualizar);
    fin.addEventListener('input', actualizar);

    // Atajos de período: cada chip fija el rango y consulta de inmediato
    const atajos = [...form.querySelectorAll('[data-preset]')];
    function rangoDe(preset) {
        const hoy = new Date();
        hoy.setHours(0, 0, 0, 0);
        const desde = new Date(hoy);
        if (preset === 'mes') desde.setDate(1);
        else if (preset === 'anio') desde.setMonth(0, 1);
        else desde.setDate(hoy.getDate() - (Number(preset) - 1));
        return [formatearFecha(desde, true), formatearFecha(hoy, true)];
    }

    function marcarAtajo() {
        atajos.forEach(chip => {
            const [desde, hasta] = rangoDe(chip.dataset.preset);
            const elegido = inicio.value === desde && fin.value === hasta;
            chip.classList.toggle('is-selected', elegido);
            chip.setAttribute('aria-pressed', elegido);
        });
    }

    atajos.forEach(chip => chip.addEventListener('click', () => {
        [inicio.value, fin.value] = rangoDe(chip.dataset.preset);
        actualizar();
        form.requestSubmit ? form.requestSubmit() : form.submit();
    }));

    actualizar();
});

// Contador de caracteres para campos con maxlength
document.querySelectorAll('[data-contador-para]').forEach(contador => {
    const campo = document.getElementById(contador.dataset.contadorPara);
    if (!campo) return;
    const pintar = () => {
        contador.textContent = `${campo.value.length} / ${campo.maxLength}`;
        contador.parentElement.classList.toggle('is-over', campo.value.length >= campo.maxLength);
    };
    campo.addEventListener('input', pintar);
    pintar();
});

// Menú lateral en pantallas pequeñas
document.querySelectorAll('[data-nav-toggle]').forEach(boton => {
    boton.addEventListener('click', () => document.body.classList.toggle('nav-open'));
});

// ---------- Formularios de captura ----------

// Mostrar u ocultar la contraseña
document.querySelectorAll('.rv-reveal').forEach(boton => {
    const campo = boton.parentElement.querySelector('input');
    boton.addEventListener('click', () => {
        const visible = campo.type === 'text';
        campo.type = visible ? 'password' : 'text';
        boton.setAttribute('aria-pressed', String(!visible));
        boton.setAttribute('aria-label', visible ? 'Mostrar contraseña' : 'Ocultar contraseña');
    });
});

// Botones − y + de los contadores; los límites salen del [Range] del modelo
document.querySelectorAll('.rv-stepper').forEach(contador => {
    const campo = contador.querySelector('input');
    const botones = contador.querySelectorAll('[data-step]');
    if (!campo.min && campo.dataset.valRangeMin) campo.min = campo.dataset.valRangeMin;
    if (!campo.max && campo.dataset.valRangeMax) campo.max = campo.dataset.valRangeMax;
    const actualizar = () => botones.forEach(boton => {
        const valor = Number(campo.value);
        boton.disabled = boton.dataset.step < 0
            ? campo.min !== '' && valor <= Number(campo.min)
            : campo.max !== '' && valor >= Number(campo.max);
    });
    botones.forEach(boton => boton.addEventListener('click', () => {
        if (boton.dataset.step < 0) campo.stepDown(); else campo.stepUp();
        campo.dispatchEvent(new Event('change', { bubbles: true }));
        actualizar();
    }));
    campo.addEventListener('input', actualizar);
    actualizar();
});

// Deslizador de porcentaje: pinta la parte llena y muestra el valor
document.querySelectorAll('.rv-range').forEach(rango => {
    const campo = rango.querySelector('input');
    const salida = rango.querySelector('output');
    const pintar = () => {
        const porcentaje = (campo.value - campo.min) / (campo.max - campo.min) * 100;
        campo.style.setProperty('--valor', porcentaje + '%');
        salida.textContent = campo.value + '%';
    };
    campo.addEventListener('input', pintar);
    pintar();
});

// Zona de fotografía: resalta al arrastrar y muestra una vista previa
document.querySelectorAll('.rv-drop').forEach(zona => {
    const campo = zona.querySelector('input[type="file"]');
    const titulo = zona.querySelector('strong');
    const detalle = zona.querySelector('small');
    const vista = zona.querySelector('img');
    const textoInicial = [titulo.textContent, detalle.textContent];
    let url = null;
    ['dragenter', 'dragover'].forEach(tipo => zona.addEventListener(tipo, () => zona.classList.add('is-over')));
    ['dragleave', 'drop'].forEach(tipo => zona.addEventListener(tipo, () => zona.classList.remove('is-over')));
    campo.addEventListener('change', () => {
        const archivo = campo.files[0];
        if (url) URL.revokeObjectURL(url);
        zona.classList.toggle('has-file', !!archivo);
        if (!archivo) {
            [titulo.textContent, detalle.textContent] = textoInicial;
            return;
        }
        url = URL.createObjectURL(archivo);
        vista.src = url;
        titulo.textContent = archivo.name;
        detalle.textContent = (archivo.size / 1048576).toFixed(2) + ' MB · haz clic para cambiarla';
    });
});

// Evita el doble envío: el botón principal muestra que se está guardando
document.querySelectorAll('.form-panel form').forEach(form => {
    form.addEventListener('submit', evento => {
        // Se espera a que la validación del cliente decida si el envío sigue
        setTimeout(() => {
            if (evento.defaultPrevented) return;
            form.querySelector('.form-foot .btn-primary').classList.add('is-loading');
        });
    });
});
