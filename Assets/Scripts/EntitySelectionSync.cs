using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySelectionSync : MonoBehaviour
{
    // Filled by simulation manager when instantiating prefab
    public int EntityId;
    public bool spawned = false;
    public GameObject selectionIndicator;

    void Update()
    {
        if (!spawned && SimulationManager.sim!.lastState.Entities.ContainsKey(EntityId))
        {
            spawned = true;
        }
        if (spawned)
        {
            var interpolatedEntity = SimulationManager.sim!.Interpolate(EntityId);
            // Set visibility of indicator based on selection state
            selectionIndicator.SetActive(SimulationManager.sim!.currentState.SelectedEntities.Contains(EntityId));
        }
    }
}
