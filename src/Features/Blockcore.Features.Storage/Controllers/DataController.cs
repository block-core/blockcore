using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Blockcore.Controllers;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage.Controllers
{
    [Authorize]
    [Route("api/data")]
    public class DataController : FeatureController
    {
        private readonly DataStore dataStore;

        private readonly StorageSchemas schemas;

        private readonly StorageFeature storageFeature;

        public DataController(IDataStore dataStore, StorageSchemas schemas, StorageFeature storageFeature)
        {
            this.dataStore = (DataStore)dataStore;
            this.schemas = schemas;
            this.storageFeature = storageFeature;
        }

        ///// <summary>
        ///// Persist the profile of an identity.
        ///// </summary>
        ///// <param name="address"></param>
        ///// <returns></returns>
        //[HttpPut("{address}")]
        //public async Task<IActionResult> PutData([FromRoute] string address, [FromBody] DataDocument document)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (string.IsNullOrWhiteSpace(address))
        //    {
        //        return BadRequest();
        //    }

        //    // Make sure that only identity documents is submitted to this API.
        //    if (document.GetContainer() != "data")
        //    {
        //        return Problem("Container must be data.");
        //    }

        //    // Make sure the route address and document owner is the same.
        //    //if (address != document.GetIdentifier())
        //    //{
        //    //    return BadRequest();
        //    //}

        //    if (!this.schemas.SupportedIdentityVersion(document.Version))
        //    {
        //        return Problem(title: "Incompatible version", detail: $"Unsupported document version: {document.Version}. Supported range: {this.schemas.IdentityMinVersion}-{this.schemas.IdentityMaxVersion}.", statusCode: 400);
        //    }

        //    // First translate from Netwonsoft JObject to string
        //    var jsonText = document.Content.ToString();

        //    // Get the bytes from the JSON.
        //    // byte[] entityBytes = MessagePackSerializer.ConvertFromJson(jsonText);

        //    //byte[] entityBytes = MessagePackSerializer.Serialize(document.Content, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        //    // byte[] entityBytes = MessagePackSerializer.Typeless.Serialize(document.Content);
        //    // byte[] entityBytes = MessagePackSerializer.Serialize(document.Content, MessagePack.Resolvers.DynamicContractlessObjectResolver.Instance);
        //    // var json2 = MessagePackSerializer.SerializeToJson(entityBytes);

        //    var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(document.Signature.Identity, ProfileNetwork.Instance);

        //    //var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature.Value);

        //    //if (!valid)
        //    //{
        //    //    // Invalid signature.
        //    //    return BadRequest();
        //    //}

        //    // Use the Identifier from the signed document to find existing document.
        //    // IdentityDocument existingIdentity = this.dataStore.GetDocumentById<IdentityDocument>("identity", document.Content.Identifier);

        //    // If the supplied identity is older, don't update. This will allow equal heights to be updated. That means updating with same height might result in different states across the nodes.
        //    //if (existingIdentity != null && existingIdentity.Content.Height > document.Content.Height)
        //    //{
        //    //    return Problem("Your document has a height lower than previously and was not accepted. Increase the height and sign document again.");
        //    //}

        //    // Appears that ID is not sent, even if it was, we should always take it from Content anyway to ensure nobody sends
        //    // us data that doesn't belong.
        //    // document.Id = "identity/" + document.Content.Identifier;

        //    // this.dataStore.SetIdentity(document);

        //    string json = JsonConvert.SerializeObject(document, JsonSettings.Storage);

        //    // Announce the recently observed identity to connected nodes.
        //    // await this.storageFeature.AnnounceDocument("identity", json);

        //    return Ok("true");
        //}
    }
}
