using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogCruncher.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.IO;

    [XmlRoot("WicaRun")]
    public class HumanReadableOutput
    {
        [XmlElement("RunInfos")]
        public RunInfos? RunInfos { get; set; }

        [XmlArray("Assets")]
        [XmlArrayItem("Asset")]
        public List<Asset>? Assets { get; set; }
    }

    public class RunInfos
    {
        [XmlElement("RunInfo")]
        public List<RunInfo>? RunInfo { get; set; }
    }

    public class RunInfo
    {
        [XmlElement("Component")]
        public List<Component>? Components { get; set; }
    }

    public class Component
    {
        [XmlAttribute("Type")]
        public string? Type { get; set; }

        [XmlAttribute("TypeIdentifier")]
        public string? TypeIdentifier { get; set; }

        [XmlElement("Property")]
        public List<Property>? Properties { get; set; }
    }

    public class Asset
    {
        [XmlElement("PropertyList")]
        public List<PropertyList>? PropertyLists { get; set; }
    }

    public class PropertyList
    {
        [XmlAttribute("Type")]
        public string? Type { get; set; }

        [XmlElement("Property")]
        public List<Property>?  Properties { get; set; }
    }

    public class Property
    {
        [XmlAttribute("Name")]
        public required string Name { get; set; }

        [XmlAttribute("Value")]
        public required string Value { get; set; }

        [XmlAttribute("Ordinal")]
        public int Ordinal { get; set; }
    }
}
