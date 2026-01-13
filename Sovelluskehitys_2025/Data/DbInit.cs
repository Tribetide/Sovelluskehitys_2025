using Microsoft.Data.Sqlite;
using System;
using System.IO;

public static class DbInit
{
    private const string AppFolder = "Sovelluskehitys_2025";
    private const string DbFileName = "sovelluskehitys.db";

    // schema.sql kopioidaan Output-kansioon (Properties: Content + Copy if newer)
    private static string SchemaPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "schema.sql");

    public static string DbFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolder,
            DbFileName
        );

    public static string ConnectionString => $"Data Source={DbFilePath};";

    public static void EnsureDatabase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbFilePath)!);

        if (!File.Exists(SchemaPath))
        {
            throw new FileNotFoundException(
                $"schema.sql puuttuu Output-kansiosta: {SchemaPath}. " +
                "Aseta Sql/schema.sql -> Build Action = Content ja Copy to Output Directory = Copy if newer."
            );
        }

        // Alusta vain jos db puuttuu tai tauluja ei ole
        bool needsInit = !File.Exists(DbFilePath);

        if (!needsInit)
        {
            using var checkConn = new SqliteConnection(ConnectionString);
            checkConn.Open();
            EnableForeignKeys(checkConn);

            using var cmd = checkConn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='tuotteet';";
            needsInit = cmd.ExecuteScalar() is null;
        }

        if (needsInit)
        {
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
