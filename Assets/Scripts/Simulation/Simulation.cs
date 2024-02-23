#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server;
using Simulation.Util;
using UnityEngine;

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
        internal EntityCallback? onEntitySpawn;
        internal EntityCallback? onEntityDespawn;

        public Simulation(int turnSpeedMs, int playerCount)
        {
            this.turnSpeedMs = turnSpeedMs;
            this.playerCount = playerCount;
            this.currentTurn = 0;

            this.lastState = new(playerCount);
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
            // FIXME: This creates a copy of all entity ids per simulation step!
            foreach (var id in this.currentState.Entities.Keys.ToArray())
            {
                this.currentState.Entities[id] = this.currentState.Entities[id].Update();
            }
        }

        private void HandleCommand(Command command)
        {
            if (command.PlayerId <= 0 || command.PlayerId > this.playerCount)
            {
                Debug.LogWarning($"invalid player ID {command.PlayerId} in command! Discarding command");
                return;
            }

            CommandHandler.HandleCommand(this, currentState, command);
        }

        public SimulationState Interpolate(float msSinceStartOfTurn)
        {
            // 1 if on current turn, 0 if last turn
            float alpha = msSinceStartOfTurn / (float)turnSpeedMs;
            if (alpha > 1.0 || alpha < 0.0)
            {
                //Debug.Log($"alpha is {alpha} for interpolation - that seems wrong! :D");
            }
            // To prevent teleporting - if we do have to clamp alpha here, the game will stutter however.
            alpha = Math.Min(1.0f, Math.Max(0.0f, alpha));

            SimulationState interpolatedState = new(lastState);

            foreach (var (entityId, entity) in interpolatedState.Entities)
            {
                // Only interpolate if the entity existed during the last turn - otherwise just use the "new" entity!
                if (!twoStepsAgoState.Entities.ContainsKey(entityId))
                {
                    interpolatedState.Entities[entityId] = entity;
                }
                // TODO: Handle case of entity despawning
                else
                {
                    interpolatedState.Entities[entityId] = Entity.Interpolate(entity, twoStepsAgoState.Entities[entityId], alpha);
                }
            }

            return interpolatedState;
        }

        public Entity Interpolate(int entityId) {
            // 1 if on current turn, 0 if last turn
            float alpha = GetTimeSinceLastStep() / (float)turnSpeedMs;
            // To prevent teleporting - if we do have to clamp alpha here, the game will stutter however.
            alpha = Math.Min(1.0f, Math.Max(0.0f, alpha));

            // TODO: Handle case of entity despawning! The entity might not exist anymore in lastState!
            var entity = lastState.Entities[entityId];

            // Only interpolate if the entity existed during the last turn - otherwise just use the "new" entity!
            if (!twoStepsAgoState.Entities.ContainsKey(entityId))
            {
                return entity;
            }
            else
            {
                return Entity.Interpolate(entity, twoStepsAgoState.Entities[entityId], alpha);
            }
        }


        /**
         * Re-run the entire simulation with the same input and assert that the outcome remains the same
         */
        public void CheckFullDeterminism()
        {
            // Run the simulation again, with the same inputs. It should yield exactly the same results.
            var determinismCheck = new Simulation(this.turnSpeedMs, this.playerCount);
            determinismCheck.AddCommands(this.allCommands);
            // FIXME: Replicate initial (!) entity state

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

            if (!stateA.Entities.Keys.ToHashSet().SetEquals(stateB.Entities.Keys))
            {
                Debug.Log("A: " + string.Join(", ", stateA.Entities.Keys.ToArray()));
                Debug.Log("B: " + string.Join(", ", stateB.Entities.Keys.ToArray()));
                throw new SimulationNotDeterministicException($"Different entity IDs in both states!");
            }

            // Check if objects are equal
            foreach (var entityId in stateA.Entities.Keys)
            {
                var objA = stateA.Entities[entityId];
                var objB = stateB.Entities[entityId];

                if (objA != objB)
                {
                    throw new SimulationNotDeterministicException($"Entity {entityId} does not match in both simulations! Real sim - {objA} | Check sim - {objB}");
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
                var serialized = JsonSerializer.Serialize(new Replay() { Commands = allCommands }, NetworkPacket.options);
                stream.Write(serialized);
            }

            Debug.Log("Successfully saved to replay file " + filename);
        }

        public void LoadReplay(string filename)
        {
            Replay replay;
            using (var stream = new StreamReader(filename))
            {
                // TODO: This probably uses a lot of memory for large files
                replay = JsonSerializer.Deserialize<Replay>(stream.ReadToEnd(), NetworkPacket.options)!;
            }
            if (replay == null)
            {
                throw new ArgumentException("Could not parse replay " + filename + " - result was null!");
            }

            // Commented for now to allow replays to use the entities already on the map.
            // FIXME: Store initial entities in replay and restore them here!
            //this.Reset();
            this.AddCommands(replay.Commands);
            for (int i = 0; i < commandQueue.Count; i++)
            {
                Debug.Log(commandQueue.UnorderedItems.ElementAt(i).ToString());
            }

            Debug.Log("Successfully loaded from replay file " + filename);
        }

        public delegate void EntityCallback(Entity e);

        public void HandleEntitySpawn(EntityCallback cb)
        {
            this.onEntitySpawn = cb;
        }

        public void HandleEntityDespawn(EntityCallback cb)
        {
            this.onEntityDespawn = cb;
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