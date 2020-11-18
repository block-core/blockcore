using System;
using System.Collections.Generic;
using Blockcore.Consensus;
using NBitcoin;

namespace Blockcore.Features.PoA
{
    public class PoAConsensusOptions : ConsensusOptions
    {
        /// <summary>Public keys and other federation members related information at the start of the chain.</summary>
        /// <remarks>
        /// Do not use this list anywhere except for at the initialization of the chain.
        /// Actual collection of the federation members can be changed with time.
        /// Use <see cref="IFederationManager.GetFederationMembers"/> as a source of
        /// up to date federation keys.
        /// </remarks>
        public List<IFederationMember> GenesisFederationMembers { get; set; }

        public uint TargetSpacingSeconds { get; set; }

        /// <summary>Adds capability of voting for adding\kicking federation members and other things.</summary>
        public bool VotingEnabled { get; set; }

        /// <summary>Makes federation members kick idle members.</summary>
        /// <remarks>Requires voting to be enabled to be set <c>true</c>.</remarks>
        public bool AutoKickIdleMembers { get; set; }

        /// <summary>Time that federation member has to be idle to be kicked by others in case <see cref="AutoKickIdleMembers"/> is enabled.</summary>
        public uint FederationMemberMaxIdleTimeSeconds { get; set; } = 60 * 60 * 24 * 7;

        /// <summary>Initializes values for networks that use block size rules.</summary>
        public PoAConsensusOptions()
        {
            if (this.AutoKickIdleMembers && !this.VotingEnabled)
                throw new ArgumentException("Voting should be enabled for automatic kicking to work.");
        }
    }
}