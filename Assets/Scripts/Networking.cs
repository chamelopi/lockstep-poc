#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Networking : MonoBehaviour
{
    public static INetworkManager? networkManager;

    public GameObject hostInput;
    public GameObject portInput;
    public GameObject statusOutput;
    public GameObject connectUI;
    public GameObject uiContainer;
    public GameObject startGameButton;
    public GameObject simulationManagerPrefab;

    private TextMeshProUGUI statusOutText;


    private float mockLoadingTimeSeconds;

    public void Start()
    {
        statusOutText = statusOutput.GetComponent<TextMeshProUGUI>();
    }

    public void Update()
    {
        if (networkManager != null)
        {
            networkManager.PollEvents();

            // If all players are ready
            if (networkManager.IsServer() && networkManager.GetClients().Where(cl => cl.State == ClientState.Ready).Count() == networkManager.GetClients().Count())
            {
                Debug.Log("All players are ready - can start the game!");
                startGameButton.SetActive(true);
            }

            statusOutText.text = networkManager is SingleplayerNetworkManager ? "Singleplayer" : (networkManager.IsServer() ? "Server" : "Client");

            if (networkManager.IsConnected())
            {
                statusOutText.text += "\nConnected players: " + networkManager.GetClients().Count();
                statusOutText.text += "\nPlayerID: " + networkManager.GetLocalClient().PlayerId;
                statusOutText.text += "\nState: " + networkManager.GetLocalClient().State;
            }

            MockLoadMap();
        }

        if (SimulationManager.sim != null)
        {
            statusOutText.text += "\nSimulation turn: " + SimulationManager.sim.currentTurn;
            statusOutText.text += "\nSimulation paused? " + SimulationManager.sim.isPaused;
            statusOutText.text += "\nSimulation entity count: " + SimulationManager.sim.currentState.Entities.Count;
            statusOutText.text += "\nSimulation turn speed (ms): " + SimulationManager.sim.turnSpeedMs;
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
        networkManager?.QueuePacket(startPacket);
        HideMenuUI();
    }

    private void HideMenuUI()
    {
        Debug.Log("Triggering game start!");
        // Hide start game button
        startGameButton.SetActive(false);
        // Hide menu background
        uiContainer.SetActive(false);
    }


    // TODO: This should probably be a separate menu script
    public void HostGame()
    {
        ushort port;
        string portStr = portInput.GetComponent<TMP_InputField>().text;

        if (ushort.TryParse(portStr.Trim(), out port))
        {
            networkManager = MultiplayerNetworkManager.NewServer(port);
            Debug.Log("Server created successfully!");

            GoToGame();
        }
        else
        {
            Debug.LogError("Could not parse port!");
            statusOutText.text = "Invalid Port!";
        }
    }

    public void JoinGame()
    {
        ushort port;
        string hostname = hostInput.GetComponent<TMP_InputField>().text.Trim();

        if (ushort.TryParse(portInput.GetComponent<TMP_InputField>().text.Trim(), out port))
        {
            networkManager = MultiplayerNetworkManager.NewClient(hostname, port);
            Debug.Log("Game joined successfully!");

            GoToGame();
        }
        else
        {
            Debug.LogError("Could not parse port!");
            statusOutText.text = "Invalid Port!";
        }
    }


    internal void GoToGame()
    {
        connectUI.SetActive(false);
        Instantiate(simulationManagerPrefab);
        SceneManager.LoadScene("GameScene", LoadSceneMode.Additive);

        mockLoadingTimeSeconds = 5;

        if (networkManager != null && !networkManager.IsServer())
        {
            // TODO: Should we handle this in network manager instead of in the scene?
            networkManager?.AddCallback(PacketType.StartGame, _ =>
            {
                // FIXME: We don't seem to have a player ID right now?
                networkManager.UpdateLocalState(ClientState.InGame);
                HideMenuUI();
                networkManager.RemoveCallback(PacketType.StartGame);
            });
        }
    }
}
