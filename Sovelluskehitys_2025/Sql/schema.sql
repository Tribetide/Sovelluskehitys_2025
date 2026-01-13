PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS tilausrivit;
DROP TABLE IF EXISTS tilaukset;
DROP TABLE IF EXISTS tuotteet;
DROP TABLE IF EXISTS asiakkaat;
DROP TABLE IF EXISTS kategoriat;

CREATE TABLE kategoriat (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    nimi        TEXT NOT NULL,
    kuvaus      TEXT
);

CREATE TABLE tuotteet (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    nimi         TEXT NOT NULL,
    hinta        NUMERIC NOT NULL,
    varastosaldo INTEGER NOT NULL,
    kategoria_id INTEGER NULL,
    FOREIGN KEY (kategoria_id) REFERENCES kategoriat(id) ON DELETE SET NULL
);

CREATE TABLE asiakkaat (
    id      INTEGER PRIMARY KEY AUTOINCREMENT,
    nimi    TEXT NOT NULL,
    osoite  TEXT NOT NULL,
    puhelin TEXT NOT NULL
);

CREATE TABLE tilaukset (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    asiakas_id INTEGER NOT NULL,
    tilaus_pvm TEXT NOT NULL DEFAULT (datetime('now')),
    toimitettu INTEGER NOT NULL DEFAULT 0 CHECK (toimitettu IN (0,1)),
    FOREIGN KEY (asiakas_id) REFERENCES asiakkaat(id) ON DELETE CASCADE
);

CREATE TABLE tilausrivit (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    tilaus_id INTEGER NOT NULL,
    tuote_id  INTEGER NOT NULL,
    maara     INTEGER NOT NULL CHECK (maara > 0),
    rivihinta NUMERIC NOT NULL,
    FOREIGN KEY (tilaus_id) REFERENCES tilaukset(id) ON DELETE CASCADE,
    FOREIGN KEY (tuote_id)  REFERENCES tuotteet(id) ON DELETE RESTRICT
);

CREATE INDEX idx_tuotteet_kategoria ON tuotteet(kategoria_id);
CREATE INDEX idx_tilaukset_asiakas  ON tilaukset(asiakas_id);
CREATE INDEX idx_rivit_tilaus       ON tilausrivit(tilaus_id);
CREATE INDEX idx_rivit_tuote        ON tilausrivit(tuote_id);

-- Testidata
INSERT INTO kategoriat (nimi, kuvaus) VALUES
('Elektroniikka', 'Laitteet ja tarvikkeet'),
('Toimisto', 'Toimistotarvikkeet'),
('Muut', 'Sekalaiset');

INSERT INTO tuotteet (nimi, hinta, varastosaldo, kategoria_id) VALUES
('Tuote A', 19.99, 50, 1),
('Tuote B', 29.99, 30, 1),
('Tuote C', 9.99, 100, 2),
('Tuote D', 49.99, 20, 3);

INSERT INTO asiakkaat (nimi, osoite, puhelin) VALUES
('Masa', 'Masaosoite 1', '0401234567'),
('Teppo', 'Teppotie 2', '0509876543');

-- Esimerkkitilaus: 1 tilaus, 2 riviä
INSERT INTO tilaukset (asiakas_id) VALUES (1);
INSERT INTO tilausrivit (tilaus_id, tuote_id, maara, rivihinta) VALUES
(last_insert_rowid(), 2, 1, 29.99);

INSERT INTO tilaukset (asiakas_id) VALUES (2);
INSERT INTO tilausrivit (tilaus_id, tuote_id, maara, rivihinta) VALUES
(last_insert_rowid(), 1, 2, 39.98);
