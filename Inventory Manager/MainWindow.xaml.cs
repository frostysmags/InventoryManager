using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace Inventory_Manager
{
    public class InventoryItem
    {
        public string InvName { get; set; }
        public int InvQuantity { get; set; }
        public int InvIdeal { get; set; }
        public int InvNeeded { get; set; }
        public string InvPrio { get; set; }
    }

    public partial class MainWindow : Window
    {
        static private string _requestPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\InventoryRequest-" + DateTime.Today.ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".txt";
        static private string _csvPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Inventory-" + DateTime.Today.ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".csv";
        static private string _flPath = "\\\\nebula\\mis\\Software\\IT Tools\\adellorfano\\IM\\";
        static private string _flFName = "Inventory.dat";
        private string _flFile = _flPath + _flFName;
        private int DataGridSelection = -1;
        public bool matchTest(object de)
        {
            InventoryItem filterTest = de as InventoryItem;
            bool tester = false;
            if (!(SearchBar.Text.Length > filterTest.InvName.Length))
            {
                for (var i = 0; i < filterTest.InvName.Length; i++)
                {
                    if (i + SearchBar.Text.Length <= filterTest.InvName.Length)
                    {
                        tester = (filterTest.InvName.Substring(i, SearchBar.Text.Length).ToLower() == SearchBar.Text.ToLower());
                        if (tester == true)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
            
        }
        public MainWindow()
        {
            InitializeComponent();
            MainDataGrid.CanUserResizeRows = false;
            ReadInventoryData();
            UpdateNeedCount();
            CreateBackup();
        }

        #region Selection Events
        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGridSelection = MainDataGrid.SelectedIndex;
            if (DataGridSelection != -1)
            {
                InventoryItem SelectedItem = MainDataGrid.SelectedItem as InventoryItem;
                SlctdInvName.Text = SelectedItem.InvName.ToString();
                SlctdInvQuantity.Text = SelectedItem.InvQuantity.ToString();
                SlctdInvIdealQuantity.Text = SelectedItem.InvIdeal.ToString();
                SlctdInvPriority.Text = SelectedItem.InvPrio.ToString();
            }
        }
        #endregion

        #region Button Click Events
        private void UpdateDataGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (SlctdInvName.Text != null && SlctdInvName.Text != "")
            {
                SearchBar.Text = "";
                InventoryItem SelectedItem = MainDataGrid.SelectedItem as InventoryItem;
                if (SlctdInvNewPrio.SelectedIndex != -1)
                {
                    SelectedItem.InvPrio = SlctdInvNewPrio.Text;
                    PushDataGridUpdate(SelectedItem);
                }
                if (SlctdInvNewIdealQuantity.Text != "" && Int32.Parse(SlctdInvNewIdealQuantity.Text) > 0)
                {
                    SelectedItem.InvIdeal = Int32.Parse(SlctdInvNewIdealQuantity.Text);
                    PushDataGridUpdate(SelectedItem);
                }
                else
                {
                    PushDataGridUpdate(SelectedItem);
                }
                SlctdInvNewIdealQuantity.Text = "";
                SlctdInvNewQuantity.Text = "";
                SlctdInvNewPrio.SelectedIndex = -1;
                QuantityOperationOption.SelectedIndex = 0;
                MainDataGrid.Items.Refresh();
                SlctdInvName.Text = SelectedItem.InvName.ToString();
                SlctdInvQuantity.Text = SelectedItem.InvQuantity.ToString();
                SlctdInvIdealQuantity.Text = SelectedItem.InvIdeal.ToString();
                UpdateFile();
                UpdateNeedCount();
            }
        }

        private void RemoveInventoryItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBar.Text == "" || SearchBar.Text == null)
            {
                if (MessageBox.Show("Are you sure you'd like to remove the selected entry?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (DataGridSelection != -1)
                    {
                        SearchBar.Text = "";
                        SlctdInvName.Text = "";
                        SlctdInvQuantity.Text = "";
                        SlctdInvIdealQuantity.Text = "";
                        MainDataGrid.Items.Remove(MainDataGrid.Items[DataGridSelection]);
                        DataGridSelection = -1;
                        MainDataGrid.SelectedItem = null;
                        UpdateFile();
                        UpdateNeedCount();
                    }
                }
            }
        }

        private void NewAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewAddName.Text != "" && NewAddQuantity.Text != "" && NewAddIdealQuantity.Text != "" 
                && Int32.Parse(NewAddIdealQuantity.Text) != 0 && NewAddPrioComboBox.SelectedIndex != -1)
            {
                SearchBar.Text = "";
                InventoryItem newInvItem = new InventoryItem();
                newInvItem.InvName = NewAddName.Text;
                newInvItem.InvQuantity = Int32.Parse(NewAddQuantity.Text);
                newInvItem.InvIdeal = Int32.Parse(NewAddIdealQuantity.Text);
                newInvItem.InvPrio = NewAddPrioComboBox.Text;
                MainDataGrid.Items.Add(newInvItem);
                NewAddName.Text = "";
                NewAddQuantity.Text = "";
                NewAddIdealQuantity.Text = "";
                NewAddPrioComboBox.SelectedIndex = -1;
                UpdateFile();
                UpdateNeedCount();
            }
        }

        private void CreateCSVButton_Click(object sender, RoutedEventArgs e)
        {
            ExportCSV();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBar.Text = "";
            MainDataGrid.Items.Clear();
            ReadInventoryData();
            UpdateNeedCount();
        }

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            CreateRequestFile();
        }

        #endregion

        #region Checks
        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValid(((TextBox)sender).Text + e.Text);
        }

        public static bool IsValid(string str)
        {
            int i;
            return int.TryParse(str, out i);
        }

        private void Grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainDataGrid.Items.Filter = new Predicate<object>(matchTest); 
        }

        void OnFileUpdate(object source, FileSystemEventArgs e)
        {
            MainDataGrid.Items.Clear();
            ReadInventoryData();
            UpdateNeedCount();
        }
        #endregion

        #region Functions
        private void PushDataGridUpdate(InventoryItem SelectedItem)
        {
            if (SlctdInvNewQuantity.Text != null && SlctdInvNewQuantity.Text != "" 
                && SlctdInvName.Text != null && SlctdInvName.Text != "")
            {
                if (QuantityOperationOption.SelectedIndex == 0)
                {
                    SelectedItem.InvQuantity = SelectedItem.InvQuantity + Int32.Parse(SlctdInvNewQuantity.Text);
                }
                else
                {
                    SelectedItem.InvQuantity = SelectedItem.InvQuantity - Int32.Parse(SlctdInvNewQuantity.Text);
                }
                UpdateNeedCount();
            }
        }

        private void RecalculateNeeded(InventoryItem SelectedItem)
        {
            switch (SelectedItem.InvIdeal - SelectedItem.InvQuantity <= 0)
            {
                case true:
                    SelectedItem.InvNeeded = 0;
                    break;
                case false:
                    SelectedItem.InvNeeded = SelectedItem.InvIdeal - SelectedItem.InvQuantity;
                    break;
            }
        }
        private void UpdateFile()
        {
            if (!File.Exists(_flFile))
            {
                File.Create(_flFile).Close();
            }
            InventoryItem[] Invlst = new InventoryItem[MainDataGrid.Items.Count];
            string[] invFileDat = new string[Invlst.Length];
            for (var i = 0; i < MainDataGrid.Items.Count; i++)
            {
                Invlst[i] = (InventoryItem)MainDataGrid.Items.GetItemAt(i);
                invFileDat[i] = Invlst[i].InvName + "☼" + Invlst[i].InvQuantity + "☼" +
                        Invlst[i].InvIdeal + "☼" + Invlst[i].InvPrio + "☼";
            }
            File.WriteAllLines(_flFile, invFileDat);
        }

        private void ReadInventoryData()
        {
            if (File.Exists(_flFile))
            {
                string[] inventoryData = new string[File.ReadLines(_flFile).Count()];
                inventoryData = File.ReadAllLines(_flFile);
                for (var i = 0; i < inventoryData.Length; i++)
                {
                    InventoryItem item = new InventoryItem();
                    string[] Ix = inventoryData[i].Split('☼');
                    item.InvName = Ix[0];
                    item.InvQuantity = Int32.Parse(Ix[1]);
                    item.InvIdeal = Int32.Parse(Ix[2]);
                    item.InvPrio = Ix[3];
                    MainDataGrid.Items.Add(item);
                }
            }
            else
            {
                File.Create(_flFile).Close();
            }
        }

        private void ExportCSV()
        {
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("Inventory Item, Item Quantity, Ideal Quantity, Approximate Needed");
            for (var i = 0; i < MainDataGrid.Items.Count; i++)
            {
                InventoryItem GridItem = (InventoryItem)MainDataGrid.Items[i];
                csvContent.AppendLine(GridItem.InvName + ", " + GridItem.InvQuantity.ToString()
                    + ", " + GridItem.InvIdeal.ToString() + ", " + GridItem.InvNeeded.ToString() + ", " + GridItem.InvPrio);
            }
            File.AppendAllText(_csvPath, csvContent.ToString());
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        private void UpdateNeedCount()
        {
            for (var i = 0; i < MainDataGrid.Items.Count; i++)
            {
                InventoryItem GridItem = (InventoryItem)MainDataGrid.Items[i];
                RecalculateNeeded(GridItem);
            }
        }

        private void CreateBackup()
        {
            if (File.Exists(_flPath + "invBak" + "-" + DateTime.Today.AddDays(-4).ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat"))
            {
                for (var i = 0; i < 10; i++)
                {
                    if (File.Exists(_flPath + "invBak" + "-" + DateTime.Today.AddDays(-5-i).ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat"))
                    {
                        File.Delete(_flPath + "invBak" + "-" + DateTime.Today.AddDays(-5 - i).ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat");
                    }
                }
            }

            if (File.Exists(_flFile))
            {
                if (!File.Exists(_flPath + "invBak" + "-" + DateTime.Today.ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat"))
                {
                    string[] rLns = File.ReadAllLines(_flFile);
                    File.Create(_flPath + "invBak" + "-" + DateTime.Today.ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat").Close();
                    File.WriteAllLines(_flPath + "invBak" + "-" + DateTime.Today.ToString().Substring(0, DateTime.Today.ToString().IndexOf(" ")).Replace("/", "-") + ".dat", rLns);
                }
            }
        }

        private void CreateRequestFile()
        {
            StringBuilder RequestContent = new StringBuilder();
            RequestContent.AppendLine("Hi, I'm making an Inventory Request out for the following items as they are in low supply or we are out of them:");
            for (var i = 0; i < MainDataGrid.Items.Count; i++)
            {
                InventoryItem GridItem = (InventoryItem)MainDataGrid.Items[i];

                double percentage = ((double)GridItem.InvQuantity / (double)GridItem.InvIdeal) * 100;

                if (GridItem.InvNeeded > 0)
                {
                    if (GridItem.InvPrio == "3 Very Low" && percentage < 10)
                    {
                        RequestContent.AppendLine(GridItem.InvName + " - " + GridItem.InvNeeded.ToString());
                    }
                    else if (GridItem.InvPrio == "2 Low" && percentage < 30)
                    {
                        RequestContent.AppendLine(GridItem.InvName + " - " + GridItem.InvNeeded.ToString());
                    }
                    else if (GridItem.InvPrio == "1 Medium" && percentage < 50)
                    {
                        RequestContent.AppendLine(GridItem.InvName + " - " + GridItem.InvNeeded.ToString());
                    }
                    else if (GridItem.InvPrio == "0 High" && percentage < 70)
                    {
                        RequestContent.AppendLine(GridItem.InvName + " - " + GridItem.InvNeeded.ToString());
                    }
                }
            }
            File.AppendAllText(_requestPath, RequestContent.ToString());
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        #endregion

    }
}
