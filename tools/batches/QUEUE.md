# Bottle build queue

`bottles.yml` runs share one concurrency group, and GitHub keeps only one *pending* run in it:
dispatching another cancels the one waiting. So batches wait in `queue.txt`, and every bottles run
starts the first queued batch when it finishes (unless another run is already waiting). The daily
upstream sync appends its batch there while a bottles run is active. To queue more, add lines.

| Order | Batch | Inputs | Notes |
|---|---|---|---|
| 1 | wave4-rest | toolchain | C/C++ formulae with tap-only dependencies |
| 2 | wave1-retry | from_run=38896600919 | publishes the bottles that run built (two chunks hit the 6-hour limit) |
| 3 | wave9-foundation | toolchain, chunk_size 8 | the dependencies that block the most formulae (tools/next_wave.py blockers report); ca-certificates and openssl@4 first |
| 4 | wave7-c-l1 | toolchain, chunk_size 15 | C/C++ level 1 |
| 5 | wave3-node | chunk_size 20 | npm formulae |
| 6 | wave6-java | chunk_size 20 | |
| 7 | wave2-retry | toolchain, chunk_size 15 | Python formulae that failed once |
| 8 | wave5-portable | toolchain, chunk_size 10 | upstream macOS/Linux-only formulae that build on Windows |
| 9 | wave8-deprecated | toolchain, chunk_size 20 | deprecated formulae still install (with Homebrew's warning) |
| 10 | wave9-heavy | toolchain, chunk_size 1, formula_timeout 300 | LLVM, Qt, GHC, Swift, PyTorch ... |

After each published batch, rerun
`python tools/next_wave.py <formula.json> <out dir> <batches still queued...>` for the next
ready set (ready-c/go/rust/python/node/java.txt) and the blockers report.
