# Datos de Prueba y Credenciales de Acceso

Este directorio contiene scripts SQL para poblar, verificar y vaciar los datos de prueba del sistema.

> [!NOTE]
> El script de limpieza **NO elimina ninguna tabla** (no hace `DROP TABLE`). Únicamente vacía los datos (`DELETE FROM`) de las tablas involucradas en la inserción para dejarlas limpias y listas para nuevas pruebas.

---

## 🔑 Usuarios de Prueba para el Frontend

Todos los usuarios tienen la misma contraseña simple para facilitar las pruebas locales:

- **Contraseña universal:** `Password123!`

| Rol | Nombre | Username | Email | Contraseña |
|---|---|---|---|---|
| **Administrador** | Admin General | `admin` | `admin@veterinaria.com` | `Password123!` |
| **Veterinario** | Dr. Carlos Mendoza | `veterinario` | `veterinario@veterinaria.com` | `Password123!` |
| **Recepcionista** | Maria Recepcion | `recepcionista` | `recepcionista@veterinaria.com` | `Password123!` |
| **Auxiliar** | Pedro Auxiliar | `auxiliar` | `auxiliar@veterinaria.com` | `Password123!` |
| **Cliente** | Ana Gomez | `cliente` | `cliente@veterinaria.com` | `Password123!` |

---

## 🚀 Comandos Rápidos de Consola (PowerShell)

### 1. Limpiar datos previos (solo vacía filas de las tablas usadas en pruebas)
```powershell
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\test_seeds\limpieza_total.sql'
```

### 2. Insertar los datos de prueba
```powershell
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\test_seeds\insercion_datos.sql'
```

### 3. Verificar los datos creados
```powershell
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\test_seeds\revision_datos.sql'
```

---

## 📋 Tablas Afectadas en la Inserción y Limpieza

1. `SPECIALTIES`
2. `SPECIES`
3. `RACES`
4. `USERS`
5. `USER_ACCOUNTS`
6. `USER_CREDENTIALS`
7. `USER_PERMISSIONS`
8. `USER_TOKENS`
9. `VETERINARIANS`
10. `CLIENTS`
11. `PETS`
12. `CLIENTS_PETS`
13. `TYPE_SERVICES`
14. `SERVICES`
15. `DIAGNOSTICS`
16. `AVAILABILITIES`
17. `APPOINTMENTS`
18. `MEDICAL_RECORDS`
19. `VACCINATIONS`

> Las tablas `MODULES`, `ROLES`, `ROLE_PERMISSIONS`, `STATUS_APPOINTMENTS` y tablas de chat/conversaciones **nunca se tocan**.
