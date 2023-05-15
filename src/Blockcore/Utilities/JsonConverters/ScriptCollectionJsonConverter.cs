using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace Blockcore.Utilities.JsonConverters
{
    /// <summary>
    /// Converter used to convert a <see cref="Script"/> or a <see cref="WitScript"/> to and from JSON.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    public class ScriptCollectionJsonConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ICollection<Script>) || objectType == typeof(ICollection<WitScript>);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            try
            {
                var array = serializer.Deserialize<string[]>(reader);

                if (objectType == typeof(ICollection<Script>))
                {
                    return array.Select(s => Script.FromBytesUnsafe(Encoders.Hex.DecodeData(s))).ToList();
                }

                if (objectType == typeof(ICollection<WitScript>))
                {
                    return array.Select(s => new WitScript(s)).ToList();
                }
            }
            catch (FormatException)
            {
            }

            throw new JsonObjectException("A script should be a byte string", reader);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                if (value is ICollection<Script> array)
                {
                    var serialize = array.Select(s => Encoders.Hex.EncodeData(s.ToBytes(false))).ToArray();
                    serializer.Serialize(writer, serialize);
                }

                if (value is ICollection<WitScript> arrayw)
                {
                    var serialize = arrayw.Select(s => s.ToString()).ToArray();
                    serializer.Serialize(writer, serialize);
                }
            }
        }
    }
}