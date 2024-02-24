#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server;
using Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    public static Simulation.Simulation? sim;

    public GameObject entityPrefab;

    // Start is called before the first frame update
    void Start()
    {
        if (sim == null)
        {
            // TODO: Hardcoded player count, take from lobby settings instead
            sim = new Simulation.Simulation(turnSpeedMs: 100, playerCount: 2);
            sim.HandleEntitySpawn(e => OnEntitySpawn(e));
            sim.HandleEntityDespawn(e => OnEntityDespawn(e));

            // TODO: Create from map
            for (int i = 1; i <= sim.playerCount; i++)
            {
                OnEntitySpawn(sim.lastState.SpawnEntity(new Entity { X = 10000 * (i + 1), Y = 0, VelocityX = 0, VelocityY = 0, Moving = false }, i));
                OnEntitySpawn(sim.lastState.SpawnEntity(new Entity { X = 10000 * (i + 1), Y = 10000, VelocityX = 0, VelocityY = 0, Moving = false }, i));
                OnEntitySpawn(sim.lastState.SpawnEntity(new Entity { X = 10000 * (i + 1), Y = 20000, VelocityX = 0, VelocityY = 0, Moving = false }, i));
            }
            sim.currentState = new(sim.lastState);
            sim.twoStepsAgoState = new(sim.lastState);
            sim.isPaused = true;

            MenuUi.networkManager!.AddCallback(PacketType.Command, packet =>
            {
                var commandPacket = (CommandPacket)packet;
                if (commandPacket.Command.PlayerId != commandPacket.PlayerId)
                {
                    Debug.LogError($"Player {commandPacket.PlayerId} cannot send command for player {commandPacket.Command.PlayerId}");
                    return;
                }
                sim.AddCommand(commandPacket.Command);
            });
        }

    }

    void OnEntitySpawn(Entity e)
    {
        var instance = Instantiate(entityPrefab);
        instance.name = "Entity" + e.EntityId;
        instance.transform.position = new Vector3(FixedPointUtil.FromFixed(e.X), 1, FixedPointUtil.FromFixed(e.Y));
        instance.GetComponent<EntityPositionSync>().EntityId = e.EntityId;
        instance.GetComponent<EntitySelectionSync>().EntityId = e.EntityId;
        foreach (var meshRenderer in instance.GetComponents<MeshRenderer>())
        {
            meshRenderer.material.color = e.OwningPlayer == 1 ? Color.red : Color.blue;
        }
    }

    void OnEntityDespawn(Entity e)
    {
        Debug.Log($"Entity killed! id={e.EntityId}");
    }

    void Update()
    {
        RunSimulation();
    }

    private void RunSimulation()
    {
        if (sim == null)
        {
            return;
        }

        if (sim.isPaused)
        {
            return;
        }

        // Fixed time step - alternative to this would be using Time.deltaTime and adding it up until the sum
        // is greater than sim.turnSpeedMs, then stepping the simulation.
        var startFrame = Clock.GetTicks();
        var timeSinceLastStep = startFrame - sim.lastTurnTimestamp;

        if (timeSinceLastStep > sim.turnSpeedMs)
        {
            // Signal next turn to other players and advance once we are allowed
            MenuUi.networkManager?.SignalNextTurn(sim.currentTurn);
            if (MenuUi.networkManager != null && MenuUi.networkManager.CanAdvanceTurn())
            {
                sim.lastTurnTimestamp = Clock.GetTicks();

                sim.Step();

                // We might disable this for a release build for performance reasons
                sim.CheckDeterminism();
            }
        }
    }
}
