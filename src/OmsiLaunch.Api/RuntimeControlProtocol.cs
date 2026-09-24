using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OmsiLaunch.Api;

// Session-scoped runtime control uses this portable envelope. The payload is
// UTF-8 JSON semantic data; the envelope is fixed-width and never carries CLR
// layout, pointer-sized fields, or DNNE-specific state.
public sealed record RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments = null);
public sealed record RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode = null, IReadOnlyDictionary<string, string>? Values = null);

public static class RuntimeCommandWire
{
    public const uint Magic = 0x4F4C5243; // OLRC
    public const ushort Version = 1;
    public const int HeaderSize = 72;
    private const ushort RequestKind = 1;
    private const ushort ResponseKind = 2;

    public static byte[] SerializeRequest(RuntimeCommand command) => Serialize(RequestKind, command.SessionId, command.RequestId, JsonSerializer.SerializeToUtf8Bytes(new RuntimePayload(command.Operation, command.Arguments, true, null, null)));
    public static byte[] SerializeResponse(RuntimeCommandResult result) => Serialize(ResponseKind, result.SessionId, result.RequestId, JsonSerializer.SerializeToUtf8Bytes(new RuntimePayload(null, result.Values, result.Succeeded, result.ErrorCode, null)));

    public static bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand? command)
    {
        command = null;
        if (!TryRead(bytes, RequestKind, out var session, out var requestId, out var payload)) return false;
        var value = JsonSerializer.Deserialize<RuntimePayload>(payload);
        if (value is null || string.IsNullOrWhiteSpace(value.Operation)) return false;
        command = new RuntimeCommand(session, requestId, value.Operation, value.Values); return true;
    }

    public static bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult? result)
    {
        result = null;
        if (!TryRead(bytes, ResponseKind, out var session, out var requestId, out var payload)) return false;
        var value = JsonSerializer.Deserialize<RuntimePayload>(payload);
        if (value is null) return false;
        result = new RuntimeCommandResult(session, requestId, value.Succeeded, value.ErrorCode, value.Values); return true;
    }

    // Reads only the request identity of a staged envelope. Both ends of the
    // mailbox use it to make sure a response answers the request that is
    // currently staged and not one that was already abandoned.
    public static bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)
    {
        requestId = 0;
        if (bytes.Length < HeaderSize || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic) return false;
        requestId = BinaryPrimitives.ReadUInt64LittleEndian(bytes[28..36]); return true;
    }

    private static byte[] Serialize(ushort kind, Guid sessionId, ulong requestId, byte[] payload)
    {
        var output = new byte[HeaderSize + payload.Length]; var header = output.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(header, Magic); BinaryPrimitives.WriteUInt16LittleEndian(header[4..], Version); BinaryPrimitives.WriteUInt16LittleEndian(header[6..], kind);
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], (uint)output.Length); sessionId.TryWriteBytes(header[12..28]); BinaryPrimitives.WriteUInt64LittleEndian(header[28..36], requestId); BinaryPrimitives.WriteUInt32LittleEndian(header[36..40], (uint)payload.Length);
        SHA256.HashData(payload, header[40..72]); payload.CopyTo(header[72..]); return output;
    }

    private static bool TryRead(ReadOnlySpan<byte> input, ushort expectedKind, out Guid session, out ulong requestId, out ReadOnlySpan<byte> payload)
    {
        session = default; requestId = 0; payload = default;
        if (input.Length < HeaderSize || BinaryPrimitives.ReadUInt32LittleEndian(input) != Magic || BinaryPrimitives.ReadUInt16LittleEndian(input[4..]) != Version || BinaryPrimitives.ReadUInt16LittleEndian(input[6..]) != expectedKind || BinaryPrimitives.ReadUInt32LittleEndian(input[8..]) != input.Length) return false;
        var length = BinaryPrimitives.ReadUInt32LittleEndian(input[36..]); if (length != input.Length - HeaderSize) return false;
        payload = input[HeaderSize..]; if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), input[40..72])) return false;
        session = new Guid(input.Slice(12, 16)); requestId = BinaryPrimitives.ReadUInt64LittleEndian(input[28..]); return true;
    }

    private sealed record RuntimePayload(string? Operation, IReadOnlyDictionary<string, string>? Values, bool Succeeded, string? ErrorCode, string? Reserved);
}
