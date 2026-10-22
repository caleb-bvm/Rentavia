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
