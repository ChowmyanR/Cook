namespace Stations
{
    public interface IInteractable
    {
        void Interact(PlayerInteractor interactor);
        bool CanInteract(PlayerInteractor interactor);
        string GetInteractionPrompt(PlayerInteractor interactor);
    }
}
