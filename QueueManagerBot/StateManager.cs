namespace QueueManagerBot
{
    public class StateManager
    {
        public Dictionary<long, UserState> states { get; } = new Dictionary<long, UserState>();

        public UserState GetState(long tgID)
        {
            if (!states.ContainsKey(tgID))
            {
                states.Add(tgID, UserState.None);
            }
            return states[tgID];
        }

        public void SetState(long tgID, UserState newState)
        {
            if (!states.ContainsKey(tgID))
            {
                states.Add(tgID, newState);
            }
            states[tgID] = newState;
        }
    }
}