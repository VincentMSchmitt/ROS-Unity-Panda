namespace Panda.PickAndPlace {
        public interface IRobotState {
        void Handle(TrajectoryPlanner planner);
    }
}