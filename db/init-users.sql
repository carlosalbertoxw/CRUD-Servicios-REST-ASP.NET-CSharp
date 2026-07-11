-- Mínimo privilegio: la aplicación usa 'notes_app' (solo DML, sin DDL);
-- 'notes_user' (creado por las variables de entorno del contenedor, con
-- todos los privilegios sobre la BD) queda reservado para las migraciones.
-- Este script lo ejecuta el contenedor como root la primera vez que se
-- crea el volumen (docker-entrypoint-initdb.d).
CREATE USER IF NOT EXISTS 'notes_app'@'%' IDENTIFIED BY 'notes_app_password';
GRANT SELECT, INSERT, UPDATE, DELETE ON `notes`.* TO 'notes_app'@'%';
