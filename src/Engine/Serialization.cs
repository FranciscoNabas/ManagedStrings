// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Xml;
using System.Text.Json;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ManagedStrings.Engine;

namespace ManagedStrings.Serialization;

// The main motivation to implement these serializers is because we use AOT
// and trimming to make our program smaller. Using these measures we managed to
// go from 82Mb to only 4Mb.
// The problem is that AOT and trimming have a lot of limitations, specially with
// reflection, which the serializing libraries use heavily.
// For JSON we can implement source generation, but XML is a little more rudimentary.
// The solution is similar, though, and we can fix it implementing the 'IXmlSerializable'.
// References:
//   https://learn.microsoft.com/dotnet/core/deploying/native-aot
//   https://learn.microsoft.com/dotnet/core/deploying/trimming/incompatibilities
//   https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation

/// <summary>
/// A result collection XML serializer.
/// </summary>
/// <param name="results">The results collection.</param>
internal sealed class ResultCollectionXmlSerializer(ResultCollection results) : IXmlSerializable
{
    private readonly ResultCollection m_results = results;

    /// <summary>
    /// Serializes the result collection to a <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="results">The result collection.</param>
    /// <param name="output">The serializer output.</param>
    /// <remarks>
    /// We implement a static method to abstract the serializer creation.
    /// </remarks>
    internal static void Serialize(ResultCollection results, TextWriter output)
    {
        XmlWriterSettings settings = new() {
            Indent = true
        };

        using XmlWriter writer = XmlWriter.Create(output, settings);
        ResultCollectionXmlSerializer serializer = new(results);
        serializer.WriteXml(writer);
    }

    // We don't use schema and only need to write.
    public XmlSchema? GetSchema()
        => null;

    public void ReadXml(XmlReader reader)
        => throw new NotImplementedException();

    /// <summary>
    /// Writes the serialized data.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("ManagedStringsResult");
        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        foreach (Result result in m_results) {
            writer.WriteStartElement("Result");
            
            // File specific data.
            if (result is FileResult fileResult) {
                writer.WriteAttributeString("type", "http://www.w3.org/2001/XMLSchema-instance", "FileResult");
                writer.WriteElementString("Flie", fileResult.File);
            }

            // Process specific data.
            else if (result is ProcessResult processResult) {
                writer.WriteAttributeString("type", "http://www.w3.org/2001/XMLSchema-instance", "ProcessResult");
                writer.WriteElementString("ProcessId", processResult.ProcessId.ToString());
                writer.WriteElementString("Name", processResult.Name);
                writer.WriteElementString("RegionType", processResult.RegionType.ToString());
                writer.WriteElementString("Details", processResult.Details);
            }

            writer.WriteElementString("Encoding", result.Encoding.ToString());
            writer.WriteElementString("OffsetStart", result.OffsetStart.ToString());
            writer.WriteElementString("OffsetEnd", result.OffsetEnd.ToString());
            writer.WriteElementString("String", result.ResultString);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
    }
}

