using System.Collections.Generic;
using System.Data;

namespace Simulation
{
    using SimulationState = List<Player>;

    

    // long due to floating point determinism stuff - would be fixed point probably
    // Stand-in struct for all game entities.
    struct Player
    {
        public long X;
        public long Y;
        public long VelocityX;
        public long VelocityY;
        public bool Moving;

        public override readonly string ToString()
        {
            return $"Player: P = {X}/{Y}, V = {VelocityX}/{VelocityY}, M = {Moving}";
        }

        // TODO: For rendering, we would probably have to calculate floats back from the fixed point variables at this point (?)
        public static Player Interpolate(Player a, Player b, float alpha) {
            return new Player {
                X = Lerp(a.X, a.Y, alpha),
                Y = Lerp(a.X, a.Y, alpha),
                VelocityX = Lerp(a.X, a.Y, alpha),
                VelocityY = Lerp(a.X, a.Y, alpha),
                // TODO: Can't interpolate bool, what to do here?
                Moving = b.Moving,
            };
        }

        static long Lerp(long a, long b, float alpha) {
            return (long) ((float)a * alpha + (float)b * (1 - alpha));
        }
    }

    // In reality, there would have to be more than one command
    // This is a simple move command
    struct Command
    {
        public int PlayerId;
        public long TargetX;
        public long TargetY;

        public int TargetTurn;
    }



    class Simulation
    {
        // Per turn!
        private readonly long PlayerSpeed = 1000;


        public readonly int turnSpeedMs;
        public readonly int playerCount;
        // State of last frame (stored for interpolation purposes)
        public SimulationState lastState;
        // State of this frame
        public SimulationState currentState;
        public int currentTurn;

        public PriorityQueue<Command, int> commandQueue;

        public Simulation(int turnSpeedMs, int playerCount)
        {
            this.turnSpeedMs = turnSpeedMs;
            this.playerCount = playerCount;
            this.currentTurn = 0;

            this.lastState = new(playerCount);
            for (int i = 0; i < playerCount; i++) {
                this.lastState.Add(new Player{ X = 0, Y = 0, VelocityX = 0, VelocityY = 0, Moving = false });
            }
            this.currentState = new(this.lastState);
            this.commandQueue = new();
        }

        public void AddCommand(Command command)
        {
            commandQueue.Enqueue(command, command.TargetTurn);
        }

        public void Step()
        {
            currentTurn++;
            while((commandQueue.Count > 0) && (commandQueue.Peek().TargetTurn == currentTurn)) {
                var command = commandQueue.Dequeue();
                HandleCommand(command);
            }

            // Update state
            for(int i = 0; i < this.currentState.Count; i++) {
                var player = this.currentState[i];
                // TODO: Refactor to update function
                // TODO: Player needs to remember his target to stop moving at some point
                if (player.Moving) {
                    var updatedPlayer = new Player {
                        X = player.X + player.VelocityX,
                        Y = player.Y + player.VelocityY,
                        VelocityX = player.VelocityX,
                        VelocityY = player.VelocityY,
                        Moving = player.Moving,
                    };
                    this.currentState[i] = updatedPlayer;
                }
            }

            // Advance to next frame
            SimulationState nextState = new(currentState);
            this.lastState = currentState;
            this.currentState = nextState;
        }

        private void HandleCommand(Command command)
        {
            if (command.PlayerId < 0 || command.PlayerId >= this.playerCount) {
                // TODO: Handle this error somehow
                Console.WriteLine($"invalid player ID {command.PlayerId} in command! Discarding command");
                return;
            }

            var affectedPlayer = this.currentState[command.PlayerId];

            var dx = command.TargetX - affectedPlayer.X;
            var dy = command.TargetY - affectedPlayer.Y;
            var dist = (long) Math.Sqrt(dx*dx + dy*dy);
            Console.WriteLine($"dx = {dx}, dy = {dy}, dist = {dist}");
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
            float alpha = msSinceStartOfTurn / (float) turnSpeedMs;

            SimulationState interpolatedState = new(currentState);

            for (int i = 0; i < interpolatedState.Count; i++) {
                // TODO: How do we handle the case where
                //    a) a new entity has been spawned in or 
                //    b) an entity was despawned in one of these frames?
                interpolatedState[i] = Player.Interpolate(lastState[i], currentState[i], alpha);
            }

            return interpolatedState;
        }
    }
}