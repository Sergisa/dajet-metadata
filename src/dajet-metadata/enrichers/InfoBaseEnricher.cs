﻿using DaJet.Metadata.Converters;
using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class InfoBaseEnricher : IContentEnricher
    {
        private const string ROOT_FILE_NAME = "root"; // Config
        private Configurator Configurator { get; }
        private IConfigFileReader FileReader { get; }
        private IConfigObjectConverter TypeInfoConverter { get; }
        public InfoBaseEnricher(Configurator configurator)
        {
            Configurator = configurator;
            FileReader = Configurator.FileReader;
            TypeInfoConverter = Configurator.GetConverter<DataTypeInfo>();
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is InfoBase infoBase)) throw new ArgumentOutOfRangeException();

            infoBase.PlatformRequiredVersion = FileReader.GetPlatformRequiredVersion();

            ConfigObject root = FileReader.ReadConfigObject(ROOT_FILE_NAME);
            ConfigObject config = FileReader.ReadConfigObject(root.GetString(new int[] { 1 }));

            ConfigureConfigInfo(infoBase, config);
            ConfigureSharedProperties(infoBase, config); // Общие реквизиты
            ConfigureCompoundTypes(infoBase, config); // Определяемые типы
        }

        private void ConfigureConfigInfo(InfoBase infoBase, ConfigObject config)
        {
            ConfigInfo info = new ConfigInfo();

            info.Name = config.GetString(new int[] { 3, 1, 1, 1, 1, 2 }); // Имя
            ConfigObject alias = config.GetObject(new int[] { 3, 1, 1, 1, 1, 3 });
            if (alias.Values.Count == 3)
            {
                info.Alias = alias.GetString(new int[] { 2 }); // Синоним
            }
            info.Comment = config.GetString(new int[] { 3, 1, 1, 1, 1, 4 }); // Комментарий

            int version = config.GetInt32(new int[] { 3, 1, 1, 26 }); // Режим совместимости
            if (version == 0) info.Version = 80216;
            else if (version == 1) info.Version = 80100;
            else if (version == 2) info.Version = 80213;
            else info.Version = version;
            // Версия конфигурации
            info.ConfigVersion = config.GetString(new int[] { 3, 1, 1, 15 });
            // Режим использования синхронных вызовов расширений платформы и внешних компонент
            info.SyncCallsMode = (SyncCallsMode)config.GetInt32(new int[] { 3, 1, 1, 41 });
            // Режим использования модальности
            info.ModalWindowMode = (ModalWindowMode)config.GetInt32(new int[] { 3, 1, 1, 36 });
            // Режим управления блокировкой данных в транзакции по умолчанию
            info.DataLockingMode = (DataLockingMode)config.GetInt32(new int[] { 3, 1, 1, 17 });
            // Режим автонумерации объектов
            info.AutoNumberingMode = (AutoNumberingMode)config.GetInt32(new int[] { 3, 1, 1, 19 });
            // Режим совместимости интерфейса
            info.UICompatibilityMode = (UICompatibilityMode)config.GetInt32(new int[] { 3, 1, 1, 38 });

            infoBase.ConfigInfo = info;
        }

        private void ConfigureSharedProperties(InfoBase infoBase, ConfigObject config)
        {
            // 3.1.8.0 = 15794563-ccec-41f6-a83c-ec5f7b9a5bc1 - идентификатор коллекции общих реквизитов
            Guid collectionUuid = config.GetUuid(new int[] { 3, 1, 8, 0 });
            if (collectionUuid == new Guid("15794563-ccec-41f6-a83c-ec5f7b9a5bc1"))
            {
                // количество объектов в коллекции
                int count = config.GetInt32(new int[] { 3, 1, 8, 1 });
                if (count == 0) return;

                // 3.1.8 - коллекция общих реквизитов
                ConfigObject collection = config.GetObject(new int[] { 3, 1, 8 });

                int offset = 2;
                SharedProperty property;
                for (int i = 0; i < count; i++)
                {
                    property = new SharedProperty()
                    {
                        FileName = collection.GetUuid(new int[] { i + offset })
                    };
                    ConfigureSharedProperty(property, infoBase);
                    infoBase.SharedProperties.Add(property.FileName, property);
                }
            }
        }
        private void ConfigureSharedProperty(SharedProperty property, InfoBase infoBase)
        {
            ConfigObject cfo = FileReader.ReadConfigObject(property.FileName.ToString());

            if (infoBase.Properties.TryGetValue(property.FileName, out MetadataProperty propertyInfo))
            {
                property.DbName = propertyInfo.DbName;
            }
            property.Name = cfo.GetString(new int[] { 1, 1, 1, 1, 2 });
            ConfigObject aliasDescriptor = cfo.GetObject(new int[] { 1, 1, 1, 1, 3 });
            if (aliasDescriptor.Values.Count == 3)
            {
                property.Alias = cfo.GetString(new int[] { 1, 1, 1, 1, 3, 2 });
            }
            property.AutomaticUsage = (AutomaticUsage)cfo.GetInt32(new int[] { 1, 6 });

            // 1.1.1.2 - описание типов значений общего реквизита
            ConfigObject propertyTypes = cfo.GetObject(new int[] { 1, 1, 1, 2 });
            property.PropertyType = (DataTypeInfo)TypeInfoConverter.Convert(propertyTypes);

            Configurator.ConfigureDatabaseFields(property);

            // 1.2.1 - количество объектов метаданных, у которых значение использования общего реквизита не равно "Автоматически"
            int count = cfo.GetInt32(new int[] { 1, 2, 1 });
            if (count == 0) return;
            int step = 2;
            count *= step;
            int uuidIndex = 2;
            int usageOffset = 1;
            while (uuidIndex <= count)
            {
                Guid uuid = cfo.GetUuid(new int[] { 1, 2, uuidIndex });
                SharedPropertyUsage usage = (SharedPropertyUsage)cfo.GetInt32(new int[] { 1, 2, uuidIndex + usageOffset, 1 });
                property.UsageSettings.Add(uuid, usage);
                uuidIndex += step;
            }
        }

        private void ConfigureCompoundTypes(InfoBase infoBase, ConfigObject config)
        {
            // 3.1.23.0 = c045099e-13b9-4fb6-9d50-fca00202971e - идентификатор коллекции определяемых типов
            Guid collectionUuid = config.GetUuid(new int[] { 3, 1, 23, 0 });
            if (collectionUuid == new Guid("c045099e-13b9-4fb6-9d50-fca00202971e"))
            {
                // 3.1.23.1 - количество объектов в коллекции
                int count = config.GetInt32(new int[] { 3, 1, 23, 1 });
                if (count == 0) return;

                // 3.1.23 - коллекция определяемых типов
                ConfigObject collection = config.GetObject(new int[] { 3, 1, 23 });

                // 3.1.23.N - идентификаторы файлов определяемых типов
                int offset = 2;
                CompoundType compound;
                for (int i = 0; i < count; i++)
                {
                    compound = new CompoundType()
                    {
                        FileName = collection.GetUuid(new int[] { i + offset })
                    };
                    ConfigureCompoundType(compound, infoBase);
                    infoBase.CompoundTypes.Add(compound.FileName, compound);
                }
            }
        }
        private void ConfigureCompoundType(CompoundType compound, InfoBase infoBase)
        {
            ConfigObject cfo = FileReader.ReadConfigObject(compound.FileName.ToString());

            compound.Name = cfo.GetString(new int[] { 1, 3, 2 });
            ConfigObject alias = cfo.GetObject(new int[] { 1, 3, 3 });
            if (alias.Values.Count == 3)
            {
                compound.Alias = cfo.GetString(new int[] { 1, 1, 1, 1, 3, 2 });
            }
            // 1.3.4 - комментарий

            // 1.4 - описание типов значений определяемого типа
            ConfigObject propertyTypes = cfo.GetObject(new int[] { 1, 4 });
            compound.TypeInfo = (DataTypeInfo)TypeInfoConverter.Convert(propertyTypes);
        }
    }
}