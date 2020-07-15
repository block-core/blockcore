using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using LiteDB;

namespace Blockcore.Features.Storage.Models
{
    public class IdentityEntity
    {
        [BsonId]
        public string EntityId { get; set; }

        public string Owner { get; set; }

        public string Signature { get; set; }

        public IdentityModel Document { get; set; }
    }

    //public class IdentityEntity
    //{
    //    [BsonId]
    //    [StringLength(255, MinimumLength = 1)]
    //    [Required]
    //    public string Id { get; set; }

    //    public string Thumbprint { get; set; }

    //    public string DisplayName { get; set; }

    //    public string Avatar { get; set; }

    //    public List<ClaimModel> Claims { get; set; }

    //    public List<ContactInformationModel> Contact { get; set; }

    //    public IDictionary<string, string> Properties { get; set; }
    //}

    //public class ClaimModel
    //{
    //    public string Type { get; set; }

    //    public string Issuer { get; set; }

    //    public string Value { get; set; }

    //    public string Service { get; set; }

    //    public string Signature { get; set; }

    //    public IDictionary<string, string> Properties { get; }
    //}

    //public class ContactInformationModel
    //{
    //    public string Type { get; set; }

    //    public string Value { get; set; }
    //}
}
