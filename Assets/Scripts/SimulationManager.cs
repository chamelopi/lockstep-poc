#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static Simulation.Simulation? sim;

    // Start is called before the first frame update
    void Start()
    {
        if (sim == null)
        {
            Debug.Log("Creating simulation");
            // TODO: Hardcoded player count, take from lobby settings instead
            sim = new Simulation.Simulation(turnSpeedMs: 100, playerCount: 2);

            // TODO: Create from map
            for (int i = 1; i <= sim.playerCount; i++)
            {
                sim.lastState.SpawnEntity(new Entity { X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false }, i);
                sim.lastState.SpawnEntity(new Entity { X = 10000 * i, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false }, i);
                sim.lastState.SpawnEntity(new Entity { X = 10000 * 2 * i, Y = 10000, VelocityX = 0, VelocityY = 0, Moving = false }, i);
            }
            sim.currentState = new(sim.lastState);
            sim.twoStepsAgoState = new(sim.lastState);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Update simulation
    }
}
