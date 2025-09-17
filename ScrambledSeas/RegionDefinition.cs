using System;
using System.Collections.Generic;

namespace ScrambledSeas
{
    public class RegionListContainer
    {
        public List<RegionDefinition> regions { get; set; }
    }
    public class RegionDefinition
    {
        public int index { get; set; }
        public string objectName { get; set; } = string.Empty;
        public string bottomPlane {  get; set; } = string.Empty;
        public List<int> islands { get; set; } = new List<int>();
    }
}
