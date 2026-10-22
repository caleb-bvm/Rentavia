# Manual de usuario de Rentavía

Rentavía es el sistema interno de la agencia. Lo usan dos tipos de cuenta:

| Rol | Puede hacer |
| --- | --- |
| Empleado | Gestionar clientes y vehículos, consultar disponibilidad, reservar, entregar, recibir, registrar pagos, programar mantenimiento y usar el recomendador |
| Administrador | Todo lo anterior, más categorías, sucursales, eliminaciones, usuarios, reportes y auditoría |

Los clientes de la agencia no ingresan al sistema; el personal registra sus datos.

## Acceso

Abre la dirección que te indique el responsable (por ejemplo `http://localhost:8080`) e ingresa tu correo y contraseña. Después de cinco intentos fallidos la cuenta se bloquea durante diez minutos.

![Pantalla de acceso](../evidencias/ui/01-acceso.png)

El menú lateral contiene Inicio, Disponibilidad, Vehículos, Clientes, Reservas, Alquileres, Mantenimiento, Recomendador IA, Categorías y Sucursales. Los administradores ven además Usuarios, Reportes y Auditoría. **Cerrar sesión** termina el acceso. Si intentas abrir una sección sin permiso, el sistema muestra un aviso.

## Panel de inicio

Muestra vehículos activos, clientes activos, reservas confirmadas, alquileres abiertos, ingresos registrados y las próximas devoluciones. Las cifras se calculan con los datos de la base en ese momento.

![Panel de inicio](../evidencias/ui/02-panel.png)

## Catálogos

### Categorías y sucursales

Solo el administrador las crea y edita. Una sucursal inactiva deja de ofrecer sus vehículos en nuevas reservas. Una categoría o sucursal que ya tiene vehículos no se puede eliminar; en el caso de la sucursal, se desactiva desde Editar.

### Vehículos

En Vehículos, **Nuevo vehículo** abre el formulario con placa, marca, modelo, año, categoría, sucursal, pasajeros, maletas, kilometraje, transmisión, estado y tarifa diaria. También se puede subir una fotografía PNG o JPEG de hasta 2 MB.

- La placa se guarda en mayúsculas y no puede repetirse.
- El kilometraje no puede bajar al editar.
- Para ofrecer un vehículo en reservas debe estar en estado Activo y tener tarifa mayor que cero.
- Cambiar la tarifa no altera las reservas ya creadas.

Si un vehículo tiene reservas o mantenimientos, al eliminarlo pasa a estado Retirado y conserva su historial.

### Clientes

Registra nombre, apellido, tipo y número de documento, licencia, vencimiento de la licencia, teléfono y correo opcional. No se permiten dos clientes con el mismo tipo y número de documento. Un cliente inactivo, o cuya licencia vence antes de terminar la reserva, no puede reservar. Si el cliente tiene reservas, al eliminarlo queda desactivado.

Si dos personas editan el mismo registro a la vez, la segunda recibe un aviso para recargar la página antes de guardar.

## Disponibilidad y reservas

1. En Disponibilidad indica inicio, fin, pasajeros y maletas. Categoría y sucursal son filtros opcionales.
2. Pulsa Buscar. La lista solo muestra vehículos sin reservas, alquileres ni mantenimientos en ese período.
3. Elige un vehículo y pulsa Reservar.
4. Selecciona el cliente, revisa las fechas y guarda.

![Consulta de disponibilidad](../evidencias/ui/03-disponibilidad.png)

Al guardar, el sistema vuelve a revisar la agenda. Si otro empleado reservó ese vehículo en el mismo período, la reserva se rechaza con un mensaje.

El cobro se calcula por períodos de 24 horas, redondeados hacia arriba, en dólares y con horario UTC-6. La reserva guarda la tarifa diaria vigente en ese momento.

En Reservas se ven el estado, las fechas y el total acordado. Editar permite cambiar cliente y fechas manteniendo el vehículo y la tarifa original. Para cambiar de vehículo hay que cancelar y crear otra reserva. Cancelar libera la agenda y conserva el registro.

![Listado de reservas](../evidencias/ui/04-reservas.png)

## Entrega, devolución y pagos

### Entregar

