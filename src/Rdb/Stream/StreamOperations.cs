using System.Collections.Concurrent;
using codecrafters_redis.Rdb.Records;

namespace codecrafters_redis.Rdb.Stream;

internal sealed class StreamOperations(ConcurrentDictionary<string, Record> records) : IDisposable
{
    private const string SpecialId = "$";

    private readonly ConcurrentDictionary<string, StreamWaitQueue> _streamWaiters = new();
    private bool _disposed;

    public StreamEntryId AddStreamEntry(string streamKey, string entryIdString, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(streamKey);
        ArgumentNullException.ThrowIfNull(fields);

        records.TryGetRecord<StreamRecord>(streamKey, out var stream);

        var entryId = StreamEntryId.Create(entryIdString, stream?.LastEntryId);

        if (entryId == StreamEntryId.Zero)
            throw new ArgumentException("The ID specified in XADD must be greater than 0-0");

        if (stream?.LastEntryId != null && entryId <= stream.LastEntryId)
            throw new ArgumentException("The ID specified in XADD is equal or smaller than the target stream top item");

        if (stream != null)
        {
            if (!stream.TryAppendEntry(entryId, fields))
                throw new InvalidOperationException("Failed to append entry to stream");
        }
        else
        {
            var newStream = StreamRecord.Create(streamKey, entryId, fields);
            records.TryAdd(streamKey, newStream);
            stream = newStream;
        }

        if (_streamWaiters.TryGetValue(streamKey, out var waitQueue))
            waitQueue.NotifyUpdate(streamKey, stream);

        return entryId;
    }

    public async Task<StreamReadResult?> Get(IReadOnlyList<StreamReadRequest> requests, TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requests);

        using var cts = new CancellationTokenSource(timeout);
        var waitTasks = new List<Task<StreamResult?>>();

        foreach (var request in requests)
        {
            var waitQueue = _streamWaiters.GetOrAdd(request.StreamKey, _ => new StreamWaitQueue());
            var startId = GetStreamStartId(request);

            waitTasks.Add(waitQueue.WaitForUpdate(request.StreamKey, startId, cts.Token));
        }

        var streams = Get(requests);
        if (streams != null)
        {
            await cts.CancelAsync();
            return streams;
        }

        try
        {
            var completedTask = await Task.WhenAny(waitTasks);
            var result = await completedTask;

            return result != null ? new StreamReadResult([result]) : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            await cts.CancelAsync();
        }
    }

    private StreamReadResult? Get(IReadOnlyList<StreamReadRequest> requests)
    {
        var results = new List<StreamResult>();

        foreach (var request in requests)
        {
            if (!records.TryGetRecord<StreamRecord>(request.StreamKey, out var stream))
                continue;

            var startId = request.StartId == SpecialId ? stream.LastEntryId ?? StreamEntryId.Zero : StreamEntryId.Create(request.StartId);

            var entries = stream.GetEntriesAfter(startId);
            if (entries.Count > 0)
                results.Add(new StreamResult(request.StreamKey, entries));
        }

        return results.Count > 0 ? new StreamReadResult(results) : null;
    }

    private StreamEntryId GetStreamStartId(StreamReadRequest request)
    {
        if (request.StartId == SpecialId)
            return records.TryGetRecord<StreamRecord>(request.StreamKey, out var stream) ? stream.LastEntryId ?? StreamEntryId.Zero : StreamEntryId.Zero;

        return StreamEntryId.Create(request.StartId);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        foreach (var waitQueue in _streamWaiters.Values)
            waitQueue.Dispose();

        _streamWaiters.Clear();
    }
}