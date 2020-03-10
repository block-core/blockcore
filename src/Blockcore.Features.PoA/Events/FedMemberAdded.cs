using Blockcore.EventBus;

namespace Blockcore.Features.PoA.Events
{
    /// <summary>
    /// Event that is executed when a new federation member is added.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class FedMemberAdded : EventBase
    {
        public IFederationMember AddedMember { get; }

        public FedMemberAdded(IFederationMember addedMember)
        {
            this.AddedMember = addedMember;
        }
    }
}
