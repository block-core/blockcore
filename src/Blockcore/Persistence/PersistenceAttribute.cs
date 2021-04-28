using System;

namespace Blockcore.Persistence
{
    /// <summary>
    /// Used to require a persistence implementation.
    /// This attribute has to be placed on top of implementation of services interface that manage persistence (e.g. IBlockStoreRepository
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PersistenceAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the persistence implementation.
        /// This is the value that will be checked against <see cref="Blockcore.Configuration.NodeSettings.DbType"/>.
        /// </summary>
        /// <remarks>Case Insensitive.</remarks>
        public string PersistenceImplementation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceAttribute" /> class.
        /// </summary>
        /// <param name="persistenceImplementation">The persistence implementation. By convention it's the name of the underlying product that manage persistence (e.g. "LevelDb", "Rocksdb", etc...)</param>
        public PersistenceAttribute(string persistenceImplementation)
        {
            this.PersistenceImplementation = persistenceImplementation;
        }
    }
}
