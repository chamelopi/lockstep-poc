using System.Collections.Generic;
using System.Data;

namespace Simulation
{
    using SimulationState = List<Player>;


    public class Simulation
    {
        // Per turn!
        private readonly long PlayerSpeed = 10000;


        public readonly int turnSpeedMs;
        public readonly int playerCount;
        // State of this frame
        public SimulationState currentState;
        // State of last frame (stored for interpolation purposes)
        public SimulationState lastState;

        // State of the frame before the last one (stored for interpolation purposes)
        public SimulationState lastLastState;
        public int currentTurn;

        public PriorityQueue<Command, int> commandQueue;
        public List<Command> allCommands;
        public List<Command> lastFrameActions;

        public Simulation(int turnSpeedMs, int playerCount)
        {
            this.turnSpeedMs = turnSpeedMs;
            this.playerCount = playerCount;
            this.currentTurn = 0;

            this.lastState = new(playerCount);
            this.lastLastState = new(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                this.lastState.Add(new Player { X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false });
                this.lastLastState.Add(new Player { X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false });
            }
            this.currentState = new(this.lastState);
            this.commandQueue = new();
            this.allCommands = new();
            this.lastFrameActions = new();
        }

        public void AddCommand(Command command)
        {
            commandQueue.Enqueue(command, command.TargetTurn);
            allCommands.Add(command);
        }

        public void AddCommands(IEnumerable<Command> commands)
        {
            foreach (var cmd in commands)
            {
                commandQueue.Enqueue(cmd, cmd.TargetTurn);
            }
            allCommands.AddRange(commands);
        }

        public void Step()
        {
            currentTurn++;
            lastFrameActions.Clear();
            while ((commandQueue.Count > 0) && (commandQueue.Peek().TargetTurn == currentTurn))
            {
                var command = commandQueue.Dequeue();
                lastFrameActions.Add(command);
                HandleCommand(command);
            }

            // Update state
            for (int i = 0; i < this.currentState.Count; i++)
            {
                var player = this.currentState[i];
                this.currentState[i] = player.Update();
            }

            // Advance to next frame
            SimulationState nextState = new(currentState);
            // Preserve the last two frames for interpolation purposes
            this.lastLastState = lastState;
            this.lastState = currentState;
            this.currentState = nextState;
        }

        private void HandleCommand(Command command)
        {
            if (command.PlayerId < 0 || command.PlayerId >= this.playerCount)
            {
                // TODO: Handle this error somehow
                Console.WriteLine($"invalid player ID {command.PlayerId} in command! Discarding command");
                return;
            }

            var affectedPlayer = this.currentState[command.PlayerId];

            var dx = command.TargetX - affectedPlayer.X;
            var dy = command.TargetY - affectedPlayer.Y;
            var dist = (long)Math.Sqrt(dx * dx + dy * dy);
            var vx = dx * PlayerSpeed / dist;
            var vy = dy * PlayerSpeed / dist;

            affectedPlayer.VelocityX = vx;
            affectedPlayer.VelocityY = vy;
            affectedPlayer.Moving = true;

            this.currentState[command.PlayerId] = affectedPlayer;
        }

        public SimulationState Interpolate(float msSinceStartOfTurn)
        {
            // 1 if on current turn, 0 if last turn
            float alpha = msSinceStartOfTurn / (float)turnSpeedMs;

            SimulationState interpolatedState = new(lastState);

            // TODO: How do we handle the case where
            //    a) a new entity has been spawned in or 
            //    b) an entity was despawned in one of these frames?
            //    do we give them a spawn turn index and let the rendering code handle that somehow?

            for (int i = 0; i < interpolatedState.Count; i++)
            {
                interpolatedState[i] = Player.Interpolate(lastState[i], lastLastState[i], alpha);
            }

            return interpolatedState;
        }


        /**
         * Re-run the entire simulation with the same input and assert that the outcome remains the same
         */
        public void CheckFullDeterminism()
        {
            // Run the simulation again, with the same inputs. It should yield exactly the same results.
            var determinismCheck = new Simulation(this.turnSpeedMs, this.playerCount);
            determinismCheck.AddCommands(this.allCommands);

            // Step until we reach the other simulations turn
            while (determinismCheck.currentTurn < this.currentTurn)
            {
                determinismCheck.Step();
            }

            AssertDeterminism(determinismCheck);
        }

        /**
         * Re-calculate the current frame from the last frame, in order to check if we are still deterministic
         */
        public void CheckDeterminism()
        {
            // Run the simulation again, with the same inputs. It should yield exactly the same results.
            var determinismCheck = new Simulation(this.turnSpeedMs, this.playerCount)
            {
                // Copy last frame's state into the simulation
                currentState = new SimulationState(this.lastState),
                currentTurn = this.currentTurn - 1,
            };
            determinismCheck.AddCommands(this.lastFrameActions);

            // Single-step
            determinismCheck.Step();

            AssertDeterminism(determinismCheck);
        }

        /**
         * Compares the current states of two simulations in order to check for any non-determinism in the simulation's code
         */
        internal void AssertDeterminism(Simulation other)
        {
            var stateA = this.currentState;
            var stateB = other.currentState;

            if (this.currentTurn != other.currentTurn)
            {
                throw new SimulationNotDeterministicException($"Turn number is not equal! Real sim {this.currentTurn} | Check sim {other.currentTurn}");
            }

            // Check if the same number of objects
            if (stateA.Count != stateB.Count)
            {
                throw new SimulationNotDeterministicException($"Number of entities in state differs: Real sim - {stateA.Count} | Check sim - {stateB.Count}");
            }

            // Check if objects are equal
            for (int i = 0; i < stateA.Count; i++)
            {
                var objA = stateA[i];
                var objB = stateB[i];

                if (objA != objB)
                {
                    throw new SimulationNotDeterministicException($"Entity {i} does not match in both simulations! Real sim - {objA} | Check sim - {objB}");
                }
            }
        }
    }
}