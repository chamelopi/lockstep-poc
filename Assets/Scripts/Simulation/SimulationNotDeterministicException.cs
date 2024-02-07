using System;

namespace Simulation
{
    public class SimulationNotDeterministicException : Exception
    {
        public SimulationNotDeterministicException(string message) : base(message) { }
        public SimulationNotDeterministicException(string message, Exception cause) : base(message, cause) { }
    }
}
