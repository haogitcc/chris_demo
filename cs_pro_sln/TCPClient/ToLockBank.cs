using NetAPI.Entities;

namespace TCPClient
{
    public class ToLockBank
    {
        private LockBank lockBank = LockBank.EPC;
        private bool isChecked = false;

        public LockBank TheLockBank
        {
            get { return lockBank; }
            set { lockBank = value; }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set { isChecked = value; }
        }
    }
}