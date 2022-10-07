using System;

namespace BlockBlockcore.Features.Miner.UI
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
