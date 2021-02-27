﻿using DaJet.Metadata.Model;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения метаданных прикладных объектов конфигурации 1С
    /// </summary>
    public interface IMetadataReader
    {
        ///<summary>Загружает метаданные прикладных объектов конфигурации 1С из SQL Server</summary>
        ///<returns>Возвращает объект, содержащий метаданные прикладных объектов конфигурации 1С</returns>
        InfoBase LoadInfoBase();
    }
    /// <summary>
    /// Класс, реализующий интерфейс <see cref="IMetadataReader"/>, для чтения метаданных из SQL Server
    /// </summary>
    public sealed class MetadataReader : IMetadataReader
    {
        private sealed class ReadMetaUuidParameters
        {
            internal InfoBase InfoBase { get; set; }
            internal MetaObject MetaObject { get; set; }
        }

        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IMetadataFileReader MetadataFileReader;
        private readonly IDBNamesFileParser DBNamesFileParser = new DBNamesFileParser();
        private readonly IMetaObjectFileParser MetaObjectFileParser = new MetaObjectFileParser();

        public MetadataReader(IMetadataFileReader metadataFileReader)
        {
            MetadataFileReader = metadataFileReader;
        }

        public InfoBase LoadInfoBase()
        {
            InfoBase infoBase = new InfoBase();
            ReadDBNames(infoBase);
            MetaObjectFileParser.UseInfoBase(infoBase);
            ReadMetaUuids(infoBase);
            ReadMetaObjects(infoBase);
            return infoBase;
        }

        private void ReadDBNames(InfoBase infoBase)
        {
            byte[] fileData = MetadataFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                DBNamesFileParser.Parse(reader, infoBase);
            }
        }
        private void ReadMetaUuids(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    ReadMetaUuidParameters parameters = new ReadMetaUuidParameters()
                    {
                        InfoBase = infoBase,
                        MetaObject = item.Value
                    };
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetaUuid,
                        parameters,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadMetaUuid(object parameters)
        {
            if (!(parameters is ReadMetaUuidParameters input)) return;

            byte[] fileData = MetadataFileReader.ReadBytes(input.MetaObject.UUID.ToString());
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetaObjectFileParser.ParseMetaUuid(stream, input.MetaObject);
            }
            input.InfoBase.MetaReferenceTypes.TryAdd(input.MetaObject.MetaUuid, input.MetaObject);
        }
        private void ReadMetaObjects(InfoBase infoBase)
        {
            ReadValueTypes(infoBase);
            ReadReferenceTypes(infoBase);
        }
        private void ReadValueTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ValueTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetaObject,
                        item.Value,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadReferenceTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetaObject,
                        item.Value,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadMetaObject(object metaObject)
        {
            MetaObject obj = (MetaObject)metaObject;
            byte[] fileData = MetadataFileReader.ReadBytes(obj.UUID.ToString());
            if (fileData == null)
            {
                return; // TODO: log error "Metadata file is not found"
            }
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetaObjectFileParser.ParseMetaObject(stream, obj);
            }
        }
    }
}