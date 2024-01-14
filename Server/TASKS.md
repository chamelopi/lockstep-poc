# Tasks

2. Implement those commands in the client
   - select
   - deselect
   - box select
   - move command
3. Implement Command serialization
   - Serialize to json, deserialize from json
   - Refactor current replay mechanic to use json
   - adjust testreplay file to use json
4. Implement dummy logic for map loading (stubs only)
   - when connected, the server should tell the player the current map
   - server should be able to set the map (i.e. change it for everyone) when in Waiting/Ready state
   - clients become ready when they completed loading the map
4. Transfer commands over network