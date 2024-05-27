using OVRSimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;

namespace ScrambledSeas
{
    [Serializable]
    public class line
    {
        [JsonInclude]
        public string description { get; set; }
        public line()
        {
            this.description = "line";
        }
    }

    [Serializable]
    public class path
    {
        [JsonInclude]
        public string description { get; set; }
        public path()
        {
            this.description = "path";
        }
    }

    [Serializable]
    public class point
    {
        [JsonInclude]
        public string description { get; set; }
        [JsonInclude]
        public List<float> pos { get; set; }
        [JsonInclude]
        public string colour { get; set; }
        [JsonInclude]
        public int day { get; set; }
        [JsonInclude]
        public int time { get; set; }
        [JsonInclude]
        public string winddir { get; set; }
        public point(string description, Vector3 pos)
        {
            this.description = description;
            this.pos = new List<float>();
            this.pos.Add(pos.x);
            this.pos.Add(pos.z);

            Main.Log("point " + this.pos[0] +" "+ this.pos[1]);
            colour = "bluepoint";
            day = 0;
            winddir = "NE";
        }
    }

    [Serializable]
    public class goal
    {
        [JsonInclude]
        public string description { get; set; }

        public goal()
        {
            this.description = "goal";
        }
    }

    [Serializable]
    public class SailwindMapExport
    {

        [JsonInclude]
        public List<line> lines { get; set; }
        [JsonInclude]
        public List<path> paths { get; set; }
        [JsonInclude]
        public List<point> points { get; set; }
        [JsonInclude]
        public List<goal> goal { get; set; }
        [JsonInclude]
        public string name { get; set; }
        [JsonInclude]
        public List<float> ft { get; set; }


        public SailwindMapExport()
        {
            name = "SailwindMapExportClass";
            lines =  new List<line> ();
            lines.Add(new line());
            paths = new List<path> ();
            points = new List<point> ();
            goal = new List<goal> ();
            ft = new List<float>();
            for(int i = 0; i < 3; i++)
            {
                ft.Add(i * 0.3f);
            }
        }

    }


}
