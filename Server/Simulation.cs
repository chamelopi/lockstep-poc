
namespace Simulation
{
    public class Simulation
    {

        // By changing this, we can speed up or slow down the simulation.
        // Can be used to control game speed, or to fast-forward in replays or spectator mode
        public int turnSpeedMs;
        public readonly int playerCount;

        // State of the current simulation step
        public SimulationState currentState;
        // State of last step (stored for interpolation purposes)
        public SimulationState lastState;

        // State of two steps ago (stored for interpolation purposes)
        public SimulationState twoStepsAgoState;

        // The current simulation turn. Automatically incremented by calling Step().
        public int currentTurn;
        public long lastTurnTimestamp;

        public PriorityQueue<Command, int> commandQueue;
        public List<Command> allCommands;
        public List<Command> lastFrameActions;
        public bool isPaused;

        public Simulation(int turnSpeedMs, int playerCount)
        {
            this.turnSpeedMs = turnSpeedMs;
            this.playerCount = playerCount;
            this.currentTurn = 0;

            this.lastState = new(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                this.lastState.Entities.Add(new Player { X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false });
            }
            this.currentState = new(this.lastState);
            this.twoStepsAgoState = new(this.lastState);
            this.commandQueue = new();
            this.allCommands = new();
            this.lastFrameActions = new();
            this.lastTurnTimestamp = Clock.GetTicks();
        }

        public void Reset()
        {
            this.currentTurn = 0;
            this.currentState = new(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                this.currentState.Entities.Add(new Player { X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false });
            }
            this.lastState = new(currentState);
            this.twoStepsAgoState = new(currentState);

            this.commandQueue = new();
            this.allCommands = new();
            this.lastFrameActions = new();
            this.lastTurnTimestamp = Clock.GetTicks();
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

        public void RunSimulation()
        {
            if (isPaused)
            {
                return;
            }

            // TODO: Refactor into clock?
            long timeSinceLastStep = 0;

            // Fixed time step
            var startFrame = Clock.GetTicks();
            timeSinceLastStep += startFrame - lastTurnTimestamp;

            while (timeSinceLastStep > turnSpeedMs)
            {
                timeSinceLastStep -= turnSpeedMs;
                lastTurnTimestamp += turnSpeedMs;
                Step();

                // We might disable this for a release build
                CheckDeterminism();
            }
        }

        public void Step()
        {
            currentTurn++;
            // Advance to next frame
            SimulationState nextState = new(currentState);
            // Preserve the last two frames for interpolation purposes
            this.twoStepsAgoState = lastState;
            this.lastState = currentState;
            this.currentState = nextState;

            lastFrameActions.Clear();
            while ((commandQueue.Count > 0) && (commandQueue.Peek().TargetTurn == currentTurn))
            {
                var command = commandQueue.Dequeue();
                lastFrameActions.Add(command);
                HandleCommand(command);
            }

            // Update state
            for (int i = 0; i < this.currentState.Entities.Count; i++)
            {
                var player = this.currentState.Entities[i];
                this.currentState.Entities[i] = player.Update();
            }
        }

        private void HandleCommand(Command command)
        {
            if (command.PlayerId <= 0 || command.PlayerId > this.playerCount)
            {
                // TODO: Handle this error somehow?
                Console.WriteLine($"invalid player ID {command.PlayerId} in command! Discarding command");
                return;
            }

            CommandHandler.HandleCommand(currentState, command);
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

            for (int i = 0; i < interpolatedState.Entities.Count; i++)
            {
                interpolatedState.Entities[i] = Player.Interpolate(lastState.Entities[i], twoStepsAgoState.Entities[i], alpha);
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
            if (stateA.Entities.Count != stateB.Entities.Count)
            {
                throw new SimulationNotDeterministicException($"Number of entities in state differs: Real sim - {stateA.Entities.Count} | Check sim - {stateB.Entities.Count}");
            }

            // Check if objects are equal
            for (int i = 0; i < stateA.Entities.Count; i++)
            {
                var objA = stateA.Entities[i];
                var objB = stateB.Entities[i];

                if (objA != objB)
                {
                    throw new SimulationNotDeterministicException($"Entity {i} does not match in both simulations! Real sim - {objA} | Check sim - {objB}");
                }
            }
        }

        

        public float GetTimeSinceLastStep()
        {
            return Clock.GetTicks() - lastTurnTimestamp;
        }

        public void SaveReplay(string filename)
        {
            using (var stream = new StreamWriter(filename))
            {
                foreach (var cmd in allCommands)
                {
                    stream.WriteLine(cmd.PlayerId + "," + cmd.TargetX + "," + cmd.TargetY + "," + cmd.TargetTurn);
                }
            }
            Console.WriteLine("Successfully saved to replay file " + filename);
        }

        public void LoadReplay(string filename)
        {
            var commands = new List<Command>();
            using (var stream = new StreamReader(filename))
            {
                var lineCounter = 1;

                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();

                    if (line != null)
                    {
                        var parts = line.Split(",");
                        if (parts.Length != 4)
                        {
                            throw new ArgumentException(lineCounter + ": Incorrect number of CSV columns: " + parts.Length);
                        }
                        // TODO: Refactor into (de-)serialize methods?
                        var command = new Command { PlayerId = int.Parse(parts[0]), TargetX = long.Parse(parts[1]), TargetY = long.Parse(parts[2]), TargetTurn = int.Parse(parts[3]) };
                        if (command.TargetTurn < 1)
                        {
                            throw new ArgumentException(lineCounter + ": Cannot have command before turn 1!");
                        }
                        commands.Add(command);

                        lineCounter++;
                    }
                }
            }
            this.Reset();
            this.AddCommands(commands);
            for (int i = 0; i < commandQueue.Count; i++)
            {
                Console.WriteLine(commandQueue.UnorderedItems.ElementAt(i).ToString());
            }

            Console.WriteLine("Successfully loaded from replay file " + filename);
        }

        internal void TogglePause()
        {
            isPaused = !isPaused;
            if (!isPaused)
            {
                lastTurnTimestamp = Clock.GetTicks();
            }
        }
    }
}