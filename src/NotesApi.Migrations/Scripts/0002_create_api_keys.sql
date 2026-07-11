-- Las API keys se almacenan hasheadas (SHA-256 en hex) y con identificador:
-- el cliente envía "X-Api-Key: <key_id>.<secreto>", el servidor busca por
-- key_id y compara el hash del secreto. Esto permite revocar y rotar keys
-- por cliente sin afectar al resto.
CREATE TABLE IF NOT EXISTS api_keys (
  key_id      VARCHAR(64)  NOT NULL,
  key_hash    CHAR(64)     NOT NULL,
  client_name VARCHAR(100) NOT NULL,
  created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revoked_at  TIMESTAMP    NULL DEFAULT NULL,
  PRIMARY KEY (key_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
