using System.Collections;
using System.Collections.Generic;

namespace VRVis.Spawner.Edges {

    /// <summary>
    /// This class provides methods to apply values to spawned edges.<para/>
    /// It modifies the color and scale properties of the edge objects.<para/>
    /// 
    /// TODO: check if we need this class at a time and not remove it
    /// </summary>
    public class EdgeModifier {


        // CONSTRUCTOR

        public EdgeModifier() {}


        // FUNCTIONALITY

        /// <summary>
        /// Apply mappings on spawned edge instances.<para/>
        /// This method is mainly to update edges that are connected to regions.<para/>
        /// All the other properties are fixed and applied on connection creation.
        /// </summary>
        public void ApplyColorMappings(List<CodeWindowEdgeConnection> edgeCons) {
            
            // apply color method updates
            foreach (CodeWindowEdgeConnection ec in edgeCons) {
                ec.ApplyEdgeSettingMethods(ec.GetEdgeSetting(), true, false);
            }
        }

    }
}