/// <summary>
/// A Results JSON serializer context.
/// </summary>
/// <seealso href="https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation">How to use source generation in System.Text.Json</seealso>
[JsonSerializable(typeof(ResultCollection))]
[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Serialization, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class ResultContext : JsonSerializerContext { }

/// <summary>
/// JSON extensions.
/// </summary>
/// <remarks>
/// There are some known issues with Native AOT and JSON code generation where
/// the compiler still emmits warnings about using serialization APIs.
/// This extension makes sure we use the code generation, and silences the compiler.
/// </remarks>
/// <seealso href="https://stackoverflow.com/a/78649561/10234464"/>
internal static partial class JsonExtensions
{
    // The serialization options.
    // Making this static so we don't have to create one every time we serialize a result collection.
    private static readonly JsonSerializerOptions s_resultSerializerOptions = new() {
        TypeInfoResolver = new ResultContext(new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        }),

        Converters = { new ResultListConverter() },
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes a result collection to a JSON string.
    /// </summary>
    /// <param name="value">The result collection.</param>
    /// <returns>The JSON serialized string.</returns>
    internal static string SerializeResultCollection(ResultCollection value)
        => JsonSerializer.Serialize(value, (JsonTypeInfo<ResultCollection>)s_resultSerializerOptions.GetTypeInfo(typeof(ResultCollection)));
}

/// <summary>
/// Converts a file result to-and-frow.
/// </summary>
public class FileResultConverter : JsonConverter<FileResult>
{
    /// <summary>
    /// Desserializes a JSON reader into a <see cref="FileResult"/>.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The result <see cref="FileResult"/>.</returns>
    public override FileResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        FileResult result = new();
        while (reader.Read()) {
            switch (reader.TokenType) {
                case JsonTokenType.PropertyName:
                    switch (reader.GetString()?.ToUpper()) {
                        case "ENCODING":
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.String)
                                result.Encoding = Enum.Parse<ValidEncoding>(reader.GetString()!);
                            else
                                result.Encoding = (ValidEncoding)reader.GetUInt32();

                            break;

                        case "OFFSETSTART":
                            reader.Read();
                            result.OffsetStart = reader.GetInt64();
                            break;

                        case "OFFSETEND":
                            reader.Read();
                            result.OffsetEnd = reader.GetInt64();
                            break;

                        case "STRING":
                            reader.Read();
                            result.ResultString = reader.GetString()!;
                            break;

                        case "FILE":
                            reader.Read();
                            result.File = reader.GetString()!;
                            break;
                    }

                    break;

                default:
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Serializes a <see cref="FileResult"/> into a JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The file result to serialize.</param>
    /// <param name="options">The serialization options.</param>
    public override void Write(Utf8JsonWriter writer, FileResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("File", value.File);
        writer.WriteString("Encoding", value.Encoding.ToString());
        writer.WriteNumber("OffsetStart", value.OffsetStart);
        writer.WriteNumber("OffsetEnd", value.OffsetEnd);
        writer.WriteString("String", value.ResultString);
        writer.WriteEndObject();
        writer.Flush();
    }
}

/// <summary>
/// Converts a process result to-and-frow.
/// </summary>
public class ProcessResultConverter : JsonConverter<ProcessResult>
{
    /// <summary>
    /// Desserializes a JSON reader into a <see cref="ProcessResult"/>.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The result <see cref="ProcessResult"/>.</returns>
    public override ProcessResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ProcessResult result = new();
        while (reader.Read()) {
            switch (reader.TokenType) {
                case JsonTokenType.PropertyName:
                    switch (reader.GetString()?.ToUpper()) {
                        case "ENCODING":
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.String)
                                result.Encoding = Enum.Parse<ValidEncoding>(reader.GetString()!);
                            else
                                result.Encoding = (ValidEncoding)reader.GetUInt32();

                            break;

                        case "OFFSETSTART":
                            reader.Read();
                            result.OffsetStart = reader.GetInt64();
                            break;

                        case "OFFSETEND":
                            reader.Read();
                            result.OffsetEnd = reader.GetInt64();
                            break;

                        case "STRING":
                            reader.Read();
                            result.ResultString = reader.GetString()!;
                            break;

                        case "PROCESSID":
                            reader.Read();
                            result.ProcessId = reader.GetUInt32();
                            break;

                        case "NAME":
                            reader.Read();
                            result.Name = reader.GetString()!;
                            break;

                        case "REGIONTYPE":
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.String)
                                result.RegionType = Enum.Parse<MemoryRegionType>(reader.GetString()!);
                            else
                                result.RegionType = (MemoryRegionType)reader.GetUInt32();

                            break;

                        case "DETAILS":
                            reader.Read();
                            result.Details = reader.GetString()!;
                            break;
                    }

                    break;

                default:
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Serializes a <see cref="ProcessResult"/> into a JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The process result to serialize.</param>
    /// <param name="options">The serialization options.</param>
    public override void Write(Utf8JsonWriter writer, ProcessResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("ProcessId", value.ProcessId);
        writer.WriteString("Name", value.Name);
        writer.WriteString("RegionType", value.RegionType.ToString());
        writer.WriteString("Details", value.Details);
        writer.WriteString("Encoding", value.Encoding.ToString());
        writer.WriteNumber("OffsetStart", value.OffsetStart);
        writer.WriteNumber("OffsetEnd", value.OffsetEnd);
        writer.WriteString("String", value.ResultString);
        writer.WriteEndObject();
        writer.Flush();
    }
}

/// <summary>
/// Converts a result collection to and from JSON.
/// </summary>
public class ResultListConverter : JsonConverter<ResultCollection>
{
    private static readonly FileResultConverter s_fileConverter = new();
    private static readonly ProcessResultConverter s_processConverter = new();

    // We don't use the desserialization, so I don't want to go through the hassle.
    public override ResultCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotImplementedException();

    /// <summary>
    /// Serializes a <see cref="ResultCollection"/> into a JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The result collection to serialize.</param>
    /// <param name="options">The serialization options.</param>
    public override void Write(Utf8JsonWriter writer, ResultCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (Result result in value) {
            if (result is FileResult fileResult)
                s_fileConverter.Write(writer, fileResult, options);
            else if (result is ProcessResult processResult)
                s_processConverter.Write(writer, processResult, options);
        }
        writer.WriteEndArray();
        writer.Flush();
    }
}