# Seeds de catálogo (roles, módulos, permisos)

Orden de ejecución (PowerShell):

```powershell
$env:NLS_LANG="SPANISH_SPAIN.AL32UTF8"
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\seeds\roles_seed.sql'
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\seeds\modules_seed.sql'
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\seeds\role_permissions_seed.sql'
& 'C:\app\ESSA\product\26ai\dbhomeFree\bin\sqlplus.exe' 'SYSTEM@//localhost:1522/FREEPDB1' '@database\seeds\chat_conversation_catalogs_seed.sql'
```

## Verificar (DB limpia)

```sql
SELECT COUNT(*) AS ROLES_COUNT FROM ROLES; -- esperado: 5
SELECT ROLE_ID, NAME FROM ROLES ORDER BY NAME;
-- Debe existir: Cliente / 77777777-7777-7777-7777-777777777777

SELECT COUNT(*) AS CLIENT_PERMS
FROM ROLE_PERMISSIONS
WHERE ROLE_ID = '77777777-7777-7777-7777-777777777777';
-- esperado: 4 (Clientes, Mascotas, Citas, Historiales Clínicos — solo View)
```

Si falta el rol Cliente, `role_permissions_seed.sql` falla por FK (`ORA-02291`).
No crear el rol solo a mano en una BD: siempre vía `roles_seed.sql`.
