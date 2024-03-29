using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{


    public class SimulationState
    {
        public Dictionary<int, int> EntityIdCounters;

        public int MaxEntitiesPerPlayer { get; }

        public Dictionary<int, Entity> Entities;
        // Selected entities per player!
        public Dictionary<int, HashSet<int>> SelectedEntities;

        public SimulationState(int numberOfPlayers)
        {
            MaxEntitiesPerPlayer = (int)(Mathf.Pow(2, 31) / numberOfPlayers);
            Entities = new();
            EntityIdCounters = new();
            SelectedEntities = new();
            for (int i = 1; i <= numberOfPlayers; i++)
            {
                EntityIdCounters[i] = (i - 1) * MaxEntitiesPerPlayer;
                SelectedEntities[i] = new();
            }
        }

        public SimulationState(SimulationState copy)
        {
            this.Entities = new(copy.Entities);
            this.SelectedEntities = new(copy.SelectedEntities);
            this.EntityIdCounters = new(copy.EntityIdCounters);
        }

        /**
         * Returns the next free entity Id for a player and increments the counter.
         * Each player uses multiples of their own player ID as entity IDs - this prevents conflicts.
         */
        private int GetNextId(int playerId)
        {
            var nextId = EntityIdCounters[playerId];
            EntityIdCounters[playerId]++;
            // TODO: Add sanity check for overflow of ids
            return nextId;
        }

        public Entity SpawnEntity(Entity entity, int playerId)
        {
            var id = GetNextId(playerId);
            entity.EntityId = id;
            entity.OwningPlayer = playerId;
            this.Entities.Add(id, entity);
            return entity;
        }
    }
}