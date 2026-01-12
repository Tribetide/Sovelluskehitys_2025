using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    public class ProductRepository
    {
        private readonly SqliteConnection _connection;

        public ProductRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public DataTable GetProducts()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT p.id,
                       p.nimi,
                       p.hinta,
                       p.varastosaldo,
                       p.kategoria_id
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
            cmd.CommandText = "SELECT id, nimi FROM tuotteet ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("tuotteet");
            table.Load(reader);
            return table;
        }

        public void AddProduct(string name, decimal price, int stock, long? categoryId)
        {
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

        public void DeleteProduct(long id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM tuotteet WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void UpdateProductCategory(long productId, long? categoryId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE tuotteet SET kategoria_id = @kategoria_id WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.Parameters.AddWithValue("@kategoria_id", (object?)categoryId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}
