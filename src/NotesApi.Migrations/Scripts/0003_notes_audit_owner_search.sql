-- Auditoría, dueño por cliente y búsqueda de texto completo en notes.
-- Convención: todas las fechas se guardan en UTC (DATETIME, no TIMESTAMP).
SET time_zone = '+00:00';

ALTER TABLE notes
  ADD COLUMN owner_key_id VARCHAR(64) NULL AFTER id,
  ADD COLUMN created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ADD COLUMN updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

-- Backfill: las notas previas a esta migración pasan a la API key más antigua.
-- Si no existe ninguna key, quedan sin dueño posible y se eliminan.
UPDATE notes
   SET owner_key_id = (SELECT key_id FROM api_keys ORDER BY created_at, key_id LIMIT 1)
 WHERE owner_key_id IS NULL;

DELETE FROM notes WHERE owner_key_id IS NULL;

-- La FK crea implícitamente el índice por owner_key_id que usa el listado.
ALTER TABLE notes
  MODIFY owner_key_id VARCHAR(64) NOT NULL,
  ADD CONSTRAINT fk_notes_owner FOREIGN KEY (owner_key_id) REFERENCES api_keys (key_id);

-- InnoDB exige crear el índice FULLTEXT en un ALTER separado.
ALTER TABLE notes
  ADD FULLTEXT INDEX ftx_notes_title_text (title, text);
