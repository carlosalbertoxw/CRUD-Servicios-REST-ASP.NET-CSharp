CREATE TABLE IF NOT EXISTS notes (
  id    INT          NOT NULL AUTO_INCREMENT,
  title VARCHAR(250) NOT NULL,
  -- MEDIUMTEXT (16 MB) en lugar de LONGTEXT: la API limita el contenido
  -- a 100.000 caracteres, no tiene sentido permitir 4 GB por fila.
  text  MEDIUMTEXT   NULL,
  PRIMARY KEY (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
