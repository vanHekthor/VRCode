using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Siro.VisualProperties {

    /**
     * The abstract visual property base class.
     * Contains methods and attributes each visual property has.
     */
    public abstract class VisProp {

	    protected string propertyType; // type of the visual property (e.g. performance)
        protected string method; // method name to be called to convert this property

        public enum ATTRIBUTE { NONE, COLOR };
        protected ATTRIBUTE attribute = ATTRIBUTE.NONE; // how this mapping affects a node (e.g. "color" means that the node color will change accordingly)


        // CONSTRUCTOR

        protected VisProp(string propertyType, string method, ATTRIBUTE attribute) {
            this.propertyType = propertyType;
            this.method = method;
            this.attribute = attribute;
        }

        protected VisProp(string propertyType, string method) {
            this.propertyType = propertyType;
            this.method = method;
        }


        // GETTER AND SETTER

        public string GetPropertyType() {
            return propertyType;
        }

        public string GetMethod() {
            return method;
        }

        public ATTRIBUTE GetAttribute() {
            return attribute;
        }

        public abstract void Apply();

    }

}