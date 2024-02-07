using UnityEngine;

namespace Simulation
{
    public class CommandHandler
    {
        // Per turn!
        private static readonly long PlayerSpeed = 10000;


        public static void HandleCommand(SimulationState currentState, Command command)
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
                    HandleSpawnCommand(currentState, command);
                    break;
                default:
                    Debug.LogError("CommandHandler: Unknown command type " + command.CommandType);
                    break;
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

        private static void HandleSpawnCommand(SimulationState currentState, Command command)
        {
            var entity = new Entity()
            {
                OwningPlayer = command.PlayerId,
                X = command.TargetX,
                Y = command.TargetY,
                Moving = false,
            };
            currentState.SpawnEntity(entity, command.PlayerId);
        }
    }

}

