using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public class UnitTests
    {
        private const string DBNAMES_FILE_NAME = "DBNames";
        private string ConnectionString { get; set; }
        private readonly IMetadataReader metadata;
        private readonly IMetadataFileReader fileReader;
        private readonly IConfigurationFileParser configReader;
        public UnitTests()
        {
            // dajet-metadata
            // trade_11_2_3_159_demo
            // accounting_3_0_72_72_demo
            ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
            fileReader = new MetadataFileReader();
            fileReader.UseConnectionString(ConnectionString);
            metadata = new MetadataReader(fileReader);
            configReader = new ConfigurationFileParser(fileReader);
        }
        [TestMethod("�������� ������� ������������")] public void ReadConfigurationProperties()
        {
            ConfigInfo config = configReader.ReadConfigurationProperties();

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
        }
        [TestMethod("�������� ����� DBNames")] public void ReadDBNames()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = new InfoBase();
            byte[] fileData = fileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader stream = fileReader.CreateReader(fileData))
            {
                IDBNamesFileParser parser = new DBNamesFileParser();
                parser.Parse(stream, infoBase);
            }

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
        [TestMethod("�������� ���� ����������")] public void ReadMetadata()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = metadata.LoadInfoBase();

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }

        [TestMethod] public void MergeFields()
        {
            string metadataName = "����������.�����������������"; //"����������.��������������������";"����������.�����������������������������";
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return;
            string typeName = names[0];
            string objectName = names[1];

            MetaObject metaObject = null;
            Dictionary<Guid, MetaObject> collection = null;
            InfoBase infoBase = metadata.LoadInfoBase();
            if (typeName == "����������") collection = infoBase.Catalogs;
            else if (typeName == "��������") collection = infoBase.Documents;
            else if (typeName == "���������������") collection = infoBase.InformationRegisters;
            else if (typeName == "�����������������") collection = infoBase.AccumulationRegisters;
            if (collection == null) return;

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null) return;

            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            ISqlMetadataReader sqlReader = new SqlMetadataReader();
            sqlReader.UseConnectionString(ConnectionString);
            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return;

            MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
            List<string> targetFields = merger.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = merger.PrepareComparison(sqlFields);

            List<string> delete_list;
            List<string> insert_list;
            merger.Compare(targetFields, sourceFields, out delete_list, out insert_list);

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine();

            int match = targetFields.Count - delete_list.Count;
            int unmatch = sourceFields.Count - match;
            Console.WriteLine("�� �������� = " + (insert_list.Count == unmatch).ToString());
            Console.WriteLine();

            ShowList("target", targetFields);
            Console.WriteLine();
            ShowList("source", sourceFields);
            Console.WriteLine();
            ShowList("delete", delete_list);
            Console.WriteLine();
            ShowList("insert", insert_list);
        }
        private void ShowList(string name, List<string> list)
        {
            Console.WriteLine(name + " (" + list.Count.ToString() + ")" + ":");
            foreach (string item in list)
            {
                Console.WriteLine(" - " + item);
            }
        }


        private readonly MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
        private readonly ISqlMetadataReader sqlReader = new SqlMetadataReader();
        [TestMethod] public void MergePerformance()
        {
            sqlReader.UseConnectionString(ConnectionString);

            //InfoBase infoBase = metadata.LoadInfoBase();
            
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = metadata.LoadInfoBase();

            foreach (var collection in infoBase.ValueTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Values.Count];
                foreach (var metaObject in collection.Values)
                {
                    tasks[i] = Task.Factory.StartNew(
                        MergeFields,
                        metaObject,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }
                Task.WaitAll(tasks);
            }

            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Values.Count];
                foreach (var metaObject in collection.Values)
                {
                    tasks[i] = Task.Factory.StartNew(
                        MergeFields,
                        metaObject,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }
                Task.WaitAll(tasks);
            }

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
        private void MergeFields(object parameters)
        {
            MetaObject metaObject = (MetaObject)parameters;
            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            List<string> targetFields = merger.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = merger.PrepareComparison(sqlFields);
            List<string> delete_list;
            List<string> insert_list;
            merger.Compare(targetFields, sourceFields, out delete_list, out insert_list);
        }



        private void ShowProperties(MetaObject metaObject)
        {
            Console.WriteLine(metaObject.Name + " (" + metaObject.TableName + "):");
            foreach (MetaProperty property in metaObject.Properties)
            {
                Console.WriteLine(" - " + property.Name + " (" + property.Field + ")");
            }
        }
        private MetaObject GetMetaObjectByName(string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return null;
            string typeName = names[0];
            string objectName = names[1];

            Dictionary<Guid, MetaObject> collection = null;
            InfoBase infoBase = metadata.LoadInfoBase();
            if (typeName == "����������") collection = infoBase.Catalogs;
            else if (typeName == "��������") collection = infoBase.Documents;
            else if (typeName == "���������������") collection = infoBase.InformationRegisters;
            else if (typeName == "�����������������") collection = infoBase.AccumulationRegisters;
            if (collection == null) return null;

            return collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
        }
        [TestMethod] public void MergeProperties()
        {
            string[] metadataName = { "����������.�����������������", "����������.��������������������", "����������.�����������������������������" };
            MetaObject metaObject = GetMetaObjectByName(metadataName[0]);
            if (metaObject == null)
            {
                Console.WriteLine($"Metaobject \"{metadataName[0]}\" is not found.");
                return;
            }

            if (metaObject != null)
            {
                ShowProperties(metaObject);
                Console.WriteLine();
                Console.WriteLine("************");
                Console.WriteLine();
            }

            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            ISqlMetadataReader sqlReader = new SqlMetadataReader();
            sqlReader.UseConnectionString(ConnectionString);
            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0)
            {
                Console.WriteLine("SQL fields are not found.");
                return;
            }

            MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
            merger.MergeProperties(metaObject, sqlFields);

            ShowProperties(metaObject);
            Console.WriteLine();
            Console.WriteLine("************");
            Console.WriteLine();

            List<string> targetFields = merger.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = merger.PrepareComparison(sqlFields);
            List<string> delete_list;
            List<string> insert_list;
            merger.Compare(targetFields, sourceFields, out delete_list, out insert_list);

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine();

            int match = targetFields.Count - delete_list.Count;
            int unmatch = sourceFields.Count - match;
            Console.WriteLine("�� �������� = " + (insert_list.Count == unmatch).ToString());
            Console.WriteLine();

            ShowList("delete", delete_list);
            Console.WriteLine();
            ShowList("insert", insert_list);
            Console.WriteLine();
            ShowList("target", targetFields);
            Console.WriteLine();
            ShowList("source", sourceFields);
        }
    }
}