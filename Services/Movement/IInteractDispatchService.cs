namespace Olympus.Services.Movement;

/// <summary>
/// Per-frame service that dispatches interact actions on configured object kinds when the
/// player walks within range.
/// </summary>
public interface IInteractDispatchService
{
    void Update();
}
