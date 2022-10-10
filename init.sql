--DROP TABLE IF EXISTS hash; DROP TABLE IF EXISTS file;

CREATE TABLE IF NOT EXISTS hash (
    hash_id     SERIAL PRIMARY KEY,
    val         TEXT,
    is_partial  BOOLEAN NOT NULL,
    created_on  TIMESTAMP NOT NULL,
    modified_on TIMESTAMP
);

CREATE TABLE IF NOT EXISTS file (
    file_id     SERIAL PRIMARY KEY,
    hash_id     INT, 
    CONSTRAINT fk_hash FOREIGN KEY(hash_id) REFERENCES hash(hash_id),
    name        TEXT,
    path        TEXT,
    source      TEXT
);

-- CREATE TABLE IF NOT EXISTS hash_file (
--    hash_id INT,
--    file_id INT,
--    PRIMARY KEY (hash_id, file_id),
--    CONSTRAINT fk_hash FOREIGN KEY(hash_id) REFERENCES hash(id),
--    CONSTRAINT fk_file FOREIGN KEY(file_id) REFERENCES file(id),

-- );

INSERT INTO hash (val, is_partial, created_on) VALUES ('test', true, now());
INSERT INTO file (hash_id, name, path, source) VALUES (1, 'filename.jpg', 'C:\users\test', 'local hard drive');
INSERT INTO file (hash_id, name, path, source) VALUES (1, 'duplicate.jpg', 'C:\users\test\new folder', 'local hard drive');

-- SELECT * FROM hash;
-- SELECT * FROM file;
