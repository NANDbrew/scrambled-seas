using OVRSimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScrambledSeas
{
    [Serializable]
    public class line
    {
        [SerializeField]
        public string description;
        public line()
        {
            this.description = "line";
        }
    }

    [Serializable]
    public class path
    {
        [SerializeField]
        public string description;
        public path()
        {
            this.description = "path";
        }
    }

    [Serializable]
    public class point
    {
        [SerializeField]
        public string description = "point";
        [SerializeField]
        public List<float> pos;
        [SerializeField]
        public string colour;
        [SerializeField]
        public int day = 0;
        [SerializeField]
        public int time = 0;
        [SerializeField]
        public string winddir = "NE";
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
        [SerializeField]
        public string description;

        public goal()
        {
            this.description = "goal";
        }
    }

    [Serializable]
    public class SailwindMapExport
    {

        [SerializeField]
        public List<line> lines;
        [SerializeField]
        public List<path> paths;
        [SerializeField]
        public List<point> points;
        [SerializeField]
        public List<goal> goal;
        [SerializeField]
        public string name;
        [SerializeField]
        public List<float> ft;


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
