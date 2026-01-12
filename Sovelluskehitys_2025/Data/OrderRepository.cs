using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    public class OrderRepository
    {
        private readonly SqliteConnection _connection;

        public OrderRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public DataTable GetOpenOrders()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id as id,
                       tr.id as rivi_id,
                       a.nimi as asiakas,
                       a.osoite as osoite,
                       tu.nimi as tuote,
                       tr.maara as maara,
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

        public DataTable GetDeliveredOrders()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.id as id,
                       a.nimi as asiakas,
                       a.osoite as osoite,
                       tu.nimi as tuote,
                       tr.maara as maara,
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

        public void SetDelivered(long orderId, bool delivered)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE tilaukset SET toimitettu = @toimitettu WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.Parameters.AddWithValue("@toimitettu", delivered ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void DeleteOrder(long orderId)
        {
            using var tx = _connection.BeginTransaction();
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = tx;
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
    }
}
