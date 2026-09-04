-- Ejecutor de seeds de producción. No incluye cleanup_seeds.sql ni datos de prueba.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;
SET DEFINE OFF;

@@roles_seed.sql
@@modules_seed.sql
@@role_permissions_seed.sql
@@status_appointments_seed.sql
@@chat_conversation_catalogs_seed.sql
@@chat_runtime_catalogs_seed.sql
@@veterinary_catalogs_seed.sql
@@verify_seeds.sql

EXIT SUCCESS;
