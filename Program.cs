using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace Docupurge
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read configuration values from the app.config file
            string connection_String = ConfigurationManager.ConnectionStrings["Servername, MyDatabaseConnection"].ConnectionString;
            string databaseServer = ConfigurationManager.AppSettings["DatabaseServer"]; // Get database server from app.config
            string aspireDatabase = ConfigurationManager.AppSettings["AspireDatabase"]; // Get database name from app.config
            bool checkSubFolders = bool.Parse(ConfigurationManager.AppSettings["CheckSubFolders"]); // Get CheckSubFolders value (true/false)
            bool reportOnly = bool.Parse(ConfigurationManager.AppSettings["ReportOnly"]); // Get ReportOnly value (true/false)
            string logPath = ConfigurationManager.AppSettings["LogPath"]; // Get log file path from app.config

            // Get the paths to check from the app.config (split by commas)
            string pathsToCheckConfig = ConfigurationManager.AppSettings["PathsToCheck"];
            List<string> pathsToCheck = new List<string>(pathsToCheckConfig.Split(','));

            // Log the config settings
            LogMessage(logPath, "Starting process with the following settings:");
            LogMessage(logPath, $"DatabaseServer: {databaseServer}");
            LogMessage(logPath, $"AspireDatabase: {aspireDatabase}");
            LogMessage(logPath, $"PathsToCheck: {string.Join(", ", pathsToCheck)}");
            LogMessage(logPath, $"CheckSubFolders: {checkSubFolders}");
            LogMessage(logPath, $"ReportOnly: {reportOnly}");
            LogMessage(logPath, $"LogPath: {logPath}");

            // SQL query to fetch documents marked as PURGED
            string sqlQuery = @"
                SELECT Name 
                FROM [YourAspireDatabase_____].[dbo].[DocumentCatalog]
                WHERE LastChangeOperator = 'PURGED'";

            // Fetch documents marked as PURGED from the database
            List<string> purgedDocuments = FetchPurgedDocuments(databaseServer, aspireDatabase, sqlQuery, logPath);

            // Check and process files in the given paths
            foreach (string path in pathsToCheck)
            {
                foreach (string document in purgedDocuments)
                {
                    // Get the list of files matching the document name
                    string[] files = Directory.GetFiles(path, document, checkSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                    if (files.Length == 0)
                    {
                        // If no files are found, log a message indicating it
                        LogMessage(logPath, $"File not found: {document} in {path}");
                    }
                    else
                    {
                        // Process the files if they are found
                        foreach (string file in files)
                        {
                            if (reportOnly)
                            {
                                LogMessage(logPath, $"Found document: {file}");
                            }
                            else
                            {
                                LogMessage(logPath, $"Deleting document: {file}");
                                File.Delete(file);
                            }
                        }
                    }
                }
            }

            LogMessage(logPath, "Process completed.");
        }



        static List<string> FetchPurgedDocuments(string databaseServer, string aspireDatabase, string sqlQuery, string logPath)
        {
            List<string> documents = new List<string>();
            string connectionString = $"Server={databaseServer};Database={aspireDatabase};Trusted_Connection=True;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                documents.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(logPath, $"Error fetching purged documents: {ex.Message}");
            }

            return documents;
        }

        static void LogMessage(string logPath, string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging message: {ex.Message}");
            }
        }
    }
}
