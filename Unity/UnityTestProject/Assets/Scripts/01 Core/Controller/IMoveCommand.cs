namespace Panda.Core.Controller {
    public interface IMoveCommand {
        void MoveClockwise(float amount);
        void MoveCounterClockwise(float amount);
    }
}