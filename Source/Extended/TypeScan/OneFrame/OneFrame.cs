using System;

namespace Nanory.Lex
{
    public class OneFrame : System.Attribute
    {

    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequestSystemAttribute : Attribute
    {
        public Type RequestComponentType;
        public RequestSystemAttribute(Type type)
        {
            RequestComponentType = type;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class EventSystemAttribute : Attribute
    {
        public Type EventComponentType;
        public EventSystemAttribute(Type type)
        {
            EventComponentType = type;
        }
    }
}
