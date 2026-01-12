CREATE TABLE tuotteet (
	id INTEGER IDENTITY(1,1) PRIMARY KEY,
	nimi VARCHAR(100) NOT NULL,
	hinta DECIMAL(10, 2) NOT NULL,
	varastosaldo INT NOT NULL
);


CREATE TABLE asiakkaat (
	id INTEGER IDENTITY(1,1) PRIMARY KEY,
	nimi VARCHAR(100) NOT NULL,
	osoite VARCHAR(100) NOT NULL,
	puhelin VARCHAR(100) NOT NULL
);


CREATE TABLE tilaukset (
	id INTEGER IDENTITY(1,1) PRIMARY KEY,
	tuote_id INTEGER REFERENCES tuotteet ON DELETE CASCADE,
	asiakas_id INTEGER REFERENCES asiakkaat ON DELETE CASCADE,
	toimitettu BIT DEFAULT 0
);

INSERT INTO tuotteet (nimi, hinta, varastosaldo) VALUES
('Tuote A', 19.99, 50),
('Tuote B', 29.99, 30),
('Tuote C', 9.99, 100),
('Tuote D', 49.99, 20);

INSERT INTO tilaukset(tuote_id, asiakas_id) VALUES(2, 1);	

SELECT id, nimi FROM tuotteet;
SELECT id, nimi FROM asiakkaat ORDER BY id DESC;

UPDATE asiakkaat SET nimi='Alpo' WHERE id=2;

SELECT ti.id as id, a.nimi as asiakas, a.osoite as osoite, tu.nimi as tuote, ti.toimitettu as toimitettu FROM tilaukset ti, asiakkaat a, tuotteet tu WHERE a.id=ti.asiakas_id AND tu.id=ti.tuote_id;

DELETE FROM tuotteet WHERE id = 1002;
