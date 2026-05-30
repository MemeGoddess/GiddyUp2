using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy.Json;
using Newtonsoft.Json.Linq;
using Verse;
using static UnityEngine.Networking.UnityWebRequest;

namespace GiddyUpCore.MCP;

internal static class RimWorldMcpServer
{
    private const int Port = 5053;
    private const string EndpointPath = "/mcp";
    private static readonly object SyncRoot = new();
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    private static TcpListener? listener;
    private static bool started;

    private static Dictionary<string, MCPTool> tools = new();

    public static void Start()
    {
        lock (SyncRoot)
        {
            if (started)
                return;

            tools = GetTools();

            try
            {
                listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Start();
                started = true;
                _ = Task.Run(ListenLoopAsync);
                Log.Message($"[Giddy-Up] MCP server listening on http://127.0.0.1:{Port}{EndpointPath}");
            }
            catch (Exception exception)
            {
                started = false;
                listener = null;
                Log.Warning($"[Giddy-Up] Failed to start MCP server on port {Port}: {exception}");
            }
        }
    }

    private static Dictionary<string, MCPTool> GetTools()
    {
        var list = new List<MCPTool>();
        foreach (var type in GenTypes.AllTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!method.TryGetAttribute<MCPTool>(out var tool))
                    continue;
                tool.Tool = method;
                list.Add(tool);
            }
        }

        return list.ToDictionary(
            x => x.Name.ToLower(),
            x => x);
    }

    private static async Task ListenLoopAsync()
    {
        while (listener != null)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                client?.Dispose();
                Log.Warning($"[Giddy-Up] MCP listener stopped unexpectedly: {exception}");
                return;
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            client.ReceiveTimeout = 5000;
            client.SendTimeout = 5000;

            try
            {
                var request = await ReadRequestAsync(stream).ConfigureAwait(false);
                if (request == null)
                    return;


                if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, 204, "No Content", "text/plain", string.Empty).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    var body = new JObject
                    {
                        ["name"] = "Giddy-Up MCP",
                        ["endpoint"] = $"http://127.0.0.1:{Port}{EndpointPath}",
                        ["status"] = "ok"
                    };
                    await WriteResponseAsync(stream, 200, "OK", "application/json; charset=utf-8", body.ToString()).ConfigureAwait(false);
                    return;
                }

                if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, 405, "Method Not Allowed", "text/plain; charset=utf-8",
                        "Only GET, POST, and OPTIONS are supported.").ConfigureAwait(false);
                    return;
                }

                if (!string.Equals(request.Path, EndpointPath, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(request.Path, "/", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, 404, "Not Found", "text/plain; charset=utf-8",
                        "Unknown MCP endpoint.").ConfigureAwait(false);
                    return;
                }

                JObject? payload;
                try
                {
                    payload = JObject.Parse(request.Body, new JsonLoadSettings());
                }
                catch (Exception exception)
                {
                    Log.Warning($"[Giddy-Up] MCP request JSON parse failed: {exception.Message}");
                    await WriteResponseAsync(stream, 400, "Bad Request", "text/plain; charset=utf-8",
                        "Invalid JSON payload.").ConfigureAwait(false);
                    return;
                }

                if (payload == null)
                {
                    await WriteResponseAsync(stream, 400, "Bad Request", "text/plain; charset=utf-8",
                        "JSON payload must be an object.").ConfigureAwait(false);
                    return;
                }

                if (!payload.TryGetValue("id", out var requestId) || requestId == null)
                {
                    await WriteResponseAsync(stream, 202, "Accepted", "application/json; charset=utf-8",
                        new JObject { ["accepted"] = true }.ToString()).ConfigureAwait(false);
                    return;
                }

                if (!int.TryParse(requestId.ToString(), out var requestIdInt))
                {
                    await WriteResponseAsync(stream, 202, "Accepted", "application/json; charset=utf-8",
                        new JObject { ["accepted"] = true }.ToString()).ConfigureAwait(false);
                    return;
                }
                var response = BuildRpcResponse(payload, requestIdInt);
                await WriteResponseAsync(stream, 200, "OK", "application/json; charset=utf-8", response.ToString(), requestId.ToString())
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warning($"[Giddy-Up] MCP client handling failed: {exception}");
            }
        }
    }

    private static JObject BuildRpcResponse(JObject payload, int requestId)
    {
        var method = payload.TryGetValue("method", out var rawMethod) ? rawMethod.ToString() : null;
        if (string.IsNullOrWhiteSpace(method))
            return Error(requestId, -32600, "Invalid Request");

        switch (method)
        {
            case "initialize":
                return Result(requestId, method, new JObject
                {
                    ["protocolVersion"] = "2025-03-26",
                    ["capabilities"] = new JObject
                    {
                        ["logging"] = new JObject(),
                        ["prompts"] = new JObject(),
                        ["resources"] = new JObject(),
                        ["tools"] = new JObject()
                    },
                    ["serverInfo"] = new JObject
                    {
                        ["name"] = "GiddyUp2.RimWorldMcp",
                        ["version"] = "1.0.0"
                    }
                });

            case "ping":
                return Result(requestId, method, new JObject());

            case "tools/list":
                return Result(requestId, method, new JObject()
                {
                    ["tools"] = JArray.FromObject(tools.Select(entry =>
                        new JObject
                        {
                            ["name"] = entry.Key,
                            ["title"] = entry.Value.Title,
                            ["description"] = entry.Value.Description,
                            ["outputSchema"] = entry.Value.OutputSchema
                        }))
                });

            case "tools/call":
                if (!TryGetToolName(payload, out var toolName))
                    return Error(requestId, -32602, "Missing tool name.");

                if (!tools.TryGetValue(toolName!.ToLowerInvariant(), out var tool))
                    return Error(requestId, -32602, $"Unknown tool '{toolName}'.");
                // TODO Input
                if (tool.Tool.ReturnType != typeof(void))
                    return Result(requestId, method, BuildToolResult(RunToolWithReturn(tool)));

                RunTool(tool);
                return Result(requestId, method, BuildToolResult($"Successfully ran '{tool.Name}'"));

            case "notifications/initialized":
                return Result(requestId, method, new JObject());

            case "logging/setLevel":
                return Result(requestId, method, new JObject());

            case "prompts/list":
                return Result(requestId, method, new JObject()
                {
                    ["prompts"] = new JObject()
                });


            default:
                return Error(requestId, -32601, $"Method '{method}' not found.");
        }
    }

    private static void RunTool(MCPTool tool)
    {
        try
        {
            MainThreadInvoker.InvokeOnMainThread(new Task(() => tool.Tool.Invoke(null, null))).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new Exception("MCP Server Error", ex);
        }
    }

    private static object RunToolWithReturn(MCPTool tool)
    {
        try
        {
            var task = new Task<object>(() => tool.Tool.Invoke(null, null));
            MainThreadInvoker.InvokeOnMainThread(task).GetAwaiter().GetResult();

            return task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new Exception("MCP Server Error", ex);
        }
    }

    private static bool TryGetToolName(JObject payload, out string? toolName)
    {
        toolName = null;
        if (!payload.TryGetValue("params", out var rawParams) || rawParams is not JObject parameters)
            return false;

        if (!parameters.TryGetValue("name", out var rawName))
            return false;

        toolName = rawName.ToString();
        return !string.IsNullOrWhiteSpace(toolName);
    }

    private static JObject Result(int requestId, string method, JObject result)
    {
        return new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = requestId,
            ["result"] = result,
            ["method"] = method
        };
    }

    private static JObject BuildToolResult(object? value)
    {
        var token = value == null
            ? JValue.CreateNull()
            : JToken.FromObject(value);

        var text = token.Type == JTokenType.String
            ? token.ToString()
            : token.ToString(Newtonsoft.Json.Formatting.Indented);

        var result = new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };

        if (value != null)
            result["structuredContent"] = token;

        return result;
    }

    private static JObject Success(int requestId, string method, string tool)
    {
        return new JObject()
        {
            ["jsonrpc"] = "2.0",
            ["id"] = requestId,
            ["result"] = $"Successfully ran '{tool}'",
            ["method"] = method
        };
    }

    private static JObject Error(int requestId, int code, string message)
    {
        return new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = requestId,
            ["error"] = new JObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }

    private static async Task<HttpRequest?> ReadRequestAsync(NetworkStream stream)
    {
        var accumulated = new MemoryStream();
        var buffer = new byte[4096];
        var headerEndIndex = -1;

        while (headerEndIndex < 0)
        {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (read <= 0)
                return null;

            accumulated.Write(buffer, 0, read);
            headerEndIndex = FindHeaderEnd(accumulated.GetBuffer(), (int)accumulated.Length);

            if (accumulated.Length > 64 * 1024)
                throw new InvalidDataException("HTTP headers exceeded 64 KB.");
        }

        var rawRequest = accumulated.ToArray();
        var headerText = Encoding.ASCII.GetString(rawRequest, 0, headerEndIndex);
        var headerLines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (headerLines.Length == 0 || string.IsNullOrWhiteSpace(headerLines[0]))
            return null;

        var requestLineParts = headerLines[0].Split(' ');
        if (requestLineParts.Length < 2)
            throw new InvalidDataException("Malformed HTTP request line.");

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < headerLines.Length; i++)
        {
            var line = headerLines[i];
            if (string.IsNullOrEmpty(line))
                continue;

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var headerName = line.Substring(0, separatorIndex).Trim();
            var headerValue = line.Substring(separatorIndex + 1).Trim();
            headers[headerName] = headerValue;
        }

        var bodyLength = 0;
        if (headers.TryGetValue("Content-Length", out var contentLengthValue)
            && !int.TryParse(contentLengthValue, out bodyLength))
        {
            throw new InvalidDataException("Invalid Content-Length header.");
        }

        var bodyBytes = new byte[bodyLength];
        var bodyStartIndex = headerEndIndex + 4;
        var copied = Math.Min(bodyLength, rawRequest.Length - bodyStartIndex);
        if (copied > 0)
            Buffer.BlockCopy(rawRequest, bodyStartIndex, bodyBytes, 0, copied);

        while (copied < bodyLength)
        {
            var read = await stream.ReadAsync(bodyBytes, copied, bodyLength - copied).ConfigureAwait(false);
            if (read <= 0)
                throw new EndOfStreamException("Unexpected end of stream while reading HTTP body.");

            copied += read;
        }

        return new HttpRequest(
            requestLineParts[0],
            requestLineParts[1],
            headers,
            bodyLength > 0 ? Encoding.UTF8.GetString(bodyBytes) : string.Empty);
    }

    private static async Task WriteResponseAsync(NetworkStream stream, int statusCode, string statusText,
        string contentType,
        string body, string? id = null)
    {

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var headers = new StringBuilder()
            .Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(statusText).Append("\r\n")
            .Append("Content-Type: ").Append(contentType).Append("\r\n")
            .Append("Content-Length: ").Append(bodyBytes.Length).Append("\r\n")
            .Append("Connection: close\r\n")
            .Append("Access-Control-Allow-Origin: *\r\n")
            .Append("Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n")
            .Append("Access-Control-Allow-Headers: Content-Type\r\n\r\n");

        var headerBytes = Encoding.ASCII.GetBytes(headers.ToString());
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
        if (bodyBytes.Length > 0)
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
        stream.Close();
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (var i = 0; i <= length - 4; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                return i;
        }

        return -1;
    }

    private sealed class HttpRequest
    {
        public HttpRequest(string method, string path, Dictionary<string, string> headers, string body)
        {
            Method = method;
            Path = path;
            Headers = headers;
            Body = body;
        }

        public string Method { get; }

        public string Path { get; }

        public Dictionary<string, string> Headers { get; }

        public string Body { get; }
    }
}