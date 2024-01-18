using System.Numerics;
using Raylib_cs;
using System.CommandLine;

namespace Server;

class Server
{
    static readonly ushort Port = 1337;
    static readonly int InitialTurnSpeedMs = 100;
    static readonly int PlayerCount = 2;


    // Entrypoint, using System.CommandLine package for arg parsing
    static async Task<int> Main(string[] args)
    {
        var replayOption = new Option<string?>("--replay", "Replay file to load on startup, if desired");
        var hostOption = new Option<string?>("--host", "Acts as a network client and connects to the specified host");
        hostOption.AddAlias("--connect");
        var serverOption = new Option<bool>("--server", () => false, "Acts as a network server");
        var rootCommand = new RootCommand("Run lockstep simulation");
        rootCommand.AddOption(replayOption);
        rootCommand.AddOption(hostOption);
        rootCommand.AddOption(serverOption);
        rootCommand.SetHandler((replay, host, server) => Run(replay, server, host), replayOption, hostOption, serverOption);

        return await rootCommand.InvokeAsync(args);
    }

    public static void Run(string? replay, bool server, string? host)
    {
        // Init rendering
        Raylib.InitWindow(1280, 1024, "Simulation");
        var camera = new Camera3D(new Vector3(50.0f, 50.0f, 50.0f), Vector3.Zero, Vector3.UnitY, 45, CameraProjection.CAMERA_PERSPECTIVE);

        INetworkManager networkManager;
        var sim = new Simulation.Simulation(InitialTurnSpeedMs, PlayerCount);

        // Argument parsing
        if (replay != null)
        {
            Console.WriteLine("Loading replay from " + replay);
            sim.LoadReplay(replay);
        }
        if (server)
        {
            networkManager = ENetNetworkManager.NewServer(Port);
            Console.WriteLine("Started server!");
        }
        else if (host != null)
        {
            networkManager = ENetNetworkManager.NewClient(host, Port);
            Console.WriteLine("Started client & connected to " + host);
        }
        else
        {
            networkManager = new NoopNetworkManager();
            Console.WriteLine("Started without network!");
        }

        Scene? currentScene = networkManager.GetType() == typeof(NoopNetworkManager)
            ? new GameScene(sim, networkManager, camera)
            : new WaitingScene(sim, networkManager, camera);
        do {
            currentScene = currentScene.Run();
            if (currentScene != null) {
                networkManager.UpdateLocalState(currentScene.GetState());
            }
        } while(currentScene != null);

        Raylib.CloseWindow();
        networkManager.Dispose();
    }
}

