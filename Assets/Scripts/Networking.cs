using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Networking : MonoBehaviour
{
    public static INetworkManager? networkManager;

    public GameObject hostInput;
    public GameObject portInput;
    public GameObject statusOutput;
    public GameObject connectUI;

    private TextMeshProUGUI statusOutText;

    public void Start() {
        statusOutText = statusOutput.GetComponent<TextMeshProUGUI>();
    }

    public void Update() {
        if (networkManager != null) {
            networkManager.PollEvents();

            statusOutText.text = networkManager is SingleplayerNetworkManager ? "Singleplayer" : (networkManager.IsServer() ? "Server" : "Client");

            try {
                statusOutText.text += "\nConnected players: " + networkManager.GetClients().Count();
                statusOutText.text += "\nPlayerID: " + networkManager.GetLocalClient().PlayerId;
                statusOutText.text += "\nState: " + networkManager.GetLocalClient().State;
            } catch (Exception e) {
            }
        }
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

            connectUI.SetActive(false);
        } else {
            Debug.LogError("Could not parse port!");
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

            connectUI.SetActive(false);
        } else {
            Debug.LogError("Could not parse port!");
        }
    }

}
