namespace DaJet.Metadata.Model;

public class TableAnnotation : ConfigObject
{
    public string getTableName()
    {
        return GetString(0);
    }

    public string getTableIndex()
    {
        return GetString(2);
    }

    public string getBaseTable()
    {
        return GetString(3);
    }

    public FieldsAnnotation getFields()
    {
        return (FieldsAnnotation)GetObject(4);
    }
}