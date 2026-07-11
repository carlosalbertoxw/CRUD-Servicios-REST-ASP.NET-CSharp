-- Endurecimiento de api_keys:
--  * TIMESTAMP -> DATETIME (evita el límite de 2038 y la conversión por zona
--    horaria de sesión; la convención es guardar UTC).
--  * expires_at: keys con caducidad para forzar rotación.
--  * last_used_at: detectar keys en desuso antes de revocarlas.
--  * key_hash CHAR(64) hex -> BINARY(32) (mitad de almacenamiento).
SET time_zone = '+00:00';

ALTER TABLE api_keys
  MODIFY created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  MODIFY revoked_at DATETIME NULL DEFAULT NULL,
  ADD COLUMN expires_at  DATETIME NULL DEFAULT NULL,
  ADD COLUMN last_used_at DATETIME NULL DEFAULT NULL;

ALTER TABLE api_keys
  ADD COLUMN key_hash_bin BINARY(32) NULL AFTER key_hash;

UPDATE api_keys SET key_hash_bin = UNHEX(key_hash);

ALTER TABLE api_keys
  DROP COLUMN key_hash;

ALTER TABLE api_keys
  CHANGE COLUMN key_hash_bin key_hash BINARY(32) NOT NULL;
