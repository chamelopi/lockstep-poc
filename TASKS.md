# Tasks

## Short-Term bugfixes/improvements
1. Refactor NetworkPacket deserialization to auto-detect type & choose appropriate class
2. On pause, send a packet to everyone to let them know about the pause

## Simulation features
3. Implement box select command
4. Implement checksum calculation for Simulation State (e.g. CRC or just a hash sum)
5. Implement map (just a list of entities, loaded on startup)
6. Store initial state of simulation in replay
   - Fix CheckFullDeterminism by replicating this initial state as basis for the re-simulation
7. Add a "replay end" command at the turn the replays are saved at & terminate the simulation when handled

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
    - Migrate replay storing
    - Migrate simulation execution
    - Migrate existing unit tests to still work
      - Can we do this without relying on *unity* unit tests?