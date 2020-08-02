using System;
using System.Collections.Generic;
using System.Text;

namespace Blockcore.Features.Storage.Payloads
{
    public enum StoragePayloadAction
    {
        SupportedCollections = 0,
        SendCollections = 1,
        SendSignatures = 2,
        SendDocuments = 3
    }
}
