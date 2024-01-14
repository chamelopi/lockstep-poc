# Tasks

4. Refactor entity class to store target position of move
5. Implement Command serialization
   - Serialize to json, deserialize from json
   - Refactor current replay mechanic to use json
   - adjust testreplay file to use json
   - write unit tests
6. Implement dummy logic for map loading (stubs only)
   - when connected, the server should tell the player the current map
   - server should be able to set the map (i.e. change it for everyone) when in Waiting/Ready state
   - clients become ready when they completed loading the map
7. Transfer commands over network
9. Implement box select command