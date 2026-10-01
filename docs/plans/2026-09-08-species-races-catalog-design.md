# Diseño: catálogo de razas por especie

## Diagnóstico

El agente acepta correctamente la especie y avanza al paso de raza. El backend responde una lista vacía porque el seeder de catálogos no crea filas en `RACES`. Además, `RACES` no tiene una relación con `SPECIES`, por lo que poblar una lista plana mezclaría razas incompatibles.

## Contrato aprobado

- Modo: `evolve-module`.
- Agregado dependiente: `RaceEntity` / tabla `RACES`.
- Relación: `Species 1:N Races` unidireccional.
- FK propietaria: `RACES.SPECIES_ID`, `VARCHAR2(36)`, requerida.
- Eliminación: `Restrict`.
- Unicidad: nombre de raza por especie mediante `(SPECIES_ID, NAME)`.
- API: `GET /api/races?speciesId=<uuid>`; el filtro es opcional para conservar el listado general.
- Escritura: `POST` y `PUT` de razas requieren `speciesId` y validan que exista.
- Respuesta: cada raza expone `id`, `name` y `speciesId`.
- Consumidor: el agente solicita las razas con el ID de la especie seleccionada.

## Migración y datos existentes

La migración añade primero una columna nullable, garantiza que exista la especie determinística `Otro`, asigna a `Otro` cualquier raza heredada sin clasificación, convierte la columna en requerida y crea índice y FK. No se eliminan tablas ni filas.

El seed idempotente crea inicialmente:

- Perro: Mestizo, Labrador Retriever, Golden Retriever, Bulldog y Poodle.
- Gato: Mestizo, Siamés, Persa y Maine Coon.
- Otro: No especificada.

Los `MERGE` se identifican por especie y nombre normalizado, por lo que aplicar el seed varias veces no duplica filas.

## Flujo del agente

Al aceptar una especie, el borrador conserva `species_id`. En el paso de raza, `PetProfileGateway.list_races(species_id, token)` llama a `/api/races?speciesId=...`. Si no existen razas para la especie, el bot informa que el catálogo no está configurado y permite cancelar; no intenta registrar una mascota con IDs inventados.

## Compatibilidad y seguridad

- Cambio aditivo de esquema con backfill; riesgo de bloqueo breve durante la migración.
- `POST` y `PUT /api/races` cambian su cuerpo y requieren `speciesId`.
- `GET /api/races` sin filtro conserva el comportamiento anterior.
- La autorización existente se mantiene sin cambios.
- No se registran JWT, cadenas de conexión ni datos personales.

## Verificación

- Pruebas enfocadas de dominio, consulta filtrada, DTO/controlador y modelo EF.
- Pruebas del adaptador y formulario de registro del agente.
- Compilación Release de la solución.
- Revisión de la migración y ausencia de cambios pendientes en el modelo.
- Aplicación únicamente sobre Oracle local autorizado y verificación SQL de especies, razas y FK.
