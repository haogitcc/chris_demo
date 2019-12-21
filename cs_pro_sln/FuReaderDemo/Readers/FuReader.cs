using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThingMagic;

namespace FuReaderDemo.Readers
{
    class FuReader
    {
        Reader reader = null;

        public void Create(FuReaderConfigure fuConfig)
        {
            switch (fuConfig.type)
            {
                case FuReaderConfigure.TYPE.COM:
                    break;
                case FuReaderConfigure.TYPE.TCP:
                    Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);
                    break;
                case FuReaderConfigure.TYPE.NETWORK:
                    break;
                default:
                    break;
            }
            reader = Reader.Create(fuConfig.Addr);
            reader.Connect();
            Init(fuConfig);
        }

        private void Init(FuReaderConfigure fuConfig)
        {
            if (reader == null)
                return;
            reader.ParamSet("", fuConfig.Region);
            foreach(int ant in fuConfig.AntList)
            {
                reader.ParamSet("", ant);
            }

            reader.ParamSet("", fuConfig.ReadPowers);
            reader.ParamSet("", fuConfig.WritePowers);


        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
