namespace TCPClient
{
    public enum E_FreqFCC
    {
        //FCC
        M902_75 = 0x00,
        M903_25 = 0x01,
        M903_75 = 0x02,
        M904_25 = 0x03,
        M904_75 = 0x04,
        M905_25 = 0x05,
        M905_75 = 0x06,
        M906_25 = 0x07,
        M906_75 = 0x08,
        M907_25 = 0x09,
        M907_75 = 0x0A,
        M908_25 = 0x0B,
        M908_75 = 0x0C,
        M909_25 = 0x0D,
        M909_75 = 0x0E,
        M910_25 = 0x0F,
        M910_75 = 0x10,
        M911_25 = 0x11,
        M911_75 = 0x12,
        M912_25 = 0x13,
        M912_75 = 0x14,
        M913_25 = 0x15,
        M913_75 = 0x16,
        M914_25 = 0x17,
        M914_75 = 0x18,
        M915_25 = 0x19,
        M915_75 = 0x1A,
        M916_25 = 0x1B,
        M916_75 = 0x1C,
        M917_25 = 0x1D,
        M917_75 = 0x1E,
        M918_25 = 0x1F,
        M918_75 = 0x20,
        M919_25 = 0x21,
        M919_75 = 0x22,
        M920_25 = 0x23,
        M920_75 = 0x24,
        M921_25 = 0x25,
        M921_75 = 0x26,
        M922_25 = 0x27,
        M922_75 = 0x28,
        M923_25 = 0x29,
        M923_75 = 0x2A,
        M924_25 = 0x2B,
        M924_75 = 0x2C,
        M925_25 = 0x2D,
        M925_75 = 0x2E,
        M926_25 = 0x2F,
        M926_75 = 0x30,
        M927_25 = 0x31,
    };

    public enum E_FreqCN
    {
        //CN
        M920_625 = 0x00,
        M920_875 = 0x01,
        M921_125 = 0x02,
        M921_375 = 0x03,
        M921_625 = 0x04,
        M921_875 = 0x05,
        M922_125 = 0x06,
        M922_375 = 0x07,
        M922_625 = 0x08,
        M922_875 = 0x09,
        M923_125 = 0x0A,
        M923_375 = 0x0B,
        M923_625 = 0x0C,
        M923_875 = 0x0D,
        M924_125 = 0x0E,
        M924_375 = 0x0F,
    };


    public enum E_FreqEU
    {
        //EU
        
    };

    public enum E_Freq
    {
        Default = 0x00
    };

    public interface IFreq
    {
        bool FreqIsChecked { get; set; }
    }

    public class FreqCN : IFreq
    {
        bool freqIsChecked = false;
        E_FreqCN freq = 0x00;

        public FreqCN(E_FreqCN freq)
        {
            this.freq = freq;
        }


        public FreqCN(E_FreqCN freq, bool freqIsChecked)
        {
            this.freq = freq;
            this.freqIsChecked = freqIsChecked;
        }

        public bool FreqIsChecked {
            get
            {
                return freqIsChecked;
            }

            set
            {
                freqIsChecked = value;
            }
        }
        public E_FreqCN Freq {
            get
            {
                return freq;
            }

            set
            {
                freq = value;
            }
        }
    }

    public class FreqFCC : IFreq
    {
        bool freqIsChecked = false;
        E_FreqFCC freq = 0x00;

        public FreqFCC(E_FreqFCC freq)
        {
            this.freq = freq;
        }

        public FreqFCC(E_FreqFCC freq, bool freqIsChecked)
        {
            this.freq = freq;
            this.freqIsChecked = freqIsChecked;
        }

        public bool FreqIsChecked
        {
            get
            {
                return freqIsChecked;
            }

            set
            {
                freqIsChecked = value;
            }
        }
        public E_FreqFCC Freq
        {
            get
            {
                return freq;
            }

            set
            {
                freq = value;
            }
        }
    }

    public class FreqEU : IFreq
    {
        bool freqIsChecked = false;
        byte freq = 0x00;

        public FreqEU(byte freq)
        {
            this.freq = freq;
        }


        public FreqEU(byte freq, bool freqIsChecked)
        {
            this.freq = freq;
            this.freqIsChecked = freqIsChecked;
        }

        public bool FreqIsChecked
        {
            get
            {
                return freqIsChecked;
            }

            set
            {
                freqIsChecked = value;
            }
        }
        public byte Freq
        {
            get
            {
                return freq;
            }

            set
            {
                freq = value;
            }
        }
    }
}