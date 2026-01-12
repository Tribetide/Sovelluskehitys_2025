using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    public class CategoryRepository
    {
        private readonly SqliteConnection _connection;

        public CategoryRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public DataTable GetCategories()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi, kuvaus FROM kategoriat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("kategoriat");
            table.Load(reader);
            return table;
        }

        public DataTable GetCategoryOptions()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi FROM kategoriat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("kategoriat");
            table.Load(reader);
            return table;
        }

        public void AddCategory(string name, string? description)
        {
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
