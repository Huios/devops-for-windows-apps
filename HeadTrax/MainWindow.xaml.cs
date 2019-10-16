using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Web;
using System.Deployment.Application;
using System.Collections.Specialized;

namespace HeadTrax
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, Person> Employees;
        public MainWindow()
        {
            InitializeComponent();

            //NameValueCollection nameValueTable = new NameValueCollection();
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                HeadcountLabel.Content = "yes";

                string queryString = "";
                try
                {
                    queryString += ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
                }
                catch (Exception e)
                {
                }
                finally
                {
                    QuickLinksLabel.Content = QuickLinksLabel.Content = queryString;
                }
                //nameValueTable = HttpUtility.ParseQueryString(queryString);
            }

            List<string> log = new List<string>();
            try
            {
                log.Add("Start writing to HKCU");
                using (RegistryKey software = Registry.CurrentUser.OpenSubKey(@"SOFTWARE", true))
                {
                    log.Add("Can open software");
                    software.CreateSubKey("Contoso");
                    using (RegistryKey manufacturer = software.OpenSubKey("Contoso", true))
                    {
                        log.Add("Can create Contoso");
                        manufacturer.CreateSubKey("HeadTrax");
                        using (RegistryKey ProductName = manufacturer.OpenSubKey("HeadTrax", true))
                        {
                            ProductName.SetValue("CurrentVersion", "2.0.0.0");
                            ProductName.SetValue("Company Name", "Contoso Corporation");
                            ProductName.SetValue("DisplayIcon", @"C:\Program Files (x86)\Contoso\HeadTrax\HeadTrax.ico");
                            ProductName.SetValue("DisplayName", "HeadTrax");
                            ProductName.SetValue("HelpLink", "http://hr.contoso.com/headtrax");
                            ProductName.SetValue("Language", "English");
                            ProductName.SetValue("Location", @"C:\Program Files (x86)\Contoso\HeadTrax\");
                            ProductName.SetValue("Publisher", "Contoso");
                            log.Add("Done writing to HKCU");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Add("Exception caught");
                log.Add(e.ToString());
                log.Add(e.Message);
            }
            finally
            {
                //File.WriteAllLines(@"C:\Users\dahoehna\Desktop\HeadTraxLog.txt", log);
            }

            //using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey PluginKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Contoso\HeadTrax\Plugins"))
            {
                if (PluginKey != null)
                {
                    var o = PluginKey.GetValue("Search");
                    if (o != null)
                    {
                        if ((o as string) == "C:\\Program Files (x86)\\Contoso\\HeadTrax\\Plugins\\Search.dll")
                        {
                            SearchGrid.Visibility = Visibility.Visible;
                            Grid.SetRow(EmployeesTable, 1);
                            Grid.SetRowSpan(EmployeesTable, 1);

                            LeftPane.Background = Brushes.DimGray;
                            QuickLinksPane.Background = Brushes.LightGray;
                            QuickLinksPane.BorderBrush = Brushes.Gray;
                            HeadcountsPane.Background = Brushes.LightGray;
                            HeadcountsPane.BorderBrush = Brushes.Gray;
                            EmployeesPane.Background = Brushes.LightGray;
                            EmployeesPane.BorderBrush = Brushes.Gray;

                            QuickLinksLabel.Foreground = Brushes.DarkBlue;
                            HeadcountLabel.Foreground = Brushes.DarkBlue;
                            EmployeesLabel.Foreground = Brushes.DarkBlue;
                            
                            PluginImage.Source = new BitmapImage(new Uri(@"C:\Program Files (x86)\Contoso\HeadTrax\Plugins\Logo.png"));
                        }
                    }
                }
            }

            SetUp();
        }

        private void SetUp()
        {
            //string[] lines = File.ReadAllLines(@"C:\Program Files (x86)\Contoso\Headtrax\Assets\HeadTraxData.csv");
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\", "HeadTraxData.csv");
            string[] lines = File.ReadAllLines(path);
            //string[] lines = File.ReadAllLines(@"Assets\HeadTraxData.csv");

            Employees = new Dictionary<string, Person>();
            foreach(string line in lines)
            {
                string[] data = line.Split(',');
                Employees.Add(data[0] + ' ' + data[1], new Person(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]));
            }

            for (int i = 0; i < Employees.Count; i++)
            {
                var employee = Employees.ElementAt(i);
                if (!(employee.Value.FirstName == "Samuel" && employee.Value.LastName == "Tirado"))
                {
                    employee.Value.ReportsTo = Employees[employee.Value.ReportsToString];
                    Employees[employee.Value.ReportsToString].Reports.Add(employee.Value);
                }
            }

            //EmployeesTable.ItemsSource = Employees.Values;

            TreeViewItem Company = new TreeViewItem() { Header = "Contoso Corporation", IsExpanded = true };
            EmployeesTree.Items.Add(Company);
            TreeViewItem CEO = new TreeViewItem() { Header = "Samuel Tirado", IsExpanded = true };
            CEO.Selected += EmployeeTreeItemSelected;
            Company.Items.Add(CEO);
            fillTree(CEO);
            CEO.RaiseEvent(new RoutedEventArgs(TreeViewItem.SelectedEvent));
        }

        private void fillTree(TreeViewItem item)
        {
            List<Person> itemReports = Employees[(string)item.Header].Reports;
            if (itemReports.Count == 0)
            {
                return;
            }
            else
            {
                for (int i = 0; i < itemReports.Count; i++)
                {
                    TreeViewItem currentReport = new TreeViewItem() { Header = itemReports[i].FirstName + " " + itemReports[i].LastName };
                    currentReport.Selected += EmployeeTreeItemSelected;
                    item.Items.Add(currentReport);
                    fillTree(currentReport);
                }
            }
        }

        private void EmployeeTreeItemSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            List<Person> currentItemReports = new List<Person>();
            currentItemReports.Add(Employees[(string)item.Header]);
            findReports(Employees[(string)item.Header], currentItemReports);
            EmployeesTable.ItemsSource = currentItemReports;
            e.Handled = true;
        }

        private void findReports(Person item, List<Person> currentItemReports)
        {
            List<Person> itemReports = item.Reports;
            if (itemReports.Count == 0)
            {
                return;
            }
            else
            {
                for (int i = 0; i < itemReports.Count; i++)
                {
                    currentItemReports.Add(itemReports[i]);
                    findReports(itemReports[i], currentItemReports);
                }
            }
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            List<Person> searchResults = new List<Person>();

            foreach (var employee in Employees)
            {
                if ((EmailField.Text == "" || employee.Value.Email.Contains(EmailField.Text)) && (FirstNameField.Text == "" || employee.Value.FirstName.Contains(FirstNameField.Text)) && (LastNameField.Text == "" || employee.Value.LastName.Contains(LastNameField.Text)) && (CostCenterField.Text == "" || employee.Value.CostCenter.Contains(CostCenterField.Text)) && (PositionNumberField.Text == "" || employee.Value.PositionNumber.Contains(PositionNumberField.Text)) && (PersonnelNumberField.Text == "" || employee.Value.PersonnelNumber.Contains(PersonnelNumberField.Text)) && (RequisitionIDField.Text == "" || employee.Value.RequisitionID.Contains(RequisitionIDField.Text)))
                {
                    searchResults.Add(employee.Value);
                }
            }

            EmployeesTable.ItemsSource = searchResults;
        }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PersonnelNumber { get; set; }
        public string PositionNumber { get; set; }
        public string RequisitionID { get; set; }
        public string OfficeNumber { get; set; }
        public string BuildingNumber { get; set; }
        public string CostCenter { get; set; }
        public string WorkPhone { get; set; }
        public string ServiceAwardDate { get; set; }
        public string ReportsToString { get; set; }
        public Person ReportsTo { get; set; }
        public List<Person> Reports { get; set; }
        public Person(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k, string l)
        {
            FirstName = a;
            LastName = b;
            Email = c;
            PersonnelNumber = d;
            PositionNumber = e;
            RequisitionID = f;
            OfficeNumber = g;
            BuildingNumber = h;
            CostCenter = i;
            WorkPhone = j;
            ServiceAwardDate = k;
            ReportsToString = l;
            Reports = new List<Person>();
        }
    }
}
