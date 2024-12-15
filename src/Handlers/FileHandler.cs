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
/// A file handler that handles string searching on files.
/// </summary>
/// <param name="options">The <see cref="CommandLineOptions"/>.</param>
/// <param name="printer">The <see cref="Printer"/> to print the results.</param>
internal sealed class FileHandler(CommandLineOptions options, Printer printer) : Handler(options, printer)
{
    /// <summary>
    /// Handles the current <see cref="CommandLineOptions"/> for one or more files.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    internal override void Handle(CancellationToken cancellationToken)
        => HandleInternal(false, cancellationToken);

    /// <summary>
    /// Handles the current <see cref="CommandLineOptions"/> for one or more files
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
    private FileBenchmarkData? HandleInternal(bool benchmark, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Options.Mode == Mode.Process)
            throw new ArgumentException($"Invalid mode '{Options.Mode}'.");

#pragma warning disable CA2208

        if (Options.FSInfo is null)
            throw new ArgumentNullException(nameof(Options.FSInfo), "FSInfo can't be null.");

#pragma warning restore CA2208

        if (Printer.IsPrintToFile) {
            WindowsConsole.WriteLine("Searching for strings...");
            WindowsConsole.Flush();
        }

        Stopwatch sw = Stopwatch.StartNew();
        List<FileMinimalInformation> files;
        if (Options.Mode == Mode.Directory || Options.Mode == Mode.FileSystemWildcard) {

            // Listing the files if we are a directory or contains wildcards.
            files = NativeIO.GetDirectoryFiles(Options.FSInfo!, Options.Recurse, cancellationToken);
        }
        else {
            // Single file.
            files = [new(Options.FSInfo.FullName, NativeIO.GetFileSize(Options.FSInfo.FullName))];
        }

        // Creating the progress handler.
        using ProgressHandler progressHandler = new(files.Sum(i => i.EndOfFile));

        if (Options.PrintHeader)
            Printer.PrintHeader();

