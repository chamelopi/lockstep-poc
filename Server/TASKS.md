# Tasks

6. Implement dummy logic for map loading (stubs only)
   - when connected, the server should tell the player the current map
   - server should be able to set the map (i.e. change it for everyone) when in Waiting/Ready state
   - clients become ready when they completed loading the map
7. Transfer commands over network
8. Split EntityID and PlayerID
   - i.e. allow multiple Entities per Player
   - assign entity IDs in a deterministic fashion (e.g. each player could use multiples of their player ID)
   - allow player to spawn entities (debug function)
   - allow player to kill entities (debug function)
9. Implement box select command
10. Implement checksum calculation for Simulation State
11. proper logging: `dotnet add package Microsoft.Extensions.Logging`
    - https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line