# Guion del video y la presentación

La guía pide un video de 5 a 10 minutos en el que participen los cinco integrantes. Este guion dura unos ocho minutos y reparte las partes según los roles del equipo.

## Video

| Tiempo | Integrante | Qué mostrar |
| --- | --- | --- |
| 0:00 a 0:45 | Josue | El problema: agencias pequeñas que controlan la flota en hojas de cálculo y mensajes, con riesgo de reservar dos veces el mismo vehículo. Objetivo de Rentavía |
| 0:45 a 1:30 | Nathaly | Arquitectura en cuatro capas, EF Core, SQL Server y el modelo de reservas, ocupaciones y alquileres |
| 1:30 a 3:00 | Caleb | Inicio de sesión, panel, navegación, CRUD de clientes y vehículos con validaciones (placa repetida) y vista móvil |
| 3:00 a 4:30 | Cristian | Disponibilidad, reserva, intento de reservar el mismo vehículo en fechas que se cruzan, entrega, pago y devolución |
| 4:30 a 5:45 | Cristian y Caleb | Recomendador: preferencias del viaje, candidatos filtrados, resultado de OpenAI con explicación y paso a la reserva |
| 5:45 a 7:00 | Nathaly | `Iniciar-docker.ps1`, `docker compose ps`, la aplicación en `localhost:8080` y los datos conservados tras reiniciar |
| 7:00 a 8:00 | Jimmy y Josue | Pruebas automatizadas, evidencias, manuales, retos y lecciones aprendidas |

Antes de grabar: cargar los datos iniciales, ocultar `.env` y cualquier contraseña o clave, y comprobar que el recomendador muestre origen **OpenAI**.

## Presentación técnica

La rúbrica pide explicar problema, solución, arquitectura, tecnologías, retos y lecciones. Temas que conviene preparar:

- Cómo se evita la doble reserva: transacción serializable y bloqueo `UPDLOCK, HOLDLOCK` sobre el vehículo antes de revisar la agenda.
- Por qué la reserva guarda su propia tarifa: así un cambio de precio en el catálogo no altera lo acordado con el cliente.
- Qué pasa si OpenAI falla: el sistema valida cada respuesta y, si algo sale mal, muestra opciones por reglas e indica el origen.
- Por qué la IA solo recibe candidatos disponibles: nunca puede sugerir un vehículo ocupado ni inventar uno.
- Por qué Docker usa un servicio `setup`: las migraciones se aplican de forma explícita y no cada vez que arranca la web.
- La propuesta Azure: Container Apps y Azure SQL Database, con costo estimado.

Una forma simple de explicar el flujo: el controlador recibe el formulario, el servicio revisa las reglas, el contexto de EF Core guarda en SQL Server y la vista muestra el resultado.
