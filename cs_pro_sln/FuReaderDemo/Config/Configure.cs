using FuReaderDemo.Readers;
using System.Collections.Generic;

namespace FuReaderDemo.Config
{
    class Configure
    {
        int count = 0;
        int cur_index = 0;

        public List<FuReaderConfigure> fuReaderList = new List<FuReaderConfigure>();

        int FuReaderCount
        {
            get { return count; }
            set { count = value; }
        }

        int Index
        {
            get { return cur_index; }
            set { cur_index = value; }
        }

        void Add(FuReaderConfigure fuConfig)
        {
            if (fuReaderList == null)
            {
                fuReaderList = new List<FuReaderConfigure>();
            }
            fuReaderList.Add(fuConfig);
        }

        void RemoveByIndex(int index)
        {
            if (fuReaderList == null)
                return;
            fuReaderList.RemoveAt(index);
        }
    }
}
