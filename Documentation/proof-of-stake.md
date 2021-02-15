## Specification of the blockcore Proof Of Stake ##


### What is POS vs POW




### Definitions

#### StakeMinConfirmations

The minimum confirmations required for a coin to be good enough to participate in staking
Must be equal or greater then MaxReorg this is to discourage attackers to stake in isolation and then force a reorg.

#### MaxReorg

Long reorganization protection or the maximal length of reorganization that the node is willing to accept.
The reaosn to prevent long reorganization is to prevent "hisotry attack" or in other words old spent coins can't be reused to create a longer chain in isolation and cause big reorgs.

Honest nodes will not switch to a chain that forked earler then maxreorg, and because StakeMinConfirmations will not allow to reuse coins before maxreorg then staking in isolation cannot cause long reorganisations.


#### Coinage

#### Proven Headers
 
#### Kernal hash

##### Target Spacing

##### Mask

##### Stake Modifiers



### How it works on blockcore

#### the parameters that are used for hashing a valid kernal

How is a valid coinstake found? this is the heart of the processes.

The processes of staking will go as following, everytime a MASK time elapses the node will iterate over all its stakable outputs (the outputs that reached maturity and are beyond maxreorg)

Then each output will be hashed with the following paramteres:

- Previous StakeModifier - the stake modifier is a chain of coinstake hashes 
- Output hash - the hash of the output of the coins that are being spent to find the kernal 
- Old Output N - the position of the output of the coins that are being spent to find the kernal 
- New coinstake current time - (the timestamp of the new output that will be created, this must fits the MASK rule)

This is called the Kernal.

The Target:
The target is the number that a kernal hash needs to be bellow in order to be considered valid.
In order to give a better chance to bigger outputs (a UTXO with more coins) The target is pushed up by a factor of haw many coins a UTXO has,
This is called the weighted target it means the target of the UTXO is higher the more coins it has, as a result statistically it will find a solution faster.

If the resulting kernal hash of the above calculations is bellow the weighted target then the coinstake is valid and can be used to create a block.

#### The importance of syncing time correctly (future drift)

Each node has a consensus rule of a fixed interval of X seconds that will enforce how far in the future it will accept blocks,
blocks with a time stamp that is X seconds further then our nodes current datetime will be rejected.

But such rejected blocks will not be considered invaid, in case our node was just had the wrong time in comparison to the network, 
and the network accepts such a block our node would fork away form the network consensus.

This means it is crucial that a all nodes on the network that participate in full consensus rules validation will be on the same UTC datetime.
To achive that we use the computers local current time, and double check that against all connected peers datetime 
(when a peer first connects it will advertise its current UTC datetime) giving the datetime samples for outbound nodes 3x [check that is actually 3x] more weight in the measurments 
(this is in order to prevent a certain attack on a node where an attacker can initiate many inbound connections and effect our nodes avg time).
If the local time and peers avg time do not match the node will print out a warrnign message and default to peers time [need to double check the default].


#### Block Signatures (why sign a block with the private key owning the UTXO)

The coinstake that found a valid kernal and thus was selected to create a block is used to proof ownership of the UTXO by providing the signature that spends the outputs
However such an output has no cryptographic strong link to the block itself, meaning an attacker can take the valid coinstake utxo and put it in another block the attacker created 
and propagate that to the network, the attacker could then censor transactions at will.

By signing the block with the same key that ownes the UTXO peers can validate the blocl was created by the owner of the coinstake.
The block signature is attached as an extention block at the end of the serialid blocks and is not part of the header hash.

#### How is the next difficulty target calculated 

#### Changes made in POSv4

Two changes where made in POSv4.

- The removal of the time stamp from the transaction serialization:
this makes POS transactions serialize the same as Bitcoin transactions, 
the benefit of that is easier to use various blockchain tools that made for Bitcoin.
That time stamp was used in the kernal hash however the kernal hash was anyway defaulting to the header timestamp 
so there was no need to serialize the time stamp also in each transaction.

- The removal of the coinstake time from the kernal hash calculations:
when checking the kernal validity a few parameters are hashed together to create randmoness,
previously the timestamp of the utxo that is being spent was also included in that hash 
however it provides no additional randomoness because the previous outpoint is used as well and that is already unique

#### Coldstaking (multisig staking using P2WSH) 

## Weaknesses

#### NAS

#### Stake Grinding

#### IBD

#### bribing



- How decentralized is POS
