using System.Collections.Generic;
using Blockcore.Features.NodeHost.Authentication;

namespace Blockcore.Features.NodeHost.Settings
{
    public class BlockcoreSettings
    {
        public ApiKeys API { get; set; }
    }

    public class ApiKeys
    {
        public List<ApiKey> Keys { get; set; }
    }
}
