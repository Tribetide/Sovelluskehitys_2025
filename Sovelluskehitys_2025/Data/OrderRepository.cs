using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    // Tilausdatan käsittely (tilaukset, rivit ja varastosaldo).
    public class OrderRepository
    {
        private readonly SqliteConnection _connection;

        public OrderRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        // Litteä lista käyttöliittymälle ennen pää- ja rivinäkymän ryhmittelyä.
        public DataTable GetOpenOrders()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id as id,
                       tr.id as rivi_id,
                       a.nimi as asiakas,
                       a.osoite as osoite,
                       t.tilaus_pvm as tilaus_pvm,
                       t.toimitus_pvm as toimitus_pvm,
                       tu.nimi as tuote,
                       tr.maara as maara,
                       tr.rivihinta as rivihinta,
                       tr.tuote_id as tuote_id,
                       t.toimitettu as toimitettu
                FROM tilaukset t
                JOIN asiakkaat a ON a.id = t.asiakas_id
                JOIN tilausrivit tr ON tr.tilaus_id = t.id
                JOIN tuotteet tu ON tu.id = tr.tuote_id
                WHERE t.toimitettu = 0
                ORDER BY t.id DESC;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("tilaukset");
            table.Load(reader);
            return table;
        }

        // Litteä lista toimitetuista tilauksista käyttöliittymälle.
        public DataTable GetDeliveredOrders()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id as id,
                       tr.id as rivi_id,
                       a.nimi as asiakas,
                       a.osoite as osoite,
                       t.tilaus_pvm as tilaus_pvm,
                       t.toimitus_pvm as toimitus_pvm,
                       tu.nimi as tuote,
                       tr.maara as maara,
                       tr.rivihinta as rivihinta,
                       tr.tuote_id as tuote_id,
                       t.toimitettu as toimitettu
                FROM tilaukset t
                JOIN asiakkaat a ON a.id = t.asiakas_id
                JOIN tilausrivit tr ON tr.tilaus_id = t.id
                JOIN tuotteet tu ON tu.id = tr.tuote_id
                WHERE t.toimitettu = 1
                ORDER BY t.id DESC;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("toimitetut");
            table.Load(reader);
            return table;
        }

        // Myyntien aggregaatti kärkikolmikon raportille.
        public DataTable GetTopProducts(int limit)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tu.nimi as tuote,
                       SUM(tr.maara) as myyty,
                       SUM(tr.rivihinta) as myynti
                FROM tilausrivit tr
                JOIN tuotteet tu ON tu.id = tr.tuote_id
                GROUP BY tu.id, tu.nimi
                ORDER BY myyty DESC, tu.nimi
                LIMIT @limit;";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("top_tuotteet");
            table.Load(reader);
            return table;
        }

        // Perinteinen: yhden rivin tilauksen luonti (säilytetään yhteensopivuuden vuoksi).
        public void CreateOrder(long customerId, long productId, int quantity)
        {
            using var tx = _connection.BeginTransaction();
            try
            {
                long orderId;

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO tilaukset (asiakas_id) VALUES (@asiakas_id); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@asiakas_id", customerId);
                    orderId = (long)cmd.ExecuteScalar()!;
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    // Hintakuva: rivihinta tallentaa hinnan * määrän tilaushetkellä.
                    cmd.CommandText = @"
                        INSERT INTO tilausrivit (tilaus_id, tuote_id, maara, rivihinta)
                        VALUES (@tilaus_id, @tuote_id, @maara,
                                (SELECT hinta FROM tuotteet WHERE id = @tuote_id) * @maara);";
                    cmd.Parameters.AddWithValue("@tilaus_id", orderId);
                    cmd.Parameters.AddWithValue("@tuote_id", productId);
                    cmd.Parameters.AddWithValue("@maara", quantity);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    // Vähennä saldoa vain jos sitä on riittävästi.
                    cmd.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo - @maara
                        WHERE id = @tuote_id AND varastosaldo >= @maara;";
                    cmd.Parameters.AddWithValue("@maara", quantity);
                    cmd.Parameters.AddWithValue("@tuote_id", productId);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new InvalidOperationException("Varastosaldo ei riitä tilaukseen.");
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Luo tilauksen useilla riveillä ja päivittää saldot transaktion sisällä.
        public void CreateOrderWithLines(long customerId, IReadOnlyList<(long productId, int quantity)> lines)
        {
            if (lines.Count == 0)
                throw new ArgumentException("Tilauksessa täytyy olla vähintään yksi rivi.");

            using var tx = _connection.BeginTransaction();
            try
            {
                long orderId;

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO tilaukset (asiakas_id) VALUES (@asiakas_id); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@asiakas_id", customerId);
                    orderId = (long)cmd.ExecuteScalar()!;
                }

                foreach (var (productId, quantity) in lines)
                {
                    if (quantity <= 0)
                        throw new ArgumentException("Määrän tulee olla positiivinen.");

                    using (var saldo = _connection.CreateCommand())
                    {
                        saldo.Transaction = tx;
                        // Varaa saldo jokaiselle riville ennen lisäystä.
                        saldo.CommandText = @"
                            UPDATE tuotteet
                            SET varastosaldo = varastosaldo - @maara
                            WHERE id = @tuote_id AND varastosaldo >= @maara;";
                        saldo.Parameters.AddWithValue("@maara", quantity);
                        saldo.Parameters.AddWithValue("@tuote_id", productId);

                        int affected = saldo.ExecuteNonQuery();
                        if (affected == 0)
                            throw new InvalidOperationException("Varastosaldo ei riitä tilaukseen.");
                    }

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        // Tallenna rivin hintahetkikopio.
                        cmd.CommandText = @"
                            INSERT INTO tilausrivit (tilaus_id, tuote_id, maara, rivihinta)
                            VALUES (@tilaus_id, @tuote_id, @maara,
                                    (SELECT hinta FROM tuotteet WHERE id = @tuote_id) * @maara);";
                        cmd.Parameters.AddWithValue("@tilaus_id", orderId);
                        cmd.Parameters.AddWithValue("@tuote_id", productId);
                        cmd.Parameters.AddWithValue("@maara", quantity);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Merkitsee tilauksen toimitetuksi/peruutetuksi.
        public void SetDelivered(long orderId, bool delivered)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE tilaukset
                SET toimitettu = @toimitettu,
                    toimitus_pvm = CASE WHEN @toimitettu = 1 THEN datetime('now') ELSE NULL END
                WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.Parameters.AddWithValue("@toimitettu", delivered ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        // Poistaa tilauksen ja palauttaa saldon tilausrivien perusteella.
        public void DeleteOrder(long orderId)
        {
            using var tx = _connection.BeginTransaction();
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    // Palauttaa saldon kaikille tilauksen tuotteille.
                    cmd.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo + (
                            SELECT SUM(tr.maara)
                            FROM tilausrivit tr
                            WHERE tr.tuote_id = tuotteet.id AND tr.tilaus_id = @tilaus_id
                        )
                        WHERE id IN (
                            SELECT tuote_id FROM tilausrivit WHERE tilaus_id = @tilaus_id
                        );";
                    cmd.Parameters.AddWithValue("@tilaus_id", orderId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "DELETE FROM tilaukset WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Päivittää tilausrivin määrän ja säätää saldoa muutoksen verran.
        public void UpdateOrderQuantity(long rowId, long productId, int newQty, int oldQty)
        {
            int delta = newQty - oldQty;
            if (delta == 0)
                return;

            using var tx = _connection.BeginTransaction();
            try
            {
                if (delta > 0)
                {
                    // Määrän kasvatus: tarvitaan lisää saldoa.
                    using var saldo = _connection.CreateCommand();
                    saldo.Transaction = tx;
                    saldo.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo - @delta
                        WHERE id = @tuote_id AND varastosaldo >= @delta;";
                    saldo.Parameters.AddWithValue("@delta", delta);
                    saldo.Parameters.AddWithValue("@tuote_id", productId);
                    int affected = saldo.ExecuteNonQuery();
                    if (affected == 0)
                        throw new InvalidOperationException("Varastosaldo ei riitä tilaukseen.");
                }
                else
                {
                    // Määrän pienennys: palautetaan saldoa.
                    using var saldo = _connection.CreateCommand();
                    saldo.Transaction = tx;
                    saldo.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo + @palauta
                        WHERE id = @tuote_id;";
                    saldo.Parameters.AddWithValue("@palauta", -delta);
                    saldo.Parameters.AddWithValue("@tuote_id", productId);
                    saldo.ExecuteNonQuery();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        UPDATE tilausrivit
                        SET maara = @maara,
                            rivihinta = (SELECT hinta FROM tuotteet WHERE id = @tuote_id) * @maara
                        WHERE id = @rivi_id;";
                    cmd.Parameters.AddWithValue("@maara", newQty);
                    cmd.Parameters.AddWithValue("@tuote_id", productId);
                    cmd.Parameters.AddWithValue("@rivi_id", rowId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Mukavuusylikuormitus: hae vanhat arvot ensin.
        public void UpdateOrderQuantity(long rowId, int newQty)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT tuote_id, maara FROM tilausrivit WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", rowId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("Tilausriviä ei löydy.");

            long productId = reader.GetInt64(0);
            int oldQty = reader.GetInt32(1);

            UpdateOrderQuantity(rowId, productId, newQty, oldQty);
        }
    }
}
