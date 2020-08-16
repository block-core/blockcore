using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Controllers;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Storage.Controllers
{
    [Route(".well-known/blockcore/node")]
    public class WellKnownController : FeatureController
    {
        private readonly IConfigurationRoot configuration;

        private readonly DataStore dataStore;

        private readonly StorageSchemas schemas;

        public WellKnownController(IConfigurationRoot configuration, IDataStore dataStore, StorageSchemas schemas)
        {
            this.configuration = configuration;
            this.dataStore = (DataStore)dataStore;
            this.schemas = schemas;
        }

        /// <summary>
        /// Returns the supported schemas on this node.
        /// </summary>
        /// <returns></returns>
        [HttpGet("storage/schemas")]
        public IActionResult GetSchemaVersions()
        {
            return Ok(this.schemas);
        }

        /// <summary>
        /// Returns the identity of the node.
        /// </summary>
        /// <returns></returns>
        [HttpGet("identity")]
        public IActionResult GetNodeIdentity()
        {
            string identifier = this.configuration.GetValue<string>("Blockcore:Node:Identifier");
            IdentityDocument identity = this.dataStore.GetDocumentById<IdentityDocument>("identity", identifier);

            // Just temporary sample data.
            // identity.Content.Url = "https://city.hub.liberstad.com";
            // identity.Content.Image = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAAACXBIWXMAAC4jAAAuIwF4pT92AAAGU2lUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgNi4wLWMwMDIgNzkuMTY0MzUyLCAyMDIwLzAxLzMwLTE1OjUwOjM4ICAgICAgICAiPiA8cmRmOlJERiB4bWxuczpyZGY9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPiA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtbG5zOmRjPSJodHRwOi8vcHVybC5vcmcvZGMvZWxlbWVudHMvMS4xLyIgeG1sbnM6cGhvdG9zaG9wPSJodHRwOi8vbnMuYWRvYmUuY29tL3Bob3Rvc2hvcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIgeG1wOkNyZWF0b3JUb29sPSJBZG9iZSBQaG90b3Nob3AgMjEuMSAoV2luZG93cykiIHhtcDpDcmVhdGVEYXRlPSIyMDIwLTA0LTIxVDEyOjA5KzAyOjAwIiB4bXA6TW9kaWZ5RGF0ZT0iMjAyMC0wNC0yMVQxMjo0NTozMSswMjowMCIgeG1wOk1ldGFkYXRhRGF0ZT0iMjAyMC0wNC0yMVQxMjo0NTozMSswMjowMCIgZGM6Zm9ybWF0PSJpbWFnZS9wbmciIHBob3Rvc2hvcDpDb2xvck1vZGU9IjMiIHBob3Rvc2hvcDpJQ0NQcm9maWxlPSJzUkdCIElFQzYxOTY2LTIuMSIgeG1wTU06SW5zdGFuY2VJRD0ieG1wLmlpZDo3NDJkZDczZS1hN2I0LTU0NGUtYTkzMy0wODJhMWU1N2NjMDEiIHhtcE1NOkRvY3VtZW50SUQ9ImFkb2JlOmRvY2lkOnBob3Rvc2hvcDpjZjk5ODY4NC1mYTFjLWNjNGMtYTNkOS1kZWNmYzAwY2IzYWYiIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDpjODg0MWM5Zi05ODViLTc3NGEtYmJhNC1lZjRjYThmYjE3YzYiPiA8eG1wTU06SGlzdG9yeT4gPHJkZjpTZXE+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJjcmVhdGVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOmM4ODQxYzlmLTk4NWItNzc0YS1iYmE0LWVmNGNhOGZiMTdjNiIgc3RFdnQ6d2hlbj0iMjAyMC0wNC0yMVQxMjowOSswMjowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDIxLjEgKFdpbmRvd3MpIi8+IDxyZGY6bGkgc3RFdnQ6YWN0aW9uPSJjb252ZXJ0ZWQiIHN0RXZ0OnBhcmFtZXRlcnM9ImZyb20gYXBwbGljYXRpb24vdm5kLmFkb2JlLnBob3Rvc2hvcCB0byBpbWFnZS9wbmciLz4gPHJkZjpsaSBzdEV2dDphY3Rpb249InNhdmVkIiBzdEV2dDppbnN0YW5jZUlEPSJ4bXAuaWlkOjc0MmRkNzNlLWE3YjQtNTQ0ZS1hOTMzLTA4MmExZTU3Y2MwMSIgc3RFdnQ6d2hlbj0iMjAyMC0wNC0yMVQxMjo0NTozMSswMjowMCIgc3RFdnQ6c29mdHdhcmVBZ2VudD0iQWRvYmUgUGhvdG9zaG9wIDIxLjEgKFdpbmRvd3MpIiBzdEV2dDpjaGFuZ2VkPSIvIi8+IDwvcmRmOlNlcT4gPC94bXBNTTpIaXN0b3J5PiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/Phy+kcsAAAg7SURBVHja7d3tVVpZGIbhx1RACdgBdkA6IB1gBUMq8KQC7UA6kKlApgLtIJRAB84PN4OZFaOJX5zzXtdarJk1PyZxAzdnb17h093dXdzc3GrePmVAjo6O+nCbHh0dTXryd3X7321oPoX3Mk5y3W4j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";

            return Ok(identity);

            //var settings = new HubEntity();
            //this.configuration.GetSection("Blockcore").GetSection("Node").Bind(settings);
            //return Ok(settings);
        }
    }
}
