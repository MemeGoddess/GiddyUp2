using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GiddyUpCore.MCP
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MCPTool(string name, string title, string description) : Attribute
    {
        public string Name { get; } = name;
        public string Title { get; } = title;
        public string Description { get; } = description;

        public MethodInfo Tool
        {
            get => _tool;
            set
            {
                _tool = value;
                OutputSchema = _tool.ReturnType != typeof(void) 
                    ? MCPHelper.ObjToMCP(_tool.ReturnType) 
                    : OutputSchema;
            }
        }

        public JObject InputSchema = new JObject();
        public JObject OutputSchema = new JObject();
        private MethodInfo _tool;
    }
}
