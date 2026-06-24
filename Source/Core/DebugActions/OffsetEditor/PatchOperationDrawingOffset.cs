using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Annotations;
using Verse;

// ReSharper disable once CheckNamespace
namespace GiddyUp
{
    [UsedImplicitly]
    public class PatchOperationDrawingOffset : PatchOperation
    {
        private XmlContainer value;
        private string def;
        private string xpath => $"Defs/ThingDef[defName=\"{def}\"]";

        public override bool ApplyWorker(XmlDocument xml)
        {
            var node = this.value.node;
            var flag = false;
            foreach (var selectNode in xml.SelectNodes(xpath))
            {
                var xmlNode = selectNode as XmlNode;
                var element = (XmlNode)xmlNode["modExtensions"];
                if (element == null)
                {
                    element = (XmlNode)xmlNode.OwnerDocument.CreateElement("modExtensions");
                    xmlNode.AppendChild(element);
                }

                foreach (XmlNode childNode in node.ChildNodes)
                    element.AppendChild(xmlNode.OwnerDocument.ImportNode(childNode, true));
                flag = true;
            }

            return flag;
        }
    }
}
