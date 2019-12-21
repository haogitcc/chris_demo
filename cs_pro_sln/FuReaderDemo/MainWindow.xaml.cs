using FuReaderDemo.Config;
using FuReaderDemo.Readers;
using System.Collections.Generic;
using System.Windows;
using ThingMagic;

namespace FuReaderDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Configure conf = null;
        List<FuReaderConfigure> FuReaderList
        {
            get
            {
                if (conf!=null)
                    return conf.fuReaderList;
                return null;
            }
        }
         

        public MainWindow()
        {
            InitializeComponent();
            conf = new Configure();
            FuReaderConfigure fuConf = new FuReaderConfigure();
            FuReader fuReader = new FuReader();

            fuConf.Type = FuReaderConfigure.TYPE.TCP;
            fuConf.Addr = "192.168.8.166";

            fuConf.AntList = new int[] { 1 };

            fuConf.ReadPowers = 5;
            fuConf.WritePowers = 31.5;

            fuConf.Region = Reader.Region.NA;

            fuConf.BLF = Gen2.LinkFrequency.LINK250KHZ;
            fuConf.Tari = Gen2.Tari.TARI_6_25US;
            fuConf.TagEncoding = Gen2.TagEncoding.M4;
            fuConf.Session = Gen2.Session.S1;
            fuConf.Target = Gen2.Target.A;
            fuConf.QValue = new Gen2.DynamicQ();

            FuReaderList.Add(fuConf);

            fuReader.Create(FuReaderList[0]);
            fuReader.Dispose();
        }
    }
}
