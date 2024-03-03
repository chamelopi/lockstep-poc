using System;
using System.Linq;
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
                    HandleDeselectCommand(currentState, command);
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
                case CommandType.MassSpawn:
                    HandleMassSpawnCommand(sim, currentState, command);
                    break;
                default:
                    Debug.LogError("CommandHandler: Unknown command type " + command.CommandType);
                    break;
            }
        }



        private static void HandleBoxSelectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities[command.PlayerId].Clear();

            foreach (var (id, entity) in currentState.Entities)
            {
                if (entity.OwningPlayer == command.PlayerId)
                {
                    // We do the check in view space because that is easier since we don't have to consider the perspective projection.
                    var pos = new Vector3(FixedPointUtil.FromFixed(command.TargetX), FixedPointUtil.FromFixed(command.TargetY), FixedPointUtil.FromFixed(command.TargetZ));
                    var size = new Vector3(FixedPointUtil.FromFixed(command.BoxX), FixedPointUtil.FromFixed(command.BoxY), FixedPointUtil.FromFixed(command.BoxZ));
                    var bounds = new Bounds(pos, size);

                    var entityPosInViewSpace = Camera.main.WorldToViewportPoint(new Vector3(FixedPointUtil.FromFixed(entity.X), FixedPointUtil.FromFixed(entity.Y), 0f));
                    if (bounds.Contains(entityPosInViewSpace))
                    {
                        currentState.SelectedEntities[command.PlayerId].Add(id);
                    }
                }
            }
        }

        private static void HandleSelectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities[command.PlayerId].Clear();
            currentState.SelectedEntities[command.PlayerId].Add(command.EntityId);
        }

        private static void HandleDeselectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities[command.PlayerId].Clear();
        }

        private static void HandleMoveCommand(SimulationState currentState, Command command)
        {
            if (currentState.SelectedEntities[command.PlayerId].Count == 1)
            {
                var id = currentState.SelectedEntities[command.PlayerId].First();
                var affectedEntity = currentState.Entities[id];

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

                currentState.Entities[id] = affectedEntity;
            }
            else if (currentState.SelectedEntities[command.PlayerId].Count > 1)
            {
                long centerX = (long)currentState.SelectedEntities[command.PlayerId].Select(e => currentState.Entities[e].X).Average();
                long centerY = (long)currentState.SelectedEntities[command.PlayerId].Select(e => currentState.Entities[e].Y).Average();

                foreach (var selected in currentState.SelectedEntities[command.PlayerId])
                {
                    var affectedEntity = currentState.Entities[selected];

                    //  Calculate center of all entities & offset of each entity to that center
                    //       then move each individual entity towards the target + offset 
                    var offsetX = centerX - affectedEntity.X;
                    var offsetY = centerY - affectedEntity.Y;

                    var targetX = command.TargetX - offsetX;
                    var targetY = command.TargetY - offsetY;

                    var dx = targetX - affectedEntity.X;
                    var dy = targetY - affectedEntity.Y;
                    var dist = (long)Mathf.Sqrt(dx * dx + dy * dy);
                    var vx = dx * PlayerSpeed / dist;
                    var vy = dy * PlayerSpeed / dist;

                    affectedEntity.TargetX = targetX;
                    affectedEntity.TargetY = targetY;
                    affectedEntity.VelocityX = vx;
                    affectedEntity.VelocityY = vy;
                    affectedEntity.Moving = true;

                    currentState.Entities[selected] = affectedEntity;
                }
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

        private static void HandleMassSpawnCommand(Simulation sim, SimulationState currentState, Command command)
        {
            // Have to seed here to maintain determinism - for actual random stuff in-game,
            // we will have to initialize a separate random generator PER PLAYER inside the simulation
            // (because we don't currently guarantee order of execution among commands within a turn, so player's commands might
            // execute in arbitrary order. Another solution would be to sort commands by player id, too)
            UnityEngine.Random.InitState(123);

            for (int i = 0; i < 100; i++)
            {
                var randomPos = UnityEngine.Random.insideUnitCircle * 10;

                var entity = new Entity()
                {
                    OwningPlayer = command.PlayerId,
                    X = command.TargetX + FixedPointUtil.ToFixed(randomPos.x),
                    Y = command.TargetY + FixedPointUtil.ToFixed(randomPos.y),
                    Moving = false,
                };
                entity = currentState.SpawnEntity(entity, command.PlayerId);
                sim!.onEntitySpawn?.Invoke(entity);
            }
        }
    }
}


