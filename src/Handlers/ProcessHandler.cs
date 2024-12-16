// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ManagedStrings.Lab;
using ManagedStrings.Engine;
using ManagedStrings.Decoders;
using ManagedStrings.Interop.Windows;
using ManagedStrings.Engine.Console;

namespace ManagedStrings.Handlers;

/// <summary>
/// A process handler that handles string searching on processes virtual memory.
/// </summary>
/// <param name="options">The <see cref="CommandLineOptions"/>.</param>
/// <param name="printer">The <see cref="Printer"/> to print the results.</param>
internal sealed class ProcessHandler(CommandLineOptions options, Printer printer) : Handler(options, printer)
{
    /// <summary>
    /// Handles the current <see cref="CommandLineOptions"/> for one or more processes.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    internal override void Handle(CancellationToken cancellationToken)
        => HandleInternal(false, cancellationToken);

    /// <summary>
    /// Handles the current <see cref="CommandLineOptions"/> for one or more processes
    /// and collects benchmarking information.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>The <see cref="BenchmarkData"/> for the operation.</returns>
    internal override BenchmarkData HandleBenchmark(CancellationToken cancellationToken)
        => HandleInternal(true, cancellationToken)!;

    /// <summary>
    /// Handles the request internally
    /// </summary>
    /// <param name="benchmark">True if we are benchmarking.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private ProcessBenchmarkData? HandleInternal(bool benchmark, CancellationToken cancellationToken)
    {
        if (Printer.IsPrintToFile) {
            WindowsConsole.WriteLine("Searching for strings...");
            WindowsConsole.Flush();
        }

        Stopwatch sw = Stopwatch.StartNew();
        if (Options.PrintHeader)
            Printer.PrintHeader();

        // Getting the total memory size for the progress handler.
        long totalProcessMemorySize = 0;
        foreach (uint processId in Options.ProcessId)
            totalProcessMemorySize += NativeProcess.GetProcessMemorySize(processId, Options.MemoryRegionType);

        // Creating the progress handler.
        using ProgressHandler progressHandler = new(totalProcessMemorySize);

        if (benchmark) {
            ProcessBenchmarkData benchmarkData = new();

            // Handling synchronously.
            if (Options.Synchronous && !Options.RunMultipleItemsAsync) {
                foreach (uint processId in Options.ProcessId)
                    benchmarkData.SingleTimeList.Add(HandleProcess(processId, progressHandler, cancellationToken));

                sw.Stop();
                benchmarkData.TotalTime = sw.Elapsed;

                return benchmarkData;
            }

            // We only do async stuff if we have more than one process.
            if (Options.ProcessId.Count > 1) {

                // Benchmarks shows there is no performance advantage in running multiple items async.
                // This is due to the fact that we are already running the decoding asynchronously, and
                // since we can't write to the console simultaneously the tasks are limited by that.
                // It also messes with the benchmark for individual items, since there's no simple way
                // to control when the tasks are going to run, and where they are going to block.
                // But this application is all about options, so you can choose to run it in parallel.
                if (Options.RunMultipleItemsAsync) {

                    // Using the 'Task.Run()' method takes a long time to start the tasks because the scheduler
                    // will wait for sometime before creating new threads(based on some heuristics).
                    // https://stackoverflow.com/questions/39994896/task-run-takes-much-time-to-start-the-task
                    Task<SingleProcessTime>[] tasks = [.. Options.ProcessId.Select(pid => Task.Factory.StartNew(
                        () => HandleProcess(pid, progressHandler, cancellationToken),
                        cancellationToken,
                        TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning,
                        TaskScheduler.Default
                    ))];

                    Task.WaitAll(tasks, cancellationToken);
                    for (int i = 0; i < tasks.Length; i++)
                        benchmarkData.SingleTimeList.Add(tasks[i].Result);

                    sw.Stop();
                    benchmarkData.TotalTime = sw.Elapsed;

                    return benchmarkData;
                }

                // Running multiple processes synchronously, but with possible async decoding.
                foreach (uint processId in Options.ProcessId)
                    benchmarkData.SingleTimeList.Add(HandleProcess(processId, progressHandler, cancellationToken));

                sw.Stop();
                benchmarkData.TotalTime = sw.Elapsed;

                return benchmarkData;
            }

            // Single process.
            benchmarkData.SingleTimeList.Add(HandleProcess(Options.ProcessId[0], progressHandler, cancellationToken));

            sw.Stop();
            benchmarkData.TotalTime = sw.Elapsed;

            return benchmarkData;
        }

        // No benchmark.
        sw.Stop();
        if (Options.Synchronous) {
            foreach (uint processId in Options.ProcessId)
                HandleProcess(processId, progressHandler, cancellationToken);

            return default;
        }

        if (Options.ProcessId.Count > 1) {
            if (Options.RunMultipleItemsAsync) {
                Task.WaitAll([.. Options.ProcessId.Select(pid => Task.Factory.StartNew(
                    () => HandleProcess(pid, progressHandler, cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                ))]);

                return default;
            }

            foreach (uint processId in Options.ProcessId)
                HandleProcess(processId, progressHandler, cancellationToken);

            return default;
        }

        HandleProcess(Options.ProcessId[0], progressHandler, cancellationToken);

        return default;
    }

    /// <summary>
    /// Handles a single process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="progressHandler">The progress handler.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>The <see cref="SingleProcessTime"/> containing the operation time information.</returns>
    /// <exception cref="EndOfStreamException">The start offset can't be greater or equal to the stream length.</exception>
    private unsafe SingleProcessTime HandleProcess(uint processId, ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch sw = Stopwatch.StartNew();

        // Opening the process. This will collect all the process virtual memory region information according to our options.
        using ProcessStream processStream = new(processId, Options.MemoryRegionType);
        if (Options.MinStringLength == 0)
            return new(processId, processStream.ImagePath, sw.Elapsed);

        // Checking start offset.
        if (Options.StartOffset > 0) {
            if (Options.StartOffset >= processStream.Length)
                throw new EndOfStreamException("The start offset can't be greater or equal to the stream length.");
        }

        // Checking number of bytes to scan.
        long bytesToScan;
        if (Options.BytesToScan > 0) {
            if (Options.BytesToScan >= processStream.Length - Options.StartOffset)
                throw new EndOfStreamException("The number of bytes to read can't be greater or equal to the remaining stream length.");

            bytesToScan = Options.BytesToScan;
        }
        else
            bytesToScan = processStream.Length - Options.StartOffset;

        // Checking the buffer size.
        int bufferSize = Options.BufferSize;
        if (bufferSize > processStream.Length)
            bufferSize = (int)processStream.Length;

        // Since UTF8 is compatible with ASCII if we have both encodings we're going to print repeated strings.
        // To avoid caching strings and performing comparisons we just default to UTF8.
        // If the dude chooses just ASCII or ASCII and Unicode the choice will be preserved.
        ValidEncoding runEncoding;
        if (Options.Encoding.HasEncodingFlag(ValidEncoding.ASCII) && Options.Encoding.HasEncodingFlag(ValidEncoding.UTF8))
            runEncoding = ValidEncoding.UTF8 | (Options.Encoding & ValidEncoding.Unicode);
        else
            runEncoding = Options.Encoding;

        // Creating the decode information list for each encoding.
        List<DecodeInformation> workInfo = [];
        foreach (ValidEncoding encoding in Enum.GetValues<ValidEncoding>()) {
            if (encoding == ValidEncoding.All)
                continue;

            if ((runEncoding & encoding) != 0)
                workInfo.Add(new(Options.MinStringLength, 0, Options.ExcludeControlCp, Printer.IsUnicode, 0, encoding, GetDecoder(encoding)));
        }

        // In case the user doesn't want offset or process specific info we construct this only once.
        ProcessStreamOffsetInfo defaultOffsetInfo = new(null, 0) {
            Image = processStream.ImagePath,
            ProcessId = processStream.ProcessId
        };

        long totalRead = 0;
        byte[] buffer = new byte[bufferSize];
        bool synchronous = Options.Synchronous;
        processStream.Position = Options.StartOffset;
        do {
            cancellationToken.ThrowIfCancellationRequested();

            // Read into the buffer.
            int currentReadCount = processStream.Read(buffer, 0, buffer.Length);
            if (currentReadCount == 0)
                break;

            // Getting the remaining and incrementing the total read.
            long remainingBytes = processStream.Length - totalRead;
            if (currentReadCount > remainingBytes)
                currentReadCount = (int)remainingBytes;

            totalRead += currentReadCount;

            // Zeroing the decode information for the new run.
            for (int i = 0; i < workInfo.Count; i++) {
                workInfo[i].Offset = 0;
                workInfo[i].BytesRead = 0;
                workInfo[i].IsRunning = true;
            }

            // Processing the current buffer.
            fixed (byte* bufferPtr = buffer) {
                if (synchronous)
                    ProcessBuffer(processStream, defaultOffsetInfo, bufferPtr, currentReadCount, workInfo, cancellationToken);
                else
                    Task.WaitAll(ProcessBufferAsync(processStream, defaultOffsetInfo, bufferPtr, currentReadCount, workInfo, cancellationToken), cancellationToken);
            }

            // Reporting the progress.
            progressHandler.IncrementProgress(currentReadCount);

        } while (totalRead < bytesToScan);

        sw.Stop();

        return new(processId, processStream.ImagePath, sw.Elapsed);
    }

    /// <summary>
    /// Processes a single buffer run.
    /// </summary>
    /// <param name="processStream">The <see cref="ProcessStream"/>.</param>
    /// <param name="offsetInfo">The <see cref="ProcessStreamOffsetInfo"/>.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="info">The list of <see cref="DecodeInformation"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    private unsafe void ProcessBuffer(ProcessStream processStream, ProcessStreamOffsetInfo offsetInfo, byte* buffer, int bufferLength, List<DecodeInformation> info, CancellationToken cancellationToken)
    {
        bool running;
        do {
            cancellationToken.ThrowIfCancellationRequested();

            running = false;

            // Running the decoding for each encoding for the current buffer offset.
            foreach (DecodeInformation runInfo in info) {
                int currentBytesRead = 0;
                if (runInfo.IsRunning)
                    ProcessBufferOffset(processStream, offsetInfo, buffer, bufferLength, runInfo, ref runInfo.RelativeOffset, out currentBytesRead, cancellationToken);

                runInfo.BytesRead += currentBytesRead;
                bool isCurrentRunning = runInfo.BytesRead < bufferLength;
                runInfo.IsRunning = isCurrentRunning;
                running |= isCurrentRunning;
            }

            // Unreal how slow this is.
            // running = info.Any(i => i.IsRunning);

        } while (running);
    }

    /// <summary>
    /// Processes a single buffer run asynchronously.
    /// </summary>
    /// <param name="processStream">The <see cref="ProcessStream"/>.</param>
    /// <param name="offsetInfo">The <see cref="ProcessStreamOffsetInfo"/>.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="info">The list of <see cref="DecodeInformation"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>An array of <see cref="Task"/> representing the decoding operation for each encoding.</returns>
    private unsafe Task[] ProcessBufferAsync(ProcessStream processStream, ProcessStreamOffsetInfo offsetInfo, byte* buffer, int bufferLength, List<DecodeInformation> info, CancellationToken cancellationToken)
    {
        return [.. info.Select(GetProcessingTask)];

        Task GetProcessingTask(DecodeInformation workInfo) => Task.Run(() => {
            int bytesRead = 0;
            long ourRelativeOffset = workInfo.RelativeOffset;
            do {
                if (cancellationToken.IsCancellationRequested)
                    return;

                ProcessBufferOffset(processStream, offsetInfo, buffer, bufferLength, workInfo, ref ourRelativeOffset, out int currentBytesRead, cancellationToken);
                bytesRead += currentBytesRead;
            }
            while (bytesRead < bufferLength);

            Interlocked.Exchange(ref workInfo.RelativeOffset, ourRelativeOffset);
        }, cancellationToken);
    }

    /// <summary>
    /// Processes a buffer run offset.
    /// </summary>
    /// <param name="processStream">The <see cref="ProcessStream"/>.</param>
    /// <param name="offsetInfo">The <see cref="ProcessStreamOffsetInfo"/>.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="decodeInformation">The current <see cref="DecodeInformation"/>.</param>
    /// <param name="relativeOffset">The output relative offset for this run.</param>
    /// <param name="currentBytesRead">The output number of bytes read for this run.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ProcessBufferOffset(ProcessStream processStream, ProcessStreamOffsetInfo offsetInfo, byte* buffer, int bufferLength, DecodeInformation decodeInformation, 
        ref long relativeOffset, out int currentBytesRead, CancellationToken cancellationToken)
    {
        // Getting the offset info is expensive because we might perform a binary search.
        // If the user doesn't want to print offset or process specific information we don't get it.
        if (Options.ProcessInfoType.HasPrintProcessInfoFlag(PrintProcessInfoType.MemoryType) ||
            Options.ProcessInfoType.HasPrintProcessInfoFlag(PrintProcessInfoType.Details) ||
            Options.PrintOffset) {
            processStream.GetRelativeOffsetInfo(decodeInformation.RelativeOffset, out offsetInfo);
        }

        if (decodeInformation.Decoder.TryGetString(buffer, bufferLength, decodeInformation, out string? outputString, out currentBytesRead, out int currentStringBytesRead, cancellationToken)) {
            if (!string.IsNullOrWhiteSpace(outputString) && IsMatch(outputString!))
                Printer.Print(new ProcessResult(offsetInfo, decodeInformation.Encoding, currentStringBytesRead, outputString!));
        }

        relativeOffset += currentBytesRead;
    }
}