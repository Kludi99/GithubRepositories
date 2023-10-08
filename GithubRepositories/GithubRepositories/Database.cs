using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubRepositories
{
    public class Database
    {
        private string ConnectionString { get; set; }
        public Database(IConfiguration config) => ConnectionString = config["ConnectionString:Sqlite"];

        public void SaveBackup(string repository, DateTime currentDate)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                //create table Backups if not exists
                var createTableQuery = "CREATE TABLE IF NOT EXISTS Backups (\r\n  REPOSITORY_NAME TEXT,\r\n  DATE TEXT\r\n  );";
                using (var cmd = new SqliteCommand(createTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                string insertQuery = "INSERT INTO Backups (REPOSITORY_NAME, DATE) VALUES (@repoName, @currentDate)";
                using (var cmd = new SqliteCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@repoName", repository);
                    cmd.Parameters.AddWithValue("@currentDate", currentDate);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"\nSaved successfully");
            }
            catch (Exception) { Console.WriteLine("Database update failed"); }

        }

        public void ShowBackups()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                string selectQuery = "SELECT REPOSITORY_NAME, MAX(DATE) as DATE FROM Backups GROUP BY REPOSITORY_NAME";
                using var cmd = new SqliteCommand(selectQuery, connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"\n\nRepository: {reader["REPOSITORY_NAME"]}, Latest Backup Date: {reader["DATE"]}");
                }
            }
            catch (Exception) { Console.WriteLine("Database select failed"); }

        }

        public string GetMaxDateFromRepository(string repository)
        {
            try
            {
                var date = string.Empty;
                using var connection = new SqliteConnection("Data Source=git-repository.db");
                connection.Open();
                string selectQuery = "SELECT MAX(DATE) as DATE FROM Backups WHERE REPOSITORY_NAME like @repoName";
                using var cmd = new SqliteCommand(selectQuery, connection);
                cmd.Parameters.AddWithValue("@repoName", repository);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"\n\n {reader["DATE"]}");
                    date = reader["DATE"].ToString();
                }
                return date;
            }
            catch (Exception)
            {
                Console.WriteLine("Database select failed");
                return null;
            }

        }

    }
}
