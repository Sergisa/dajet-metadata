using System.Text;
using System.Windows;
using DaJet.Metadata.Model;

namespace WpfApp2;

public partial class TableTextView : BaseFormBehavior
{
    public TableTextView(ApplicationObject table)
    {
        InitializeComponent();
        FormTitle.Text = table.TableName + " " + table.Alias;
        StringBuilder sb = new StringBuilder();
        foreach (MetadataProperty tableField in table.Properties)
        {
            sb.AppendFormat("{0}", tableField.DbName);
            if (tableField.Alias != "")
            {
                sb.AppendFormat(" ({0})", tableField.Alias);
            }

            if (tableField.RelativeTableDbName != "")
            {
                sb.Append("\t" + tableField.RelativeTableDbName);
            }

            sb.Append("\r");
        }

        TableDescription.AppendText(sb.ToString());
    }
}