using System;
using System.Collections.Generic;
using System.Linq;
using DaJet.Metadata.Model;
using static Crayon.Output;

namespace DaJet.Metadata.CLI;

public class TableDescriber
{
    private readonly Dictionary<Guid, ApplicationObject> _databaseStructure;
    private readonly ApplicationObject _table;

    public TableDescriber(Dictionary<Guid, ApplicationObject> databaseStructure, ApplicationObject table)
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

    private void PrintField(int level, MetadataProperty field)
    {
        PrintLevel(level + 1, $"{field.Name} <{Green().Bold().Text(field.DbName)}>");
        if (field.PropertyType.CanBeReference)
        {
            ApplicationObject relatedTable = _databaseStructure.Values.FirstOrDefault(o =>
            {
                return o.Uuid.ToString() == field.PropertyType.ReferenceTypeUuid.ToString();
            });
            if (relatedTable != null)
            {
                Console.WriteLine(Reversed($"\t{relatedTable.TableName} " + Red($" {relatedTable.Name}")));
            }
            else
            {
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine();
        }
    }

    private void DescribeTable(int level, ApplicationObject table)
    {
        PrintLnLevel(level, Bold().Underline().Text(table.TableName + " -> " + table.Name + ":"));
        //поля
        table.Properties.ForEach(property => { PrintField(level, property); });
    }
}