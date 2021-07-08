using System;
using System.Runtime.Serialization;

namespace gROPC.Package.Exceptions
{
    [Serializable]
    internal class OPCUnknownNode : Exception
    {
        private string _reason = "";
        public string Reason
        {
            get { return _reason; }
            set { }
        }

        public OPCUnknownNode()
        {

        }

        public OPCUnknownNode(string description) :
            base(String.Format("Unknown node name ({0})", description))
        {
            _reason = description;
        }
    }
}