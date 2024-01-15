namespace Simulation;

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
                default:
                    Console.WriteLine("CommandHandler: Unknown command type " + command.CommandType);
                    break;
            }
        }

        private static void HandleSelectCommand(SimulationState currentState, Command command)
        {
            currentState.SelectedEntities.Clear();
            currentState.SelectedEntities.Add(command.PlayerId-1);
        }

        private static void HandleDeselectCommand(SimulationState currentState)
        {
            currentState.SelectedEntities.Clear();
        }

        private static void HandleMoveCommand(SimulationState currentState, Command command)
        {
            foreach(var selected in currentState.SelectedEntities) {
                var affectedPlayer = currentState.Entities[selected];

                var dx = command.TargetX - affectedPlayer.X;
                var dy = command.TargetY - affectedPlayer.Y;
                var dist = (long)Math.Sqrt(dx * dx + dy * dy);
                var vx = dx * PlayerSpeed / dist;
                var vy = dy * PlayerSpeed / dist;

                affectedPlayer.VelocityX = vx;
                affectedPlayer.VelocityY = vy;
                affectedPlayer.TargetX = command.TargetX;
                affectedPlayer.TargetY = command.TargetY;
                affectedPlayer.Moving = true;

                currentState.Entities[selected] = affectedPlayer;
            }
        }
    }
