# Lockstep PoC

Proof of Concept for a lockstep simulation using ENet and C#.

## Feature list

Currently done are
- Command recording & execution for (de-)selecting units and basic move commands
- Very basic fixed-point math (emulated using `long`) to prevent floating point indeterminism
- Loading, saving and playing back replays
- Recorded commands are synced over the network
- Synchronization of simulation turns to keep the game in sync
- Single-Player mode without network (only one player is controllable in this mode)
- Local determinism check between two simulation frames (i.e. the simulation step is calculated twice, and checked against itself)
- Pausing the simulation

## Networking notes

#### How to handle entities spawing and despawning? Find a deterministic algorithm for assigning IDs maybe (we can first test this with a purely local simulation)
- Spawning could be a command in some cases, in other cases it will be automatically during simulation. We have to support both. The order spawning objects of this will have to be deterministic, too.
- De-spawning will occur on death, destruction, resource harvesting, etc. This might be tricky to get right
- We need to interpolate between frames which might have different entity counts!

#### Every turn
- Do like the AoE paper says :D
- Collect inputs and broadcast them to the other player's sims (INPUT/COMMAND)
- Sync turn increments (NEXT TURN)
- Pause game if a player drops/misses a NEXT TURN, but close connection the game after a timeout (saving all commands to a replay to allow loading)

### Data stored per peer

- PlayerID (server-assigned)
- Current state
- Last message timestamp (for dc detection)
- Player name (?)

### Data that might be of interest for debugging/statistics and should be collected

- `NetworkManager` Round trip time per peer
- `NetworkManager` Traffic (i.e. total size of packages sent/received)
- `Simulation` Average time to calculate a simulation step
- `Simulation` Checksums of simulation state

## Unity - How to use a managed plugin

see https://docs.unity3d.com/Manual/UsingDLL.html

1. build this project as a DLL
2. TODO: target .net framework needs to match (4.x?)
   - https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/unity-scripting-upgrade
   - https://forum.unity.com/threads/which-version-of-c-and-net-necessary-for-unity-now-and-in-the-future.1322457/
   - https://forum.unity.com/threads/unable-to-load-attribute-info-unloading-broken-assembly-help-appreciated.1445707/
3. Do "Import Asset" in unity editor, drag&drop does not work for me for some reason
4. ???
5. Does not work :(
