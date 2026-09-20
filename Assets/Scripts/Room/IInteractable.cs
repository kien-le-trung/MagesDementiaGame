namespace MagesDementiaGame
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact(InteractionController interactor);
    }
}