Desde una reserva confirmada, pulsa Entregar. Se puede hacer desde cinco minutos antes del inicio y mientras la reserva siga vigente. Registra kilometraje, porcentaje de combustible y observaciones de la inspección. La entrega crea el alquiler y mantiene ocupada la misma agenda de la reserva.

### Detalle del alquiler

Muestra cliente, vehículo, período, tarifa, inspecciones, total, pagos y saldo. El botón Imprimir genera una versión para papel.

### Registrar pagos

Indica monto, método (Efectivo, Transferencia o Tarjeta) y una referencia opcional. El monto no puede superar el saldo pendiente. Es un registro interno de la agencia; el sistema no cobra a tarjetas ni se conecta con bancos.

### Devolver

Registra el kilometraje final (no menor al de entrega), combustible, observaciones y, si corresponde, un cargo adicional con su concepto. Al confirmar, el alquiler se cierra, se actualiza el kilometraje del vehículo y se libera la agenda. El total final es el estimado de la reserva más el cargo. Si queda saldo, sigue visible en el detalle y se puede pagar después.

Un alquiler cuya fecha de fin ya pasó aparece como Atrasado en el panel de inicio, su detalle muestra un aviso y el vehículo no se ofrece en nuevas operaciones hasta registrar la devolución.

## Mantenimiento

Elige el vehículo, el tipo (Preventivo o Correctivo), las fechas, una descripción y el costo. Si el vehículo ya tiene reservas en ese período, el sistema rechaza el mantenimiento; primero hay que reprogramar o cancelar esas reservas. **Finalizar** libera la agenda y el registro queda en el historial.

## Recomendador IA

El recomendador ayuda a elegir un vehículo según el viaje.

1. Abre Recomendador IA.
2. Indica fechas, pasajeros, maletas y presupuesto diario.
3. Describe en texto libre lo que necesita el cliente, por ejemplo: "viaje a la playa con equipaje de surf, prefiere automático". No escribas nombres, documentos ni datos personales.
4. Pulsa **Obtener recomendaciones**.

El sistema filtra primero los vehículos disponibles que cumplen capacidad y presupuesto (hasta veinte). OpenAI elige entre ellos hasta tres opciones y explica por qué cada una encaja con lo solicitado.

![Formulario del recomendador](../evidencias/ui/05-recomendador.png)

La pantalla indica el origen del resultado:

- **OpenAI**: la recomendación la generó el modelo de IA.
- **Reglas (sin IA)**: el servicio no estaba disponible y el sistema muestra las opciones más económicas.

Captura por agregar: `docs/evidencias/ui/08-recomendacion-openai.png`, con un resultado de origen OpenAI.

La recomendación no crea reservas. El botón **Elegir vehículo** de cada sugerencia abre el formulario normal de reserva, que vuelve a comprobar la disponibilidad al guardar.

## Reportes, auditoría y usuarios

Estas secciones son solo para administradores.

**Reportes** permite elegir un rango de hasta un año y muestra pagos registrados, reservas creadas, entregas, costo de mantenimiento y el uso de cada vehículo. El uso se mide en horas reales entre entrega y devolución; los alquileres abiertos cuentan hasta el momento de la consulta.

**Auditoría** lista las últimas doscientas operaciones con fecha, usuario, acción y registro afectado.

**Usuarios** permite crear cuentas de Administrador o Empleado y bloquearlas o desbloquearlas. Nadie puede bloquear su propia cuenta. Al bloquear una cuenta, su sesión abierta se cierra en aproximadamente un minuto.

## Uso en el teléfono

La interfaz se adapta a pantallas pequeñas: el menú pasa a la parte superior y los formularios y tarjetas se acomodan en una sola columna.

![Vista móvil](../evidencias/ui/06-movil.png)

## Mensajes frecuentes

| Mensaje | Qué hacer |
| --- | --- |
| El vehículo ya tiene una reserva, alquiler o mantenimiento en ese período | Elige otro vehículo u otras fechas |
| Otro usuario modificó este registro | Recarga la página y vuelve a aplicar el cambio |
| No se pudo guardar. Revisa duplicados | Revisa placa o documento repetidos |
| La categoría o sucursal está referenciada | Desactívala en lugar de eliminarla |
| No tienes permiso | Pide la operación a un administrador |
| Error con un código de referencia | Inténtalo de nuevo y comunica el código al responsable si persiste |
