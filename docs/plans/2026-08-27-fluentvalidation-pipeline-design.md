# Diseño: activación del pipeline de FluentValidation

## Problema

La capa Application registra los validadores de FluentValidation, pero no registra
`ValidationBehavior<,>` como comportamiento de MediatR. Por ello, los comandos
pueden llegar a sus handlers sin ejecutar las reglas declaradas.

## Alcance

- Registrar el comportamiento abierto de validación en `AddApplication`.
- Crear `tests/Application.Tests` y agregarlo a la solución.
- Probar el comportamiento mediante `IMediator`, utilizando un comando real de
  Application y una dependencia controlada que no requiera Oracle.
- Confirmar que una solicitud inválida lanza `ValidationException` antes de llegar
  al servicio y que una solicitud válida continúa normalmente.

## Límites

No se modificarán reglas de negocio, controladores, configuración JWT,
persistencia, migraciones ni contratos HTTP. La corrección no requerirá Oracle.

## Verificación

Se observará primero el fallo de la prueba de regresión, se aplicará el registro
mínimo y luego se ejecutarán las pruebas y la compilación completa en Release.
