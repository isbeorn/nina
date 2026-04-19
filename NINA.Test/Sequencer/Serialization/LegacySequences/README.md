# Legacy Sequence Migration Corpus

This folder contains immutable sequence JSON generated from released `master` branches. The files are not examples for users and should not be hand-edited.

`v3.2/master-3.2-all-sequence-entities.sequence.json` was generated from `master` commit `2393eae581145ed5b8114bf07c48ca2580540fd5`. It includes every built-in sequencer item, condition, trigger, and container export discovered by the 3.2 sequencer assembly. For each exported entity the corpus keeps a default instance and a populated scalar-value instance when the entity is not the root container.

The paired manifest records the source branch, source commit, entity export lists, and the concrete default/populated variants in the sequence file. Treat these JSON files as golden old-version inputs: regenerate them only from the original release branch when intentionally adding a new legacy corpus version.
