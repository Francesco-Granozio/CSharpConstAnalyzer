using System;

namespace ConstAttribute.Analyzer
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ConstAttribute : Attribute
    {
    }

}
