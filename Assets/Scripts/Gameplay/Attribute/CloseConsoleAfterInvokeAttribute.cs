using System;

namespace Gameplay
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class CloseConsoleAfterInvokeAttribute : Attribute
    {
    }
}
