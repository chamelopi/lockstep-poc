using System;
using UnityEngine;

namespace Simulation
{
    public class CommandHandler
    {
        // Per turn!
        private static readonly long PlayerSpeed = 10000;


        public static void HandleCommand(Simulation sim, SimulationState currentState, Command command)
        {
            switch (command.CommandType)
            {
                case CommandType.Select:
                    HandleSelectCommand(currentState, command);
                    break;
                case CommandType.Deselect:
                    HandleDeselectCommand(currentState);
                    break;
                case CommandType.Move:
                    HandleMoveCommand(currentState, command);
                    break;
                case CommandType.Spawn:
                    HandleSpawnCommand(sim, currentState, command);
                    break;
                case CommandType.BoxSelect:
                    HandleBoxSelectCommand(currentState, command);
                    break;
                default:
                    Debug.LogError("CommandHandler: Unknown command type " + command.CommandType);
                    break;
            }
        }

        private static void HandleBoxSelectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities.Clear();

            foreach (var (id, entity) in currentState.Entities)
            {
                if (entity.OwningPlayer == MenuUi.networkManager.GetLocalPlayer())
                {
                    // We do the check in view space because that is easier since we don't have to consider the perspective projection.
                    var pos = new Vector3(FixedPointUtil.FromFixed(command.TargetX), FixedPointUtil.FromFixed(command.TargetY), FixedPointUtil.FromFixed(command.TargetZ));
                    var size = new Vector3(FixedPointUtil.FromFixed(command.BoxX), FixedPointUtil.FromFixed(command.BoxY), FixedPointUtil.FromFixed(command.BoxZ));
                    var bounds = new Bounds(pos, size);

                    var entityPosInViewSpace = Camera.main.WorldToViewportPoint(new Vector3(FixedPointUtil.FromFixed(entity.X), FixedPointUtil.FromFixed(entity.Y), 0f));
                    if (bounds.Contains(entityPosInViewSpace))
                    {
                        currentState.SelectedEntities.Add(id);
                    }
                }
            }
        }

        private static void HandleSelectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities.Clear();
            currentState.SelectedEntities.Add(command.EntityId);
        }

        private static void HandleDeselectCommand(SimulationState currentState)
        {
            currentState.SelectedEntities.Clear();
        }

        private static void HandleMoveCommand(SimulationState currentState, Command command)
        {
            // TODO: Calculate center of all entities & offset of each entity to that center
            //       then move each individual entity towards the target + offset 
            foreach (var selected in currentState.SelectedEntities)
            {
                var affectedEntity = currentState.Entities[selected];

                var dx = command.TargetX - affectedEntity.X;
                var dy = command.TargetY - affectedEntity.Y;
                var dist = (long)Mathf.Sqrt(dx * dx + dy * dy);
                var vx = dx * PlayerSpeed / dist;
                var vy = dy * PlayerSpeed / dist;

                affectedEntity.TargetX = command.TargetX;
                affectedEntity.TargetY = command.TargetY;
                affectedEntity.VelocityX = vx;
                affectedEntity.VelocityY = vy;
                affectedEntity.Moving = true;

                currentState.Entities[selected] = affectedEntity;
            }
        }

        private static void HandleSpawnCommand(Simulation sim, SimulationState currentState, Command command)
        {
            var entity = new Entity()
            {
                OwningPlayer = command.PlayerId,
                X = command.TargetX,
                Y = command.TargetY,
                Moving = false,
            };
            entity = currentState.SpawnEntity(entity, command.PlayerId);
            sim!.onEntitySpawn?.Invoke(entity);
        }
    }

}

