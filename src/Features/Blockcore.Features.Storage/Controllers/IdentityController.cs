using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Features.Storage.Controllers
{
    /// <summary>
    /// All write operations to the identity controller must be signed and will be validated. 
    /// Identities can only be edited by the owner of the private key, which must sign the data before submitting to the API.
    /// </summary>
    [Route("api/identity")]
    public class IdentityController : FeatureController
    {
        private string currentPublicKey = "4b9715afa903c82b383abb74b6cd746bdd3beea3";

        public IdentityController()
        {
 

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

            // If we can't find the data.
            if (address != currentPublicKey)
            {
                return NotFound();
            }

            //var response = new Response();
            //response.Root = currentPublicKey;
            //response.Containers = new List<string>();
            //response.Containers.Add("gridmap");
            //response.Containers.Add("pos");
            //response.Containers.Add("friends");
            //response.Containers.Add("reviews");
            //response.Containers.Add("devices");
            //response.Containers.Add("pos");

            return Ok(address);

            //return NoContent();
        }

        /// <summary>
        /// Persist the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPut("{address}")]
        public async Task<IActionResult> PutIdentity([FromRoute] string address)
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
