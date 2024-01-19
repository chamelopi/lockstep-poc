# Tasks

## Short-Term bugfixes
1. Refactor NetworkPacket deserialization to auto-detect type & choose appropriate class

## More features
4. Split EntityID and PlayerID
   - allow player to spawn entities (debug function)
   - allow player to kill entities (debug function)
5. Implement box select command
6. Implement checksum calculation for Simulation State (e.g. CRC)

## Even more features
7. send our own state as heartbeat every few seconds
    - this might also fix the above bugs
8. react to disconnects & timeouts

## Production-ready features
9. test more than 2 players
10. stress test with many entities
11. collect statistics (see readme)
12. proper logging: `dotnet add package Microsoft.Extensions.Logging`
    - https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
13. Use or implement a fixed point math lib

## Unity
14. (FINALLY) port to Unity
    - Decide if this should be an engine-agnostic library, or tightly integrated in our HGLG codebase
    - Refactor scenes so that they can be called/ported to MonoBehaviour
    - Use timing based on Unity's delta
    - Check if we should use DOTS.Net or keep ENet