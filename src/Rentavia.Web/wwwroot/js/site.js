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
