using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using Microsoft.Data.SqlClient;

namespace DaJet.Metadata.CLI
{
    public static class Program
    {
        private const string ServerIsNotDefinedError = "Server address is not defined.";
        private const string DatabaseIsNotDefinedError = "Database name is not defined.";
        private static Dictionary<Guid, ApplicationObject> _infoBaseDocuments = null;

        public static int Main(string[] args)
        {
            args = new[]
                { "--ms", "127.0.0.1", "--u", "root", "--p", "isakovs", "--d", "master", "--schema", "_InfoRg5108" };

            RootCommand command = new RootCommand()
            {
                new Option<string>(new[] { "--ms" }, "Server address or name"),
                new Option<string>(new[] { "--d" }, "Database name"),
                new Option<string>(new[] { "--t" }, "mssql or PGSQL"),
                new Option<string>(new[] { "--u" }, "User name (Windows authentication is used if not defined)"),
                new Option<string>(new[] { "--p" }, "User password if SQL Server authentication is used"),
                new Option<string>(new[] { "--schema" }, "Metadata object to get SQL schema for"),
            };
            command.Description = "DaJet (metadata reader utility)";
            command.Handler =
                CommandHandler.Create<string, string, string, string, string, string>(
                    ExecuteCommand);
            return command.Invoke(args);
        }

        private static void ShowErrorMessage(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ExecuteCommand(string ms, string d, string t, string u, string p, string schema)
        {
            if (string.IsNullOrWhiteSpace(ms))
            {
                ms = "127.0.0.1";
                //ShowErrorMessage(SERVER_IS_NOT_DEFINED_ERROR);
                return;
            }

            if (string.IsNullOrWhiteSpace(d))
            {
                ShowErrorMessage(DatabaseIsNotDefinedError);
                return;
            }

            if (!schema.StartsWith("_")) schema = "_" + schema;

            IMetadataService metadataService = new MetadataService();
            if (t == "pg")
            {
                metadataService
                    .UseDatabaseProvider(DatabaseProvider.PostgreSQL)
                    .ConfigureConnectionString(ms, d, u, p);
            }
            else if (string.IsNullOrWhiteSpace(t) || t == "ms")
            {
                string connectionString = BuildConnectionString(ms, d, u, p);
                metadataService
                    .UseDatabaseProvider(DatabaseProvider.SQLServer)
                    .UseConnectionString(connectionString);
            }

            /*TableRelationStructureBase relationBase = metadataService.OpenRelationBase();
            TableObject table = relationBase.Tables.FirstOrDefault(o => schema == o.TableName);
            */
            InfoBase infoBase = metadataService.OpenInfoBase();

            /*if (!string.IsNullOrWhiteSpace(schema))
            {
                _infoBaseDocuments = infoBase.GetApplicationObjectByTableName(schema);
                TableDescriber describer = new TableDescriber(infoBase, relationBase,
                    _infoBaseDocuments.Values.First(o => o.TableName == schema)
                );
                describer.Describe();
            }*/
        }

        private static string BuildConnectionString(string server, string database, string userName, string password)
        {
            SqlConnectionStringBuilder connectionString = new()
            {
                DataSource = server,
                InitialCatalog = database
            };
            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionString.UserID = userName;
                connectionString.Password = password;
            }

            connectionString.IntegratedSecurity = string.IsNullOrWhiteSpace(userName);
            connectionString.Encrypt = false;

            return connectionString.ToString();
        }
    }
}