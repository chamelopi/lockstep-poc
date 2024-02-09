// using System.Linq;
// using System.Numerics;
// using Raylib_cs;
// using Simulation;
// using UnityEngine;

// namespace Server
// {
//     public class WaitingScene : Scene
//     {
//         private Simulation.Simulation sim;
//         private INetworkManager networkManager;
//         private Camera3D camera;

//         // For dummy loading
//         private long loadStart;
//         private long loadDuration;
//         private bool loaded = false;
//         private bool canStart = false;
//         private bool startSent = false;

//         public WaitingScene(Simulation.Simulation sim, INetworkManager networkManager, Camera3D camera)
//         {
//             this.sim = sim;
//             this.networkManager = networkManager;
//             this.camera = camera;
//             this.loadStart = Clock.GetTicks();
//             // Simulate loading duration between 5 and 8 seconds
//             this.loadDuration = Random.Shared.NextInt64(5000, 8000);

//             if (!networkManager.IsServer())
//             {
//                 networkManager.AddCallback(PacketType.StartGame, (_) =>
//                 {
//                     Debug.Log("Start Game message received!");
//                     canStart = true;
//                     networkManager.RemoveCallback(PacketType.StartGame);
//                 });
//             }
//         }

//         public ClientState GetState()
//         {
//             return ClientState.Waiting;
//         }

//         public Scene? Run()
//         {
//             while (!canStart)
//             {
//                 if (Raylib.WindowShouldClose())
//                 {
//                     return null;
//                 }

//                 networkManager.PollEvents();

//                 Raylib.BeginDrawing();
//                 Raylib.ClearBackground(Color.LIGHTGRAY);
//                 Raylib.DrawText($"Waiting for players, {networkManager.GetPlayerIds().Count()}/{sim.playerCount}", 20, 20, 28, Color.BLACK);

//                 CheckLoading();
//                 if (networkManager.IsServer() && !startSent)
//                 {
//                     if (CheckAllClientsReady())
//                     {
//                         startSent = true;
//                     }
//                 }

//                 // TODO: Duplicate from GameScene
//                 Raylib.DrawText($"Local player: {networkManager.GetLocalPlayer()}", 800, 100, 24, Color.BLACK);
//                 var clients = networkManager.GetClients();
//                 foreach (var client in clients)
//                 {
//                     var height = 100 + client.PlayerId * 30;
//                     Raylib.DrawText($"Client {client.PlayerName} ({client.PlayerId}): State: {client.State} Turn done? {client.CurrentTurnDone}", 10, height, 24, Color.BLACK);
//                 }
//                 Raylib.EndDrawing();
//             }

//             // Later, we will only allow this once every player is ready & the host manually started the game
//             return new GameScene(sim, networkManager, camera);
//         }

//         private void CheckLoading()
//         {
//             if (Clock.GetTicks() < (loadStart + loadDuration))
//             {
//                 Raylib.DrawText("Loading map.......", 540, 500, 24, Color.RED);
//                 Raylib.DrawRectanglePro(new Rectangle { X = 750, Y = 510, Width = 30, Height = 30 }, new Vector2(15, 15), (Clock.GetTicks() / 3) % 360, Color.RED);
//             }
//             else
//             {
//                 if (!loaded)
//                 {
//                     loaded = true;
//                     Debug.Log("finished loading, notifying other players!");
//                     networkManager.UpdateLocalState(ClientState.Ready);
//                 }
//                 Raylib.DrawText("Loading finished!", 600, 500, 24, Color.GREEN);
//             }
//         }

//         private bool CheckAllClientsReady()
//         {
//             // Wait for the desired amount of players
//             if (networkManager.GetPlayerIds().Count() < sim.playerCount)
//             {
//                 return false;
//             }

//             foreach (var client in networkManager.GetClients())
//             {
//                 if (client.State != ClientState.Ready)
//                 {
//                     return false;
//                 }
//             }

//             // This could also be triggered by hand (= button press) instead
//             var startPacket = new StartGamePacket()
//             {
//                 PkgType = PacketType.StartGame,
//             };
//             networkManager.QueuePacket(startPacket);
//             Debug.Log("Triggering game start!");
//             // Allow ourselves to start, too
//             canStart = true;
//             return true;
//         }
//     }
// }

