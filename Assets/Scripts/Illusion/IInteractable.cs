namespace Hypoxia
{
    /// <summary>
    /// Antarmuka umum untuk semua objek yang bisa diinteraksikan pemain (E).
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact();
    }
}
