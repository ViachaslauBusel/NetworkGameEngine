# Copilot Instructions

## Project Guidelines
- For TaskScheduler, user requires internal execution thread instead of external manual calls.
- User expects a complete redo when prior solution quality is unsatisfactory ('давай по новой').
- User prefers concise, direct answers focused strictly on the requested code change.
- Prefer improving API/method naming to clear, descriptive names when refactoring. This includes clearer, more descriptive method and member names during refactoring.
- When optimizing selected code, provide changes only for the selected block and avoid unrelated refactors.
- `GameObjectCallRegistry.GetTargetsFor` is currently called from only one thread, and a new call does not start until iteration of the previous result is finished.
- `GameObjectCallRegistry.Register` and `GameObjectCallRegistry.Unregister` may be called from different threads, so synchronization must be preserved for those methods. User reports hangs and requires synchronization solutions that prioritize correctness (no deadlocks) over speculative micro-optimizations.
- User expects strict correctness for `ManualResetEventSlim` semantics: workers must wake only on `Set()`, execute once per signal, then block again on next `Wait()` until next `Set()`.
- User expects a direct yes/no clarification when asked about tool visibility limitations (e.g., whether uncommitted changes are visible).