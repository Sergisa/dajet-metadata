﻿using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;

namespace DaJet.Metadata.CLI
{
    public static class Program
    {
        private const string SERVER_IS_NOT_DEFINED_ERROR = "Server address is not defined.";
        private const string DATABASE_IS_NOT_DEFINED_ERROR = "Database name is not defined.";

        public static int Main(string[] args)
        {
            RootCommand command = new RootCommand()
            {
                new Option<string>("-s", "SQL Server address or name"),
                new Option<string>("-d", "Database name"),
                new Option<string>("-u", "User name (Windows authentication is used if not defined)"),
                new Option<string>("-p", "User password if SQL Server authentication is used"),
                new Option<string>("-m", "MetaObject name (example: \"Справочник.Номенклатура\")"),
                new Option<FileInfo>("-out-file", "File path to save metaobject information"),
                new Option<FileInfo>("-out-root", "File path to save configuration information")
            };
            command.Description = "DaJet (metadata reader utility)";
            command.Handler = CommandHandler.Create<string, string, string, string, FileInfo, string, FileInfo>(ExecuteCommand);
            return command.Invoke(args);
        }
        private static void ShowErrorMessage(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void ExecuteCommand(string s, string d, string u, string p, FileInfo outRoot, string m, FileInfo outFile)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                ShowErrorMessage(SERVER_IS_NOT_DEFINED_ERROR); return;
            }
            if (string.IsNullOrWhiteSpace(d))
            {
                ShowErrorMessage(DATABASE_IS_NOT_DEFINED_ERROR); return;
            }

            IMetadataFileReader fileReader = new MetadataFileReader();
            fileReader.ConfigureConnectionString(s, d, u, p);
            IConfigurationFileParser configParser = new ConfigurationFileParser(fileReader);
            ConfigInfo config = configParser.ReadConfigurationProperties();

            Console.WriteLine("Name = " + config.Name);
            Console.WriteLine("Alias = " + config.Alias);
            Console.WriteLine("Comment = " + config.Comment);
            Console.WriteLine("Version = " + config.Version);
            Console.WriteLine("ConfigVersion = " + config.ConfigVersion);
            Console.WriteLine("SyncCallsMode = " + config.SyncCallsMode.ToString());
            Console.WriteLine("DataLockingMode = " + config.DataLockingMode.ToString());
            Console.WriteLine("ModalWindowMode = " + config.ModalWindowMode.ToString());
            Console.WriteLine("AutoNumberingMode = " + config.AutoNumberingMode.ToString());
            Console.WriteLine("UICompatibilityMode = " + config.UICompatibilityMode.ToString());

            if (outRoot != null)
            {
                SaveConfigToFile(outRoot.FullName, fileReader, configParser);
            }

            if (outFile != null && !string.IsNullOrWhiteSpace(m))
            {
                SaveMetaObjectToFile(outFile.FullName, fileReader, m);
            }
        }
        private static void SaveConfigToFile(string filePath, IMetadataFileReader fileReader, IConfigurationFileParser configParser)
        {
            string fileName = configParser.GetConfigurationFileName();
            byte[] fileData = fileReader.ReadBytes(fileName);
            using (StreamReader reader = fileReader.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
        private static void SaveMetaObjectToFile(string filePath, IMetadataFileReader fileReader, string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return;
            string typeName = names[0];
            string objectName = names[1];

            MetaObject metaObject = null;
            Dictionary<Guid, MetaObject> collection = null;
            IMetadataReader metadata = new MetadataReader(fileReader);
            InfoBase infoBase = metadata.LoadInfoBase();
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            if (collection == null) return;

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null) return;

            byte[] fileData = fileReader.ReadBytes(metaObject.UUID.ToString());
            if (fileData == null) return;

            using (StreamReader reader = fileReader.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
    }
}