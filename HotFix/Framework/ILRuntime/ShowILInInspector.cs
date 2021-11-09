using System;
using JetBrains.Annotations;

namespace HotFix.Framework.ILRuntime
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class ShowILInInspectorAttribute : Attribute
    {
        
    }
}