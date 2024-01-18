# Tasks

7. Transfer commands over network
    - Sync turn ends
7. Refactor NetworkPacket deserialization to auto-detect type & choose appropriate class
7. Fix rare bug where Ready packet arrives before Hello packet -> KeyNotFoundException
7. Fix bug where start game trigger gets swallowed if players load too fast
   - one player does not receive the ready message?
   - can we fix this with periodic hellos?
8. Split EntityID and PlayerID
   - i.e. allow multiple Entities per Player
   - assign entity IDs in a deterministic fashion (e.g. each player could use multiples of their player ID)
   - allow player to spawn entities (debug function)
   - allow player to kill entities (debug function)
9. Implement box select command
10. Implement checksum calculation for Simulation State
11. proper logging: `dotnet add package Microsoft.Extensions.Logging`
    - https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
12. Implement CRC checking for the game state
13. send our own state as heartbeat every few seconds
14. react to disconnects & timeouts
15. test more than 2 players
16. stress test with many entities
