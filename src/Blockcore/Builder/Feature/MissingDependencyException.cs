using System;

namespace Blockcore.Builder.Feature
{
    /// <summary>
    /// Exception thrown when feature dependencies are missing.
    /// </summary>
    [Serializable]
    public class MissingDependencyException : Exception
    {
        /// <inheritdoc />
        public MissingDependencyException()
            : base()
        {
        }

        /// <inheritdoc />
        public MissingDependencyException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public MissingDependencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MissingDependencyException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
