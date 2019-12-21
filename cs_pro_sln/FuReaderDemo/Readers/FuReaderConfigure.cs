using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThingMagic;

namespace FuReaderDemo.Readers
{
    class FuReaderConfigure
    {
        FuReader reader = null;

        public enum TYPE
        {
            COM = 0,
            TCP,
            NETWORK
        }
        public TYPE type = TYPE.TCP;

        string addr = null;

        

        public string Addr
        {
            get { return addr; }
            set
            {
                switch (type)
                {
                    case TYPE.COM:
                        addr = string.Format("tmr:///{0}", value);
                        break;
                    case TYPE.TCP:
                        addr = string.Format("tcp://{0}:8086", value);
                        break;
                    case TYPE.NETWORK:
                        addr = string.Format("tmr://{0}", value);
                        break;
                    default:
                        Utils.Log(string.Format("defalut case, TYPE={0}", type));
                        addr = string.Format("tmr://{0}", value);
                        break;
                }
            }
        }

        public TYPE Type
        {
            get { return type; }
            set { }
        }

        public string Port { get;  set; }
        public int[] AntList { get; set; }
        public double ReadPowers { get; set; }
        public double WritePowers { get; set; }
        public Reader.Region Region { get; set; }
        public Gen2.LinkFrequency BLF { get; set; }
        public Gen2.Tari Tari { get; set; }
        public Gen2.TagEncoding TagEncoding { get; set; }
        public Gen2.Session Session { get; set; }
        public Gen2.Target Target { get; set; }
        public Gen2.DynamicQ QValue { get; set; }
    }
}
