namespace Simulation {
    // In reality, there would have to be more than one command
    // This is a simple move command
    public struct Command
    {
        public int PlayerId;
        public long TargetX;
        public long TargetY;

        public int TargetTurn;
    }
}