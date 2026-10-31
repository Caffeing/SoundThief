namespace SoundThief
{
    // Scene-specific interactions enter through the same F target and core permission checks.
    public interface IThiefInteractionHandler
    {
        bool TryInteract(ThiefPlayer player);
    }
}
