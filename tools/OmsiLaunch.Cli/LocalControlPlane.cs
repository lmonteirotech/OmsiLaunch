using System.IO.Pipes;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OmsiLaunch.Api;

internal sealed record LocalControlRequest(string ProtocolVersion, string Command, IReadOnlyDictionary<string, string>? Arguments = null);
// Metadata carries observable facts about the reply itself, for example that
// an event history was truncated to fit one frame.
internal sealed record LocalControlResponse(bool Ok, object? Result = null, string? ErrorCode = null, string? Message = null, IReadOnlyDictionary<string, string>? Metadata = null);

// A local, current-user-only command pipe. This is deliberately distinct from
// the host-to-plugin runtime mailbox and exposes only semantic session actions.
// One endpoint exists per installation root, so two OMSI installations owned by
// the same user never share a control channel.
internal sealed class LocalControlPlane : IAsyncDisposable
{
    private const string PipePrefix = "OmsiLaunch.Control.0.1.";
    private const int MaxMessageBytes = 65_536;
    private const int FrameBufferBytes = MaxMessageBytes + sizeof(int);
    // A client that never reads its reply must not pin a server task until the
    // session ends.
    private static readonly TimeSpan ReplyWriteTimeout = TimeSpan.FromSeconds(5);
    private readonly string pipeName;
    private readonly Func<LocalControlRequest, Task<LocalControlResponse>> handler;
    private readonly CancellationTokenSource stop = new();
    private readonly ConcurrentDictionary<int, Task> clients = new();
    private Task? loop;
    private int nextClientId;
    private Exception? listenFault;

    public LocalControlPlane(string installationRoot, Func<LocalControlRequest, Task<LocalControlResponse>> handler) { pipeName = PipeNameFor(installationRoot); this.handler = handler; }
    public string PipeName => pipeName;
    // Set when the listener stopped for a reason other than disposal (for
    // example the pipe name was already owned by another process).
    public Exception? ListenFault => listenFault;
    public void Start() => loop = Task.Run(ListenAsync);

