using Raylib_cs;

namespace Server;

public class WaitingScene : Scene
{
    private Simulation.Simulation sim;
    private INetworkManager networkManager;
    private Camera3D camera;    

    public WaitingScene(Simulation.Simulation sim, INetworkManager networkManager, Camera3D camera)
    {
        this.sim = sim;
        this.networkManager = networkManager;
        this.camera = camera;
    }

    public ClientState GetState()
    {
        return ClientState.Waiting;
    }

    public Scene? Run()
    {
        while(networkManager.GetPlayerIds().Count() != sim.playerCount) {
            if (Raylib.WindowShouldClose()) {
                return null;
            }

            networkManager.PollEvents();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.LIGHTGRAY);
            Raylib.DrawText($"Waiting for players, {networkManager.GetPlayerIds().Count()}/{sim.playerCount}", 20, 20, 28, Color.BLACK);

            // TODO: Duplicate from GameScene
            Raylib.DrawText($"Local player: {networkManager.GetLocalPlayer()}", 800, 100, 24, Color.BLACK);
            var clients = networkManager.GetClients();
            foreach(var client in clients) {
                var height = 100 + client.PlayerId * 30;
                Raylib.DrawText($"Client {client.PlayerName} ({client.PlayerId}): State: {client.State} Turn done? {client.CurrentTurnDone}", 10, height, 24, Color.BLACK);
            }
            Raylib.EndDrawing();
        }

        // Later, we will only allow this once every player is ready & the host manually started the game
        return new GameScene(sim, networkManager, camera);
    }
}