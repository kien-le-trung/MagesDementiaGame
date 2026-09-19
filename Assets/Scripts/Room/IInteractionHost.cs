namespace MagesDementiaGame
{
    public interface IInteractionHost
    {
        bool IsInteractionBlocked { get; }
        void SetInteractionPrompt(string prompt);
    }
}
