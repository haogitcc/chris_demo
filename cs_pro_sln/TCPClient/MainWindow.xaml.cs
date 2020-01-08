using Microsoft.Win32;
using NetAPI;
using NetAPI.Core;
using NetAPI.Entities;
using NetAPI.Protocol.VRP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TCPClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialTransportTCP tcpClient = null;
        string server_ip = null;
        int server_port = 0;

        /// <summary>
        /// Delegates 
        /// </summary>
        delegate void del();
        private delegate void EmptyDelegate();

        private DateTime startAsyncReadTime;
        public double continuousreadElapsedTime = 0.0;
        DispatcherTimer readRatePerSec = null;
        DispatcherTimer dispatchtimer = null;

        LogUtils logUtils = new LogUtils();
        TagDatabase tagdb = new TagDatabase();

        public MainWindow()
        {
            InitializeComponent();
            IsTagRead_stackpanel.Visibility = Visibility.Collapsed;
            IsTagInventory_stackpanel.Visibility = Visibility.Collapsed;
            connect_type_combobox.ItemsSource = null;
            connect_type_combobox.Items.Clear();
            connect_type_combobox.ItemsSource = InitConnectType();
            connect_type_combobox.SelectedIndex = 0;

            readRatePerSec = new DispatcherTimer();
            readRatePerSec.Tick += new EventHandler(readRatePerSec_Tick);
            readRatePerSec.Interval = TimeSpan.FromMilliseconds(900);
            //readRatePerSec.Start();

            dispatchtimer = new DispatcherTimer();
            dispatchtimer.Tick += new EventHandler(dispatchtimer_Tick);
            dispatchtimer.Interval = TimeSpan.FromMilliseconds(50);
            //dispatchtimer.Start();

            InitUI();
            //InitCmdList();
        }

        private void dispatchtimer_Tick(object sender, EventArgs e)
        {
            try
            {
                TagResults.tagagingColourCache.Clear();
                lock(tagdb)
                {
                    tagdb.Repaint();
                }
            }
            catch { }
        }

        private void readRatePerSec_Tick(object sender, EventArgs e)
        {
            if (lbltotalTagsReadValue.Content.ToString() != "")
                UpdateReadRate(CalculateElapsedTime());
        }

        private double CalculateElapsedTime()
        {
            TimeSpan elapsed = (DateTime.Now - startAsyncReadTime);
            // elapsed time + previous cached async read time
            double totalseconds = continuousreadElapsedTime + elapsed.TotalSeconds;
            lbltotalReadTimeValue.Content = Math.Round(totalseconds, 2).ToString();
            return totalseconds;
        }

        private void UpdateReadRate(double totalElapsedSeconds)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                long temp = Convert.ToInt64(lbltotalTagsReadValue.Content.ToString());
                lblReadRatePerSecValue.Content = (Math.Round((temp / totalElapsedSeconds), 2)).ToString();
            }));
        }

        private IEnumerable InitConnectType()
        {
            List<string> list = new List<string>();
            list.Add("TCP");
            list.Add("RS232");
            //list.Add("RS485");
            return list;
        }

        private void InitCmdList()
        {
            cmd_combobox.Items.Clear();
            cmd_combobox.ItemsSource = InitCommandSet();
        }

        private List<string> InitCommandSet()
        {
            List<string> list = new List<string>();
            for (byte cmd = 0x09; cmd <= 0x53; cmd++)
            {
                Byte[] b = new byte[] { 0x02, (byte)cmd };

                list.Add(ByteFormat.ToHex(b, "", ""));
            }
            for (byte cmd = 0x01; cmd <= 0x16; cmd++)
            {
                Byte[] b = new byte[] { 0x03, (byte)cmd };

                list.Add(ByteFormat.ToHex(b, "", ""));
            }
            for (byte cmd = 0x01; cmd <= 0x07; cmd++)
            {
                Byte[] b = new byte[] { 0x06, (byte)cmd };

                list.Add(ByteFormat.ToHex(b, "", ""));
            }
            for (byte cmd = 0x01; cmd <= 0x0D; cmd++)
            {
                Byte[] b = new byte[] { 0x10, (byte)cmd };

                list.Add(ByteFormat.ToHex(b, "", ""));
            }
            return list;
        }
        
        private void Connec_button_Click(object sender, RoutedEventArgs e)
        {
            if(connec_button.Content.Equals("Connect"))
            {
                connec_button.Content = "DisConnect";
                server_ip = server_ip_textbox.Text;
                server_port = int.Parse(server_port_textbox.Text);
                tcpClient = new SerialTransportTCP(server_ip, server_port);
                tcpClient.Open();
            }
            else if (connec_button.Content.Equals("DisConnect"))
            {
                connec_button.Content = "Connect";
                if (tcpClient.IsOpen)
                {
                    tcpClient.Shutdown();
                }
            }
        }

        private void Send_button_Click(object sender, RoutedEventArgs e)
        {
            if(tcpClient.IsOpen)
            {
                byte[] message = null;
                int length = 0;
                int offset = 0;
                message = MakeCmd(cmd_combobox.SelectedValue.ToString(), "");
                //message = ByteFormat.FromHex(cmd_combobox.SelectedValue.ToString());
                length = message.Length;
                tcpClient.SendBytes(length, message, offset);

                byte[] messageSpace = new byte[256];
                length = messageSpace.Length;
                int count = tcpClient.ReceiveBytes(length, messageSpace, offset);
                if (count > 0)
                {
                    log_listview.Items.Add(string.Format("{0}, {1}", count, ByteFormat.ToHex(messageSpace, "", " ").Substring(0, count * 2 + (count -1))));
                }
            }
        }

        private byte[] MakeCmd(string hexStr_cmd, string hexStr_data)
        {
            Console.WriteLine(string.Format("Cmd={0}, data={1}", hexStr_cmd, hexStr_data));
            string temp = string.Empty;
            byte hdr = 0x55;
            byte pc = 0x00; // 232 - 0x00, 232 - 0x10, 485 - 0x20, 485 - 0x30

            byte[] cmd = ByteFormat.FromHex(hexStr_cmd);
            byte[] data = ByteFormat.FromHex(hexStr_data);
            byte[] len = ByteConv.EncodeU32((uint)cmd.Length + (uint)data.Length);
            byte[] crc = null;
            string msg = string.Empty;

            if (pc == 0x20 || pc == 0x30)
            {
                byte dev_addr = 0x00; // pc - 0x20/0x30 才有效，否则不包含
                return null;
            }

            temp = string.Format("{0}{1}{2}{3}", ByteFormat.ToHex(new byte[] { pc }, "", ""),
                                                ByteFormat.ToHex(len, "", "").Substring(4, 4),
                                                ByteFormat.ToHex(cmd, "", ""),
                                                ByteFormat.ToHex(data, "", ""));

            crc = CalcCRC_16(ByteFormat.FromHex(temp));

            msg = string.Format("{0}{1}{2}{3}{4}{5}", ByteFormat.ToHex(new byte[] { hdr }, "", ""),
                                                           ByteFormat.ToHex(new byte[] { pc }, "", ""),
                                                           ByteFormat.ToHex(len, "", "").Substring(4, 4),
                                                           ByteFormat.ToHex(cmd, "", ""),
                                                           ByteFormat.ToHex(data, "", ""),
                                                           ByteFormat.ToHex(crc, "", ""));

            log_listview.Items.Add(temp);
            log_listview.Items.Add(msg);
            return ByteFormat.FromHex(msg);
        }



        #region CRC Calculation Methods

        //*******************************************************
        //*              Calculates CRC                         *
        //*******************************************************

        #region CalcCRC

        /// <summary>
        /// Calculates CRC
        /// </summary>
        /// <param name="command">Byte Array that needs CRC calculation</param>
        /// <returns>CRC Byte Array</returns>
        private static byte[] CalcCRC(byte[] command)
        {
            UInt16 tempcalcCRC1 = CalcCRC8(65535, command[1]);
            tempcalcCRC1 = CalcCRC8(tempcalcCRC1, command[2]);
            byte[] CRC = new byte[2];

            if (command[1] != 0)
            {
                for (int i = 0; i < command[1]; i++)
                    tempcalcCRC1 = CalcCRC8(tempcalcCRC1, command[3 + i]);
            }

            CRC = BitConverter.GetBytes(tempcalcCRC1);

            Array.Reverse(CRC);

            return CRC;
        }


        private static byte[] CalcCRC_16(byte[] command)
        {
            ushort tempcalcCRC1 = CalcCRC16(0x0000, command[1]);
            tempcalcCRC1 = CalcCRC16(tempcalcCRC1, command[2]);
            byte[] CRC = new byte[2];

            if (command[1] != 0)
            {
                for (int i = 0; i < command[1]; i++)
                    tempcalcCRC1 = CalcCRC16(tempcalcCRC1, command[3 + i]);
            }

            CRC = BitConverter.GetBytes(tempcalcCRC1);

            Array.Reverse(CRC);

            return CRC;
        }


        #endregion

        #region CalcReturnCRC

        /// <summary>
        /// Calculates CRC of the data returned from the M5e,
        /// </summary>
        /// <param name="command">Byte Array that needs CRC calculation</param>
        /// <returns>CRC Byte Array</returns>
        private static byte[] CalcReturnCRC(byte[] command)
        {
            UInt16 tempcalcCRC1 = CalcCRC8(65535, command[1]);
            tempcalcCRC1 = CalcCRC8(tempcalcCRC1, command[2]);
            byte[] CRC = new byte[2];

            //if (command[1] != 0)
            {
                for (int i = 0; i < (command[1] + 2); i++)
                    tempcalcCRC1 = CalcCRC8(tempcalcCRC1, command[3 + i]);
            }

            CRC = BitConverter.GetBytes(tempcalcCRC1);

            Array.Reverse(CRC);

            return CRC;
        }

        #endregion

        #region CalcCRC8

        private static UInt16 CalcCRC8(UInt16 beginner, byte ch)
        {
            byte[] tempByteArray;
            byte xorFlag;
            byte element80 = new byte();
            element80 = 0x80;
            byte chAndelement80 = new byte();
            bool[] forxorFlag = new bool[16];

            for (int i = 0; i < 8; i++)
            {
                tempByteArray = BitConverter.GetBytes(beginner);
                Array.Reverse(tempByteArray);
                BitArray tempBitArray = new BitArray(tempByteArray);

                for (int j = 0; j < tempBitArray.Count; j++)
                    forxorFlag[j] = tempBitArray[j];

                Array.Reverse(forxorFlag, 0, 8);
                Array.Reverse(forxorFlag, 8, 8);

                for (int k = 0; k < tempBitArray.Count; k++)
                    tempBitArray[k] = forxorFlag[k];

                xorFlag = BitConverter.GetBytes(tempBitArray.Get(0))[0];
                beginner = (UInt16)(beginner << 1);
                chAndelement80 = (byte)(ch & element80);

                if (chAndelement80 != 0)
                    ++beginner;

                if (xorFlag != 0)
                    beginner = (UInt16)(beginner ^ 0x1021);

                element80 = (byte)(element80 >> 1);
            }

            return beginner;
        }

        #endregion

        #region CalcCRC16
        private static ushort[] CRCtable = new ushort[256]{
            0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
            0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
            0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
            0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
            0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
            0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
            0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
            0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
            0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
            0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
            0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
            0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
            0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
            0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
            0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
            0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
            0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
            0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
            0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
            0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
            0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
            0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
            0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
            0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
            0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
            0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
            0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
            0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
            0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
            0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
            0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
            0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
        };

        private static ushort CalcCRC16(ushort LastCRC, byte CheckByte)
        {
            return (ushort)((LastCRC >> 8) ^ CRCtable[(LastCRC & 0xFF) ^ CheckByte]);
        }
        #endregion

        #endregion

        #region Vboto

        Reader reader = null;
        string readerName = string.Empty;
        IPort port = null;
        private byte antennasCount = 0;
        private byte antennasMinPower = 0;
        private byte antennasMaxPower = 0;

        private void Vboto_connect_button_Click(object sender, RoutedEventArgs e)
        {
            if(vboto_connect_button.Content.Equals("Connect"))
            {
                vboto_connect_button.Content = "Disconnect";
                InitVRPReader();
                reader =new Reader(readerName, port);
                ConnectResponse connectRespone = reader.Connect();
                if(connectRespone.IsSucessed)
                {
                    logUtils.Log(string.Format("connectRespone.IsSucessed={0}", connectRespone.IsSucessed));
                    reader.OnBrokenNetwork += OnVPRBrokenNetwork;
                    Vboto_clear_button_Click(sender, e);
                    bool ret = false;
                    MsgPowerOff powerOff = new MsgPowerOff();
                    ret = reader.Send(powerOff);
                    if(ret)
                    {
                        InitStatus();
                        TagResults.dgTagResults.ItemsSource = null;
                        TagResults.dgTagResults.ItemsSource = tagdb.TagList;
                    }
                    else
                    {
                        if (reader != null && reader.IsConnected)
                        {
                            Reader.OnApiException -= OnVRPApiException;
                            Reader.OnErrorMessage -= OnVRPErrorMessage;

                            reader.OnBrokenNetwork -= OnVPRBrokenNetwork;
                            reader.Disconnect();
                            reader.Dispose();
                            reader = null;
                        }
                        vboto_connect_button.Content = "Connect";
                        InitUI();
                        return;
                    }
                }
                else
                {
                    logUtils.Log(string.Format("connectRespone.ErrorInfo={0}, {1}", 
                        connectRespone.ErrorInfo.ErrCode,
                        connectRespone.ErrorInfo.ErrMsg));
                    vboto_connect_button.Content = "Connect";
                }
                
            }
            else if (vboto_connect_button.Content.Equals("Disconnect"))
            {
                if (vboto_scan_button.Content.Equals("Scaning"))
                {
                    Vboto_scan_button_Click(sender, e);
                }
                vboto_connect_button.Content = "Connect";
                readRatePerSec.Stop();
                if (reader != null && reader.IsConnected)
                {
                    Reader.OnApiException -= OnVRPApiException;
                    Reader.OnErrorMessage -= OnVRPErrorMessage;

                    reader.OnBrokenNetwork -= OnVPRBrokenNetwork;
                    reader.Disconnect();
                    reader.Dispose();
                    reader = null;
                }
                InitUI();
            }
        }

        private void Vboto_clear_button_Click(object sender, RoutedEventArgs e)
        {
            Thread st = new Thread(delegate ()
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate ()
                {
                    lock (tagdb)
                    {
                        tagdb.Clear();
                        tagdb.Repaint();
                        startAsyncReadTime = DateTime.Now;
                        continuousreadElapsedTime = 0.0;
                    }
                }));

                Dispatcher.BeginInvoke(new ThreadStart(delegate ()
                {
                    msg_get_listview.Items.Clear();
                    lbltotalTagsReadValue.Content = "0";
                    lblUniqueTagsReadValue.Content = "0";
                    lbltotalReadTimeValue.Content = "0";
                    lblReadRatePerSecValue.Content = "0";
                }));
            });
            st.Start();
        }

        private void InitStatus()
        {
            reader_info_listview.ItemsSource = null;
            reader_info_listview.Items.Clear();
            reader_info_listview.ItemsSource = GetReaderInfo();

            baudrate_combobox.SelectedValue = GetBaudrate();
            region_combobox.SelectedValue = GetRegion();

            antennas_info_label.Content = GetAntennasInfo();
            SetAntennasCapability();


            UpdateAntennas();

            frequency_combobox.SelectedValue = GetFrequency();
            protocol_combobox.SelectedValue = GetProtocol();
        }

        private string GetAntennasInfo()
        {
            bool ret = false;
            MsgReaderCapabilityQuery msgReaderCapabilityQuery = new MsgReaderCapabilityQuery();
            ret = reader.Send(msgReaderCapabilityQuery);
            logUtils.Log(string.Format("msgReaderCapabilityQuery: {0}", ret));
            if (ret)
            {
                return string.Format("Model={0},Count={1},[{2},{3}]",
                    msgReaderCapabilityQuery.ReceivedMessage.ModelNumber,
                    msgReaderCapabilityQuery.ReceivedMessage.AntennaCount,
                    msgReaderCapabilityQuery.ReceivedMessage.MinPowerValue,
                    msgReaderCapabilityQuery.ReceivedMessage.MaxPowerValue);
            }
            else
            {
                return string.Format("Error={0}, {1}", msgReaderCapabilityQuery.ErrorInfo.ErrCode, msgReaderCapabilityQuery.ErrorInfo.ErrMsg);
            }
            return null;
        }

        private void SetAntennasCapability()
        {
            bool ret = false;
            MsgReaderCapabilityQuery msgReaderCapabilityQuery = new MsgReaderCapabilityQuery();
            ret = reader.Send(msgReaderCapabilityQuery);
            logUtils.Log(string.Format("msgReaderCapabilityQuery: {0}", ret));
            if (ret)
            {

                antennasCount = msgReaderCapabilityQuery.ReceivedMessage.AntennaCount;
                antennasMinPower = msgReaderCapabilityQuery.ReceivedMessage.MinPowerValue;
                antennasMaxPower = msgReaderCapabilityQuery.ReceivedMessage.MaxPowerValue;
            }
            else
            {

            }
        }

        private void UpdateAntennas()
        {
            List<AntennaPowerStatus> list = GetAntennas();
            CheckBox[] ant_cb = new CheckBox[] { ant1_checkbox, ant2_checkbox, ant3_checkbox, ant4_checkbox };
            TextBox[] ant_txt = new TextBox[] { ant1_power, ant2_power, ant3_power, ant4_power };
            foreach (AntennaPowerStatus ant in list)
            {
                ant_cb[ant.AntennaNO - 1].IsChecked = ant.IsEnable;
                ant_txt[ant.AntennaNO - 1].Text = string.Format("{0}", ant.PowerValue);
            }
        }

        private String GetFrequency()
        {
            bool ret = false;
            MsgFrequencyConfig msgFrequencyConfig = new MsgFrequencyConfig();
            ret = reader.Send(msgFrequencyConfig);
            logUtils.Log(string.Format("msgGetFrequencyConfig: {0}", ret));
            if (ret)
            {

            }
            else
            {

            }
            return null;
        }

        private AirProtocol GetProtocol()
        {
            bool ret = false;
            MsgAirProtocolConfig msgAirProtocolConfig = new MsgAirProtocolConfig();
            ret = reader.Send(msgAirProtocolConfig);
            logUtils.Log(string.Format("Get AirProtocolConfig: {0}", ret));
            if (ret)
            {
                return msgAirProtocolConfig.ReceivedMessage.Protocol;
            }
            else
            {

            }
            return 0;
        }

        private void SetProtocol(AirProtocol protocol)
        {
            bool ret = false;
            MsgAirProtocolConfig msgAirProtocolConfig = new MsgAirProtocolConfig(protocol);
            ret = reader.Send(msgAirProtocolConfig);
            logUtils.Log(string.Format("msgSetAirProtocolConfig: {0}", ret));
            if (ret)
            {
                logUtils.Log(string.Format("Set AirProtocolConfig {0} success", protocol));
            }
            else
            {

            }
        }

        private FrequencyArea GetRegion()
        {
            bool ret = false;
            MsgUhfBandConfig msgUhfBandConfig = new MsgUhfBandConfig();
            ret = reader.Send(msgUhfBandConfig);
            logUtils.Log(string.Format("Get Region (UhfBandConfig): {0}", ret));
            if (ret)
            {
                return msgUhfBandConfig.ReceivedMessage.UhfBand;
            }
            else
            {

            }
            return 0;
        }

        private BaudRate GetBaudrate()
        {
            bool ret = false;
            MsgRs232BaudRateConfig msgRs232BaudRateConfig = new MsgRs232BaudRateConfig();
            ret = reader.Send(msgRs232BaudRateConfig);
            logUtils.Log(string.Format("msgGetRs232BaudRateConfig: {0}", ret));
            if (ret)
            {
                return msgRs232BaudRateConfig.ReceivedMessage.RS232BaudRate;
            }
            else
            {

            }
            return 0;
        }

        private void SetBaudrate(BaudRate baudRate)
        {
            return;
            // Todo : Now our tcp transport not support set baudrate when we are connect
            bool ret = false;
            MsgRs232BaudRateConfig msgRs232BaudRateConfig = new MsgRs232BaudRateConfig(baudRate);
            ret = reader.Send(msgRs232BaudRateConfig);
            logUtils.Log(string.Format("msgSetRs232BaudRateConfig: {0}", ret));
            if (ret)
            {
                
            }
            else
            {

            }
        }

        private List<AntennaPowerStatus> GetAntennas()
        {
            List<AntennaPowerStatus> list = new List<AntennaPowerStatus>();
            bool ret = false;
            MsgRfidStatusQuery msgRfidStatusQuery = new MsgRfidStatusQuery();
            ret = reader.Send(msgRfidStatusQuery);
            logUtils.Log(string.Format("msgRfidStatusQuery: {0}", ret));
            if (ret)
            {
                logUtils.Log(string.Format("Protocol:{0}", msgRfidStatusQuery.ReceivedMessage.Protocol));
                logUtils.Log(string.Format("Region:{0}", msgRfidStatusQuery.ReceivedMessage.UhfBand));
                logUtils.Log(string.Format("Status:{0}", msgRfidStatusQuery.ReceivedMessage.Status));
                

                foreach (AntennaPowerStatus antenna in msgRfidStatusQuery.ReceivedMessage.Antennas)
                {
                    if(antenna.AntennaNO <= 4)
                    {
                        logUtils.Log(string.Format("antennaPower [{0}, {1}, {2}]", antenna.AntennaNO, antenna.IsEnable, antenna.PowerValue));
                        list.Add(antenna);
                    }
                }
            }
            else
            {

            }
            return list;
        }

        

        // TagInventory 
        private void ScanEPC(bool IsSetParam)
        {
            if (IsSetParam == true)
            {
                InventoryTagParameter param = new InventoryTagParameter();
                //public ushort ReadCount;
                //public ushort TotalReadTime;
                //public ushort TagFilteringTime;
                //public ushort ReadTime;
                //public ushort StopTime;
                //public ushort IdleTime;
                //public TagParameter SelectTagParam;
                TagParameter tagParameter = null;
                if (is_MemBank_use_checkbox.IsChecked == true)
                {
                    tagParameter = new TagParameter();
                    MemoryBank_combobox.SelectedIndex = MemoryBank_combobox.Items.IndexOf(MemoryBank.EPCMemory);// Not set TID
                    tagParameter.Ptr = Convert.ToUInt16(MemoryBank_startaddr_textbox.Text);
                    tagParameter.TagData = ByteFormat.FromHex(MemoryBank_data_textbox.Text);
                    logUtils.Log(string.Format("TagData {0}", ByteFormat.ToHex(tagParameter.TagData, "", " ")));
                }

                param.SelectTagParam = tagParameter;
                
                param.ReadTime = Convert.ToUInt16(TagInventory_readtime_textbox.Text);
                param.StopTime = Convert.ToUInt16(TagInventory_stoptime_textbox.Text);

                param.TotalReadTime = Convert.ToUInt16(TagInventory_totalreadtime_textbox.Text);

                param.ReadCount = Convert.ToUInt16(TagInventory_readcount_textbox.Text);

                param.IdleTime = Convert.ToUInt16(TagInventory_idletime_textbox.Text);

                param.TagFilteringTime = Convert.ToUInt16(TagInventory_tagfilteringtime_textbox.Text);

                TagInventory(param);
            }
            else
            {
                TagInventory();
            }
        }

        private void Scan(bool IsSetParam)
        {
            if (IsSetParam == true)
            {
                ReadTagParameter param = new ReadTagParameter();
                param.IsLoop = (bool)isloop_checkbox.IsChecked;
                param.AccessPassword = new byte[] { 0x00, 0x00, 0x00, 0x00 };
                param.IsReturnEPC = (bool)epc_checkbox.IsChecked;
                param.IsReturnTID = (bool)tid_checkbox.IsChecked;// 32 ～112bits
                param.IsReturnReserved = (bool)reserved_checkbox.IsChecked; // kill pass + access pass
                if ((bool)user_checkbox.IsChecked)
                {
                    param.UserPtr = Convert.ToUInt32(userptr_textbox.Text);
                    param.UserLen = Convert.ToByte(userlen_textbox.Text);// 0~127 word，0代表不返回User数据
                }
                else
                {
                    param.UserPtr = 0;
                    param.UserLen = 0;
                }
                param.ReadCount = Convert.ToUInt16(readcount_textbox.Text);

                param.ReadTime = Convert.ToUInt16(readtime_textbox.Text);

                param.ReadTime = Convert.ToUInt16(totalreadtime_textbox.Text);

                tagdb.Update(param);

                TagRead(param);
            }
            else
            {
                TagRead();
            }
        }

        private void TagInventory()
        {
            bool ret = false;
            MsgTagInventory tagInventory = new MsgTagInventory();
            ret = reader.Send(tagInventory);
            logUtils.Log(string.Format("TagInventory: {0}", ret));
            if (ret == true)
            {
                logUtils.Log("TagInventory success");
            }
            else
            {
                logUtils.Log("TagInventory", tagInventory.ErrorInfo);
            }
        }

        private void TagInventory(InventoryTagParameter param)
        {
            bool ret = false;
            MsgTagInventory tagInventory = new MsgTagInventory(param);
            ret = reader.Send(tagInventory);
            logUtils.Log(string.Format("TagInventory with param: {0}", ret));
            if (ret == true)
            {
                logUtils.Log("TagInventory with param success");
            }
            else
            {
                logUtils.Log("TagInventory with param", tagInventory.ErrorInfo);
            }
        }

        private void TagRead()
        {
            //public bool IsLoop = false;
            //public byte[] AccessPassword = new byte[4];
            //public bool IsReturnEPC = false;
            //public bool IsReturnTID = false;
            //public UInt32 UserPtr;
            //public byte UserLen;
            //public bool IsReturnReserved = false;
            //public ushort ReadCount = 1;// 注意：此处默认读取 1 次
            //public ushort ReadTime = 100;
            bool ret = false;
            MsgTagRead msgTagRead = new MsgTagRead();
            ret = reader.Send(msgTagRead);
            logUtils.Log(string.Format("TagRead: {0}", ret));
            if (ret == true)
            {
                logUtils.Log("TagRead success");
            }
            else
            {
                logUtils.Log("TagRead", msgTagRead.ErrorInfo);
            }
        }

        private void TagRead(ReadTagParameter param)
        {
            bool ret = false;
            MsgTagRead msgTagRead = new MsgTagRead(param);
            ret = reader.Send(msgTagRead);
            logUtils.Log(string.Format("TagRead with param: {0}", ret));
            if (ret == true)
            {

            }
            else
            {
                logUtils.Log("TagRead with param", msgTagRead.ErrorInfo);
            }
        }

        private void Vboto_scan_button_Click(object sender, RoutedEventArgs e)
        {
            if (vboto_scan_button.Content.Equals("Scan"))
            {
                vboto_scan_button.Content = "Scaning";
                vboto_clear_button.IsEnabled = false;
               
                startAsyncReadTime = DateTime.Now;
                readRatePerSec.Start();
                if (cbRefreshRate.IsChecked.Value == true)
                {
                    ValidateRefreshRate();
                    dispatchtimer.Start();
                }

                reader.OnInventoryReceived += OnVRPInventoryReceived;
                tagdb.UniqueByteTID = is_unique_by_membank_checkbox.IsChecked.Value;

                if (is_TagInventory_radiobutton.IsChecked == true)
                {
                    ScanEPC((bool)IsTagInventory_use_param_checkbox.IsChecked);
                }
                else if (is_TagRead_radiobutton.IsChecked == true)
                {
                    Scan((bool)IsTagRead_use_param_checkbox.IsChecked);
                }
            }
            else if (vboto_scan_button.Content.Equals("Scaning"))
            {
                vboto_scan_button.Content = "Scan";
                vboto_clear_button.IsEnabled = true;
                continuousreadElapsedTime = CalculateElapsedTime();
                readRatePerSec.Stop();
                if (cbRefreshRate.IsChecked.Value == true)
                    dispatchtimer.Stop();
                reader.OnInventoryReceived -= OnVRPInventoryReceived;

                bool ret = false;
                MsgPowerOff msgPowerOff = new MsgPowerOff();
                ret = reader.Send(msgPowerOff);
                logUtils.Log(string.Format("msgPowerOff: {0}", ret));
                if (ret)
                {
                    logUtils.Log("MsgPowerOff success");
                }
                else
                {
                    logUtils.Log("MsgPowerOff", msgPowerOff.ErrorInfo);
                }
            }

        }

        private void ValidateRefreshRate()
        {
            if (txtRefreshRate.Text != "")
            {
                int refreshrate = 0;
                try
                {
                    refreshrate = Convert.ToInt32(txtRefreshRate.Text.TrimEnd());
                }
                catch { throw new Exception("Please input the refresh rate between 100 and 999"); }
                if ((refreshrate < 100) || (refreshrate > 999))
                {
                    throw new Exception("Please input the refresh rate between 100 and 999");
                }
                else
                {
                    try
                    {
                        if (null != dispatchtimer)
                        {
                            dispatchtimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(txtRefreshRate.Text));
                        }
                    }
                    catch (Exception ex)
                    {
                        Onlog(ex);
                    }
                }
            }
            else
            {
                throw new Exception("Please input the refresh rate between 100 and 999");
            }
        }

        private void Onlog(Exception ex)
        {
            try
            {
                bool disconnectReader = false;
                if (!string.IsNullOrWhiteSpace(ex.Message))
                {
                    if (!(ex is NullReferenceException || ex is IndexOutOfRangeException) && !ex.Message.Contains("ItemsControl"))
                    {
                        Onlog(ex.Message);
                        Dispatcher.BeginInvoke(new ThreadStart(delegate ()
                        {
                            if (ex.Message.ToLower().Contains("the operation has timed out.") || ex.Message.ToLower().Contains("timeout") || ex.Message.ToLower().Contains("the device is not connected"))
                            {
                                MessageBox.Show("Connection to the reader is lost. Disconnecting the reader.", "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                                disconnectReader = true;
                            }
                        }));
                    }
                }
            }
            catch (Exception)
            { }
        }

        void Onlog(string message)
        {
            Console.WriteLine(string.Format("######## message={0}", message));
        }

        private void Baudrate_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reader == null)
                return;
            BaudRate baudRate = (BaudRate)baudrate_combobox.SelectedValue;
            logUtils.Log(string.Format("{0}", baudRate));
            if (GetBaudrate() == baudRate)
            {
                return;
            }
            SetBaudrate(baudRate);
        }

        private void Region_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reader == null)
                return;
            FrequencyArea region = FrequencyArea.Unknown;
            if ((FrequencyArea)region_combobox.SelectedValue == FrequencyArea.Unknown)
            {
                region = FrequencyArea.CN; //默认使用 CN
            }
            else
            {
                region = (FrequencyArea)region_combobox.SelectedValue;
                logUtils.Log(string.Format("{0}", region));
                if (GetRegion() == region)
                {
                    return;
                }
            }
            SetRegion(region);

            UpdateRegion();
        }

        private void UpdateRegion()
        {
            region_combobox.SelectedValue = GetRegion();
            FrequencyConfig_region_combobox.SelectedValue = GetRegion();
        }

        private void Protocol_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reader == null)
                return;
            AirProtocol protocol = (AirProtocol)protocol_combobox.SelectedValue;
            logUtils.Log(string.Format("{0}", protocol));
            if (GetProtocol() == protocol)
            {
                return;
            }
            SetProtocol(protocol);
        }

        

        private void Frequency_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reader == null)
                return;
            logUtils.Log(string.Format("{0}", frequency_combobox.SelectedValue));
        }

        private void Antennas_power_update_Click(object sender, RoutedEventArgs e)
        {
            SetAntennasPower();
            SetAntennasEnable();
            UpdateAntennas();
        }

        private void Antennas_all_use_Checked(object sender, RoutedEventArgs e)
        {
            CheckAllAntUse();
        }

        private void Antennas_all_use_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckAllAntUse();
        }

        private void CheckAllAntUse()
        {
            if (antennas_all_use.IsChecked == true)
            {
                TextBox[] tbs = new TextBox[] { ant1_power, ant2_power, ant3_power, ant4_power };

                foreach (TextBox tb in tbs)
                {
                    tb.IsEnabled = false;
                }
            }
            else if (antennas_all_use.IsChecked == false)
            {
                {
                    TextBox[] tbs = new TextBox[] { ant1_power, ant2_power, ant3_power, ant4_power };

                    foreach (TextBox tb in tbs)
                    {
                        tb.IsEnabled = true;
                    }
                }
            }
        }

        private List<string> GetReaderInfo()
        {
            List<string> list = new List<string>();
            bool ret = false;
            MsgReaderVersionQuery msgReaderVersionQuery = new MsgReaderVersionQuery();
            msgReaderVersionQuery.IsReturn = true;
            ret = reader.Send(msgReaderVersionQuery);
            logUtils.Log(string.Format("msgReaderVersionQuery: {0}", ret));
            if (ret == true)
            {
                list.Add(string.Format("ModelNumber     : {0}", msgReaderVersionQuery.ReceivedMessage.ModelNumber));
                list.Add(string.Format("HardwareVersion : {0}", msgReaderVersionQuery.ReceivedMessage.HardwareVersion));
                list.Add(string.Format("SoftwareVersion : {0}", msgReaderVersionQuery.ReceivedMessage.SoftwareVersion));
                list.Add(string.Format("Reader Type     : {0}", reader.ReaderName));
                list.Add(string.Format("Reader Name     : {0}", reader.ReaderName, reader));
                logUtils.Log(string.Format("model={0}," +
                    "\r\nhardware={1}," +
                    "\r\nsoftware={2}",
                    msgReaderVersionQuery.ReceivedMessage.ModelNumber,
                    msgReaderVersionQuery.ReceivedMessage.HardwareVersion,
                    msgReaderVersionQuery.ReceivedMessage.SoftwareVersion));
            }
            else
            {

            }
            return list;
        }

        private void OnVRPReaderMessageReceived(AbstractReader reader, IReaderMessage msg)
        {
            logUtils.Log(string.Format("OnVRPReaderMessageReceived {0}", reader.ReaderName));
        }

        private void InitVRPReader()
        {
            if(connect_type_combobox.SelectedValue.Equals("TCP"))
            {
                string IP = vboto_ip_textbox.Text;//192.168.8.166
                int PORT = Convert.ToInt32(vboto_port_textbox.Text.ToString());//8086
                readerName = string.Format("VRP TCP Reader {0}:{1}", IP, PORT);
                port = new TcpClientPort(IP, PORT);
            }
            else if (connect_type_combobox.SelectedValue.Equals("RS232"))
            {
                string portName = string.Format("{0}", rs232_combobox.SelectedValue);
                readerName = string.Format("VRP RS232 Reader {0}", portName);
                MatchCollection mc = Regex.Matches(portName, @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                        portName = m.ToString();
                }
                
                BaudRate baudRate = BaudRate.R115200;
                if(baudrate_combobox.SelectedIndex < 0)
                {

                }
                else
                {
                    baudRate = (BaudRate)baudrate_combobox.SelectedValue;
                }
                port = new Rs232Port(portName, baudRate);
            }
            else if (connect_type_combobox.SelectedValue.Equals("RS485"))
            {
                string portName = null;
                BaudRate baudRate = default(BaudRate);
                byte[] addresses = null;//1 -255
                port = new Rs485Port(portName, baudRate, addresses);
            }

            Reader.OnApiException += OnVRPApiException;
            Reader.OnErrorMessage += OnVRPErrorMessage;            
        }

        private void InitUI()
        {
            baudrate_combobox.ItemsSource = null;
            baudrate_combobox.ItemsSource = GetSupportBaudrate();

            region_combobox.ItemsSource = null;
            region_combobox.ItemsSource = GetSupportRegion();
            
            frequency_combobox.ItemsSource = null;
            frequency_combobox.ItemsSource = GetSupportFrequency();

            protocol_combobox.ItemsSource = null;
            protocol_combobox.ItemsSource = GetSupportProtocol();

            is_TagInventory_radiobutton.IsChecked = true;

            TagResults.protocolColumn.Visibility = Visibility.Collapsed;
            TagResults.frequencyColumn.Visibility = Visibility.Collapsed;
            TagResults.phaseColumn.Visibility = Visibility.Collapsed;
        }

        private List<AntennaPowerStatus> GetPowers()
        {
            return GetAntennas();
        }

        private void SetPowers(byte[] powers)
        {
            //查询天线功率
            bool ret = false;

            // Todo : 有问题
            //MsgPowerConfig msgPowerConfig = new MsgPowerConfig();
            //ret = reader.Send(msgPowerConfig);
            //Log(string.Format("msgPowerConfig: {0}", ret));
            //if (ret)
            //{
            //    foreach (AntennaPower antenna in msgPowerConfig.ReceivedMessage.Powers)
            //    {
            //        Log(string.Format("antennaPower [{0}, {1}]", antenna.AntennaNO, antenna.PowerValue));
            //    }
            //}
            //else
            //{

            //}

            //设置功率
            MsgPowerConfig msgSetPowerConfig = new MsgPowerConfig(powers);
            ret = reader.Send(msgSetPowerConfig);
            logUtils.Log(string.Format("msgSetPowerConfig: {0}", ret));
            if (ret == true)
            {

            }
            else
            {
                MessageBox.Show(string.Format("ret={0}, {1}", ret, msgSetPowerConfig.ErrorInfo.ErrMsg));
            }
        }


        private void SetAntennas(AntennaStatus[] antennaStatusesList)
        {
            //查询Antenna
            bool ret = false;
            //MsgAntennaConfig msgAntennaConfig = new MsgAntennaConfig();
            //ret = reader.Send(msgAntennaConfig);
            //Log(string.Format("msgUhfBandConfig: {0}", ret));
            //if (ret)
            //{
            //    foreach(AntennaStatus antenna in msgAntennaConfig.ReceivedMessage.AntennaStatusAry)
            //    {
            //        Log(string.Format("antenna [{0}, {1}]", antenna.AntennaNO, antenna.IsEnable));
            //    }
            //}
            //else
            //{

            //}

            // //设置Antenna
            List<AntennaStatus> antennaStatuses = new List<AntennaStatus>();
            AntennaStatus ant1 = new AntennaStatus();
            ant1.AntennaNO = 1;
            ant1.IsEnable = true;

            antennaStatuses.Add(ant1);
            
            MsgAntennaConfig msgSetAntennaConfig = new MsgAntennaConfig(antennaStatuses.ToArray());
            ret = reader.Send(msgSetAntennaConfig);
            logUtils.Log(string.Format("msgSetUhfBandConfig: {0}", ret));
            if (ret)
            {

            }
            else
            {

            }
        }

        private void SetRegion(FrequencyArea region)
        {
            //查询Region
            bool ret = false;
            //MsgUhfBandConfig msgUhfBandConfig = new MsgUhfBandConfig();
            //ret = reader.Send(msgUhfBandConfig);
            //Log(string.Format("msgUhfBandConfig: {0}", ret));
            //if (ret)
            //{
            //    region_combobox.SelectedValue = msgUhfBandConfig.ReceivedMessage.UhfBand;
            //}
            //else
            //{

            //}

            //设置Region
            MsgUhfBandConfig msgSetUhfBandConfig = new MsgUhfBandConfig(region);
            ret = reader.Send(msgSetUhfBandConfig);
            logUtils.Log(string.Format("msgSetUhfBandConfig: {0}", ret));
            if (ret)
            {
                logUtils.Log(string.Format("Set Region {0} success", region));
            }
            else
            {

            }
        }

        private List<BaudRate> GetSupportBaudrate()
        {
            List<BaudRate> list = new List<BaudRate>();
            foreach(BaudRate baudRate in Enum.GetValues(typeof(BaudRate)))
            {
                list.Add(baudRate);
            }
            return list;
        }
        
        private List<FrequencyArea> GetSupportRegion()
        {
            List<FrequencyArea> list = new List<FrequencyArea>();
            foreach(FrequencyArea region in Enum.GetValues(typeof(FrequencyArea)))
            {
                list.Add(region);
            }
            return list;
        }

        private IEnumerable GetSupportFrequency()
        {
            List<UhfBandTable.Name> list = new List<UhfBandTable.Name>();
            foreach (UhfBandTable.Name freq in Enum.GetValues(typeof(UhfBandTable.Name)))
            {
                list.Add(freq);
            }
            return list;
        }

        private IEnumerable GetSupportProtocol()
        {
            List<AirProtocol> list = new List<AirProtocol>();
            foreach (AirProtocol protocol in Enum.GetValues(typeof(AirProtocol)))
            {
                list.Add(protocol);
            }
            return list;
        }

        private void ReleaseVRPReader()
        {
            readerName = string.Empty;
            port = null;
            Reader.OnApiException -= OnVRPApiException;
            Reader.OnErrorMessage -= OnVRPErrorMessage;
        }

        private void OnVRPErrorMessage(IReaderMessage e)
        {
            logUtils.Log(string.Format("{0}:", "OnVRPErrorMessage"), e);
        }
        
        private void OnVRPApiException(string senderName, ErrInfo e)
        {
            logUtils.Log(string.Format("{0}: {1}", "OnVRPApiException", senderName), e);
        }

        private void OnVPRBrokenNetwork(string senderName, ErrInfo e)
        {
            logUtils.Log(string.Format("{0}: {1}", "OnVPRBrokenNetwork", senderName), e);
        }

        private void OnVRPInventoryReceived(string readerName, RxdTagData tagData)
        {
            PrintTag(tagData);
        }

        private void PrintTag(RxdTagData tagData)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                lock(tagdb)
                {
                    tagdb.Add(tagData);
                    if (cbRefreshRate.IsChecked.Value == false)
                        tagdb.Repaint();
                }
            }));

            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                lblUniqueTagsReadValue.Content = tagdb.UniqueTagCount;
                lbltotalTagsReadValue.Content = tagdb.TotalTagCount;
            }));
        }

        private void GetUTC()
        {
            bool ret = false;
            MsgUtcConfig msgUtcConfig = new MsgUtcConfig();
            ret = reader.Send(msgUtcConfig);
            if (ret == true)
            {
                logUtils.Log(string.Format("GetUTC {0}", msgUtcConfig.ReceivedMessage.UTC));
            }
            else
            {
                logUtils.Log("GetUTC", msgUtcConfig.ErrorInfo);
            }

        }


        #endregion

        private void Connect_type_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string connectType = connect_type_combobox.SelectedValue.ToString();
            switch(connectType)
            {
                case "TCP":
                    tcp_stackpanel.Visibility = Visibility.Visible;
                    rs232_stackpanel.Visibility = Visibility.Collapsed;
                    rs485_combobox.Visibility = Visibility.Collapsed;
                    baudrate_combobox.IsEnabled = false;
                    break;
                case "RS232":
                    tcp_stackpanel.Visibility = Visibility.Collapsed;
                    rs232_stackpanel.Visibility = Visibility.Visible;
                    rs485_combobox.Visibility = Visibility.Collapsed;
                    baudrate_combobox.IsEnabled = true;
                    UpdateRs232();
                    break;
                case "RS485":
                    tcp_stackpanel.Visibility = Visibility.Collapsed;
                    rs232_stackpanel.Visibility = Visibility.Collapsed;
                    rs485_combobox.Visibility = Visibility.Visible;
                    baudrate_combobox.IsEnabled = false;
                    break;
            }
        }

        private void UpdateRs232()
        {
            logUtils.Log("########### UpdateRs232");
            rs232_combobox.ItemsSource = null;
            rs232_combobox.ItemsSource = GetComPortNames();
            if(rs232_combobox.Items.Count > 0)
            {
                rs232_combobox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Returns the COM port names as list
        /// </summary>
        private List<string> GetComPortNames()
        {
            List<string> portNames = new List<string>();
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if ((queryObj != null) && (queryObj["Name"] != null))
                    {
                        if (queryObj["Name"].ToString().Contains("(COM"))
                            portNames.Add(queryObj["Name"].ToString());
                    }
                }
            }
            return portNames;
        }

        public List<string> GetComList()
        {
            try
            {
                
                RegistryKey keyCom = Registry.LocalMachine.OpenSubKey("Hardware\\DeviceMap\\SerialComm");

                List<string> portNames = new List<string>();
                portNames.AddRange(keyCom.GetValueNames());
                return portNames;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<string> GetPort()
        {
            try
            {
                RegistryKey hklm = Registry.LocalMachine;

                RegistryKey software11 = hklm.OpenSubKey("HARDWARE");

                //打开"HARDWARE"子健
                RegistryKey software = software11.OpenSubKey("DEVICEMAP");

                RegistryKey sitekey = software.OpenSubKey("SERIALCOMM");

                List<string> portNames = new List<string>();
                portNames.AddRange(sitekey.GetValueNames());
                return portNames;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        private void Antennas_enable_button_Click(object sender, RoutedEventArgs e)
        {
            SetAntennasEnable();
        }

        private void Antennas_power_button_Click(object sender, RoutedEventArgs e)
        {
            SetAntennasPower();
        }

        private void SetAntennasEnable()
        {
            bool ret = false;
            CheckBox[] cbs = new CheckBox[] { ant1_checkbox, ant2_checkbox, ant3_checkbox, ant4_checkbox };
            // SetEnable
            List<AntennaStatus> antennasList = new List<AntennaStatus>();
            foreach (CheckBox cb in cbs)
            {
                byte antNo = Convert.ToByte(cb.Content);
                bool antEnable = cb.IsChecked.Value;
                logUtils.Log(string.Format("antNo={0}, {1}", antNo, antEnable));
                AntennaStatus ant = new AntennaStatus();
                ant.AntennaNO = antNo;
                ant.IsEnable = antEnable;
                antennasList.Add(ant);
            }

            AntennaStatus[] antennas = antennasList.ToArray();
            MsgAntennaConfig msgAntennaConfig = new MsgAntennaConfig(antennas);
            ret = reader.Send(msgAntennaConfig);
            logUtils.Log(string.Format("msgAntennaConfig: {0}", ret));
            if (ret == true)
            {

            }
            else
            {
                MessageBox.Show(string.Format("ret={0}, {1}", ret, msgAntennaConfig.ErrorInfo.ErrMsg));
            }
        }

        private void SetAntennasPower()
        {
            TextBox[] tbs = new TextBox[] { ant1_power, ant2_power, ant3_power, ant4_power };
            // SetPower
            if (antennas_all_use.IsChecked == true)
            {
                byte power = Byte.Parse(ant_readpower.Text);
                if(power < antennasMinPower || power >= antennasMaxPower)
                {
                    MessageBox.Show(string.Format("power all use one {0} not in [{1}, {2}), set to min", power, antennasMinPower, antennasMaxPower));
                    power = antennasMinPower;
                }
                byte[] powers = new byte[] { power, power, power, power };
                SetPowers(powers);
            }
            else
            {
                List<byte> list = new List<byte>();
                foreach (TextBox tb in tbs)
                {
                    byte power = Byte.Parse(tb.Text);
                    if (power < antennasMinPower || power >= antennasMaxPower)
                    {
                        MessageBox.Show(string.Format("power {0} not in [{1}, {2}), , set to min", power, antennasMinPower, antennasMaxPower));
                        power = antennasMinPower;
                    }
                    list.Add(power);
                }
                byte[] powers = list.ToArray();
                SetPowers(powers);
            }
        }

        private void Get_FilteringTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", GetFilteringTimeConfig()));
        }

        private void Set_FilteringTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            ushort timeout = Convert.ToUInt16(set_FilteringTimeConfig_textbox.Text);
            msg_get_listview.Items.Add(string.Format("{0}", SetFilteringTimeConfig(timeout)));
        }

        // FilteringTimeConfig
        private object GetFilteringTimeConfig()
        {
            bool ret = false;
            MsgFilteringTimeConfig msgFilteringTimeConfig = new MsgFilteringTimeConfig();
            ret = reader.Send(msgFilteringTimeConfig);
            if(ret == true)
            {
                ushort time = msgFilteringTimeConfig.ReceivedMessage.Time;
                return string.Format("Get FilteringTimeConfig time={0} (100ms), [0 - 65535]", time);
            }
            else
            {
                ErrInfo errInfo = msgFilteringTimeConfig.ErrorInfo;
                return string.Format("Get FilteringTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object SetFilteringTimeConfig(ushort timeout)
        {
            bool ret = false;
            MsgFilteringTimeConfig msgFilteringTimeConfig = new MsgFilteringTimeConfig(timeout);
            ret = reader.Send(msgFilteringTimeConfig);
            if (ret == true)
            {
                return string.Format("Set FilteringTimeConfig time={0} (100ms) success", timeout);
            }
            else
            {
                ErrInfo errInfo = msgFilteringTimeConfig.ErrorInfo;
                return string.Format("Set FilteringTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private void Get_IntervalTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", GetIntervalTimeConfig()));
        }

        private void Set_IntervalTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            ushort readtime = Convert.ToUInt16(set_IntervalTimeConfig_readtime_textbox.Text);
            ushort stoptime = Convert.ToUInt16(set_IntervalTimeConfig_stoptime_textbox.Text);
            msg_get_listview.Items.Add(string.Format("{0}", SetIntervalTimeConfig(readtime, stoptime)));
        }
        // IntervalTimeConfig
        private object GetIntervalTimeConfig()
        {
            bool ret = false;
            MsgIntervalTimeConfig msgIntervalTimeConfig = new MsgIntervalTimeConfig();
            ret = reader.Send(msgIntervalTimeConfig);
            if (ret == true)
            {
                ushort readtime = msgIntervalTimeConfig.ReceivedMessage.ReadTime;
                ushort stoptime = msgIntervalTimeConfig.ReceivedMessage.StopTime;
                return string.Format("Get IntervalTimeConfig readtime={0} ms, stoptime={1} ms", readtime, stoptime);
            }
            else
            {
                ErrInfo errInfo = msgIntervalTimeConfig.ErrorInfo;
                return string.Format("Get IntervalTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object SetIntervalTimeConfig(ushort readtime, ushort stoptime)
        {
            bool ret = false;
            MsgIntervalTimeConfig msgIntervalTimeConfig = new MsgIntervalTimeConfig(readtime, stoptime);
            ret = reader.Send(msgIntervalTimeConfig);
            if (ret == true)
            {
                return string.Format("Set IntervalTimeConfig readtime={0} ms, stoptime={1} ms success", readtime, stoptime);
            }
            else
            {
                ErrInfo errInfo = msgIntervalTimeConfig.ErrorInfo;
                return string.Format("Set IntervalTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private void Get_RssiThresholdConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", GetRssiThresholdConfig()));
        }

        private void Set_RssiThresholdConfig_button_Click(object sender, RoutedEventArgs e)
        {
            byte rssi = Convert.ToByte(set_RssiThresholdConfig_textbox.Text);
            msg_get_listview.Items.Add(string.Format("{0}", SetRssiThresholdConfig(rssi)));
        }

        // RssiThresholdConfig
        private object GetRssiThresholdConfig()
        {
            bool ret = false;
            MsgRssiThresholdConfig msgRssiThresholdConfig = new MsgRssiThresholdConfig();
            ret = reader.Send(msgRssiThresholdConfig);
            if (ret == true)
            {
                byte rssi = msgRssiThresholdConfig.ReceivedMessage.RSSI;
                return string.Format("Get RssiThresholdConfig rssi={0}", rssi);
            }
            else
            {
                ErrInfo errInfo = msgRssiThresholdConfig.ErrorInfo;
                return string.Format("Get RssiThresholdConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object SetRssiThresholdConfig(byte rssi)
        {
            bool ret = false;
            MsgRssiThresholdConfig msgRssiThresholdConfig = new MsgRssiThresholdConfig(rssi);
            ret = reader.Send(msgRssiThresholdConfig);
            if (ret == true)
            {
                return string.Format("Set RssiThresholdConfig rssi={0} success", rssi);
            }
            else
            {
                ErrInfo errInfo = msgRssiThresholdConfig.ErrorInfo;
                return string.Format("Set RssiThresholdConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private void Get_FrequencyConfig_button_Click(object sender, RoutedEventArgs e)
        {
            FrequencyArea region = GetRegion();
            FrequencyConfig_region_combobox.SelectedValue = region;

            msg_get_listview.Items.Add(string.Format("{0}", GetFrequencyConfigLog()));
            FrequencyTable frequencyTable = GetFrequencyConfig();
            FrequencyConfig_IsAutoSet_checkbox.IsChecked = frequencyTable.IsAutoSet;
            FrequencyConfig_FreqTable_combobox.ItemsSource = null;
            FrequencyConfig_FreqTable_combobox.Items.Clear();
            if(region == FrequencyArea.CN)
            {
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqCNTable(frequencyTable);
            }
            else if(region == FrequencyArea.FCC)
            {
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqFCCTable(frequencyTable);
            }
            else if (region == FrequencyArea.EU)
            {
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqTable(frequencyTable);
            }
            else
            {
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqTable(frequencyTable);
            }
        }

        private IEnumerable GetFreqTable(FrequencyTable frequencyTable)
        {
            List<FreqEU> list = new List<FreqEU>();
            foreach (byte b in frequencyTable.FreqTable)
            {
                list.Add(new FreqEU(b));
            }
            return list;
        }

        private void Set_FrequencyConfig_button_Click(object sender, RoutedEventArgs e)
        {
            FrequencyTable frequencyTable = new FrequencyTable();
            frequencyTable.IsAutoSet = FrequencyConfig_IsAutoSet_checkbox.IsChecked.Value;

            List<byte> list = new List<byte>();
            if(FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("CN"))
            {
                SetRegion(FrequencyArea.CN);
                logUtils.Log("+++++++++++++" + FrequencyConfig_region_combobox.SelectedValue);
                foreach (FreqCN freqCN in FrequencyConfig_FreqTable_combobox.ItemsSource)
                {
                    if (freqCN.FreqIsChecked == true)
                    {
                        logUtils.Log(string.Format("{0}, {1}", freqCN.Freq, freqCN.FreqIsChecked));
                        list.Add((byte)freqCN.Freq);
                    }
                }
            }
            else if (FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("FCC"))
            {
                SetRegion(FrequencyArea.FCC);
                logUtils.Log("------------" + FrequencyConfig_region_combobox.SelectedValue);
                foreach (FreqFCC freqFCC in FrequencyConfig_FreqTable_combobox.ItemsSource)
                {
                    if (freqFCC.FreqIsChecked == true)
                    {
                        logUtils.Log(string.Format("{0}, {1}", freqFCC.Freq, freqFCC.FreqIsChecked));
                        list.Add((byte)freqFCC.Freq);
                    }
                }
            }
            else if (FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("EU"))
            {
                SetRegion(FrequencyArea.EU);
                msg_get_listview.Items.Add(string.Format("{0} {1}", "Not Support Set FrequencyConfig ", FrequencyConfig_region_combobox.SelectedValue.ToString()));
                return; 
            }
            else
            {
                SetRegion(FrequencyArea.Unknown);
                msg_get_listview.Items.Add(string.Format("{0} {1}", "Not Support Set FrequencyConfig ", FrequencyConfig_region_combobox.SelectedValue.ToString()));
                return;
            }
            frequencyTable.FreqTable = list.ToArray();
            msg_get_listview.Items.Add(string.Format("{0}", SetFrequencyConfig(frequencyTable)));
        }

        // FrequencyConfig
        private void Msg_tabitem_Loaded(object sender, RoutedEventArgs e)
        {
            logUtils.Log("################");
            FrequencyConfig_region_combobox.ItemsSource = null;
            FrequencyConfig_region_combobox.Items.Clear();
            FrequencyConfig_region_combobox.ItemsSource = GetSupportRegion();
        }

        private void FrequencyConfig_region_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FrequencyConfig_FreqTable_combobox.ItemsSource = null;
            FrequencyConfig_FreqTable_combobox.Items.Clear();
            if (FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("FCC"))
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqFCCTable();
            if (FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("CN"))
                FrequencyConfig_FreqTable_combobox.ItemsSource = GetFreqCNTable();
        }

        private List<FreqFCC> GetFreqFCCTable()
        {
            List<FreqFCC> list = new List<FreqFCC>();
            foreach (E_FreqFCC freq in Enum.GetValues(typeof(E_FreqFCC)))
            {
                list.Add(new FreqFCC(freq));
            }
            return list;
        }

        private List<FreqFCC> GetFreqFCCTable(FrequencyTable frequencyTable)
        {
            List<FreqFCC> list = new List<FreqFCC>();
            foreach (E_FreqFCC freq in Enum.GetValues(typeof(E_FreqFCC)))
            {
                list.Add(new FreqFCC(freq));
            }
            foreach (byte b in frequencyTable.FreqTable)
            {
                list[b].FreqIsChecked = true;
            }
            return list;
        }

        private List<FreqCN> GetFreqCNTable()
        {
            List<FreqCN> list = new List<FreqCN>();
            foreach (E_FreqCN freq in Enum.GetValues(typeof(E_FreqCN)))
            {
                list.Add(new FreqCN(freq));
            }
            return list;
        }

        private List<FreqCN> GetFreqCNTable(FrequencyTable frequencyTable)
        {
            List<FreqCN> list = new List<FreqCN>();
            foreach (E_FreqCN freq in Enum.GetValues(typeof(E_FreqCN)))
            {
                list.Add(new FreqCN(freq));
            }

            foreach (byte b in frequencyTable.FreqTable)
            {
                list[b].FreqIsChecked = true;
            }
            return list;
        }

        private object GetFrequencyConfigLog()
        {
            bool ret = false;
            MsgFrequencyConfig msgFrequencyConfig = new MsgFrequencyConfig();
            ret = reader.Send(msgFrequencyConfig);
            if (ret == true)
            {
                FrequencyTable frequencyTable = msgFrequencyConfig.ReceivedMessage.FreqInfo;
                if(frequencyTable.IsAutoSet == true)
                {
                    return string.Format("Get FrequencyConfig IsAutoSet={0}, table={1}", frequencyTable.IsAutoSet, ByteFormat.ToHex(frequencyTable.FreqTable, "0x", " "));
                }
                else
                {
                    
                    return string.Format("Get FrequencyConfig IsAutoSet={0}, table={1}", frequencyTable.IsAutoSet, ByteFormat.ToHex(frequencyTable.FreqTable, "0x", " "));
                }
            }
            else
            {
                ErrInfo errInfo = msgFrequencyConfig.ErrorInfo;
                return string.Format("Get FrequencyConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private FrequencyTable GetFrequencyConfig()
        {
            bool ret = false;
            MsgFrequencyConfig msgFrequencyConfig = new MsgFrequencyConfig();
            ret = reader.Send(msgFrequencyConfig);
            if (ret == true)
            {
                FrequencyTable frequencyTable = msgFrequencyConfig.ReceivedMessage.FreqInfo;
                return frequencyTable;
            }
            else
            {
                return null;
            }
        }

        private object SetFrequencyConfig(FrequencyTable frequencyTable)
        {
            bool ret = false;

            MsgFrequencyConfig msgFrequencyConfig = new MsgFrequencyConfig(frequencyTable);
            ret = reader.Send(msgFrequencyConfig);
            if (ret == true)
            {
                return string.Format("Set FrequencyConfig IsAutoSet={0}, table={1} success", frequencyTable.IsAutoSet, ByteFormat.ToHex(frequencyTable.FreqTable, "", " "));
            }
            else
            {
                ErrInfo errInfo = msgFrequencyConfig.ErrorInfo;
                return string.Format("Set FrequencyConfig Error={0}, {1}, table={2}", errInfo.ErrCode, errInfo.ErrMsg, ByteFormat.ToHex(frequencyTable.FreqTable, "", " "));
            }
        }

        private void FrequencyConfig_IsAutoSet_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckIsAutoSet();
        }

        private void FrequencyConfig_IsAutoSet_checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckIsAutoSet();
        }

        private void CheckIsAutoSet()
        {
            
        }

        // IdleTimeConfig
        private void Get_IdleTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", GetIdleTimeConfig()));
        }

        private void Set_IdleTimeConfig_button_Click(object sender, RoutedEventArgs e)
        {
            ushort time = Convert.ToUInt16(set_IdleTimeConfig_textbox.Text);
            msg_get_listview.Items.Add(string.Format("{0}", SetIdleTimeConfig(time)));
        }

        private object GetIdleTimeConfig()
        {
            bool ret = false;
            MsgIdleTimeConfig msgIdleTimeConfig = new MsgIdleTimeConfig();
            ret = reader.Send(msgIdleTimeConfig);
            if (ret == true)
            {
                ushort time = msgIdleTimeConfig.ReceivedMessage.Time;
                return string.Format("Get IdleTimeConfig time={0} ms", time);
            }
            else
            {
                ErrInfo errInfo = msgIdleTimeConfig.ErrorInfo;
                return string.Format("Get IdleTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object SetIdleTimeConfig(ushort timeout)
        {
            bool ret = false;
            MsgIdleTimeConfig msgIdleTimeConfig = new MsgIdleTimeConfig(timeout);
            ret = reader.Send(msgIdleTimeConfig);
            if (ret == true)
            {
                return string.Format("Set IdleTimeConfig time={0} ms success", timeout);
            }
            else
            {
                ErrInfo errInfo = msgIdleTimeConfig.ErrorInfo;
                return string.Format("Set IdleTimeConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        // IpAddressConfig
        private void Get_IpAddressConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", GetIpAddressConfig()));
        }

        private void Set_IpAddressConfig_button_Click(object sender, RoutedEventArgs e)
        {
            IpAddressConfig ipAddressConfig = new IpAddressConfig();
            msg_get_listview.Items.Add(string.Format("{0}", SetIpAddressConfig(ipAddressConfig)));
        }

        private object GetIpAddressConfig()
        {
            bool ret = false;
            MsgIpAddressConfig msgIpAddressConfig = new MsgIpAddressConfig();
            ret = reader.Send(msgIpAddressConfig);
            if (ret == true)
            {
                string ip = msgIpAddressConfig.ReceivedMessage.IP;
                string subnet = msgIpAddressConfig.ReceivedMessage.Subnet;
                string gateway = msgIpAddressConfig.ReceivedMessage.Gateway;
                return string.Format("Get IpAddressConfig:\r\nIP={0}, \r\nSubnet={1}, \r\nGateway={2}", ip, subnet, gateway);
            }
            else
            {
                ErrInfo errInfo = msgIpAddressConfig.ErrorInfo;
                return string.Format("Get IpAddressConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object SetIpAddressConfig(IpAddressConfig ipAddressConfig)
        {
            bool ret = false;
            MsgIpAddressConfig msgIpAddressConfig = new MsgIpAddressConfig();
            ret = reader.Send(msgIpAddressConfig);
            if (ret == true)
            {
                return string.Format("Set IpAddressConfig:\r\nIP={0}, \r\nSubnet={1}, \r\nGateway={2} \r\nsuccess", ipAddressConfig.Ip, ipAddressConfig.Subnet, ipAddressConfig.Gateway);
            }
            else
            {
                ErrInfo errInfo = msgIpAddressConfig.ErrorInfo;
                return string.Format("Set IpAddressConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        // MacConfig
        private object GetMacConfig()
        {
            bool ret = false;
            MsgMacConfig msgMacConfig = new MsgMacConfig();
            ret = reader.Send(msgMacConfig);
            logUtils.Log(string.Format("Get MacConfig: {0}", ret));
            if (ret == true)
            {
                return string.Format("MacConfig Mac Addr={0}", msgMacConfig.ReceivedMessage.getStringMAC());
            }
            else
            {
                ErrInfo errInfo = msgMacConfig.ErrorInfo;
                return string.Format("Set MacConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        // 6CTagFieldConfig
        private void Get_6CTagFieldConfig_button_Click(object sender, RoutedEventArgs e)
        {
            msg_get_listview.Items.Add(string.Format("{0}", Get6CTagFieldConfig()));
        }

        private void Set_6CTagFieldConfig_button_Click(object sender, RoutedEventArgs e)
        {
            bool IsEnableRSSI = _6CTagFieldConfig_IsEnableRSSI_checkbox.IsChecked.Value;
            bool IsEnableAntenna = _6CTagFieldConfig_IsEnableAntenna_checkbox.IsChecked.Value;
            msg_get_listview.Items.Add(string.Format("{0}", Set6CTagFieldConfig(IsEnableRSSI, IsEnableAntenna)));
        }

        private object Get6CTagFieldConfig()
        {
            bool ret = false;
            Msg6CTagFieldConfig msg6CTagFieldConfig = new Msg6CTagFieldConfig();
            ret = reader.Send(msg6CTagFieldConfig);
            logUtils.Log(string.Format("Get 6CTagFieldConfig: {0}", ret));
            if (ret == true)
            {
                return string.Format("6CTagFieldConfig IsEnableAntenna={0}, IsEnableRSSI={1}", 
                    msg6CTagFieldConfig.ReceivedMessage.IsEnableAntenna, 
                    msg6CTagFieldConfig.ReceivedMessage.IsEnableRSSI);
            }
            else
            {
                ErrInfo errInfo = msg6CTagFieldConfig.ErrorInfo;
                return string.Format("Set 6CTagFieldConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private object Set6CTagFieldConfig(bool IsEnableAntenna, bool IsEnableRSSI)
        {
            bool ret = false;
            Msg6CTagFieldConfig msg6CTagFieldConfig = new Msg6CTagFieldConfig(IsEnableAntenna, IsEnableRSSI);
            ret = reader.Send(msg6CTagFieldConfig);
            logUtils.Log(string.Format("Get 6CTagFieldConfig: {0}", ret));
            if (ret == true)
            {
                return string.Format("6CTagFieldConfig IsEnableAntenna={0}, IsEnableRSSI={1} success", IsEnableAntenna, IsEnableRSSI);
            }
            else
            {
                ErrInfo errInfo = msg6CTagFieldConfig.ErrorInfo;
                return string.Format("Set 6CTagFieldConfig Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg);
            }
        }

        private void Is_TagInventory_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            IsTagInventory_stackpanel.Visibility = Visibility.Visible;
            IsTagRead_stackpanel.Visibility = Visibility.Collapsed;

            MemoryBank_combobox.ItemsSource = null;
            MemoryBank_combobox.ItemsSource = GetMemoryBank(); ;
        }

        private IEnumerable GetMemoryBank()
        {
            List<MemoryBank> list = new List<MemoryBank>();
            foreach (MemoryBank bank in Enum.GetValues(typeof(MemoryBank)))
            {
                list.Add(bank);
            }
            return list;
        }

        private void Is_TagRead_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            IsTagInventory_stackpanel.Visibility = Visibility.Collapsed;
            IsTagRead_stackpanel.Visibility = Visibility.Visible;
        }

        private void Is_MemBank_use_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            MemBank_stackpanel.Visibility = Visibility.Visible;
        }

        private void Is_MemBank_use_checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            MemBank_stackpanel.Visibility = Visibility.Collapsed;
        }

        private void User_checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            TagRead_user_stackpanel.Visibility = Visibility.Collapsed;
        }

        private void User_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            TagRead_user_stackpanel.Visibility = Visibility.Visible;
        }

        // TagWrite
        private void TagWrite_button_Click(object sender, RoutedEventArgs e)
        {
            bool isloop = TagWrite_IsLoop_checkbox.IsChecked.Value;
            byte[] accessPassword = ByteFormat.FromHex(TagWrite_password_textbox.Text);
            MemoryBank bank = (MemoryBank)TagWrite_select_membank_combobox.SelectedValue;
            foreach (ToExecuteTagOp twb in TagWrite_selected_combobox.Items)
            {
                TagWrite(isloop, accessPassword, bank, twb.StartAddr, twb.Data, UpdateWriteBank());
            }
        }

        private List<WriteBank> UpdateWriteBank()
        {
            List<WriteBank> wbs = new List<WriteBank>();
            foreach (WriteBank wb in TagWrite_write_combobox.Items)
            {
                wbs.Add(wb);
            }
            return wbs;
        }

        private void TagWrite(bool isloop, byte[] accessPassword, MemoryBank bank, uint startaddr, byte[] data, List<WriteBank> wbs)
        {
            WriteTagParameter param = new WriteTagParameter();
            param.IsLoop = isloop;
            param.AccessPassword = accessPassword;

            TagParameter selecttagparam = null;
            selecttagparam = new TagParameter();
            selecttagparam.MemoryBank = bank;
            selecttagparam.Ptr = startaddr;
            selecttagparam.TagData = data;

            param.SelectTagParam = selecttagparam;

            List<TagParameter> list = new List<TagParameter>();

            foreach (WriteBank wb in wbs)
            {
                if (wb.IsChecked == true)
                {
                    TagParameter w1 = new TagParameter();
                    w1.MemoryBank = wb.MemBank;
                    w1.Ptr = wb.StartAddr;
                    w1.TagData = wb.Data;
                    logUtils.Log("TagWrite with param", w1);
                    list.Add(w1);
                }
            }

            TagParameter[] writedata = list.ToArray();
            param.WriteDataAry = writedata;

            string str = "Writedata:\r\n";
            foreach (TagParameter tp in param.WriteDataAry)
            {
                str += string.Format("---> {0},{1}, data[{2}]\r\n", tp.MemoryBank, tp.Ptr, ByteFormat.ToHex(tp.TagData, "", " "));
            }
            logUtils.Log(string.Format("{0}\r\n", str));

            try
            {
                TagWrite(param);
            }
            catch(Exception e)
            {
                logUtils.Log("TagWrite", e);
            }
        }
        
        private bool TagWrite(WriteTagParameter param)
        {
            logUtils.Log("TagWrite with param", param);
            bool ret = false;
            MsgTagWrite msgTagWrite = new MsgTagWrite(param);
            ret = reader.Send(msgTagWrite);
            logUtils.Log(string.Format("TagWrite: {0}", ret));
            if (ret == true)
            {
                string str = "";
                foreach(TagParameter tp in param.WriteDataAry)
                {
                    str += string.Format("---> {0},{1}, data[{2}]\r\n", tp.MemoryBank, tp.Ptr, ByteFormat.ToHex(tp.TagData, "", " "));
                }
                logUtils.Log(string.Format("TagWrite success\r\n{0}", str));
                return true;
            }
            else
            {
                ErrInfo errInfo = msgTagWrite.ErrorInfo;
                logUtils.Log(string.Format("TagWrite Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg));
                return false;
            }
        }

        private void TagOP_Write_tabitem_Loaded(object sender, RoutedEventArgs e)
        {
            TagWrite_selected_combobox.ItemsSource = null;
            TagWrite_selected_combobox.ItemsSource = GetSelectTagReadList();
            if (TagWrite_selected_combobox.Items.Count > 0)
            {
                TagWrite_selected_combobox.SelectedIndex = 0;
            }

            TagWrite_select_membank_combobox.ItemsSource = null;
            TagWrite_select_membank_combobox.ItemsSource = GetMemoryBankForSelect();
            if(TagWrite_select_membank_combobox.Items.Count>0)
            {
                TagWrite_select_membank_combobox.SelectedIndex = 0;
            }

            TagWrite_write_combobox.ItemsSource = null;
            TagWrite_write_combobox.ItemsSource = GetWriteBank();
            if (TagWrite_write_combobox.Items.Count > 0)
                TagWrite_write_combobox.SelectedIndex = 0;
        }

        private IEnumerable GetMemoryBankForSelect()
        {
            List<MemoryBank> list = new List<MemoryBank>();
            foreach (MemoryBank bank in Enum.GetValues(typeof(MemoryBank)))
            {
                if (bank == MemoryBank.ReservedMemory)
                    continue;
                list.Add(bank);
            }
            return list;
        }

        private List<WriteBank> GetWriteBank()
        {
            List<WriteBank> list = new List<WriteBank>();
            foreach (MemoryBank bank in Enum.GetValues(typeof(MemoryBank)))
            {
                if (bank == MemoryBank.TIDMemory)
                    continue;
                WriteBank writeBank = new WriteBank();
                writeBank.MemBank = bank;
                if (bank == MemoryBank.EPCMemory)
                {
                    writeBank.IsChecked = true;
                    writeBank.StartAddr = 2;

                }
                else
                {
                    writeBank.IsChecked = false;
                    writeBank.StartAddr = 0;

                }
                writeBank.DataHexStr = "1122";
                list.Add(writeBank);
            }
            return list;
        }

        private void TagWrite_select_membank_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TagWrite_update_select_button_Click(sender, e);
        }

        private void TagWrite_update_select_button_Click(object sender, RoutedEventArgs e)
        {
            List<TagReadRecord> list = GetSelectTagReadList();
            
            TagWrite_selected_combobox.ItemsSource = null;
            TagWrite_selected_combobox.ItemsSource = GetSelectForExecuteTagOpList(list, (MemoryBank)TagWrite_select_membank_combobox.SelectedValue);
            if (TagWrite_selected_combobox.Items.Count > 0)
            {
                TagWrite_selected_combobox.SelectedIndex = 0;
            }
        }

        private List<ToExecuteTagOp> GetSelectForExecuteTagOpList(List<TagReadRecord> list, MemoryBank bank)
        {
            logUtils.Log(string.Format("GetSelectForExecuteTagOpList {0}", bank));
            List<ToExecuteTagOp> towrites = new List<ToExecuteTagOp>();
            uint start = 0;

            if (bank == MemoryBank.EPCMemory)
            {
                start = 2;
                foreach (TagReadRecord trd in list)
                {
                    ToExecuteTagOp twb = new ToExecuteTagOp();
                    twb.StartAddr = start;
                    twb.Data = trd.EPC;
                    towrites.Add(twb);
                }
            }
            else if (bank == MemoryBank.TIDMemory)
            {
                start = 0;
                foreach (TagReadRecord trd in list)
                {
                    ToExecuteTagOp twb = new ToExecuteTagOp();
                    twb.StartAddr = start;
                    twb.Data = trd.TID;
                    towrites.Add(twb);
                }
            }
            else if (bank == MemoryBank.UserMemory)
            {
                start = 0;
                foreach (TagReadRecord trd in list)
                {
                    ToExecuteTagOp twb = new ToExecuteTagOp();
                    twb.StartAddr = start;
                    twb.Data = trd.User;
                    towrites.Add(twb);
                }
            }
            return towrites;
        }

        private List<TagReadRecord> GetSelectTagReadList()
        {
            List<TagReadRecord> list = new List<TagReadRecord>();
            foreach (TagReadRecord mf in TagResults.dgTagResults.Items)
            {
                if (mf.Checked == true)
                {
                    list.Add(mf);
                }
            }
            return list;
        }

        // TagLock
        private void TagLock_button_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToExecuteTagOp toTagOpBank in TagLock_selected_combobox.ItemsSource)
            {
                foreach(ToLockBank toLockBank in TagLock_lockbank_combobox.ItemsSource)
                {
                    if(toLockBank.IsChecked == true)
                    {
                        logUtils.Log(string.Format("\r\n ******** To ExecuteTagOp the bank={0}", toLockBank.TheLockBank));
                        LockTagParameter param = new LockTagParameter();
                        param.LockBank = toLockBank.TheLockBank;
                        param.AccessPassword = ByteFormat.FromHex(TagLock_password_textbox.Text);
                        param.LockType = (LockType)TagLock_locktype_combobox.SelectedValue;
                        TagParameter tag = new TagParameter();
                        tag.MemoryBank = (MemoryBank)TagLock_select_membank_combobox.SelectedValue;
                        tag.Ptr = toTagOpBank.StartAddr;
                        tag.TagData = toTagOpBank.Data;
                        param.SelectTagParam = tag;

                        TagLock(param);
                    }
                }
            }
        }

        private bool TagLock(LockTagParameter param)
        {
            logUtils.Log("TagLock", param);
            bool ret = false;
            MsgTagLock msgTagLock = new MsgTagLock(param);
            ret = reader.Send(msgTagLock);
            if(ret == true)
            {
                logUtils.Log(string.Format("TagLock ExecuteTagOp {0} success ", param.LockType));
                return true;
            }
            else
            {
                ErrInfo errInfo = msgTagLock.ErrorInfo;
                logUtils.Log(string.Format("TagLock Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg));
                return false;
            }
        }

        private void TagOP_TagLock_tabitem_Loaded(object sender, RoutedEventArgs e)
        {
            // 选中标签的作为筛选的bank区
            TagLock_select_membank_combobox.ItemsSource = null;
            TagLock_select_membank_combobox.ItemsSource = GetMemoryBankForSelect();
            if (TagLock_select_membank_combobox.Items.Count > 0)
            {
                TagLock_select_membank_combobox.SelectedIndex = 0;
            }

            // 选中要去锁定的标签
            TagLock_selected_combobox.ItemsSource = null;
            TagLock_selected_combobox.ItemsSource = GetSelectTagReadList();
            if (TagLock_selected_combobox.Items.Count > 0)
            {
                TagLock_selected_combobox.SelectedIndex = 0;
            }

            // 要去锁定的区域
            TagLock_lockbank_combobox.ItemsSource = null;
            TagLock_lockbank_combobox.ItemsSource = GetLockBank();
            if (TagLock_lockbank_combobox.Items.Count > 0)
            {
                TagLock_lockbank_combobox.SelectedIndex = 0;
            }

            // 要去锁定的类型
            TagLock_locktype_combobox.ItemsSource = null;
            TagLock_locktype_combobox.ItemsSource = GetLockType();
            if (TagLock_locktype_combobox.Items.Count > 0)
            {
                TagLock_locktype_combobox.SelectedIndex = 0;
            }
        }

        private List<ToLockBank> GetLockBank()
        {
            List<ToLockBank> list = new List<ToLockBank>();
            foreach (LockBank bank in Enum.GetValues(typeof(LockBank)))
            {
                ToLockBank toLockBank = new ToLockBank();
                toLockBank.TheLockBank = bank;
                toLockBank.IsChecked = false;
                list.Add(toLockBank);
            }
            return list;
        }

        private List<LockType> GetLockType()
        {
            List<LockType> list = new List<LockType>();
            foreach (LockType bank in Enum.GetValues(typeof(LockType)))
            {
                list.Add(bank);
            }
            return list;
        }

        private void TagLock_update_select_button_Click(object sender, RoutedEventArgs e)
        {
            List<TagReadRecord> list = GetSelectTagReadList();

            TagLock_selected_combobox.ItemsSource = null;
            TagLock_selected_combobox.ItemsSource = GetSelectForExecuteTagOpList(list, (MemoryBank)TagLock_select_membank_combobox.SelectedValue);
            if (TagLock_selected_combobox.Items.Count > 0)
            {
                TagLock_selected_combobox.SelectedIndex = 0;
            }
        }

        private void TagLock_select_membank_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TagLock_update_select_button_Click(sender, e);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        // TagKill
        private void TagKill_button_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToExecuteTagOp toTagOpBank in TagKill_selected_combobox.ItemsSource)
            {
                KillTagParameter param = new KillTagParameter();
                param.KillPassword = ByteFormat.FromHex(TagKill_killpassword_textbox.Text);
                TagParameter tag = new TagParameter();
                tag.MemoryBank = (MemoryBank)TagKill_select_membank_combobox.SelectedValue;
                tag.Ptr = toTagOpBank.StartAddr;
                tag.TagData = toTagOpBank.Data;
                param.SelectTagParam = tag;

                TagKill(param);
            }
        }

        private bool TagKill(KillTagParameter param)
        {
            logUtils.Log("TagKill", param);
            bool ret = false;
            MsgTagKill msgTagKill = new MsgTagKill(param);
            ret = reader.Send(msgTagKill);
            if (ret == true)
            {
                logUtils.Log(string.Format("TagKill {0}{1} success ", param.SelectTagParam.MemoryBank, param.SelectTagParam.TagData));
                return true;
            }
            else
            {
                ErrInfo errInfo = msgTagKill.ErrorInfo;
                logUtils.Log(string.Format("TagKill Error={0}, {1}", errInfo.ErrCode, errInfo.ErrMsg));
                return false;
            }
        }

        private void TagKill_update_select_button_Click(object sender, RoutedEventArgs e)
        {
            List<TagReadRecord> list = GetSelectTagReadList();

            TagKill_selected_combobox.ItemsSource = null;
            TagKill_selected_combobox.ItemsSource = GetSelectForExecuteTagOpList(list, (MemoryBank)TagKill_select_membank_combobox.SelectedValue);
            if (TagKill_selected_combobox.Items.Count > 0)
            {
                TagKill_selected_combobox.SelectedIndex = 0;
            }
        }

        private void TagOP_TagKilltabitem_Loaded(object sender, RoutedEventArgs e)
        {
            // 选中标签的作为筛选的bank区
            TagKill_select_membank_combobox.ItemsSource = null;
            TagKill_select_membank_combobox.ItemsSource = GetMemoryBankForSelect();
            if (TagKill_select_membank_combobox.Items.Count > 0)
            {
                TagKill_select_membank_combobox.SelectedIndex = 0;
            }

            // 选中要去杀死的标签
            TagKill_selected_combobox.ItemsSource = null;
            TagKill_selected_combobox.ItemsSource = GetSelectTagReadList();
            if (TagKill_selected_combobox.Items.Count > 0)
            {
                TagKill_selected_combobox.SelectedIndex = 0;
            }
        }

        private void cbxBigNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != cbxBigNum)
            {
                try
                {
                    string text = ((ComboBoxItem)cbxBigNum.SelectedItem).Content.ToString();
                    switch (text)
                    {
                        case "Remove Big Num":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Unique Tag Count":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Visible;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Total Tag Count":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Visible;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Summary of Tag Result":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Visible;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    cbxBigNum.SelectedIndex = 0;
                }
            }
        }
    }
}
