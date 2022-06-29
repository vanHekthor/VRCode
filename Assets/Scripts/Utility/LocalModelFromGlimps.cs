using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Utilities.Glimps {
    public class LocalModelFromGlimps {
        public class Option {
            bool from;
            bool to;
            string option;
        }

        public class Effect {
            List<Option> options;
            double time;
        }

        public class Term {
            List<Effect> options;
            string name;
        }

        public class LocalModel {
            private List<Term> terms; 
        }

        private List<LocalModel> models;
        
    }

}