using NetAPI;
using NetAPI.Core;
using NetAPI.Entities;
using NetAPI.Protocol.VRP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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

        public MainWindow()
        {
            InitializeComponent();
            param_stackpanel.Visibility = Visibility.Collapsed;
            connect_type_combobox.ItemsSource = null;
            connect_type_combobox.Items.Clear();
            connect_type_combobox.ItemsSource = InitConnectType();
            connect_type_combobox.SelectedIndex = 0;
            //InitCmdList();
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
                    Log(string.Format("connectRespone.IsSucessed={0}", connectRespone.IsSucessed));
                    reader.OnBrokenNetwork += OnVPRBrokenNetwork;
                    bool ret = false;
                    MsgPowerOff powerOff = new MsgPowerOff();
                    ret = reader.Send(powerOff);
                    if(ret)
                    {
                        InitStatus();
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
                    Log(string.Format("connectRespone.ErrorInfo={0}, {1}", 
                        connectRespone.ErrorInfo.ErrCode,
                        connectRespone.ErrorInfo.ErrMsg));
                    vboto_connect_button.Content = "Connect";
                }
                
            }
            else if (vboto_connect_button.Content.Equals("Disconnect"))
            {
                vboto_connect_button.Content = "Connect";
                if(reader != null && reader.IsConnected)
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
            msg_get_listview.Items.Clear();
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
            Log(string.Format("msgReaderCapabilityQuery: {0}", ret));
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
            Log(string.Format("msgReaderCapabilityQuery: {0}", ret));
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
            Log(string.Format("msgGetFrequencyConfig: {0}", ret));
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
            Log(string.Format("Get AirProtocolConfig: {0}", ret));
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
            Log(string.Format("msgSetAirProtocolConfig: {0}", ret));
            if (ret)
            {
                Log(string.Format("Set AirProtocolConfig {0} success", protocol));
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
            Log(string.Format("Get Region (UhfBandConfig): {0}", ret));
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
            Log(string.Format("msgGetRs232BaudRateConfig: {0}", ret));
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
            Log(string.Format("msgSetRs232BaudRateConfig: {0}", ret));
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
            Log(string.Format("msgRfidStatusQuery: {0}", ret));
            if (ret)
            {
                Log(string.Format("Protocol:{0}", msgRfidStatusQuery.ReceivedMessage.Protocol));
                Log(string.Format("Region:{0}", msgRfidStatusQuery.ReceivedMessage.UhfBand));
                Log(string.Format("Status:{0}", msgRfidStatusQuery.ReceivedMessage.Status));
                

                foreach (AntennaPowerStatus antenna in msgRfidStatusQuery.ReceivedMessage.Antennas)
                {
                    if(antenna.AntennaNO <= 4)
                    {
                        Log(string.Format("antennaPower [{0}, {1}, {2}]", antenna.AntennaNO, antenna.IsEnable, antenna.PowerValue));
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
        private void Vboto_scan_epc_button_Click(object sender, RoutedEventArgs e)
        {
            if (vboto_scan_epc_button.Content.Equals("ScanEPC"))
            {
                vboto_scan_epc_button.Content = "Scaning";
                reader.OnInventoryReceived += OnVRPInventoryReceived;
                ScanEPC();
            }
            else if (vboto_scan_epc_button.Content.Equals("Scaning"))
            {
                vboto_scan_epc_button.Content = "ScanEPC";
                reader.OnInventoryReceived -= OnVRPInventoryReceived;
                bool ret = false;
                ret = reader.Send(new MsgPowerOff());
                Log(string.Format("msgPowerOff: {0}", ret));
                if (ret)
                {
                    
                }
                else
                {

                }
            }
        }

        private void ScanEPC()
        {
            TagInventory();
        }

        private void TagInventory()
        {
            bool ret = false;
            MsgTagInventory tagInventory = new MsgTagInventory();
            ret = reader.Send(tagInventory);
            Log(string.Format("TagInventory: {0}", ret));
            if (ret == true)
            {

            }
            else
            {
                Log("TagInventory", tagInventory.ErrorInfo);
            }
        }

        private void Vboto_scan_button_Click(object sender, RoutedEventArgs e)
        {
            if (vboto_scan_button.Content.Equals("Scan"))
            {
                vboto_scan_button.Content = "Scaning";
                reader.OnInventoryReceived += OnVRPInventoryReceived;
                Scan((bool)is_param_checkbox.IsChecked);
            }
            else if (vboto_scan_button.Content.Equals("Scaning"))
            {
                vboto_scan_button.Content = "Scan";
                reader.OnInventoryReceived -= OnVRPInventoryReceived;
                bool ret = false;
                ret = reader.Send(new MsgPowerOff());
                Log(string.Format("msgPowerOff: {0}", ret));
                if (ret)
                {

                }
                else
                {

                }
            }
        }

        private void Is_param_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (is_param_checkbox.IsChecked == true)
            {
                param_stackpanel.Visibility = Visibility.Visible;
            }
            else if (is_param_checkbox.IsChecked == false)
            {
                param_stackpanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Baudrate_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reader == null)
                return;
            BaudRate baudRate = (BaudRate)baudrate_combobox.SelectedValue;
            Log(string.Format("{0}", baudRate));
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
                Log(string.Format("{0}", region));
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
            Log(string.Format("{0}", protocol));
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
            Log(string.Format("{0}", frequency_combobox.SelectedValue));
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
            Log(string.Format("msgReaderVersionQuery: {0}", ret));
            if (ret == true)
            {
                list.Add(string.Format("ModelNumber     : {0}", msgReaderVersionQuery.ReceivedMessage.ModelNumber));
                list.Add(string.Format("HardwareVersion : {0}", msgReaderVersionQuery.ReceivedMessage.HardwareVersion));
                list.Add(string.Format("SoftwareVersion : {0}", msgReaderVersionQuery.ReceivedMessage.SoftwareVersion));
                Log(string.Format("model={0}," +
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
            Log(string.Format("OnVRPReaderMessageReceived {0}", reader.ReaderName));
        }

        private void Scan(bool IsSetParam)
        {
            bool ret = false;
            if(IsSetParam)
            {
                ReadTagParameter param = new ReadTagParameter();
                /***
                 * public bool IsLoop;
                 * public byte[] AccessPassword;
                 * public bool IsReturnEPC;
                 * public bool IsReturnTID;
                 * public uint UserPtr;
                 * public byte UserLen;
                 * public bool IsReturnReserved;
                 * public ushort ReadCount;
                 * public ushort ReadTime;
                 */
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
                param.ReadCount = 0;
                param.ReadTime = 0;
                param.TotalReadTime = 1;
                MsgTagRead msgTagRead = new MsgTagRead(param);
                ret = reader.Send(msgTagRead);
                Log(string.Format("ScanTID msgTagRead with param: {0}", ret));
            }
            else
            {
                MsgTagRead msgTagRead = new MsgTagRead();
                ret = reader.Send(msgTagRead);
                Log(string.Format("ScanTID msgTagRead: {0}", ret));
            }

            if (ret)
            {
                
            }
            else
            {

            }
        }
        
        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        private void Log(string msg, ErrInfo e)
        {
            Console.WriteLine(msg);
            Log(string.Format("ErrCode:{0}" +
                "\r\nErrMsg:{1}", e.ErrCode, e.ErrMsg));
        }

        private void Log(string msg, IReaderMessage e)
        {
            Console.WriteLine(msg);
            Log(string.Format("MessageID:{0}" +
                "\r\nStatus:{1}" +
                "\r\nErrorInfo.ErrCode:{2}" +
                "\r\nErrorInfo.ErrMsg:{3}" +
                "\r\nReceivedData={4}", e.MessageID, e.Status, e.ErrorInfo.ErrCode, e.ErrorInfo.ErrMsg, ByteFormat.ToHex(e.ReceivedData, "", " ")));
        }

        private void Log(string msg, RxdTagData tagData)
        {
            Console.WriteLine(msg);
            Log(string.Format("EPC:{0}" +
                "\r\nTID:{1}" +
                "\r\nUser:{2}" +
                "\r\nReserved:{3}" +
                "\r\nAnetnna:{4}" +
                "\r\nRSSI:{5}", 
                ByteFormat.ToHex(tagData.EPC, "", " "), 
                ByteFormat.ToHex(tagData.TID, "", " "), 
                ByteFormat.ToHex(tagData.User, "", " "),
                ByteFormat.ToHex(tagData.Reserved, "", " "), 
                tagData.Antenna, 
                tagData.RSSI));
        }

        private void InitVRPReader()
        {
            readerName = string.Format("VRP Reader {0}", vboto_ip_textbox.Text) ;
            if(connect_type_combobox.SelectedValue.Equals("TCP"))
            {
                string IP = vboto_ip_textbox.Text;//192.168.8.166
                int PORT = Convert.ToInt32(vboto_port_textbox.Text.ToString());//8086
                port = new TcpClientPort(IP, PORT);
            }
            else if (connect_type_combobox.SelectedValue.Equals("RS232"))
            {
                string portName = string.Format("{0}", rs232_combobox.SelectedValue);//COM1
                BaudRate baudRate = (BaudRate)baudrate_combobox.SelectedValue;//R115200
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

            InitUI();
        }

        private void InitUI()
        {
            baudrate_combobox.ItemsSource = null;
            baudrate_combobox.Items.Clear();
            baudrate_combobox.ItemsSource = GetSupportBaudrate();

            region_combobox.ItemsSource = null;
            region_combobox.Items.Clear();
            region_combobox.ItemsSource = GetSupportRegion();
            
            frequency_combobox.ItemsSource = null;
            frequency_combobox.Items.Clear();
            frequency_combobox.ItemsSource = GetSupportFrequency();

            protocol_combobox.ItemsSource = null;
            protocol_combobox.Items.Clear();
            protocol_combobox.ItemsSource = GetSupportProtocol();
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
            Log(string.Format("msgSetPowerConfig: {0}", ret));
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
            Log(string.Format("msgSetUhfBandConfig: {0}", ret));
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
            Log(string.Format("msgSetUhfBandConfig: {0}", ret));
            if (ret)
            {
                Log(string.Format("Set Region {0} success", region));
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
            Log(string.Format("{0}:", "OnVRPErrorMessage"), e);
        }
        
        private void OnVRPApiException(string senderName, ErrInfo e)
        {
            Log(string.Format("{0}: {1}", "OnVRPApiException", senderName), e);
        }

        private void OnVPRBrokenNetwork(string senderName, ErrInfo e)
        {
            Log(string.Format("{0}: {1}", "OnVPRBrokenNetwork", senderName), e);
        }

        private void OnVRPInventoryReceived(string readerName, RxdTagData tagData)
        {
            
            Log(string.Format("{0}: {1}", "OnVRPInventoryReceived", readerName), tagData);
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
                    break;
                case "RS232":
                    tcp_stackpanel.Visibility = Visibility.Collapsed;
                    rs232_stackpanel.Visibility = Visibility.Visible;
                    rs485_combobox.Visibility = Visibility.Collapsed;
                    break;
                case "RS485":
                    tcp_stackpanel.Visibility = Visibility.Collapsed;
                    rs232_stackpanel.Visibility = Visibility.Collapsed;
                    rs485_combobox.Visibility = Visibility.Visible;
                    break;
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
                Log(string.Format("antNo={0}, {1}", antNo, antEnable));
                AntennaStatus ant = new AntennaStatus();
                ant.AntennaNO = antNo;
                ant.IsEnable = antEnable;
                antennasList.Add(ant);
            }

            AntennaStatus[] antennas = antennasList.ToArray();
            MsgAntennaConfig msgAntennaConfig = new MsgAntennaConfig(antennas);
            ret = reader.Send(msgAntennaConfig);
            Log(string.Format("msgAntennaConfig: {0}", ret));
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
                Log("+++++++++++++" + FrequencyConfig_region_combobox.SelectedValue);
                foreach (FreqCN freqCN in FrequencyConfig_FreqTable_combobox.ItemsSource)
                {
                    if (freqCN.FreqIsChecked == true)
                    {
                        Log(string.Format("{0}, {1}", freqCN.Freq, freqCN.FreqIsChecked));
                        list.Add((byte)freqCN.Freq);
                    }
                }
            }
            else if (FrequencyConfig_region_combobox.SelectedValue.ToString().Equals("FCC"))
            {
                SetRegion(FrequencyArea.FCC);
                Log("------------" + FrequencyConfig_region_combobox.SelectedValue);
                foreach (FreqFCC freqFCC in FrequencyConfig_FreqTable_combobox.ItemsSource)
                {
                    if (freqFCC.FreqIsChecked == true)
                    {
                        Log(string.Format("{0}, {1}", freqFCC.Freq, freqFCC.FreqIsChecked));
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
            Log("################");
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
            Log(string.Format("Get MacConfig: {0}", ret));
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
            Log(string.Format("Get 6CTagFieldConfig: {0}", ret));
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
            Log(string.Format("Get 6CTagFieldConfig: {0}", ret));
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

    }
}
