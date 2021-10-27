## Overview of the Blockcore Proof Of Stake protocol ##

### What is POS vs POW

Proof Of Stake is an alternative way to achieve consensus to Proof Of Work, the difference with POS is that block producers use ownership of coins as the right to produce blocks and participating nodes can verify such claims by validating cryptographic signatures and the chain history.
A good comparison is that POW uses CPU cycles as measurement while POS uses units of coins.

### Definitions and explanations:

#### Coinbase

A special transaction that is produced by the miners (the producers of POW blocks) and contains information about the block.

#### Coinstake

A special transaction that is produced by the stakers (the producers of POS blocks) and contains the tx input and outputs of coins used to create a block.  
A coinstake input can be split in to several outputs, this is done in order to reduce the size of a staking output.  
Splitting a big output to many smaller outputs increases the chance of producing new blocks.  

#### StakeMinConfirmations

The minimum confirmations required for a coin to be good enough to participate in staking.  
Must be equal or greater than MaxReorg this is to discourage attackers to stake in isolation and then force a reorg.

#### MaxReorg

Long reorganization protection or the maximal length of reorganization that the node is willing to accept.
The reason to prevent long reorganization is to prevent "history attack" or in other words old spent coins can't be reused to create a longer chain in isolation and cause big reorgs.

Honest nodes will not switch to a chain that forked earlier than MaxReorg, and because StakeMinConfirmations will not allow to reuse coins before MaxReorg then staking in isolation cannot cause long reorganisations.

#### Coin maturity

The number of confirmations a newly found coinstake needs to be buried under before it can be spent.
(Not to be confused with StakeMinConfirmations which is for staking)

#### Proven Headers
 
Those are headers that contain all the information that is needed to validate a coinstake.
Proven headers are used as a `headers first` approach where the node will first download the headers of blocks and only if the header is valid will the node fetch the entire block for full validation.

The full Proven Headers specification can be found here
https://github.com/block-core/blockcore/blob/master/Documentation/Features/ProvenHeaders.md

#### Target Difficulty

The difficulty target for the next block, or how hard will it be to find the next valid Kernel to satisfy the target difficulty.

#### Kernel hash

A sha256 hash created from a number of parameters corresponding to the coinstake.
A valid stake kernel hash satisfies the target difficulty.

#### Target Spacing

The expected block time in seconds, or how often do we expect the network to produce a block.  
The target spacing should be multiples of the Mask.  

#### Future Drift

Future drift is maximal allowed block's timestamp difference over adjusted time.
We set the future drift to be a fixed value of 15 seconds which is close to the Mask value.

#### Mask

A bit mask for the coinstake header's timestamp. Used to decrease granularity of timestamp.  
This corresponds to the number of blocks that can be produced in a given time span.

For example if the bit mask is 15 (0x0000000F) then a valid coinstake's timestamp must be divisible by 16.

#### Stake Modifiers

The stake modifier forms a chain of hashes made from the previous stake modifier and the kernel all the way bacl to the first POS block.
It's used to introduce an additional input parameter to the Kernel calculations, in order to scramble computation to make it very difficult to precompute future proof-of-stake

### How it works on Blockcore

#### Hashing a valid kernel

How is a valid coinstake found? This is the heart of the processes.

The processes of staking will go as following, every time the masking of the timestamp is valid the node will iterate over all its stakeable outputs (the outputs that reached maturity and are beyond MaxReorg)

Then each output will be hashed with the following parameters:

- Previous StakeModifier - the stake modifier is a chain of coinstake hashes 
- Output hash - the hash of the output of the coins that are being spent to find the kernel 
- Output N - the position of the output of the coins that are being spent to find the kernel 
- New coinstake current time - (the timestamp of the new output that will be created, this must fit the MASK rule)

The output hash of the above is called the Kernel.

The Target:
The target is the number of which a kernel hash needs to be below in order to be considered valid.
In order to give a better chance to bigger outputs (a UTXO with more coins) The target is pushed up by a factor to the number of coins a UTXO has,
This is called the weighted target, it means the target of the UTXO is higher the more coins it has, as a result statistically it will find a solution faster.

If the resulting kernel hash of the above calculations is below the weighted target it means the coinstake is valid and can be used to create a block.

#### The importance of syncing time correctly (future drift)

Each node has a consensus rule of a fixed interval of 15 seconds that will enforce how far in the future it will accept blocks,
blocks with a time stamp that is 15 seconds further than local nodes current datetime will be rejected.

But such rejected blocks will not be considered invalid, in case our local node just had the wrong time in comparison to the network, 
and the network accepts such a block our local node would fork away form the network consensus.

This means it is crucial that nodes on the network that participate in full consensus rules validation will be on the same UTC datetime.
To achieve that we use the computers local current time, and double check that against all connected peers datetime 
(when a peer first connects it will advertise its current UTC datetime) giving the datetime samples for outbound nodes 3x more weight in the measurements 
(this is in order to prevent a certain attack on a node where an attacker can initiate many inbound connections and effect our local nodes avg time).
If the local time and peers avg time do not match the node will print out a warning message and default to the peers time.

#### Block Signatures (why sign a block with the private key owning the UTXO)

