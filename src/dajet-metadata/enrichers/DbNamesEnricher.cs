using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

namespace DaJet.Metadata.Enrichers
{
    public sealed class DbNamesEnricher : IContentEnricher
    {
        private const string DBNAMES_FILE_NAME = "DBNames"; // Params
        private const string DBSCHEMA_FILE_NAME = "DBSchema"; // Params

        private Configurator Configurator { get; }

        public DbNamesEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }

        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is InfoBase infoBase)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(DBNAMES_FILE_NAME);
            ConfigObject relationDescriberObject =
                Configurator.FileReader.ReadConfigObject(DBSCHEMA_FILE_NAME).GetObject(1);
            List<object> tables = relationDescriberObject.Values;
            tables.RemoveAt(0);


            int entryCount = configObject.GetInt32(new[] { 1, 0 });
            Console.WriteLine($"Обнаружено {entryCount} объектов в DBNames");
            for (int i = 1; i <= entryCount; i++)
            {
                try
                {
                    Guid uuid = configObject.GetUuid(new[] { 1, i, 0 });
                    // FIXME: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')


                    if (uuid == Guid.Empty) continue;

                    string token = configObject.GetString(new[] { 1, i, 1 });
                    int code = configObject.GetInt32(new[] { 1, i, 2 });

                    ConfigObject tableDescription = (ConfigObject)tables.Find(o =>
                    {
                        return ((ConfigObject)o).GetString(0) == (token + code);
                    });
                    ProcessEntry(infoBase, uuid, token, code, tableDescription);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Ошибка парсинга {i} компонента из DBNames ");
                    Console.Error.WriteLine(e);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        private void ProcessEntry(InfoBase infoBase, Guid uuid, string token, int code, ConfigObject tableDescription)
        {
            if (token == MetadataTokens.Fld || token == MetadataTokens.LineNo)
            {
                //if it is field
                processField(infoBase, uuid, token, code);
                return;
            }

            Type type = Configurator.GetTypeByToken(token);
            if (type == null) return; // unsupported type of metadata object


            ApplicationObject metaObject = Configurator.CreateObject(uuid, token, code); //Parts and tables

            if (tableDescription != null)
            {
                ConfigObject tableFieldsDescription = tableDescription.GetObject(4);
                ConfigObject tablePartsDescription = tableDescription.GetObject(5);
                metaObject.Annotation = tableFieldsDescription;
                metaObject.PartsAnnotation = tablePartsDescription;
            }

            if (metaObject == null) return; // unsupported type of metadata object

            if (token == MetadataTokens.VT) //if it is a partitial table
            {
                _ = infoBase.TableParts.TryAdd(uuid, metaObject);
                //Add to table prorerties
                return;
            }

            if (!infoBase.AllTypes.TryGetValue(type, out Dictionary<Guid, ApplicationObject> collection))
            {
                return; // unsupported collection of metadata objects
            }
            //mergereRelationsOfTable(metaObject, tableRelationDescription);

            _ = collection.TryAdd(uuid, metaObject); //all except parts and table fields
        }

        void processField(InfoBase infoBase, Guid uuid, string token, int code)
        {
            MetadataProperty property = Configurator.CreateProperty(uuid, token, code);
            _ = infoBase.Properties.TryAdd(uuid, property);
            return;
        }
    }
}