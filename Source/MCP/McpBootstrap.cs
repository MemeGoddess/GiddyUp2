using Verse;

namespace GiddyUpCore.MCP;

[StaticConstructorOnStartup]
internal static class McpBootstrap
{
    static McpBootstrap()
    {
        RimWorldMcpServer.Start();
    }
}