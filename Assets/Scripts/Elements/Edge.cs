using System.Collections;
using System.Collections.Generic;
using VRVis.JSON.Serialization;

namespace VRVis.Elements {

    /// <summary>
    /// Holds all the information about an edge.<para/>
    /// An edge is a connection between two nodes.<para/>
    /// A node is considered to be a line of code.<para/>
    /// So an edge can connect two lines of code of the same file or of distinct files.<para/>
    /// 
    /// This class is similar to the JSON Deserialization class "JSONEdge"<para/>
    /// but provides additional methods and functionality.
    /// </summary>
    public class Edge {

        private readonly uint id;
        private readonly string type;
        private readonly string label;
        private JSONEdge.NodeLocation from;
        private JSONEdge.NodeLocation to;
        private readonly float value;


	    // CONSTRUCTOR
        
        public Edge(uint id, string type, string label, JSONEdge.NodeLocation from, JSONEdge.NodeLocation to, float value) {
            this.id = id;
            this.label = label;
            this.type = type;
            this.from = from;
            this.to = to;
            this.value = value;

            ValidateRange(this.from.lines);
            ValidateRange(this.to.lines);
        }

        public Edge(uint id, JSONEdge jsonEdge)
        : this(id, jsonEdge.type.ToLower(), jsonEdge.label, jsonEdge.from, jsonEdge.to, jsonEdge.value) {}


        // GETTER AND SETTER
       
        public uint GetID() { return id; }

        public string GetEdgeType() { return type; }

        public string GetLabel() { return label; }

        /// <summary>Holds e.g. relative file path of start file.</summary>
        public JSONEdge.NodeLocation GetFrom() { return from; }
        public void SetFrom(JSONEdge.NodeLocation from) {
            this.from = from;
            ValidateRange(this.from.lines);
        }

        /// <summary>Holds e.g. relative file path of end file.</summary>
        public JSONEdge.NodeLocation GetTo() { return to; }
        public void SetTo(JSONEdge.NodeLocation to) {
            this.to = to;
            ValidateRange(this.from.lines);
        }

        public float GetValue() { return value; }


        // FUNCTIONALITY
        
        /// <summary>
        /// Ensures that the range "to" is not less than "from"
        /// and that "from" is at least 0
        /// </summary>
        private void ValidateRange(JSONEdge.Range range) {
            if (range.from < 0) { range.from = 0; }
            if (range.to < range.from) { range.to = range.from; }
        }

    }
}
