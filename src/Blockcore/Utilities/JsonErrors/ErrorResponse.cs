using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blockcore.Utilities.JsonErrors
{
    public class ErrorResponse : ErrorResponseTypes
    {
        [JsonProperty(PropertyName = "errors")]
        public List<ErrorModel> Errors { get; set; }
    }

    public class ErrorResponseLists : ErrorResponseTypes
    {
        [JsonProperty(PropertyName = "errors")]
        public ErrorModel Errors { get; set; }
    }

    public class ErrorResponseTypes
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }
    }

    public class ErrorModel
    {
        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "password")]
        public List<string> Password { get; set; }

        [JsonProperty(PropertyName = "name")]
        public List<string> Name { get; set; }

        [JsonProperty(PropertyName = "mnemonic")]
        public List<string> Mnemonic { get; set; }

        [JsonProperty(PropertyName = "feeType")]
        public List<string> FeeType { get; set; }

        [JsonProperty(PropertyName = "deleteAll")]
        public List<string> DeleteAll { get; set; }
    }
}