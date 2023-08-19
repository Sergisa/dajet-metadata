using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using Microsoft.Data.SqlClient;

namespace WpfApp2;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : BaseFormBehavior
{
    private InfoBase infoBase;
    private IMetadataService metadataService;

    string defultConnectionString =
        "Data Source=127.0.0.1;Initial Catalog=master;Integrated Security=False;User ID=root;Password=isakovs;Encrypt=False";

    public MainWindow()
    {
        InitializeComponent();
        metadataService = new MetadataService();

        ConnectionAddress.Text = defultConnectionString;
        metadataService
            .UseDatabaseProvider(DatabaseProvider.SQLServer)
            .UseConnectionString(defultConnectionString);
        FieldsList.Items.Add(new MetadataProperty()
        {
            DbName = "_Fld153",
            Alias = "AliasAliasAliasAliasAliasAlias",
            Name = "ГруппыГруппыГруппыГруппыГруппыГруппыГруппыГруппы",
            RelativeTableDbName = "Reference3212",
            RelativeTableName = "Кафедры"
        });
        PartTablesList.Items.Add(new TablePart()
        {
            TableName = "Document1247_VT4287",
            Name = "Списки"
        });
        SelectedTableFieldsList.Items.Add(new MetadataProperty()
        {
            DbName = "_Fld153",
            Alias = "AliasAliasAliasAliasAliasAlias",
            Name = "ГруппыГруппыГруппыГруппыГруппыГруппыГруппыГруппы",
            RelativeTableDbName = "Reference3212",
            RelativeTableName = "Кафедры"
        });
    }

    private void search(string text)
    {
        FieldsList.Items.Clear();
        PartTablesList.Items.Clear();
        ApplicationObject table = infoBase.GetApplicationObjectByTableName(Comand.Text);
        if (table == null)
        {
            Description.Text = "Ничего не нашлось";
        }
        else
        {
            Description.Text = "";
            TableName.Text = table.Name;
            TableAlias.Text = table.Alias;
            fillTableDescription(table, FieldsList);
            table.TableParts.ForEach(table => { PartTablesList.Items.Add(table); });
        }
    }

    private void Search_button_OnClickearch(object sender, RoutedEventArgs e)
    {
        search("");
    }

    private void FieldsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MetadataProperty selectedItem = (MetadataProperty)((ListBox)e.Source).SelectedItem;
        if (selectedItem != null)
        {
            string relatedTableName = selectedItem.RelativeTableDbName;
            if (selectedItem.PropertyType.CanBeReference)
            {
                ApplicationObject relatedTableObject = infoBase.GetApplicationObjectByTableName(relatedTableName);
                fillTableDescription(relatedTableObject, SelectedTableFieldsList);
                selectedTableName.Text = relatedTableObject.Name;
                selectedTableAlias.Text = relatedTableObject.Alias;
            }
        }
    }

    private void fillTableDescription(ApplicationObject table, ListBox listView)
    {
        listView.Items.Clear();
        if (table != null)
        {
            table.Properties.ForEach(property => { listView.Items.Add(property); });
        }
    }

    private void partTableSelection(object sender, SelectionChangedEventArgs e)
    {
        ApplicationObject partTableObject = (ApplicationObject)((ListBox)e.Source).SelectedItem;
        if (partTableObject != null)
        {
            fillTableDescription(partTableObject, SelectedTableFieldsList);
            selectedTableName.Text = partTableObject.Name;
        }
    }

    private void EditConnectionAction(object sender, RoutedEventArgs e)
    {
        DbConnectionForm connectionForm = new DbConnectionForm();
        connectionForm.OnSaved += ConnectionOptionsUpdated;
        connectionForm.Show();
    }

    private void ConnectionOptionsUpdated(object sender, string data)
    {
        MessageBox.Show(data);
        metadataService = new MetadataService();
        string connectionString = data;
        metadataService
            .UseDatabaseProvider(DatabaseProvider.SQLServer)
            .UseConnectionString(connectionString);
        infoBase = metadataService.OpenInfoBase();
        ConnectionAddress.Text = connectionString;
    }

    public void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Comand_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            search("");
        }
    }

    private void ConnectionAction(object sender, RoutedEventArgs e)
    {
        metadataService = new MetadataService();
        metadataService
            .UseDatabaseProvider(DatabaseProvider.SQLServer)
            .UseConnectionString(defultConnectionString);
        infoBase = metadataService.OpenInfoBase();
        ConnectionAddress.Text = defultConnectionString;
    }
}