namespace TCPClient
{
    public class ToExecuteTagOp
    {
        uint startaddr = 0;
        byte[] data = null;

        public uint StartAddr
        {
            get { return startaddr; }
            set { startaddr = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public string DataStr
        {
            get
            {
                return ByteFormat.ToHex(data, "", " ");
            }
            set
            {
                data = ByteFormat.FromHex(value);
            }
        }
    }
}