using A2UI.Blazor.Protocol;

namespace A2UI.Blazor.Services;

/// <summary>
/// Routes incoming A2UI messages to the SurfaceManager by message type.
/// </summary>
public sealed class MessageDispatcher
{
    private readonly SurfaceManager _surfaceManager;

    public MessageDispatcher(SurfaceManager surfaceManager)
    {
        _surfaceManager = surfaceManager;
    }

    public void Dispatch(A2UIMessage message)
    {
        switch (message.Type)
        {
            case "createSurface":
                HandleCreateSurface(message);
                break;
            case "updateComponents":
                HandleUpdateComponents(message);
                break;
            case "updateDataModel":
                HandleUpdateDataModel(message);
                break;
            case "deleteSurface":
                HandleDeleteSurface(message);
                break;
        }
    }

    private void HandleCreateSurface(A2UIMessage message)
    {
        if (message.SurfaceId is null) return;
        _surfaceManager.CreateSurface(
            message.SurfaceId,
            message.CatalogId,
            message.SendDataModel ?? false);
    }

    private void HandleUpdateComponents(A2UIMessage message)
    {
        if (message.SurfaceId is null || message.Components is null) return;
        _surfaceManager.UpdateComponents(message.SurfaceId, message.Components);
    }

    private void HandleUpdateDataModel(A2UIMessage message)
    {
        if (message.SurfaceId is null) return;
        _surfaceManager.UpdateDataModel(message.SurfaceId, message.Path, message.Value);
    }

    private void HandleDeleteSurface(A2UIMessage message)
    {
        if (message.SurfaceId is null) return;
        _surfaceManager.DeleteSurface(message.SurfaceId);
    }
}
