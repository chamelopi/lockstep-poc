# Tasks

6. Implement dummy logic for map loading (stubs only)
   - Do an arbitrary, short wait & then change own state to ready (later this will be replaced with map gen/loading)
   - Have server send out `StartGame` packet once everyone is ready
   - Only transition state if the `StartGame` packet is received
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