using System;
using System.Collections;
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

namespace TCPClient
{
    /// <summary>
    /// TagResults.xaml 的交互逻辑
    /// </summary>
    public partial class TagResults : UserControl
    {
        List<TagReadRecord> _tagList = new List<TagReadRecord>();

        public List<TagReadRecord> TagList
        {
            get { return _tagList; }
            set { _tagList = value; }
        }

        public TagResults()
        {
            InitializeComponent();
            GenerateColmnsForDataGrid();
            this.DataContext = TagList;
        }

        /// <summary>
        /// Generate columns for datagrid
        /// </summary>
        public void GenerateColmnsForDataGrid()
        {
            serialNoColumn.Binding = new Binding("SerialNumber");
            serialNoColumn.Header = "#";
            serialNoColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

            epcColumn.Binding = new Binding("epcString");
            epcColumn.Header = "EPC";
            epcColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            
            tidColumn.Binding = new Binding("tidString");
            tidColumn.Header = "TID";
            tidColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            reservedColumn.Binding = new Binding("reservedString");
            reservedColumn.Header = "Reserved";
            reservedColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            userColumn.Binding = new Binding("userString");
            userColumn.Header = "User";
            userColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            
            rssiColumn.Binding = new Binding("RSSI");
            rssiColumn.Header = "RSSI(dBm)";
            rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            readcountColumn.Binding = new Binding("ReadCount");
            readcountColumn.Header = "ReadCount";
            readcountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            //readtimeColumn.Binding = new Binding("ReadTime");
            //readtimeColumn.Header = "ReadTime";
            //readtimeColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            dgTagResults.ItemsSource = _tagList;
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HeadCheck(sender, e, true);
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HeadCheck(sender, e, false);
        }

        private void HeadCheck(object sender, RoutedEventArgs e, bool IsChecked)
        {
            foreach (TagReadRecord mf in dgTagResults.Items)
            {
                mf.Checked = IsChecked;
            }
            dgTagResults.Items.Refresh();
        }
    }
}
