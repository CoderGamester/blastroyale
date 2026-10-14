namespace Quantum
{
    public static class GameHfsmPreUpdate
    {
        // SDK states now live in its core assembly. Extend the real state instead
        // of declaring a second type; custom physics actions remain in simulation.
        public static void PreUpdateState(this HFSMState state, Frame frame, EntityRef entity)
        {
            state.Parent?.PreUpdateState(frame, entity);
            foreach (var action in state.OnUpdate)
                if (action is AIAction gameAction) gameAction.PreUpdate(frame, entity);
        }
    }
}
