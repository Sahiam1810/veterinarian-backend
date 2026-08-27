# Diseño: migración de JWT HS256 a RS256

## Objetivo

Mantener al backend .NET como único emisor de JWT y permitir que consumidores
como el agente Python validen los tokens sin conocer material capaz de firmarlos.

## Configuración

El material RSA se entregará exclusivamente mediante variables de entorno:

- `Jwt__PrivateKeyPemBase64`: PEM de clave privada codificado en Base64; solo .NET.
- `Jwt__PublicKeyPemBase64`: PEM de clave pública codificado en Base64; .NET y consumidores.
- `Jwt__KeyId`: identificador no secreto incluido como `kid` en el encabezado JWT.

Se retirará `Jwt__SigningKey`. `.env.example` contendrá únicamente nombres y
valores vacíos, nunca claves reales.

## Emisión y validación

`JwtTokenIssuer` importará la clave privada, firmará mediante RS256 y establecerá
`kid`. La autenticación Bearer importará únicamente la clave pública y mantendrá
la validación actual de issuer, audience, expiración y clock skew. Login, refresh,
claims y tiempos de vida conservarán sus contratos.

La configuración fallará al inicio cuando falte material, el Base64 o PEM sea
inválido, la clave sea menor de 2048 bits, el `kid` esté vacío o las claves no
formen el mismo par.

## Integración posterior con el agente

El agente recibirá solo la clave pública Base64, issuer, audience y el algoritmo
permitido `RS256`. No recibirá la clave privada, no emitirá tokens y no consultará
Oracle para validar una solicitud.

## Pruebas

Las claves utilizadas por las pruebas se generarán en memoria. Se verificará:

- emisión RS256 con `kid` y conservación de claims;
- aceptación con la clave pública correspondiente;
- rechazo de firma diferente, HS256, issuer/audience incorrectos y expiración;
- validación de configuración ausente, inválida, débil o con claves no coincidentes;
- suite completa y compilación Release sin Oracle.
