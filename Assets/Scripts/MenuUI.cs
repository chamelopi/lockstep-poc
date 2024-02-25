#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

// TODO: Move network manager singleton to a different MonoBehaviour & game object
public class MenuUi : MonoBehaviour
{
    public static INetworkManager? networkManager;

    public GameObject hostInput;
    public GameObject portInput;
    public GameObject statusOutput;
    public GameObject connectUI;
    public GameObject uiContainer;
    public GameObject startGameButton;
    public GameObject replayDropdown;
    public GameObject saveReplayButton;
    public GameObject waitingMessage;
    public GameObject simulationManagerPrefab;
    public GameObject groundPlane;

    public string ReplayPath = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\ColoniaPrimaReplays";
    private TextMeshProUGUI statusOutText;


    private float mockLoadingTimeSeconds;
    private bool inGame = false;

    public void Start()
    {
        statusOutText = statusOutput.GetComponent<TextMeshProUGUI>();
        var dropdown = replayDropdown.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(Directory.EnumerateFiles(ReplayPath, "*.json").Select(path => Path.GetFileName(path)).ToList());
    }

    public void Update()
    {
        statusOutText.text = "";
        if (networkManager != null)
        {
            networkManager.PollEvents();

            if (networkManager.IsConnected())
            {
                statusOutText.text = networkManager is SingleplayerNetworkManager ? "Singleplayer" : (networkManager.IsServer() ? "Server" : "Client");
                statusOutText.text += "\nConnected players: " + networkManager.GetClients().Count();
                statusOutText.text += "\nPlayerID: " + networkManager.GetLocalClient().PlayerId;
                statusOutText.text += "\nState: " + networkManager.GetLocalClient().State;
            }
            else
            {
                statusOutText.text = "Not connected to network!";
            }

            if (!inGame)
            {
                var areAllPlayersReady = networkManager.GetClients().Where(cl => cl.State == ClientState.Ready).Count() == networkManager.GetClients().Count();

                // If all players are ready, enable the start button
                startGameButton.SetActive(networkManager.IsServer() && areAllPlayersReady);

                // Show waiting message if not everyone is ready
                waitingMessage.SetActive(!areAllPlayersReady);

                MockLoadMap();
            }
        }

        if (SimulationManager.sim != null)
        {
            statusOutText.text += "\nSimulation turn: " + SimulationManager.sim.currentTurn;
            statusOutText.text += "\nSimulation paused? " + SimulationManager.sim.isPaused;
            statusOutText.text += "\nSimulation entity count: " + SimulationManager.sim.currentState.Entities.Count;
            statusOutText.text += "\nSimulation turn speed (ms): " + SimulationManager.sim.turnSpeedMs;
            statusOutText.text += "\nNumber of selected entities: " + SimulationManager.sim.currentState.SelectedEntities.Select(e => e.Value.Count).Sum();
        }
    }

    private void MockLoadMap()
    {
        if (mockLoadingTimeSeconds > 0)
        {
            mockLoadingTimeSeconds -= Time.deltaTime;
            if (mockLoadingTimeSeconds <= 0)
            {
                networkManager?.UpdateLocalState(ClientState.Ready);
                Debug.Log("Finished mock loading the map - sending ready message!");
            }
        }
    }

    public void StartGame()
    {
        var startPacket = new StartGamePacket()
        {
            PkgType = PacketType.StartGame,
        };
        networkManager!.QueuePacket(startPacket);
        HideMenuUI();

        // Start immediately for server!
        networkManager!.UpdateLocalState(ClientState.InGame);
        inGame = true;
        SimulationManager.sim!.isPaused = false;
    }

    private void HideMenuUI()
    {
        Debug.Log("Triggering game start!");
        // Hide start game button
        startGameButton.SetActive(false);
        // Hide menu background
        uiContainer.SetActive(false);
        // Activate RTS camera controls
        GameObject.Find("InGameCamera").GetComponent<CameraController>().enabled = true;
        // Show replay save button
        saveReplayButton.SetActive(true);
    }


    public void HostGame()
    {
        string portStr = portInput.GetComponent<TMP_InputField>().text;

        if (ushort.TryParse(portStr.Trim(), out ushort port))
        {
            networkManager = MultiplayerNetworkManager.NewServer(port);
            Debug.Log("Server created successfully!");

            LoadGameScene();
        }
        else
        {
            Debug.LogError("Could not parse port!");
            statusOutText.text = "Invalid Port!";
        }
    }

    public void JoinGame()
    {
        string hostname = hostInput.GetComponent<TMP_InputField>().text.Trim();

        if (ushort.TryParse(portInput.GetComponent<TMP_InputField>().text.Trim(), out ushort port))
        {
            networkManager = MultiplayerNetworkManager.NewClient(hostname, port);
            Debug.Log("Game joined successfully!");

            LoadGameScene();
        }
        else
        {
            Debug.LogError("Could not parse port!");
            statusOutText.text = "Invalid Port!";
        }
    }

    public void SaveReplay()
    {
        SimulationManager.sim!.SaveReplay(Path.Combine(ReplayPath, "replay-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json"));
    }


    public void LoadReplay()
    {
        StartCoroutine(LoadReplayCoro());
    }

    public IEnumerator LoadReplayCoro()
    {
        var dropdown = replayDropdown.GetComponent<TMP_Dropdown>();
        var replayName = dropdown.options[dropdown.value].text;
        Debug.Log("Loading replay " + replayName);

        networkManager = new SingleplayerNetworkManager();
        LoadGameScene();
        // Immediately go in game
        networkManager.UpdateLocalState(ClientState.InGame);
        inGame = true;

        // Wait until simulation manager is intantiated
        yield return new WaitForEndOfFrame();
        // Tell simulation about replay
        SimulationManager.sim!.LoadReplay(Path.Combine(ReplayPath, replayName));
        HideMenuUI();
        SimulationManager.sim!.isPaused = false;
    }

    internal void LoadGameScene()
    {
        connectUI.SetActive(false);
        groundPlane.SetActive(true);
        Instantiate(simulationManagerPrefab);

        mockLoadingTimeSeconds = 5;

        if (networkManager != null && !networkManager.IsServer())
        {
            // TODO: Should we handle this in network manager instead of in the scene?
            networkManager?.AddCallback(PacketType.StartGame, _ =>
            {
                Debug.Log("Start game callback!");
                networkManager.UpdateLocalState(ClientState.InGame);
                inGame = true;
                SimulationManager.sim!.isPaused = false;
                HideMenuUI();
                networkManager.RemoveCallback(PacketType.StartGame);
            });
        }
    }
}
