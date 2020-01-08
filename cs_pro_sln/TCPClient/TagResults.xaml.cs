using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TCPClient
{
    /// <summary>
    /// TagResults.xaml 的交互逻辑
    /// </summary>
    public partial class TagResults : UserControl
    {
        TagReadRecordBindingList _tagList = new TagReadRecordBindingList();

        public TagReadRecordBindingList TagList
        {
            get { return _tagList; }
            set { _tagList = value; }
        }

        public TagResults()
        {
            InitializeComponent();
            GenerateColmnsForDataGrid();
            DataContext = TagList;
        }

        /// <summary>
        /// Generate columns for datagrid
        /// </summary>
        public void GenerateColmnsForDataGrid()
        {
            dgTagResults.AutoGenerateColumns = false;

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

            timeStampColumn.Binding = new Binding("TimeStamp");
            timeStampColumn.Header = "TimeStamp";
            timeStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            rssiColumn.Binding = new Binding("RSSI");
            rssiColumn.Header = "RSSI(dBm)";
            rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            readcountColumn.Binding = new Binding("ReadCount");
            readcountColumn.Header = "ReadCount";
            readcountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            antennaColumn.Binding = new Binding("Antenna");
            antennaColumn.Header = "Antenna";
            antennaColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            protocolColumn.Binding = new Binding("Protocol");
            protocolColumn.Header = "Protocol";
            protocolColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            frequencyColumn.Binding = new Binding("Frequency");
            frequencyColumn.Header = "Frequency";
            frequencyColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            phaseColumn.Binding = new Binding("Phase");
            phaseColumn.Header = "Phase";
            phaseColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            dgTagResults.ItemsSource = TagList;
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

        /// <summary>
        /// Retain aged tag cell colour
        /// </summary>
        public Dictionary<string, Brush> tagagingColourCache = new Dictionary<string, Brush>();
        private void dgTagResults_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                if (true)//chkEnableTagAging)
                {
                    var data = (TagReadRecord)e.Row.DataContext;
                    TimeSpan difftimeInSeconds = (DateTime.UtcNow - data.TimeStamp.ToUniversalTime());
                    BrushConverter brush = new BrushConverter();
                    if (true)//enableTagAgingOnRead)
                    {
                        if (difftimeInSeconds.TotalSeconds < 12)
                        {
                            switch (Math.Round(difftimeInSeconds.TotalSeconds).ToString())
                            {
                                case "5":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFEEEEEE");
                                    break;
                                case "6":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFD3D3D3");
                                    break;
                                case "7":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFCCCCCC");
                                    break;
                                case "8":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFC3C3C3");
                                    break;
                                case "9":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFBBBBBB");
                                    break;
                                case "10":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFA1A1A1");
                                    break;
                                case "11":
                                    e.Row.Background = new SolidColorBrush(Colors.Gray);
                                    break;
                            }
                            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate () { RetainAgingOnStopRead(data.SerialNumber.ToString(), e.Row.Background); }));
                        }
                        else
                        {
                            e.Row.Background = (Brush)brush.ConvertFrom("#FF888888");
                            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate () { RetainAgingOnStopRead(data.SerialNumber.ToString(), e.Row.Background); }));
                        }
                    }
                    else
                    {
                        if (tagagingColourCache.ContainsKey(data.SerialNumber.ToString()))
                        {
                            e.Row.Background = tagagingColourCache[data.SerialNumber.ToString()];
                        }
                        else
                        {
                            e.Row.Background = Brushes.White;
                        }
                    }
                }
            }
            catch { }
        }

        private void RetainAgingOnStopRead(string slno, Brush row)
        {
            if (!tagagingColourCache.ContainsKey(slno))
            {
                tagagingColourCache.Add(slno, row);
            }
            else
            {
                tagagingColourCache.Remove(slno);
                tagagingColourCache.Add(slno, row);
            }
        }
    }
}
