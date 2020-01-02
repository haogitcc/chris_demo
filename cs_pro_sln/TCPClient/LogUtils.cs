using NetAPI;
using NetAPI.Core;
using NetAPI.Entities;
using System;
using System.Text;

namespace TCPClient
{
    public class LogUtils
    {
        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Log(string msg, ErrInfo e)
        {
            Console.WriteLine(msg);
            Log(string.Format("ErrCode:{0}" +
                "\r\nErrMsg:{1}", e.ErrCode, e.ErrMsg));
        }

        public void Log(string msg, IReaderMessage e)
        {
            Console.WriteLine(msg);
            Log(string.Format("MessageID:{0}" +
                "\r\nStatus:{1}" +
                "\r\nErrorInfo.ErrCode:{2}" +
                "\r\nErrorInfo.ErrMsg:{3}" +
                "\r\nReceivedData={4}", e.MessageID, e.Status, e.ErrorInfo.ErrCode, e.ErrorInfo.ErrMsg, ByteFormat.ToHex(e.ReceivedData, "", " ")));
        }

        public void Log(string msg, RxdTagData tagData)
        {
            Console.WriteLine(msg);
            Log(string.Format("EPC:{0}" +
                "\r\nTID:{1}" +
                "\r\nUser:{2}" +
                "\r\nReserved:{3}" +
                "\r\nAnetnna:{4}" +
                "\r\nRSSI:{5}",
                ByteFormat.ToHex(tagData.EPC, "", " "),
                ByteFormat.ToHex(tagData.TID, "", " "),
                ByteFormat.ToHex(tagData.User, "", " "),
                ByteFormat.ToHex(tagData.Reserved, "", " "),
                tagData.Antenna,
                tagData.RSSI));
        }

        internal void Log(string msg, WriteTagParameter param)
        {
            try
            {
                Log(string.Format("{0}:\r\n" +
                "IsLoop={1}\r\n" +
                "AccessPassword={2}\r\n" +
                "selectMemoryBank={3}\r\n" +
                "Ptr={4}\r\n" +
                "TagData={5}\r\n" +
                "IsFixedSize={6}\r\n" +
                "IsReadOnly={7}\r\n" +
                "IsSynchronized={8}\r\n" +
                "Length={9}\r\n" +
                "LongLength={10}\r\n" +
                "Rank={11}\r\n" +
                "SyncRoot{12}\r\n",
                msg,
                param.IsLoop,
                ByteFormat.ToHex(param.AccessPassword, "", " "),
                param.SelectTagParam.MemoryBank,
                param.SelectTagParam.Ptr,
                ByteFormat.ToHex(param.SelectTagParam.TagData, "", " "),
                param.WriteDataAry.IsFixedSize,
                param.WriteDataAry.IsReadOnly,
                param.WriteDataAry.IsSynchronized,
                param.WriteDataAry.Length,
                param.WriteDataAry.LongLength,
                param.WriteDataAry.Rank,
                param.WriteDataAry.SyncRoot));
            }
            catch(Exception e)
            {
                Log("", e);
            }
        }

        public void Log(string v, Exception ex)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append("*************************************** \r\n");
            msg.AppendFormat(" 异常发生时间： {0} \r\n", DateTime.Now);
            msg.AppendFormat(" 异常类型： {0} \r\n", ex.HResult);
            msg.AppendFormat(" 导致当前异常的 Exception 实例： {0} \r\n", ex.InnerException);
            msg.AppendFormat(" 导致异常的应用程序或对象的名称： {0} \r\n", ex.Source);
            msg.AppendFormat(" 引发异常的方法： {0} \r\n", ex.TargetSite);
            msg.AppendFormat(" 异常堆栈信息： {0} \r\n", ex.StackTrace);
            msg.AppendFormat(" 异常消息： {0} \r\n", ex.Message);
            msg.Append("***************************************");

            Log(string.Format("{0} Error Log\r\n {1}", msg));
        }

        public void Log(string msg, TagParameter w1)
        {
            Log(string.Format("______________________________\r\n{0}:\r\n" +
                "MemoryBank={1}\r\n" +
                "Ptr={2}\r\n" +
                "TagData={3}\r\n" +
                "-----------------------------------",
                msg,
                w1.MemoryBank,
                w1.Ptr,
                ByteFormat.ToHex(w1.TagData, "", " ")));
        }

        public void Log(string msg, LockTagParameter param)
        {
            Log(string.Format("****---------->Msg={0}\r\n LockBank={1}\r\n LockType={2}\r\n AccessPassword={3}\r\n Select:\r\n MemoryBank={4}\r\n Ptr={5}\r\n TagData={6}\r\n<---------****",
                msg, 
                param.LockBank,
                param.LockType,
                ByteFormat.ToHex(param.AccessPassword, "", " "),
                param.SelectTagParam.MemoryBank,
                param.SelectTagParam.Ptr,
                ByteFormat.ToHex(param.SelectTagParam.TagData, "", " ")));
        }

        internal void Log(string msg, KillTagParameter param)
        {
            Log(string.Format("****---------->Msg={0}\r\n " +
                "KillPassword={1}\r\n " +
                "Select:\r\n " +
                "MemoryBank={2}\r\n " +
                "Ptr={3}\r\n " +
                "TagData={4}\r\n<---------****",
                msg,
                ByteFormat.ToHex(param.KillPassword, "", " "),
                param.SelectTagParam.MemoryBank,
                param.SelectTagParam.Ptr,
                ByteFormat.ToHex(param.SelectTagParam.TagData, "", " ")));
        }
    }
}
