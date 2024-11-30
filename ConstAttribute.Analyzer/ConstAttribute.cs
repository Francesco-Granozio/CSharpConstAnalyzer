using System;

namespace ConstAttribute.Analyzer
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ConstAttribute : Attribute { }

    internal struct ConstAttributeInfo
    {
        //TODO: ...
    }
}
