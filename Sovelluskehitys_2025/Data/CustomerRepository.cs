using Microsoft.Data.Sqlite;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    public class CustomerRepository
    {
        private readonly SqliteConnection _connection;

        public CustomerRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public DataTable GetCustomers()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi, osoite, puhelin FROM asiakkaat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("asiakkaat");
            table.Load(reader);
            return table;
        }

        public DataTable GetCustomerOptions()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi FROM asiakkaat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("asiakkaat");
            table.Load(reader);
            return table;
        }

        public void AddCustomer(string name, string address, string phone)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO asiakkaat (nimi, osoite, puhelin) VALUES (@nimi, @osoite, @puhelin);";
            cmd.Parameters.AddWithValue("@nimi", name);
            cmd.Parameters.AddWithValue("@osoite", address);
            cmd.Parameters.AddWithValue("@puhelin", phone);
            cmd.ExecuteNonQuery();
        }
    }
}
