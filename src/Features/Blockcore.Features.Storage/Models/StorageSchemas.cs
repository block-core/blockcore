using System;
using System.Collections.Generic;
using System.Text;

namespace Blockcore.Features.Storage.Models
{
    public class StorageSchemas
    {
        /// <summary>
        /// Highest version of the identity schema that is currently supported.
        /// </summary>
        public short IdentityMaxVersion { get; set; }

        /// <summary>
        /// Lowest version of the identity schema that is currently supported.
        /// </summary>
        public short IdentityMinVersion { get; set; }


        public bool SupportedIdentityVersion(short version)
        {
            return !(version < this.IdentityMinVersion || version > this.IdentityMaxVersion);
        }

        /// <summary>
        /// Highest version of the generic data types supported.
        /// </summary>
        public short DataMaxVersion { get; set; }

        /// <summary>
        /// Lowest version of the generic data types supported.
        /// </summary>
        public short DataMinVersion { get; set; }

        public bool SupportedDataVersion(short version)
        {
            return !(version > this.DataMaxVersion && version < this.DataMinVersion);
        }

        /// <summary>
        /// Highest version of the hub schema that is currently supported.
        /// </summary>
        public short HubMaxVersion { get; set; }

        /// <summary>
        /// Lowest version of the hub schema that is currently supported.
        /// </summary>
        public short HubMinVersion { get; set; }

        public bool SupportedHubVersion(short version)
        {
            return !(version > this.HubMaxVersion && version < this.HubMinVersion);
        }
    }
}
