using CtfDeck.Terminal.Features.Media;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.Media;

namespace CtfDeck.Terminal.Handlers;

public sealed class MediaMessageHandler : MessageHandlerBase
{
    private const int MaxUploadSize = 8 * 1024 * 1024; // 8 MB

    private readonly MediaService _mediaService;

    public MediaMessageHandler(MediaService mediaService)
    {
        _mediaService = mediaService;
    }

    protected override bool CanHandle(MessageType type)
        => MediaProtocolDeserializer.IsMediaMessage(type);

    protected override byte[] SerializeError(Guid messageId, string error)
        => MediaProtocolSerializer.SerializeError(messageId, error);

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            MessageType.MediaUpload => HandleUpload(data),
            MessageType.MediaLoad => HandleLoad(data),
            MessageType.MediaDelete => HandleDelete(data),
            MessageType.MediaList => HandleList(data),
            _ => throw new InvalidOperationException($"Unknown media message type: {type}")
        };
    }

    private byte[] HandleUpload(ReadOnlySpan<byte> data)
    {
        var request = new MediaUploadRequest(data);

        if (request.Data.Length > MaxUploadSize)
            throw new InvalidOperationException($"File exceeds maximum upload size of {MaxUploadSize / (1024 * 1024)} MB");

        var media = _mediaService.Create(request.FileName, request.MimeType, request.Data.ToArray());
        return MediaProtocolSerializer.SerializeUploadResult(request.MessageId, true, media.Id);
    }

    private byte[] HandleLoad(ReadOnlySpan<byte> data)
    {
        var request = new MediaLoadRequest(data);
        var media = _mediaService.GetById(request.MediaId);
        return MediaProtocolSerializer.SerializeLoadResult(request.MessageId, media != null, media?.Id ?? Guid.Empty, media?.Data);
    }

    private byte[] HandleDelete(ReadOnlySpan<byte> data)
    {
        var request = new MediaDeleteRequest(data);
        var success = _mediaService.Delete(request.MediaId);
        return MediaProtocolSerializer.SerializeDeleteResult(request.MessageId, success);
    }

    private byte[] HandleList(ReadOnlySpan<byte> data)
    {
        var request = new MediaListRequest(data);
        var mediaList = _mediaService.GetAll();
        return MediaProtocolSerializer.SerializeListResult(request.MessageId, mediaList);
    }
}
