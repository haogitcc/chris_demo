using NetAPI.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TCPClient
{
    public class TagDatabase
    {
        bool IsUniqueByteTID = false;
        
        public bool IsLoop = false;
        public byte[] AccessPassword;
        public bool IsReturnEPC = true;
        public bool IsReturnTID;
        public uint UserPtr;
        public byte UserLen;
        public bool IsReturnReserved;
        public ushort ReadCount;
        public ushort ReadTime;

        LogUtils logUtils = new LogUtils();
        TagReadRecordBindingList _tagList = new TagReadRecordBindingList();
        public Dictionary<string, TagReadRecord> EpcIndex = new Dictionary<string, TagReadRecord>();
        static long UniqueTagCounts = 0;
        static long TotalTagCounts = 0;

        private readonly object _datasLock = new object();

        public TagDatabase()
        {
            // GUI can't keep up with fast updates, so disable automatic triggers
            _tagList.RaiseListChangedEvents = false;
        }

        public TagDatabase(ReadTagParameter param)
        {
            Update(param);
        }

        public TagReadRecordBindingList TagList
        {
            get { return _tagList; }
        }
        public long UniqueTagCount
        {
            get { return UniqueTagCounts; }
        }
        public long TotalTagCount
        {
            get { return TotalTagCounts; }
        }
        public bool UniqueByteTID
        {
            get { return IsUniqueByteTID; }
            set { IsUniqueByteTID = value; }
        }

        public void Clear()
        {
            EpcIndex.Clear();
            UniqueTagCounts = 0;
            TotalTagCounts = 0;
            _tagList.Clear();
            // Clear doesn't fire notifications on its own
            _tagList.ResetBindings();
        }

        public void Update(ReadTagParameter param)
        {
            IsLoop = param.IsLoop;
            AccessPassword = param.AccessPassword;
            IsReturnEPC = param.IsReturnEPC;
            IsReturnTID = param.IsReturnTID;
            UserPtr = param.UserPtr;
            UserLen = param.UserLen;
            IsReturnReserved = param.IsReturnReserved;
            ReadCount = param.ReadCount;
            ReadTime = param.ReadTime;
        }

        public void Add(RxdTagData addData)
        {
            lock (new Object())
            {
                string key = null;
                if (IsUniqueByteTID == false)
                {
                    key = ByteFormat.ToHex(addData.EPC, "", " "); //if only keying on EPCID
                }
                else
                {
                    key = string.Format("epc={0}, tid={1}", ByteFormat.ToHex(addData.EPC, "", " "), ByteFormat.ToHex(addData.TID, "", " "));
                }

                UniqueTagCounts = 0;
                TotalTagCounts = 0;

                if (!EpcIndex.ContainsKey(key))
                {
                    logUtils.Log(string.Format("Add {0}", key));
                    TagReadRecord value = new TagReadRecord(addData);
                    value.SerialNumber = (uint)EpcIndex.Count + 1;
                    value.TimeStamp = DateTime.Now.ToLocalTime();
                    value.ReadCount = 1;
                    
                    _tagList.Add(value);
                    EpcIndex.Add(key, value);
                    UpdateTagCount(EpcIndex);
                }
                else
                {
                    EpcIndex[key].Update(addData);
                    UpdateTagCount(EpcIndex);
                }
            }
        }

        public void AddRange(ICollection<RxdTagData> reads)
        {
            foreach (RxdTagData read in reads)
            {
                Add(read);
            }
        }

        //Calculate total tag reads and unique tag reads.
        public void UpdateTagCount(Dictionary<string, TagReadRecord> EpcIndex)
        {
            UniqueTagCounts += EpcIndex.Count;
            TagReadRecord[] dataRecord = new TagReadRecord[EpcIndex.Count];
            EpcIndex.Values.CopyTo(dataRecord, 0);
            TotalTagCounts = 0;
            for (int i = 0; i < dataRecord.Length; i++)
            {
                TotalTagCounts += dataRecord[i].ReadCount;
            }
        }

        public void Repaint()
        {
            _tagList.RaiseListChangedEvents = true;

            //Causes a control bound to the BindingSource to reread all the items in the list and refresh their displayed values.
            _tagList.ResetBindings();

            _tagList.RaiseListChangedEvents = false;
        }
    }

    public class TagReadRecordBindingList : SortableBindingList<TagReadRecord>
    {
        protected override Comparison<TagReadRecord> GetComparer(PropertyDescriptor prop)
        {
            Comparison<TagReadRecord> comparer = null;
            switch (prop.Name)
            {
                case "SerialNumber":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return (int)(a.SerialNumber - b.SerialNumber);
                    });
                    break;
                case "epcString"://"EPC":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.epcString, b.epcString);;
                    });
                    break;
                case "tidString":// "Tid":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.tidString, b.tidString); ;
                    });
                    break;
                case "reservedString"://"Reserved":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.reservedString, b.reservedString); ;
                    });
                    break;
                case "userString"://"User":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.userString, b.userString); ;
                    });
                    break;
                
                case "TimeStamp":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return DateTime.Compare(a.TimeStamp, b.TimeStamp);
                    });
                    break;

                case "RSSI":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return a.RSSI - b.RSSI;
                    });
                    break;
                case "ReadCount":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return a.ReadCount - b.ReadCount;
                    });
                    break;
                case "Antenna":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return a.Antenna - b.Antenna;
                    });
                    break;
                case "Protocol":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.Protocol.ToString(), b.Protocol.ToString());
                    });
                    break;
                case "Frequency":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return a.Frequency - b.Frequency;
                    });
                    break;
                case "Phase":
                    comparer = new Comparison<TagReadRecord>(delegate (TagReadRecord a, TagReadRecord b)
                    {
                        return a.Phase - b.Phase;
                    });
                    break;
            }
            return comparer;
        }
    }

    public class TagReadRecord : INotifyPropertyChanged
    {
        protected RxdTagData RawRead = null;
        protected UInt32 serialNo = 0;
        public bool dataChecked = false;
        private DateTime Time;
        private int readCount = 0;

        public TagReadRecord(RxdTagData newData)
        {
            lock (new Object())
            {
                RawRead = newData;
            }
        }

        public void Update(RxdTagData mergeData)
        {
            DateTime now = DateTime.Now.ToLocalTime();
            TimeSpan timediff = now - this.TimeStamp.ToUniversalTime();
            if (0 <= timediff.TotalMilliseconds)
            {
                RawRead = mergeData;
                this.TimeStamp = now;
                this.readCount += 1;
            }

            OnPropertyChanged(null);
        }
        
        public UInt32 SerialNumber
        {
            get { return serialNo; }
            set { serialNo = value; }
        }

        public byte Antenna
        {
            get { return RawRead.Antenna; }
        }

        public byte[] EPC
        {
            get { return RawRead.EPC; }
        }

        public string epcString
        {
            get { return ByteFormat.ToHex(RawRead.EPC, "", " "); }
        }

        public byte[] TID
        {
            get { return RawRead.TID; }
        }

        public string tidString
        {
            get { return ByteFormat.ToHex(RawRead.TID, "", " "); }
        }

        public byte[] User
        {
            get { return RawRead.User; }
        }

        public string userString
        {
            get { return ByteFormat.ToHex(RawRead.User, "", " "); }
        }

        public byte[] Reserved
        {
            get { return RawRead.Reserved; }
        }

        public string reservedString
        {
            get { return ByteFormat.ToHex(RawRead.Reserved, "", " "); }
        }

        public byte RSSI
        {
            get { return RawRead.RSSI; }
        }

        public bool Checked
        {
            get { return dataChecked; }
            set
            {
                dataChecked = value;
            }
        }

        public int ReadCount
        {
            get { return readCount; }
            set
            {
                readCount = value;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                //return DateTime.Now.ToLocalTime();
                TimeSpan difftime = (DateTime.Now.ToUniversalTime() - Time.ToUniversalTime());
                //double a1111 = difftime.TotalSeconds;
                if (difftime.TotalHours > 24)
                    return DateTime.Now.ToLocalTime();
                else
                    return Time.ToLocalTime();
            }
            set
            {
                Time = value;
            }
        }
        public object Protocol { get; internal set; }
        public int Frequency { get; internal set; }
        public int Phase { get; internal set; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventArgs td = new PropertyChangedEventArgs(name);
            try
            {

                if (null != PropertyChanged)
                {
                    PropertyChanged(this, td);
                }
            }
            finally
            {
                td = null;
            }
        }

        #endregion
    }
}