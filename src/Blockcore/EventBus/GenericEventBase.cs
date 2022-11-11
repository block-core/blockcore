namespace Blockcore.EventBus
{
    /// <summary>
    /// Basic implementation of a generic <see cref="EventBase"/> that exposes a typed Content property.
    /// This is abstract to force to create a specific event.
    /// </summary>
    /// <seealso cref="EventBase" />
    /// <typeparam name="TContent">The type of the content.</typeparam>
    /// <seealso cref="EventBase" />
    public abstract class GenericEventBase<TContent> : EventBase
    {
        /// <summary>
        /// Gets or sets the content of the event.
        /// </summary>
        /// <value>
        /// The event content.
        /// </value>
        public TContent Content { get; protected set; }

        /// <summary>
        /// Create a new instance of the GenericEventBase class.
        /// </summary>
        /// <param name="content">Content of the event</param>
        protected GenericEventBase(TContent content)
        {
            this.Content = content;
        }
    }
}
