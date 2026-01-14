using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    // Kategoriadatan käsittely (listaus/lisäys/poisto).
    public class CategoryRepository
    {
        private readonly SqliteConnection _connection;

        public CategoryRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        // Täysi kategorialista taulunäkymään.
        public DataTable GetCategories()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi, kuvaus FROM kategoriat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("kategoriat");
            table.Load(reader);
            return table;
        }

        // Suppea kategorialista pudotusvalikoihin.
        public DataTable GetCategoryOptions()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi FROM kategoriat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("kategoriat");
            table.Load(reader);
            return table;
        }

        // Lisää uusi kategoria ja tarkistaa duplikaattinimet.
        public void AddCategory(string name, string? description)
        {
            using (var check = _connection.CreateCommand())
            {
                // Iso- ja pienikirjaimet ohittava duplikaattitarkistus.
                check.CommandText = "SELECT 1 FROM kategoriat WHERE lower(nimi) = lower(@nimi) LIMIT 1;";
                check.Parameters.AddWithValue("@nimi", name);
                if (check.ExecuteScalar() != null)
                    throw new InvalidOperationException("Kategoria on jo olemassa.");
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO kategoriat (nimi, kuvaus) VALUES (@nimi, @kuvaus);";
            cmd.Parameters.AddWithValue("@nimi", name);
            cmd.Parameters.AddWithValue("@kuvaus", (object?)description ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeleteCategory(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM kategoriat WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}
