# Blockcore Storage Feature

Custom feature for Blockcore that enables nodes to storage and sync identities and other Web3 / dapp data.

## Identity

The primary document that the storage feature hold, is identities. These are free for anyone to create and is synced across all nodes that has the storage feature enabled.

## Syncing

1. Upon new connection and handshake is verified.
1. Announce what collections we support.
1. The receiving node will respond with full list (chunked response) of signatures for documents in each collection.
1. The same is repeated in the other direction, as both will perform the same announcement.
1. As chuncks of signatures is received, respond with request to download documents that are missing.
1. When the document is downloaded and parsed, it might be discovered that it already exists, but with a different signature. That means that the document has been updated. Updated document will be saved if needed.

Documents have versions, and the peer won't store versions of document it does not support. This is because it must be serialized to perform full validation.

At this moment, both nodes have the same amount of documents with same IDs. They are not guaranteed to have the same (updated) documents.

- User post identity to hub (Node A).
- Node A knows this is incoming through API, so it will distribute the whole identity to all connected nodes (Node B) immediately.
- Node B will announce to its connected not, except sender, the signature it recently saw.
- Node C will ask Node B to send, if it doesn't already have the document.


## Document Signatures

There has been various standards proposed for document signing up through the years and it is likely that one of them should 
be adopted for use in this feature.

JSON Web Signature (JWS): https://tools.ietf.org/html/rfc7515

Linked Data Signatures vs. Javascript Object Signing and Encryption: http://manu.sporny.org/2013/lds-vs-jose/

Linked Data Signatures: https://decentralized-id.com/rwot-dir/rwot3-sf/topics-and-advance-readings/blockchain-extensions-for-linked-data-signatures/

Linked Data Proofs (what Linked Data Signatures has evolved into): https://w3c-ccg.github.io/ld-proofs/

RDF Dataset Normalization: http://json-ld.github.io/normalization/spec/

https://decentralized-id.com/
