using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Blockcore.Controllers;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using Blockcore.Jose;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage.Controllers
{
    /// <summary>
    /// All write operations to the identity controller must be signed and will be validated. 
    /// Identities can only be edited by the owner of the private key, which must sign the data before submitting to the API.
    /// </summary>
    [Authorize]
    [Route("api/identity")]
    public class IdentityController : FeatureController
    {
        private readonly DataStore dataStore;

        private readonly StorageSchemas schemas;

        private readonly StorageFeature storageFeature;

        public IdentityController(IDataStore dataStore, StorageSchemas schemas, StorageFeature storageFeature)
        {
            this.dataStore = (DataStore)dataStore;
            this.schemas = schemas;
            this.storageFeature = storageFeature;
        }

        /// <summary>
        /// Returns all registered identities. This API will be removed and is only available for testing.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> GetIdentities()
        {
            IEnumerable<IdentityDocument> identities = this.dataStore.GetIdentities();
            return Ok(identities);
        }

        /// <summary>
        /// Retrieve the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public async Task<IActionResult> GetIdentity([FromRoute] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            IdentityDocument document = this.dataStore.GetDocumentById<IdentityDocument>("identity", address);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }

        /// <summary>
        /// Persist the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> PutIdentity([FromBody] Message message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //if (string.IsNullOrWhiteSpace(address))
            //{
            //    return BadRequest();
            //}

            // Make sure that only identity documents is submitted to this API.
            //if (document.GetContainer() != "identity")
            //{
            //    return Problem("Container must be identity.");
            //}

            // Make sure the route address and document owner is the same.
            //if (address != document.GetIdentifier())
            //{
            //    return BadRequest();
            //}

            if (!this.schemas.SupportedIdentityVersion(message.Version))
            {
                return Problem(title: "Incompatible version", detail: $"Unsupported document version: {message.Version}. Supported range: {this.schemas.IdentityMinVersion}-{this.schemas.IdentityMaxVersion}.", statusCode: 400);
            }

            // Important that we require signature to be included and verified.
            Identity identity = JWT.Decode<Identity>(message.Content, requireSignature: true);

            // Parse the header as we want to verify the identity.
            JwsHeader header = JWT.Headers<JwsHeader>(message.Content);

            // Make sure the key ID is and the identifier is the same. This is only done for identities, other data structures does not perform this validation.

            var identitySplit = identity.Identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);

            // To avoid anyone putting too many ":" into the identity, let's ensure there are only 2.
            if (identitySplit.Length != 3)
            {
                return Problem(title: "Incorrect identifier", detail: "The identifier is not in correct format. Should be \"did:is:identifier\".", statusCode: 400);
            }

            if (header.KeyID != identitySplit[2])
            {
                return Problem(title: "Incorrect identifier", detail: "The identifier in header and payload is not equal.", statusCode: 400);
            }

            string[] values = message.Content.Split('.');

            IdentityDocument document = new IdentityDocument();

            document.Header = values[0];
            document.Payload = values[1];
            document.Signature = values[2];

            document.Content = identity;
            document.Version = message.Version;

            // Use the Identifier from the signed document to find existing document.
            IdentityDocument existingIdentity = this.dataStore.GetDocumentById<IdentityDocument>("identity", document.Content.Identifier);

            // If the supplied identity is older, don't update. This will allow equal heights to be updated. That means updating with same height might result in different states across the nodes.
            if (existingIdentity != null && existingIdentity.Content.Timestamp > document.Content.Timestamp)
            {
                return Problem("Your document has a height lower than previously and was not accepted. Increase the height and sign document again.");
            }

            // Appears that ID is not sent, even if it was, we should always take it from Content anyway to ensure nobody sends
            // us data that doesn't belong.
            document.Id = document.Content.Identifier;

            this.dataStore.SetIdentity(document);

            // string json = JsonConvert.SerializeObject(document, JsonSettings.Storage);

            // Announce the recently observed identity to connected nodes.
            await this.storageFeature.AnnounceDocument("identity", message.Content);

            return Ok("true");

            //byte[] entityBytes = MessagePackSerializer.Serialize(document.Content, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            //var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(address, ProfileNetwork.Instance);
            //var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature.Value);

            //if (!valid)
            //{
            //    // Invalid signature.
            //    return BadRequest();
            //}
        }

        ///// <summary>
        ///// Persist the profile of an identity.
        ///// </summary>
        ///// <param name="address"></param>
        ///// <returns></returns>
        //[HttpPut("{address}")]
        //public async Task<IActionResult> PutIdentity2([FromRoute] string address, [FromBody] IdentityDocument document)
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
        //    if (document.GetContainer() != "identity")
        //    {
        //        return Problem("Container must be identity.");
        //    }

        //    // Make sure the route address and document owner is the same.
        //    if (address != document.GetIdentifier())
        //    {
        //        return BadRequest();
        //    }

        //    if (!this.schemas.SupportedIdentityVersion(document.Version))
        //    {
        //        return Problem(title: "Incompatible version", detail: $"Unsupported document version: {document.Version}. Supported range: {this.schemas.IdentityMinVersion}-{this.schemas.IdentityMaxVersion}.", statusCode: 400);
        //    }

        //    byte[] entityBytes = MessagePackSerializer.Serialize(document.Content, MessagePack.Resolvers.ContractlessStandardResolver.Options);

        //    var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(address, ProfileNetwork.Instance);

        //    var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature.Value);

        //    if (!valid)
        //    {
        //        // Invalid signature.
        //        return BadRequest();
        //    }

        //    // Use the Identifier from the signed document to find existing document.
        //    IdentityDocument existingIdentity = this.dataStore.GetDocumentById<IdentityDocument>("identity", document.Content.Identifier);

        //    // If the supplied identity is older, don't update. This will allow equal heights to be updated. That means updating with same height might result in different states across the nodes.
        //    if (existingIdentity != null && existingIdentity.Content.Height > document.Content.Height)
        //    {
        //        return Problem("Your document has a height lower than previously and was not accepted. Increase the height and sign document again.");
        //    }

        //    // Appears that ID is not sent, even if it was, we should always take it from Content anyway to ensure nobody sends
        //    // us data that doesn't belong.
        //    document.Id = "identity/" + document.Content.Identifier;

        //    this.dataStore.SetIdentity(document);

        //    string json = JsonConvert.SerializeObject(document, JsonSettings.Storage);

        //    // Announce the recently observed identity to connected nodes.
        //    await this.storageFeature.AnnounceDocument("identity", json);

        //    return Ok(valid.ToString());
        //}

        ///// <summary>
        ///// Remove the profile of an identity.
        ///// </summary>
        ///// <param name="address"></param>
        ///// <returns></returns>
        //[HttpDelete("{address}")]
        //public async Task<IActionResult> DeleteIdentity([FromRoute] string address, [FromBody] IdentityDocument document)
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
        //    if (document.GetContainer() != "identity")
        //    {
        //        return Problem("Container must be identity.");
        //    }

        //    // Make sure the route address and document owner is the same.
        //    if (address != document.GetIdentifier())
        //    {
        //        return BadRequest();
        //    }

        //    if (!this.schemas.SupportedIdentityVersion(document.Version))
        //    {
        //        return Problem(title: "Incompatible version", detail: $"Unsupported document version: {document.Version}. Supported range: {this.schemas.IdentityMinVersion}-{this.schemas.IdentityMaxVersion}.", statusCode: 400);
        //    }

        //    byte[] entityBytes = MessagePackSerializer.Serialize(document.Content, MessagePack.Resolvers.ContractlessStandardResolver.Options);

        //    var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(address, ProfileNetwork.Instance);

        //    var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature.Value);

        //    if (!valid)
        //    {
        //        // Invalid signature.
        //        return BadRequest();
        //    }

        //    // Use the Identifier from the signed document to find existing document.
        //    IdentityDocument existingIdentity = this.dataStore.GetDocumentById<IdentityDocument>("identity", document.Content.Identifier);

        //    // If the supplied identity is older, don't update. This will allow equal heights to be updated. That means updating with same height might result in different states across the nodes.
        //    if (existingIdentity != null && existingIdentity.Content.Height > document.Content.Height)
        //    {
        //        return Problem("Your document has a height lower than previously and was not accepted. Increase the height and sign document again.");
        //    }

        //    // Appears that ID is not sent, even if it was, we should always take it from Content anyway to ensure nobody sends
        //    // us data that doesn't belong.
        //    document.Id = "identity/" + document.Content.Identifier;

        //    this.dataStore.SetIdentity(document);

        //    string json = JsonConvert.SerializeObject(document, JsonSettings.Storage);

        //    // Announce the recently observed identity to connected nodes.
        //    await this.storageFeature.AnnounceDocument("identity", json);

        //    return Ok(valid.ToString());

        //    //if (string.IsNullOrWhiteSpace(address))
        //    //{
        //    //    return BadRequest();
        //    //}

        //    //IdentityDocument document = this.dataStore.GetDocumentById<IdentityDocument>("identity", address);

        //    //if (document == null)
        //    //{
        //    //    return NotFound();
        //    //}

        //    //// Perform validation of signature and allow delete.

        //    //return Ok(address);
        //}
    }
}
