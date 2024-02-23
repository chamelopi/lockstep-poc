using System.Collections;
using System.Collections.Generic;
using Simulation;
using UnityEngine;

public class EntityPositionSync : MonoBehaviour
{
    // Filled by simulation manager when instantiating prefab
    public int EntityId;
    public bool spawned = false;

    void Update()
    {
        if (!spawned && SimulationManager.sim!.lastState.Entities.ContainsKey(EntityId))
        {
            spawned = true;
        }
        if (spawned)
        {
            var interpolatedEntity = SimulationManager.sim!.Interpolate(EntityId);
            transform.position = new Vector3(FixedPointUtil.FromFixed(interpolatedEntity.X), 1, FixedPointUtil.FromFixed(interpolatedEntity.Y));
        }
    }
}
