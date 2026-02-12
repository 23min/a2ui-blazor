using A2UI.Blazor.Diagnostics;
using A2UI.Blazor.Protocol;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Services;

/// <summary>
/// Routes incoming A2UI messages to the SurfaceManager by message type.
/// </summary>
public sealed class MessageDispatcher
{
    private readonly SurfaceManager _surfaceManager;
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(SurfaceManager surfaceManager, ILogger<MessageDispatcher> logger)
    {
        _surfaceManager = surfaceManager;
        _logger = logger;
    }

    public void Dispatch(A2UIMessage message)
    {
        _logger.LogDebug("Dispatching message type {MessageType} for surface {SurfaceId}", message.Type, message.SurfaceId);

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
            case "error":
                HandleError(message);
                break;
            default:
                _logger.LogWarning(LogEvents.UnknownMessageType,
                    "Unknown A2UI message type {MessageType} for surface {SurfaceId}",
                    message.Type, message.SurfaceId);
                break;
        }
    }

    private void HandleCreateSurface(A2UIMessage message)
    {
        if (message.SurfaceId is null)
        {
            _logger.LogWarning(LogEvents.NullSurfaceId, "createSurface message missing surfaceId");
            return;
        }
        _surfaceManager.CreateSurface(
            message.SurfaceId,
            message.CatalogId,
            message.SendDataModel ?? false,
            message.Theme);
    }

    private void HandleUpdateComponents(A2UIMessage message)
    {
        if (message.SurfaceId is null)
        {
            _logger.LogWarning(LogEvents.NullSurfaceId, "updateComponents message missing surfaceId");
            return;
        }
        if (message.Components is null)
        {
            _logger.LogWarning("updateComponents message missing components for surface {SurfaceId}", message.SurfaceId);
            return;
        }
        _surfaceManager.UpdateComponents(message.SurfaceId, message.Components);
    }

    private void HandleUpdateDataModel(A2UIMessage message)
    {
        if (message.SurfaceId is null)
        {
            _logger.LogWarning(LogEvents.NullSurfaceId, "updateDataModel message missing surfaceId");
            return;
        }
        _surfaceManager.UpdateDataModel(message.SurfaceId, message.Path, message.Value);
    }

    private void HandleDeleteSurface(A2UIMessage message)
    {
        if (message.SurfaceId is null)
        {
            _logger.LogWarning(LogEvents.NullSurfaceId, "deleteSurface message missing surfaceId");
            return;
        }
        _surfaceManager.DeleteSurface(message.SurfaceId);
    }

    private void HandleError(A2UIMessage message)
    {
        if (message.SurfaceId is null)
        {
            _logger.LogWarning(LogEvents.NullSurfaceId, "error message missing surfaceId");
            return;
        }
        if (message.Path is not null && message.ErrorMessage is not null)
        {
            _surfaceManager.SetValidationError(message.SurfaceId, message.Path, message.ErrorMessage);
        }
    }
}
