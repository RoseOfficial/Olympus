namespace Olympus.Services.Movement.Probes;

/// <summary>
/// Wraps <c>TargetSystem.InteractWithObject</c> for testability. Production fires the native call;
/// tests record the dispatched object IDs.
/// </summary>
public interface IObjectInteractor
{
    void Interact(ulong gameObjectId);
}
