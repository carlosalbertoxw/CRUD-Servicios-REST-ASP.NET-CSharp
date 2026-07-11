-- Datos SOLO para desarrollo local (se aplican con la opción --seed).
-- La API key de desarrollo es:  X-Api-Key: local-dev.dev-secret
-- (key_hash = SHA-256 de "dev-secret" en binario)
INSERT INTO api_keys (key_id, key_hash, client_name) VALUES
  ('local-dev', UNHEX('298754db2dbab6ec62605ceb0379eb7ee376580359449efe0caa3aa06cd56736'), 'Desarrollo local');

INSERT INTO notes (owner_key_id, title, text) VALUES
  ('local-dev', 'Nota de ejemplo 1', 'Contenido de la primera nota de ejemplo.'),
  ('local-dev', 'Nota de ejemplo 2', 'Contenido de la segunda nota de ejemplo.');
