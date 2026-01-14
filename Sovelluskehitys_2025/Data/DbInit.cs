using Microsoft.Data.Sqlite;
using System;
using System.IO;

// SQLite-tietokannan alustus ja yhteysapu.
public static class DbInit
{
    // AppData\Local-alikansio tietokantatiedostolle.
    private const string AppFolder = "Sovelluskehitys_2025";
    private const string DbFileName = "sovelluskehitys.db";

    // schema.sql kopioidaan output-kansioon (Build Action: Content).
    private static string SchemaPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "schema.sql");

    // Täysi polku tietokantatiedostoon LocalAppData-kansion alle.
    public static string DbFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolder,
            DbFileName
        );

    // SQLite-yhteysmerkkijono.
    public static string ConnectionString => $"Data Source={DbFilePath};";

    public static void EnsureDatabase()
    {
        // Varmista kansion olemassaolo ennen tietokantatiedoston tarkistusta.
        Directory.CreateDirectory(Path.GetDirectoryName(DbFilePath)!);

        if (!File.Exists(SchemaPath))
        {
            throw new FileNotFoundException(
                $"schema.sql puuttuu Output-kansiosta: {SchemaPath}. " +
                "Aseta Sql/schema.sql -> Build Action = Content ja Copy to Output Directory = Copy if newer."
            );
        }

        // Alusta vain jos tietokanta puuttuu tai tauluja ei löydy.
        bool needsInit = !File.Exists(DbFilePath);

        if (!needsInit)
        {
            using var checkConn = new SqliteConnection(ConnectionString);
            checkConn.Open();
            EnableForeignKeys(checkConn);

            using var cmd = checkConn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='tuotteet';";
            needsInit = cmd.ExecuteScalar() is null;

            if (!needsInit)
            {
                // Lisää toimitus_pvm-sarake vanhoihin tietokantoihin tarvittaessa.
                using var pragma = checkConn.CreateCommand();
                pragma.CommandText = "PRAGMA table_info(tilaukset);";
                using var reader = pragma.ExecuteReader();
                bool hasToimitusPvm = false;
                while (reader.Read())
                {
                    var columnName = reader.GetString(1);
                    if (string.Equals(columnName, "toimitus_pvm", StringComparison.OrdinalIgnoreCase))
                    {
                        hasToimitusPvm = true;
                        break;
                    }
                }

                if (!hasToimitusPvm)
                {
                    using var alter = checkConn.CreateCommand();
                    alter.CommandText = "ALTER TABLE tilaukset ADD COLUMN toimitus_pvm TEXT;";
                    alter.ExecuteNonQuery();

                    using var backfill = checkConn.CreateCommand();
                    backfill.CommandText = "UPDATE tilaukset SET toimitus_pvm = tilaus_pvm WHERE toimitettu = 1 AND toimitus_pvm IS NULL;";
                    backfill.ExecuteNonQuery();
                }
            }
        }

        if (needsInit)
        {
            // Luo tietokanta uudelleen schema.sql:n perusteella.
            if (File.Exists(DbFilePath))
                File.Delete(DbFilePath);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            EnableForeignKeys(conn);

            var sql = File.ReadAllText(SchemaPath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        EnableForeignKeys(conn);
        return conn;
    }

    private static void EnableForeignKeys(SqliteConnection conn)
    {
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
    }
}
