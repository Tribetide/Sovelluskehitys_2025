using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    // Tuotedatan käsittely (luonti/luku/päivitys/poisto + varastosaldo).
    public class ProductRepository
    {
        private readonly SqliteConnection _connection;

        public ProductRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        // Tuotelista käyttöliittymälle, sisältää matalan saldon lipun korostusta varten.
        public DataTable GetProducts()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT p.id,
                       p.nimi,
                       p.hinta,
                       p.varastosaldo,
                       p.kategoria_id,
                       -- Lippu käyttöliittymän korostusta varten, kun saldo on matala.
                       CASE WHEN p.varastosaldo <= 5 THEN 1 ELSE 0 END AS saldo_matala
                FROM tuotteet p
                ORDER BY p.nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("tuotteet");
            table.Load(reader);
            return table;
        }

        public DataTable GetProductOptions()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi, hinta FROM tuotteet ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("tuotteet");
            table.Load(reader);
            return table;
        }

        // Lisää uuden tuotteen ja estää duplikaattinimet.
        public void AddProduct(string name, decimal price, int stock, long? categoryId)
        {
            using (var check = _connection.CreateCommand())
            {
                // Iso- ja pienikirjaimet ohittava nimen tarkistus.
                check.CommandText = "SELECT 1 FROM tuotteet WHERE lower(nimi) = lower(@nimi) LIMIT 1;";
                check.Parameters.AddWithValue("@nimi", name);
                if (check.ExecuteScalar() != null)
                    throw new InvalidOperationException("Tuote on jo olemassa.");
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO tuotteet (nimi, hinta, varastosaldo, kategoria_id)
                VALUES (@nimi, @hinta, @saldo, @kategoria_id);";
            cmd.Parameters.AddWithValue("@nimi", name);
            cmd.Parameters.AddWithValue("@hinta", price);
            cmd.Parameters.AddWithValue("@saldo", stock);
            cmd.Parameters.AddWithValue("@kategoria_id", (object?)categoryId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Poistaa tuotteen id:n perusteella.
        public void DeleteProduct(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM tuotteet WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // Päivittää vain kategorian (käytetään käyttöliittymän comboboxissa).
        public void UpdateProductCategory(long productId, long? categoryId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE tuotteet SET kategoria_id = @kategoria_id WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.Parameters.AddWithValue("@kategoria_id", (object?)categoryId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Päivittää nimen/hinnan/kategorian, validoi ja estää duplikaatit.
        public void UpdateProductDetails(long productId, string name, decimal price, long? categoryId)
        {
            // Vartioehdot pitävät tietokannan eheänä.
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Tuotenimi ei voi olla tyhjä.");
            if (price < 0)
                throw new InvalidOperationException("Hinta ei voi olla negatiivinen.");

            using (var check = _connection.CreateCommand())
            {
                // Vältä duplikaatit uudelleennimeämisessä.
                check.CommandText = @"
                    SELECT 1
                    FROM tuotteet
                    WHERE lower(nimi) = lower(@nimi) AND id <> @id
                    LIMIT 1;";
                check.Parameters.AddWithValue("@nimi", name);
                check.Parameters.AddWithValue("@id", productId);
                if (check.ExecuteScalar() != null)
                    throw new InvalidOperationException("Tuotenimi on jo olemassa.");
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE tuotteet
                SET nimi = @nimi,
                    hinta = @hinta,
                    kategoria_id = @kategoria_id
                WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.Parameters.AddWithValue("@nimi", name);
            cmd.Parameters.AddWithValue("@hinta", price);
            cmd.Parameters.AddWithValue("@kategoria_id", (object?)categoryId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Lisää tuotteen varastosaldoa (vain positiivinen määrä).
        public void AddStock(long productId, int amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Lisättävän saldon tulee olla positiivinen.");

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE tuotteet SET varastosaldo = varastosaldo + @amount WHERE id = @id;";
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.ExecuteNonQuery();
        }
    }
}
