using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PAGui.Visualizer
{
    public class NodeTipEventArgs : RoutedEventArgs
    {
        public NodeTipEventArgs(RoutedEvent routedEvent, object source, object tag)
            : base(routedEvent, source)
        {
            Tag = tag;
        }

        public readonly object Tag;
        public object Content;
    }

    public delegate void NodeTipEventHandler(object sender, NodeTipEventArgs e);
}
