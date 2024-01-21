# Tasks

## Short-Term bugfixes/improvements
1. Refactor NetworkPacket deserialization to auto-detect type & choose appropriate class

## Simulation features
2. Split EntityID and PlayerID
   - allow player to spawn entities (debug function)
   - allow player to kill entities (debug function)
3. Implement box select command
4. Implement checksum calculation for Simulation State (e.g. CRC)
5. Implement map (just a list of entities, loaded on startup)
6. Store initial state of simulation in replay
   - Fix CheckFullDeterminism by replicating this initial state as basis for the re-simulation

## Networking features
7. send our own state as heartbeat every few seconds?
8. react to disconnects & timeouts

## Production-ready features
9. test more than 2 players
10. stress test with thousands of entities
11. collect statistics (see readme)
12. proper logging: `dotnet add package Microsoft.Extensions.Logging`
    - https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
13. Use or implement a fixed point math lib

## Unity
14. Port to Unity/Integrate into Unity
    - Decide if this should be an engine-agnostic library, or tightly integrated in our HGLG codebase
        - If Engine agnostic, how do we use a .NET DLL in Unity?
    - Refactor scenes so that they can be called/ported to MonoBehaviour
    - Use timing based on Unity's delta
    - Refactor ENetNetworkManager to allow us to use another networking library/RPCs instead
    - Check if we should use DOTS.Net or keep ENet
