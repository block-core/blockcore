##  (2020-05-04)

* Add contact information in the API documentation ([240d10e](https://github.com/block-core/blockcore/commit/240d10e))
* Update City Node with generated code ([162d01a](https://github.com/block-core/blockcore/commit/162d01a))
* Update version preparing for next future release ([95f4496](https://github.com/block-core/blockcore/commit/95f4496))



## <small>1.0.6 (2020-04-24)</small>

* Add a minor unit test to verify the new format for specifying magic values for network connectivity. ([144f47e](https://github.com/block-core/blockcore/commit/144f47e))
* Add city chain (#98) ([9e6b946](https://github.com/block-core/blockcore/commit/9e6b946)), closes [#98](https://github.com/block-core/blockcore/issues/98)
* Add signal when transaction found in wallet, and filtering on wallet (#102) ([8e2fe77](https://github.com/block-core/blockcore/commit/8e2fe77)), closes [#102](https://github.com/block-core/blockcore/issues/102)
* Get rid of prefix logger and its dependency on nlog ([244a0a8](https://github.com/block-core/blockcore/commit/244a0a8))
* If finality is ahead of consensus don't fail the indexer. (#103) ([a5d688f](https://github.com/block-core/blockcore/commit/a5d688f)), closes [#103](https://github.com/block-core/blockcore/issues/103)
* Make sure City Network is packaged for release ([1520378](https://github.com/block-core/blockcore/commit/1520378))
* Update release number ([4031f72](https://github.com/block-core/blockcore/commit/4031f72))



## <small>1.0.5 (2020-04-22)</small>

* ChainRepository optimization (#96) ([26fbe62](https://github.com/block-core/blockcore/commit/26fbe62)), closes [#96](https://github.com/block-core/blockcore/issues/96)
* Change the NuGet publish to use Ubuntu ([2f1c781](https://github.com/block-core/blockcore/commit/2f1c781))
* Enable Web Hook trigger for package release ([b41233f](https://github.com/block-core/blockcore/commit/b41233f))
* Fix link and formatting on README ([4ebfc7d](https://github.com/block-core/blockcore/commit/4ebfc7d))
* Fix the event type for NuGet publish ([1e84cc4](https://github.com/block-core/blockcore/commit/1e84cc4))
* Increment version ([a61f318](https://github.com/block-core/blockcore/commit/a61f318))
* Introduce recent header cache (to speed up header sync) ([081619f](https://github.com/block-core/blockcore/commit/081619f))
* Make sure that pack ends up in a folder ([2665fc1](https://github.com/block-core/blockcore/commit/2665fc1))
* Move projects in to folders (#97) ([0a26390](https://github.com/block-core/blockcore/commit/0a26390)), closes [#97](https://github.com/block-core/blockcore/issues/97)
* Rename LeveldbHeaderStore to LeveldbChainStore ([fae6db0](https://github.com/block-core/blockcore/commit/fae6db0))
* Temporarily enable trigger on edit ([f444a8d](https://github.com/block-core/blockcore/commit/f444a8d))
* Upgrade the icon from 64 to 256, better prepare for high DPI monitors ([f2c048b](https://github.com/block-core/blockcore/commit/f2c048b))



## <small>1.0.4 (2020-04-19)</small>

* Add a note to direct users to blockcore-node (#93) ([8bfc9e7](https://github.com/block-core/blockcore/commit/8bfc9e7)), closes [#93](https://github.com/block-core/blockcore/issues/93)
* Add instructions on how to update when forking our repository ([dbbc2e1](https://github.com/block-core/blockcore/commit/dbbc2e1))
* Create and update release draft on master branch builds (#95) ([e64b260](https://github.com/block-core/blockcore/commit/e64b260)), closes [#95](https://github.com/block-core/blockcore/issues/95)
* Move properties to the root .props file (#94) ([da12fd3](https://github.com/block-core/blockcore/commit/da12fd3)), closes [#94](https://github.com/block-core/blockcore/issues/94)



## <small>1.0.3 (2020-04-18)</small>

* Add BlockcoreLogo as template ([787f95d](https://github.com/block-core/blockcore/commit/787f95d))
* Change daemon logo to blockcore ([0153bfa](https://github.com/block-core/blockcore/commit/0153bfa))
* Fix build break ([1a35b95](https://github.com/block-core/blockcore/commit/1a35b95))
* Fix getStakingNotExpired endpoint ([d7358a7](https://github.com/block-core/blockcore/commit/d7358a7))
* Fix wanings ([c18e2ce](https://github.com/block-core/blockcore/commit/c18e2ce))
* Increment version ([fead614](https://github.com/block-core/blockcore/commit/fead614))
* Remove all warnings from solution ([337b44d](https://github.com/block-core/blockcore/commit/337b44d))
* Rename DBreezeSerializer to DataStoreSerializer ([eaca740](https://github.com/block-core/blockcore/commit/eaca740))



## <small>1.0.2 (2020-04-15)</small>

* Add block header store (#88) ([c0c8422](https://github.com/block-core/blockcore/commit/c0c8422)), closes [#88](https://github.com/block-core/blockcore/issues/88)
* Add XDS network (#83) ([2273fd4](https://github.com/block-core/blockcore/commit/2273fd4)), closes [#83](https://github.com/block-core/blockcore/issues/83)
* increment version ([d7f1d31](https://github.com/block-core/blockcore/commit/d7f1d31))
* Move to leveldb block store, provenheader and chainrepo (#90) ([138ad7c](https://github.com/block-core/blockcore/commit/138ad7c)), closes [#90](https://github.com/block-core/blockcore/issues/90)
* Provenheader not as inheritance class (#91) ([cb3d5cc](https://github.com/block-core/blockcore/commit/cb3d5cc)), closes [#91](https://github.com/block-core/blockcore/issues/91)
* Using the improved Uint256 form mithrilshards (#86) ([72aa513](https://github.com/block-core/blockcore/commit/72aa513)), closes [#86](https://github.com/block-core/blockcore/issues/86)



## <small>1.0.1 (2020-03-14)</small>

* Add . to version suffix to ensure correct version ordering ([72a7464](https://github.com/block-core/blockcore/commit/72a7464))
* Add Directory.Build.props for missing packages ([10c1972](https://github.com/block-core/blockcore/commit/10c1972))
* Bump patch version for nuget packages (#84) ([0121f47](https://github.com/block-core/blockcore/commit/0121f47)), closes [#84](https://github.com/block-core/blockcore/issues/84)
* Increment asm version (#85) ([ae37d6d](https://github.com/block-core/blockcore/commit/ae37d6d)), closes [#85](https://github.com/block-core/blockcore/issues/85)
* Separate networks in to their own projects (#82) ([e37b6d4](https://github.com/block-core/blockcore/commit/e37b6d4)), closes [#82](https://github.com/block-core/blockcore/issues/82)