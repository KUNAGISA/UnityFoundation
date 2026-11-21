using System;
using System.Diagnostics;

namespace Gameplay
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SceneEventIdAttribute : Attribute
    {
    }
}
