using Blockcore.EventBus;

namespace Blockcore.Features.PoA.Events
{
    /// <summary>
    /// Event that is executed when federation member is kicked.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class FedMemberKicked : EventBase
    {
        public IFederationMember KickedMember { get; }

        public FedMemberKicked(IFederationMember  kickedMember)
        {
            this.KickedMember = kickedMember;
        }
    }
}
