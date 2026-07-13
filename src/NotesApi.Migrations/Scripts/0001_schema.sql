-- Esquema completo de la BD (estado final consolidado).
-- Convención: todas las fechas se guardan en UTC con DATETIME (no TIMESTAMP,
-- que se ve afectado por el límite de 2038 y la zona horaria de sesión).
SET time_zone = '+00:00';

-- Las API keys se almacenan hasheadas (SHA-256, BINARY(32)) y con identificador:
-- el cliente envía "X-Api-Key: <key_id>.<secreto>", el servidor busca por
-- key_id y compara el hash del secreto. Esto permite revocar y rotar keys
-- por cliente sin afectar al resto.
CREATE TABLE IF NOT EXISTS api_keys (
  key_id       VARCHAR(64)  NOT NULL,
  key_hash     BINARY(32)   NOT NULL,
  client_name  VARCHAR(100) NOT NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revoked_at   DATETIME     NULL DEFAULT NULL,
  -- expires_at: keys con caducidad para forzar rotación.
  expires_at   DATETIME     NULL DEFAULT NULL,
  -- last_used_at: detectar keys en desuso antes de revocarlas.
  last_used_at DATETIME     NULL DEFAULT NULL,
  PRIMARY KEY (key_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

CREATE TABLE IF NOT EXISTS notes (
  id           INT          NOT NULL AUTO_INCREMENT,
  owner_key_id VARCHAR(64)  NOT NULL,
  title        VARCHAR(250) NOT NULL,
  -- MEDIUMTEXT (16 MB) en lugar de LONGTEXT: la API limita el contenido
  -- a 100.000 caracteres, no tiene sentido permitir 4 GB por fila.
  text         MEDIUMTEXT   NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  -- La FK crea implícitamente el índice por owner_key_id que usa el listado.
  CONSTRAINT fk_notes_owner FOREIGN KEY (owner_key_id) REFERENCES api_keys (key_id),
  -- Búsqueda de texto completo sobre título y contenido.
  FULLTEXT INDEX ftx_notes_title_text (title, text)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
