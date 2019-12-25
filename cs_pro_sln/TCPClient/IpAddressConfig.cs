namespace TCPClient
{
    class IpAddressConfig
    {
        string IP = string.Empty;
        string SUBNET = string.Empty;
        string GATEWAY = string.Empty;

        public IpAddressConfig()
        {
            this.IP = "192.168.8.166";
            this.SUBNET = "255.255.255.0";
            this.GATEWAY = "192.168.8.1";
        }

        IpAddressConfig(string ip, string subnet, string gateway)
        {
            this.IP = ip;
            this.SUBNET = subnet;
            this.GATEWAY = gateway;
        }
        
        public string Ip {
            get
            {
                return IP;
            }
            set
            {
                IP = value;
            }
        }

        public string Subnet {
            get
            {
                return SUBNET;
            }
            set
            {
                SUBNET = value;
            }
        }
        public string Gateway {
            get
            {
                return GATEWAY;
            }
            set
            {
                GATEWAY = value;
            }
        }
    }
}