using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Configuration;
using Blockcore.Controllers;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

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
        private string currentPublicKey = "PTe6MFNouKATrLF5YbjxR1bsei2zwzdyLU";
        private readonly DataStore dataStore;

        public IdentityController(IDataStore dataStore)
        {
            this.dataStore = (DataStore)dataStore;
        }

        /// <summary>
        /// Returns all registered identities. This API will be removed and is only available for testing.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> GetIdentities()
        {
            IEnumerable<IdentityEntity> identities = this.dataStore.GetIdentities();
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
            // Just an example.
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            var document = this.dataStore.GetIdentity(address);

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
        [HttpPut("{address}")]
        public async Task<IActionResult> PutIdentity([FromRoute] string address, [FromBody] IdentityDocument document)
        {
            // Just an example.
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            // Make sure the route address and document owner is the same.
            if (address != document.Owner)
            {
                // Invalid address (public key)
                return BadRequest();
            }

            // Testing to sign only name.
            byte[] entityBytes = MessagePackSerializer.Serialize(document.Body);
            
            // THIS WORKS, VALIDATES TO TRUE WHEN ONLY STRING IS USED!
            //entityBytes = Encoding.UTF8.GetBytes(document.Body.Name);

            var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(address, ProfileNetwork.Instance);

            var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature);

            if (!valid)
            {
                // Invalid signature.
                return BadRequest();
            }

            IdentityEntity entity = new IdentityEntity();
            entity.Owner = document.Owner;
            entity.Signature = document.Signature;

            // The EntityId should combine type/path and address (pubkey).
            // entity.EntityId = "identity/" + address;
            entity.EntityId = address;

            entity.Document = document.Body;

            dataStore.SetIdentity(entity);

            return Ok(valid.ToString());
        }

        /// <summary>
        /// Remove the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpDelete("{address}")]
        public async Task<IActionResult> DeleteIdentity([FromRoute] string address)
        {
            // Just an example.
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            // If we can't find the data.
            if (address != currentPublicKey)
            {
                return NotFound();
            }

            return Ok(address);
        }
    }
}
