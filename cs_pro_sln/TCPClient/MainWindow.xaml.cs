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
            InitCmdList();
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


        private void Vboto_connect_button_Click(object sender, RoutedEventArgs e)
        {
            if(vboto_connect_button.Content.Equals("Connect"))
            {
                vboto_connect_button.Content = "Disconnect";
            }
            else if (vboto_connect_button.Content.Equals("Disconnect"))
            {
                vboto_connect_button.Content = "Connect";
            }
        }


        #endregion
    }
}
