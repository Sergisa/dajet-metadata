using System;
using System.Collections.Generic;
using System.Linq;
using DaJet.Metadata.Model;
using static Crayon.Output;

namespace DaJet.Metadata.CLI;

public class TableDescriber
{
    private readonly InfoBase _databaseStructure;
    private readonly ApplicationObject _table;

    public TableDescriber(InfoBase databaseStructure, ApplicationObject table)
    {
        this._databaseStructure = databaseStructure;
        this._table = table;
    }

    public void Describe()
    {
        DescribeTable(0, _table);
        //Добавочне таблицы
        _table.TableParts.ForEach(part => { DescribeTable(1, part); });
    }

    private void PrintLnLevel(int level, string message)
    {
        PrintLevel(level, message);
        Console.WriteLine();
    }

    private void PrintLevel(int level, string message)
    {
        string preffix = "";
        for (int i = 0; i < level; i++)
        {
            preffix += '\t';
        }

        Console.Write(preffix + message);
    }

    private void DescribeField(int level, MetadataProperty field)
    {
        PrintLevel(level + 1, $"{field.Name} <{Green().Bold().Text(field.DbName)}>");
        if (field.PropertyType.CanBeReference)
        {
            Console.WriteLine(Reversed($"\t {field.RelativeTableDbName} " + Red($" {getTableName(field.RelativeTableDbName)} ")));
        }
        else
        {
            Console.WriteLine();
        }
    }

    private string getTableName(string tableName)
    {
       return _databaseStructure.GetApplicationObjectByTableName(tableName).Name;
    }

    private void DescribeTable(int level, ApplicationObject table)
    {
        PrintLnLevel(level, Bold().Underline().Text(table.TableName + " -> " + table.Name + ":"));
        //поля
        table.Properties.ForEach(property => { DescribeField(level, property); });
    }
}