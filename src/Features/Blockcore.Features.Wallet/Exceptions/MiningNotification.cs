using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockcore.Features.Wallet.Exceptions
{
    public static class MiningNotification
    {
        public static EventHandler<bool> MiningChanged;

        private static bool _miningchanged;
        public static bool OnChangedStatus
        {
            get => _miningchanged;
            set
            {
                if (_miningchanged != value)
                {
                    _miningchanged = value;
                    MiningChanged?.Invoke(typeof(MiningNotification), _miningchanged);
                }
            }
        }

    }
}