        if (benchmark) {
            FileBenchmarkData benchmarkData = new();

            // Handling synchronously.
            if (Options.Synchronous && !Options.RunMultipleItensAsync) {
                foreach (FileMinimalInformation file in files)
                    benchmarkData.SingleTimeList.Add(HandleFile(file.FullName, progressHandler, cancellationToken));

                sw.Stop();
                benchmarkData.TotalTime = sw.Elapsed;

                return benchmarkData;
            }

            // We only do async stuff if we have more than one file.
            if (files.Count > 1) {

                // Benchmarks shows there is no performance advantage in running multiple items async.
                // This is due to the fact that we are already running the decoding asynchronously, and
                // since we can't write to the console simultaneously the tasks are limited by that.
                // It also messes with the benchmark for individual items, since there's no simple way
                // to control when the tasks are going to run, and where they are going to block.
                // But this application is all about options, so you can choose to run it in parallel.
                if (Options.RunMultipleItensAsync) {

                    // Using the 'Task.Run()' method takes a long time to start the tasks because the scheduler
                    // will wait for sometime before creating new threads(based on some heuristics).
                    // https://stackoverflow.com/questions/39994896/task-run-takes-much-time-to-start-the-task
                    Task<SingleFileTime>[] tasks = [.. files.Select(f => Task.Factory.StartNew(
                        () => HandleFile(f.FullName, progressHandler, cancellationToken),
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

                // Running multiple files synchronously, but with possible async decoding.
                foreach (FileMinimalInformation file in files)
                    benchmarkData.SingleTimeList.Add(HandleFile(file.FullName, progressHandler, cancellationToken));

                sw.Stop();
                benchmarkData.TotalTime = sw.Elapsed;

                return benchmarkData;
            }

            // Single file.
            benchmarkData.SingleTimeList.Add(HandleFile(files[0].FullName, progressHandler, cancellationToken));

            sw.Stop();
            benchmarkData.TotalTime = sw.Elapsed;

            return benchmarkData;
        }

        // No benchmark.
        sw.Stop();
        if (Options.Synchronous) {
            foreach (FileMinimalInformation file in files)
                HandleFile(file.FullName, progressHandler, cancellationToken);

            return default;
        }

        if (files.Count > 1) {
            if (Options.RunMultipleItensAsync) {
                Task.WaitAll([.. files.Select(f => Task.Factory.StartNew(
                    () => HandleFile(f.FullName, progressHandler, cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                ))]);

                return default;
            }

            foreach (FileMinimalInformation file in files)
                HandleFile(file.FullName, progressHandler, cancellationToken);

            return default;
        }

        HandleFile(files[0].FullName, progressHandler, cancellationToken);

        return default;
    }

    /// <summary>
    /// Handles a single file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="progressHandler">The progress handler.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>The <see cref="SingleFileTime"/> containing the operation time information.</returns>
    /// <exception cref="EndOfStreamException">The start offset can't be greater or equal to the stream length.</exception>
    private unsafe SingleFileTime HandleFile(string path, ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch sw = Stopwatch.StartNew();

        // If you are stupid like me you're going to save the output file in the same folder you're scanning.
        // In this case the constructor down there is going to throw an exception.
        if (path.Equals(Options.OutFile, StringComparison.InvariantCultureIgnoreCase)) {
            sw.Stop();
            return new(path, sw.Elapsed);
        }

        // Opening the file.
        using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

        // Checking start offset.
        if (Options.StartOffset > 0) {
            if (Options.StartOffset >= fileStream.Length)
                throw new EndOfStreamException("The start offset can't be greater or equal to the stream length.");
        }

        // Checking number of bytes to scan.
        long bytesToScan;
        if (Options.BytesToScan > 0) {
            if (Options.BytesToScan >= fileStream.Length - Options.StartOffset)
                throw new EndOfStreamException("The number of bytes to read can't be greater or equal to the remaining stream length.");

            bytesToScan = Options.BytesToScan;
        }
        else
            bytesToScan = fileStream.Length - Options.StartOffset;

        // Storing the file name according to the options.
        string file = Options.PrintFileName switch {
            PrintFileType.Relative => Options.Mode switch {
                Mode.SingleFile => path,
                Mode.Directory => path.Replace(Options.Path!, "").TrimStart('\\'),
                _ => string.Empty
            },
            PrintFileType.FullPath => path,
            _ => Path.GetFileName(path),
        };

        // Checking the buffer size.
        int bufferSize = Options.BufferSize;
        if (bufferSize > fileStream.Length)
            bufferSize = (int)fileStream.Length;

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

        long totalRead = 0;
        byte[] buffer = new byte[bufferSize];
        bool synchronous = Options.Synchronous;
        fileStream.Position = Options.StartOffset;
        do {
            cancellationToken.ThrowIfCancellationRequested();

            // Read into the buffer.
            int currentReadCount = fileStream.Read(buffer, 0, buffer.Length);
            if (currentReadCount == 0)
                break;

            // Getting the remaining and incrementing the total read.
            long remainingBytes = fileStream.Length - totalRead;
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
                    ProcessBuffer(file, bufferPtr, currentReadCount, workInfo, cancellationToken);
                else
                    Task.WaitAll(ProcessBufferAsync(file, bufferPtr, currentReadCount, workInfo, cancellationToken), cancellationToken);
            }

            // Reporting the progress.
            progressHandler.IncrementProgress(currentReadCount);
            
        } while (totalRead < bytesToScan);

        sw.Stop();

        return new(path, sw.Elapsed);
    }

    /// <summary>
    /// Processes a single buffer run.
    /// </summary>
    /// <param name="file">The file name.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="info">The list of <see cref="DecodeInformation"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    private unsafe void ProcessBuffer(string file, byte* buffer, int bufferLength, List<DecodeInformation> info, CancellationToken cancellationToken)
    {
        bool running;
        do {
            cancellationToken.ThrowIfCancellationRequested();

            running = false;

            // Running the decoding for each encoding for the current buffer offset.
            foreach (DecodeInformation runInfo in info) {
                int currentBytesRead = 0;
                if (runInfo.IsRunning)
                    ProcessBufferOffset(file, buffer, bufferLength, runInfo, ref runInfo.RelativeOffset, out currentBytesRead, cancellationToken);

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
    /// <param name="file">The file name.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="info">The list of <see cref="DecodeInformation"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>An array of <see cref="Task"/> representing the decoding operation for each encoding.</returns>
    private unsafe Task[] ProcessBufferAsync(string file, byte* buffer, int bufferLength, List<DecodeInformation> info, CancellationToken cancellationToken)
    {
        return [.. info.Select(GetProcessingTask)];

        Task GetProcessingTask(DecodeInformation workInfo) => Task.Run(() => {
            int bytesRead = 0;
            long ourRelativeOffset = workInfo.RelativeOffset;
            do {
                if (cancellationToken.IsCancellationRequested)
                    return;

                ProcessBufferOffset(file, buffer, bufferLength, workInfo, ref ourRelativeOffset, out int currentBytesRead, cancellationToken);
                bytesRead += currentBytesRead;
            }
            while (bytesRead < bufferLength);

            Interlocked.Exchange(ref workInfo.RelativeOffset, ourRelativeOffset);
        }, cancellationToken);
    }

    /// <summary>
    /// Processes a buffer run offset.
    /// </summary>
    /// <param name="file">The file name.</param>
    /// <param name="buffer">The buffer pointer.</param>
    /// <param name="bufferLength">The buffer length.</param>
    /// <param name="decodeInformation">The current <see cref="DecodeInformation"/>.</param>
    /// <param name="relativeOffset">The output relative offset for this run.</param>
    /// <param name="currentBytesRead">The output number of bytes read for this run.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ProcessBufferOffset(string file, byte* buffer, int bufferLength, DecodeInformation decodeInformation,
        ref long relativeOffset, out int currentBytesRead, CancellationToken cancellationToken)
    {
        long startOffset = relativeOffset;
        if (decodeInformation.Decoder.TryGetString(buffer, bufferLength, decodeInformation, out string? outputString, out currentBytesRead, out int currentStringBytesRead, cancellationToken)) {
            if (!string.IsNullOrWhiteSpace(outputString) && IsMatch(outputString))
                Printer.Print(new FileResult(file, decodeInformation.Encoding, startOffset, relativeOffset + currentStringBytesRead, outputString));
        }

        relativeOffset += currentBytesRead;
    }
}