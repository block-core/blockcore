using System;

namespace Blockcore.Features.Wallet.UI
{
    public static class AddressBookNotification
    {
        public static EventHandler<bool> AddressBookChanged;

        private static bool _addressbookchanged;
        public static bool OnChangedAddressBook
        {
            get => _addressbookchanged;
            set
            {
                if (_addressbookchanged != value)
                {
                    _addressbookchanged = value;
                    AddressBookChanged?.Invoke(typeof(AddressBookNotification), _addressbookchanged);
                }
            }
        }
    }
}
