# How to use a managed plugin

see https://docs.unity3d.com/Manual/UsingDLL.html

1. build this project as a DLL
2. TODO: target .net framework needs to match (4.x?)
   - https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/unity-scripting-upgrade
   - https://forum.unity.com/threads/which-version-of-c-and-net-necessary-for-unity-now-and-in-the-future.1322457/
   - https://forum.unity.com/threads/unable-to-load-attribute-info-unloading-broken-assembly-help-appreciated.1445707/
3. Do "Import Asset" in unity editor, drag&drop does not work for me for some reason
4. ???
5. Does not work :(

TODO: Write or add a serializer for commands & packets

## Network protocol outline

TODO: Figure out if we need to manually relay messages on the server, or if that happens automatically
(should happen automatically)

Most messages are broadcasted to all clients. Clients take note of their own and other client's states.

On connect:
- If server, send SERVER GREETING to client, assigning a new player ID.
- New player broadcasts a HELLO package. This will tell all other clients about it and its ID.
- Other clients take note of the new peer and respond with their own ID, allowing the player to register them and their state
- All clients start in the game in the state WAITING.
  - They load/generate the game world
  - Once they're done, they broadcast a READY message and transition to state ready
- The first clients who connects becomes the host and may start the game

START GAME message:
- tell all clients that the first simulation turn starts (NEXT TURN)
- all clients need to be ready first (READY)

TODO: How to handle entities spawing and despawning? Find a deterministic algorithm for assigning IDs maybe (we can first test this with a purely local simulation)

Every turn:
- TODO: Do like the AoE paper says :D
- Collect inputs and broadcast them to the other player's sims (INPUT/COMMAND)
- Sync turn increments (NEXT TURN)
- Pause game if a player drops/misses a NEXT TURN, but close connection the game after a timeout (saving all commands to a replay to allow loading)
