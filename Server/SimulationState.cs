namespace Simulation
{
    public class SimulationState
    {
        public List<Player> Entities;
        public List<int> SelectedEntities;

        public SimulationState(int numberOfPlayers) {
            Entities = new(numberOfPlayers);
            SelectedEntities = new();
        }

        public SimulationState(SimulationState copy) {
            this.Entities = new(copy.Entities);
            this.SelectedEntities = new(copy.SelectedEntities);
        }
    }
}
