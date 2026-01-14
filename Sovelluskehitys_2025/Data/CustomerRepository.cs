using Microsoft.Data.Sqlite;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    // Asiakasdatan käsittely (listaus + lisäys).
    public class CustomerRepository
    {
        private readonly SqliteConnection _connection;

        public CustomerRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        // Täysi asiakaslista taulunäkymään.
        public DataTable GetCustomers()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi, osoite, puhelin FROM asiakkaat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("asiakkaat");
            table.Load(reader);
            return table;
        }

        // Suppea asiakaslista pudotusvalikoihin.
        public DataTable GetCustomerOptions()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT id, nimi FROM asiakkaat ORDER BY nimi;";
            using var reader = cmd.ExecuteReader();
            var table = new DataTable("asiakkaat");
            table.Load(reader);
            return table;
        }

        // Lisää uusi asiakas ja tarkistaa duplikaattinimet.
        public void AddCustomer(string name, string address, string phone)
        {
            using (var check = _connection.CreateCommand())
            {
                // Iso- ja pienikirjaimet ohittava duplikaattitarkistus.
                check.CommandText = "SELECT 1 FROM asiakkaat WHERE lower(nimi) = lower(@nimi) LIMIT 1;";
                check.Parameters.AddWithValue("@nimi", name);
                if (check.ExecuteScalar() != null)
                    throw new InvalidOperationException("Asiakas on jo olemassa.");
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO asiakkaat (nimi, osoite, puhelin) VALUES (@nimi, @osoite, @puhelin);";
            cmd.Parameters.AddWithValue("@nimi", name);
            cmd.Parameters.AddWithValue("@osoite", address);
            cmd.Parameters.AddWithValue("@puhelin", phone);
            cmd.ExecuteNonQuery();
        }
    }
}
