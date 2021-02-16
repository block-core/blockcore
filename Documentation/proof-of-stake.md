## Specification of the blockcore Proof Of Stake ##


### What is POS vs POW

[TBD]


### Definitions

#### StakeMinConfirmations

The minimum confirmations required for a coin to be good enough to participate in staking
Must be equal or greater then MaxReorg this is to discourage attackers to stake in isolation and then force a reorg.

#### MaxReorg

Long reorganization protection or the maximal length of reorganization that the node is willing to accept.
The reason to prevent long reorganization is to prevent "history attack" or in other words old spent coins can't be reused to create a longer chain in isolation and cause big reorgs.

Honest nodes will not switch to a chain that forked earlier then maxreorg, and because StakeMinConfirmations will not allow to reuse coins before maxreorg then staking in isolation cannot cause long reorganisations.

#### Coin maturity

The number of confirmations a newly found coinstake needs to be buried under before it can be spent.
(Not to be confused with StakeMinConfirmations whic is for staking)

#### Proven Headers
 
Those are headers that curry all the information that is needed to validate a coinstake.
Proven headers are used as a headers first approach where the node will first download the headers of blocks and only if the header is valid will the node fetch the entire block for full validation.

The full Proven Headers specification can be found here
https://github.com/block-core/blockcore/blob/master/Documentation/Features/ProvenHeaders.md

#### Target Difficulty

The difficulty target for the next block, or how hard will it be to find the next valid Kernal to satisfy the target difficulty.

#### Kernal hash

A random sha256 hash created from a number of parameters corresponding to the coinstake.
A valid stake kernel hash satisfies the target difficulty.

#### Target Spacing

The expected block time in seconds, or how often do we expect the network to produce a block.

#### Future Drift

Future drift is maximal allowed block's timestamp difference over adjusted time.
We set the future drift to a fixed value of 15 seconds (the time it takes a block to propagate around the world is roughly 12 seconds)

#### Mask

A mask for coinstake header's timestamp. Used to decrease granularity of timestamp
This corresponds to the number of blocks that can be produced in a given time span.

For example if the Mask = 16 and the TargetSpacing = 64 then a valid coinstake timestamp can be found only 4 times within the target spacing.
Staking nodes will try to find a valid coinstake kernal every time the Mask is elapsed (in the example above every 16 seconds but no more then future drift seconds forward)

#### Stake Modifiers

The stake modifier is a chain of coinstake hashes all the way from the first POS block.
It's used to introduce an element of randomness to the Kernal calculations, in order to scramble computation to make it very difficult to precompute future proof-of-stake

### How it works on blockcore

#### the parameters that are used for hashing a valid kernel

How is a valid coinstake found? This is the heart of the processes.

The processes of staking will go as following, every time a MASK time elapses the node will iterate over all its stakeable outputs (the outputs that reached maturity and are beyond maxreorg)

Then each output will be hashed with the following parameters:

- Previous StakeModifier - the stake modifier is a chain of coinstake hashes 
- Output hash - the hash of the output of the coins that are being spent to find the kernel 
- Old Output N - the position of the output of the coins that are being spent to find the kernel 
- New coinstake current time - (the timestamp of the new output that will be created, this must fits the MASK rule)

This is called the Kernel.

The Target:
The target is the number that a kernel hash needs to be bellow in order to be considered valid.
In order to give a better chance to bigger outputs (a UTXO with more coins) The target is pushed up by a factor of haw many coins a UTXO has,
This is called the weighted target it means the target of the UTXO is higher the more coins it has, as a result statistically it will find a solution faster.

If the resulting kernel hash of the above calculations is below the weighted target then the coinstake is valid and can be used to create a block.

#### The importance of syncing time correctly (future drift)

Each node has a consensus rule of a fixed interval of 15 seconds that will enforce how far in the future it will accept blocks,
blocks with a time stamp that is 15 seconds further then our nodes current datetime will be rejected.

But such rejected blocks will not be considered invalid, in case our node was just had the wrong time in comparison to the network, 
and the network accepts such a block our node would fork away form the network consensus.

This means it is crucial that nodes on the network that participate in full consensus rules validation will be on the same UTC datetime.
To achieve that we use the computers local current time, and double check that against all connected peers datetime 
(when a peer first connects it will advertise its current UTC datetime) giving the datetime samples for outbound nodes 3x [check that is actually 3x] more weight in the measurements 
(this is in order to prevent a certain attack on a node where an attacker can initiate many inbound connections and effect our nodes avg time).
If the local time and peers avg time do not match the node will print out a warning message and default to peers time [need to double check the default].


#### Block Signatures (why sign a block with the private key owning the UTXO)

The coinstake that found a valid kernel and thus was selected to create a block is used to proof ownership of the UTXO by providing the signature that spends the outputs
However such an output has no cryptographic strong link to the block itself, meaning an attacker can take the valid coinstake utxo and put it in another block the attacker created 
and propagate that to the network, the attacker could then censor transactions at will.

By signing the block with the same key that owns the UTXO peers can validate the block was created by the owner of the coinstake.
The block signature is attached at the end of the serialized block and is not part of the header hash.

#### How is the next difficulty target calculated 

The calculation of the next target is based on the last target value and the block time (aka spacing) (i.e. difference in time stamp of this block and its immediate predecessor). 
The target changes every block and it is adjusted down (i.e. towards harder to reach, or more difficult) if the time to mine last block was lower than the target block time.
And it is adjusted up if it took longer than the target block time. The adjustments are done in a way the target is moving towards the target-spacing (expected block time) exponentially, so even a big change in the mining power on the network will be fixed by retargeting relatively quickly.

#### Changes made in POSv4

Two changes where made in POSv4.

- The removal of the time stamp from the transaction serialization:
this makes POS transactions serialize the same as Bitcoin transactions, 
the benefit of that is easier to use various blockchain tools that made for Bitcoin.
That time stamp was used in the kernel hash however the kernel hash was anyway defaulting to the header timestamp 
so there was no need to serialize the time stamp also in each transaction.

- The removal of the coinstake time from the kernel hash calculations:
when checking the kernel validity a few parameters are hashed together to create randomness,
previously the timestamp of the utxo that is being spent was also included in that hash 
however it provides no additional randomness because the previous outpoint is used as well and that is already unique

#### Coldstaking (multisig staking using P2WSH) 

Coldstaking is a mechanism that eliminates the need to keep the coins in a hot wallet.
When setting up coldstaking a user generates two wallets (two different private keys) one key can only be used for staking (creating other coinstakes) and the other key is used for spending the coins. 

Cold staking still requires to have a fully synced node running and connected to the network.

The full Coldstaking specification can be found here
https://github.com/block-core/blockcore/blob/master/Documentation/Features/ColdStaking.md

## Weaknesses

#### NAS

#### Stake Grinding

#### IBD

#### bribing



- How decentralized is POS
