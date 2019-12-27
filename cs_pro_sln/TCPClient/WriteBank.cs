using NetAPI.Entities;

namespace TCPClient
{
    public class WriteBank
    {
        MemoryBank memoryBank = MemoryBank.EPCMemory;
        uint startAddr = 32;
        string dataHexStr = "";
        bool isChecked = false;
        
        public MemoryBank MemBank
        {
            get { return memoryBank; }
            set { memoryBank = value; }
        }
        
        public uint StartAddr
        {
            get { return startAddr; }
            set { startAddr = value; }
        }

        public string DataHexStr
        {
            get { return dataHexStr; }
            set { dataHexStr = value; }
        }

        public byte[] Data
        {
            get { return ByteFormat.FromHex(dataHexStr); }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set { isChecked = value; }
        }
    }
}