The coinstake that found a valid kernel and thus was selected to create a block is used to proof ownership of the UTXO by providing the signature that spends the outputs.  
However such an output has no cryptographic strong link to the block itself, meaning an attacker can take the valid coinstake utxo and put it in another block which the attacker created and propagate that to the network, the attacker could then censor transactions at will.

By signing the block with the same key that owns the UTXO peers can validate the block was created by the owner of the coinstake.
The block signature is attached at the end of the serialized block and is not part of the header hash.

#### How is the next difficulty target calculated 

The calculation of the next target is based on the last target value and the block time (aka spacing) (i.e. difference in time stamp of this block and its immediate predecessor). 
The target changes every block and it is adjusted down (i.e. towards harder to reach, or more difficult) if the time to mine last block was lower than the target block time.
And it is adjusted up if it took longer than the target block time. The adjustments are done in a way the target is moving towards the target-spacing (expected block time) exponentially, so even a big change in the mining power on the network will be fixed by retargeting relatively quickly.

#### Changes made in POSv4

Two changes were made in POSv4.

- The removal of the time stamp from the transaction serialization:
this makes POS transactions serialize the same as Bitcoin transactions, 
the benefit of that is easier to use various blockchain tools that are made for Bitcoin.
That time stamp was used in the kernel hash however the kernel hash was anyway defaulting to the header timestamp 
so there was no need to serialize the time stamp also in each transaction.

- The removal of the coinstake time from the kernel hash calculations:
when checking the kernel validity a few parameters are hashed together to find a valid kernel,
previously the timestamp of the utxo that is being spent was also included in that hash 
however it provides no additional value because the previous outpoint is used as well and that is already unique

#### Coldstaking (multisig staking) 

Coldstaking is a mechanism that eliminates the need to keep the coins in a hot wallet.
When setting up coldstaking a user generates two wallets (two different private keys) one key can only be used for staking (creating other coinstakes) and the other key is used for spending the coins. 

Cold staking still requires to have a fully synced node running and connected to the network.

The full Coldstaking specification can be found here
https://github.com/block-core/blockcore/blob/master/Documentation/Features/ColdStaking.md

## Weaknesses

#### NAS (nothing at stake)

Nothing-at-stake is a theoretical security issue in proof-of-stake consensus systems in which validators have a financial incentive to mine on every fork of the blockchain that takes place, which is disruptive to consensus and potentially makes the system more vulnerable to attacks

Assuming the majority of staking power (coins at stake) are honest an attacker which exercises NAS can make it very hard for honest nodes to know what is the chain with the total honest staking power (even if the attacker stakes on forks with less total stake this can confuse nodes in IBD) 

However this attack is not obvious to execute as an attacker would have to be economically invested in the chain in order to execute the attack and will be risking the value of their own coins.

https://golden.com/wiki/Nothing-at-stake_problem
https://medium.com/coinmonks/understanding-proof-of-stake-the-nothing-at-stake-theory-1f0d71bc027

#### Stake Grinding
 
Stake grinding is a class of attack where a validator performs some computation or takes some other step to try to bias the randomness in their own favour.  

In a stake grinding attack, the attacker has a small amount of stake and goes through the history of the blockchain and finds places where their stake wins a block. In order to consecutively win, they modify the next block header until some stake they own wins once again.
 
https://dyor-crypto.fandom.com/wiki/Grinding_Attack
  
#### The IBD problem

Proof of stake networks are more vulnerable during Initial Block Download (IBD), during initial sync a local node will try to find peers to sync the consensus history, however if a fake chain is presented (a fake chain is any chain that is not the chain accepted by the majority of stakers) a local node cannot rewind away from the fake chain if it's fork is beyond the maxreorg parameter and will result in our local node being stuck on the "wrong" chain.  

To address that the local node uses checkpoints (regularly hard coding in to the software the correct chain), and to mitigate that attack during IBD a node will only accept outgoing connections.  

#### Known issues of POS

POS is considered less decentralized than POW because: 
- Complexity: the POS protocol is more complex, more unknown attacks may be found 
- The IBD problem: means in some cases users needs to use some external trust in order to find the best chain.
- In case of a 51% attack: user intervention is needed like checkpoints in order to recover.
- IBD: the reliance on checkpoints for IBD.
- Time sync: the requirement that a majority of nodes have the correct global time.

### References

#### Older whitepapers  
POS whitepaper - [pos.pdf](/Documentation/pos-whitepapers/pos.pdf)  
POSv2 whitepaper - [posv2.pdf](/Documentation/pos-whitepapers/posv2.pdf)  
POSv3 whitepaper - [posv3.pdf](/Documentation/pos-whitepapers/posv3.pdf)  

#### Additional references  
https://en.bitcoin.it/wiki/Proof_of_Stake  
Bitcointalk discussion on the issues of POS https://bitcointalk.org/index.php?topic=1382241.0  
https://github.com/libbitcoin/libbitcoin-system/wiki/Proof-of-Stake-Fallacy  
http://earlz.net/view/2017/07/27/1904/the-missing-explanation-of-proof-of-stake-version  
https://www.reddit.com/r/Bitcoin/comments/1oi7su/criticisms_of_proofofstake/  
https://blog.ethereum.org/2014/07/05/stake/  
https://eprint.iacr.org/2018/248.pdf  
http://tselab.stanford.edu/downloads/PoS_LC_SBC2020.pdf  

