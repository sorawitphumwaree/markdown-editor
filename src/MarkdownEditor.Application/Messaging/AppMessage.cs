using System.Text.Json;

namespace MarkdownEditor.Application.Messaging;

public sealed record AppMessage(
    string Type,
    int ProtocolVersion,
    string? RequestId,
    Guid? DocumentId,
    int? Version,
    JsonElement Payload)
{
    public const int CurrentProtocolVersion = 1;
}

