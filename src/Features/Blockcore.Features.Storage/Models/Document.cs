using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace Blockcore.Features.Storage.Models
{
    /// <summary>
    /// The entity encapsulates the data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Document<T>
    {
        /// <summary>
        /// Id of the entity, which follows a format of: [container]/[owner]/[type]/[instance]
        /// </summary>
        /// <example>
        /// identity/PDhyn2Gavb4p5kdNa2dhg4XSWeEwLfwAwD
        /// data/PDhyn2Gavb4p5kdNa2dhg4XSWeEwLfwAwD/music/1
        /// </example>
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [BsonId]
        public string Id { get; set; }

        public string Signature { get; set; }

        public T Content { get; set; }

        public string GetIdentifier()
        {
            var values = this.Id.Split("/", System.StringSplitOptions.RemoveEmptyEntries);
            return values[1];
        }

        public string GetContainer()
        {
            var values = this.Id.Split("/", System.StringSplitOptions.RemoveEmptyEntries);
            return values[0];
        }
    }
}
