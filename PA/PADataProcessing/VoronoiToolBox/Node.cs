using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADataProcessing.VoronoiToolBox
{
    public class Node
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Orientation { get; set; }

        public Node()
        {
            Id = "";
            X = 0;
            Y = 0;
            Orientation = 0;
        }

        public Node(double X, double Y)
        {
            Id = "";
            this.X = X;
            this.Y = Y;
            Orientation = 0;
        }

        public Node(string Id, double X, double Y, double Orientation)
        {
            this.Id = Id;
            this.X = X;
            this.Y = Y;
            this.Orientation = Orientation;
        }
    }
}