    public static string PipeNameFor(string installationRoot)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(InstallationPaths.IdentityKey(installationRoot))));
        return PipePrefix + key[..16];
    }

    // Returns null only when no owner endpoint could be reached. Once a
    // connection exists, every failure is a typed response: the owner is known
    // to be present, so "no active session" would be wrong.
    public static async Task<LocalControlResponse?> TryRequestAsync(string installationRoot, LocalControlRequest request, TimeSpan timeout)
    {
        var connected = false;
        try
        {
            using var cancellation = new CancellationTokenSource(timeout);
            await using var pipe = new NamedPipeClientStream(".", PipeNameFor(installationRoot), PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(cancellation.Token).ConfigureAwait(false);
            connected = true;
            await WriteAsync(pipe, request, cancellation.Token).ConfigureAwait(false);
            var reply = await ReadAsync<LocalControlResponse>(pipe, cancellation.Token).ConfigureAwait(false);
            if (!reply.Received) return new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "The owner closed the connection without a reply.");
            return reply.Value ?? new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "The owner reply was empty.");
        }
        catch (OperationCanceledException) when (connected) { return new LocalControlResponse(false, ErrorCode: "OL_E_TIMEOUT", Message: "The owner did not reply within " + timeout.TotalMilliseconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms."); }
        catch (IOException) when (connected) { return new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "The connection to the owner was interrupted."); }
        catch (TimeoutException) { return null; }
        catch (OperationCanceledException) { return null; }
        // A request the caller built too large is the caller's error, not an
        // absent owner; a malformed reply from the owner is a protocol error.
        catch (InvalidDataException exception) { return new LocalControlResponse(false, ErrorCode: exception.Message.StartsWith("OL_E_", StringComparison.Ordinal) ? exception.Message : "OL_E_CONTROL_MESSAGE_INVALID", Message: "The control message could not be transmitted."); }
        catch (JsonException) { return new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "The owner reply could not be decoded."); }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    // Mutating commands must name the session they act on. The client first
    // reads the status, then binds the request to that session id.
    public static async Task<LocalControlResponse?> TryRequestBoundAsync(string installationRoot, string command, IReadOnlyDictionary<string, string>? arguments, TimeSpan timeout)
    {
        var status = await TryRequestAsync(installationRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(750)).ConfigureAwait(false);
        if (status is null) return null;
        if (!status.Ok) return status;
        var sessionId = status.Result is JsonElement element && element.ValueKind == JsonValueKind.Object && element.TryGetProperty("SessionId", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(sessionId)) return new(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "The active owner did not report a session id.");
        var bound = new Dictionary<string, string>(arguments ?? new Dictionary<string, string>(), StringComparer.Ordinal) { ["session_id"] = sessionId };
        return await TryRequestAsync(installationRoot, new(PublicCapabilityRegistry.ProtocolVersion, command, bound), timeout).ConfigureAwait(false);
    }

    private async Task ListenAsync()
    {
        while (!stop.IsCancellationRequested)
        {
            NamedPipeServerStream pipe;
            try
            {
                // Buffers hold one whole frame in each direction. With zero-quota
                // buffers a client whose frame is rejected before it is fully read
                // stays blocked in its write, and the typed reply deadlocks
                // behind it (runtime closure R04).
                pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, FrameBufferBytes, FrameBufferBytes);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The name is owned by someone else or cannot be created. The
                // owner keeps running; clients see no endpoint and the fault is
                // reported through ListenFault.
                listenFault = exception; return;
            }
            try
            {
                await pipe.WaitForConnectionAsync(stop.Token).ConfigureAwait(false);
                var clientId = Interlocked.Increment(ref nextClientId);
                var task = ServeAsync(pipe);
                clients[clientId] = task;
                _ = task.ContinueWith(completed => { clients.TryRemove(clientId, out var ignored); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            catch (OperationCanceledException) { pipe.Dispose(); }
            catch (IOException) { pipe.Dispose(); }
        }
    }

    // A native command can legitimately occupy the OMSI main-thread mailbox
    // for seconds. Keep accepting status, stop and later runtime requests
    // rather than making the control endpoint disappear behind that request.
    // Every failure, including a handler exception, is answered with a typed
    // error so a client never mistakes it for an absent owner.
    private async Task ServeAsync(NamedPipeServerStream pipe)
    {
        await using (pipe)
        {
            LocalControlResponse response;
            try
            {
                var incoming = await ReadAsync<LocalControlRequest>(pipe, stop.Token).ConfigureAwait(false);
                // Client disconnected before sending anything: nobody to answer.
                if (!incoming.Received) return;
                var request = incoming.Value;
                response = request is null || string.IsNullOrWhiteSpace(request.Command)
                    ? new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_MESSAGE_INVALID", Message: "The control request is empty.")
                    : request.ProtocolVersion != PublicCapabilityRegistry.ProtocolVersion
                    ? new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_PROTOCOL", Message: "Unsupported local control protocol.")
                    : await handler(request).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { return; }
            catch (Exception exception) when (exception is InvalidDataException or JsonException)
            {
                response = new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_MESSAGE_INVALID", Message: "The control request could not be decoded.");
            }
            catch (Exception exception)
            {
                var code = ExtractCode(exception.Message) ?? "OL_E_CONTROL_HANDLER_FAILED";
                response = new LocalControlResponse(false, ErrorCode: code, Message: exception.Message);
            }
            LocalControlResponse? fallback = null;
            using var replyDeadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            replyDeadline.CancelAfter(ReplyWriteTimeout);
            try { await WriteAsync(pipe, response, replyDeadline.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            // A reply that does not fit the frame limit, or cannot be serialized
            // at all, is answered with a small fixed typed error; silence would
            // read as "no active owner".
            catch (InvalidDataException) { fallback = new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_RESPONSE_TOO_LARGE", Message: "The control reply exceeds the 64 KiB frame limit."); }
            catch (Exception exception) when (exception is NotSupportedException or JsonException or InvalidOperationException or ArgumentException) { fallback = new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_HANDLER_FAILED", Message: "The control reply could not be serialized."); }
            if (fallback is not null)
            {
                try { await WriteAsync(pipe, fallback, replyDeadline.Token).ConfigureAwait(false); }
                catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException) { }
            }
        }
    }

    // Replies must fit one frame. Session status and event replies carry a
    // bounded event history; the oldest events are dropped until the reply fits,
    // and the reply states how many were omitted.
    public const int ReplyBudgetBytes = MaxMessageBytes - 4096;
    public static IReadOnlyList<RuntimeEvent> FitEvents(IReadOnlyList<RuntimeEvent> events, int overheadBytes, out int omitted)
    {
        omitted = 0;
        var kept = events;
        while (kept.Count > 0 && JsonSerializer.SerializeToUtf8Bytes(kept).Length + overheadBytes > ReplyBudgetBytes)
        {
            var drop = Math.Max(1, kept.Count / 4);
            kept = kept.Skip(drop).ToArray(); omitted += drop;
        }
        return kept;
    }
    public static SessionStatus FitStatus(SessionStatus status, out int omitted)
    {
        var overhead = JsonSerializer.SerializeToUtf8Bytes(status with { RuntimeEvents = Array.Empty<RuntimeEvent>() }).Length;
        var events = FitEvents(status.RuntimeEvents ?? Array.Empty<RuntimeEvent>(), overhead, out omitted);
        return omitted == 0 ? status : status with { RuntimeEvents = events };
    }
    // Reply metadata for a trimmed event history. Absent when nothing was
    // trimmed. Sequence numbers remain monotonic, so gaps are also visible.
    public static IReadOnlyDictionary<string, string>? TruncationMetadata(IReadOnlyList<RuntimeEvent>? returned, int omitted)
    {
        if (omitted == 0) return null;
        var count = returned?.Count ?? 0;
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["events_truncated"] = "true",
            ["events_returned_count"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["events_dropped_count"] = omitted.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["events_available_count"] = (count + omitted).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        if (count > 0) metadata["events_first_returned_sequence"] = returned![0].Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return metadata;
    }

    private static string? ExtractCode(string message)
    {
        var start = message.IndexOf("OL_E_", StringComparison.Ordinal); if (start < 0) return null;
        var end = start; while (end < message.Length && ((char.IsLetterOrDigit(message[end]) && message[end] < 128) || message[end] == '_')) end++;
        return message[start..end];
    }

    private static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        if (bytes.Length > MaxMessageBytes) throw new InvalidDataException("OL_E_CONTROL_MESSAGE_TOO_LARGE");
        await stream.WriteAsync(BitConverter.GetBytes(bytes.Length), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    // Received=false means the peer closed before a complete frame arrived;
    // Received=true with a null value means the frame decoded to JSON null.
    private static async Task<(bool Received, T? Value)> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[sizeof(int)];
        if (!await ReadExactlyAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false)) return (false, default);
        var length = BitConverter.ToInt32(lengthBytes, 0);
        if (length < 0 || length > MaxMessageBytes) throw new InvalidDataException("OL_E_CONTROL_MESSAGE_INVALID");
        var bytes = new byte[length];
        if (!await ReadExactlyAsync(stream, bytes, cancellationToken).ConfigureAwait(false)) return (false, default);
        return (true, length == 0 ? default : JsonSerializer.Deserialize<T>(bytes));
    }

    private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(read), cancellationToken).ConfigureAwait(false);
            if (count == 0) return false;
            read += count;
        }
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        if (loop is not null) { try { await loop.ConfigureAwait(false); } catch (Exception) { } }
        var pending = clients.Values.ToArray();
        // A faulted client task must not turn disposal into an exception after
        // the session has already restored and closed.
        if (pending.Length != 0) { try { await Task.WhenAll(pending).ConfigureAwait(false); } catch (Exception) { } }
        stop.Dispose();
    }
}
