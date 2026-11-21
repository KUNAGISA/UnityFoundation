namespace Gameplay
{
    public readonly struct InteractionCandidate
    {
        public InteractionToken Token { get; }
        public string DisplayText { get; }
        public int Priority { get; }

        internal InteractionCandidate(InteractionToken token, string displayText, int priority)
        {
            Token = token;
            DisplayText = displayText;
            Priority = priority;
        }
    }
}
