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

    public Scene? Run()
    {
        while(networkManager.GetConnectedClients().Count() != sim.playerCount) {
            if (Raylib.WindowShouldClose()) {
                return null;
            }

            networkManager.PollEvents();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.LIGHTGRAY);
            Raylib.DrawText($"Waiting for players, {networkManager.GetConnectedClients().Count()}/{sim.playerCount}", 20, 20, 28, Color.BLACK);
            Raylib.EndDrawing();
        }

        // Later, we will only allow this once every player is ready & the host manually started the game
        return new GameScene(sim, networkManager, camera);
    }
}