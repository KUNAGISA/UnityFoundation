using System;
using Aoiro;

namespace Gameplay
{
    public readonly struct UIFocusScopeToken : IEquatable<UIFocusScopeToken>, ICancelToken
    {
        internal readonly UIViewNavigation Navigation;
        internal readonly int ScopeId;

        internal UIFocusScopeToken(UIViewNavigation navigation, int scopeId)
        {
            Navigation = navigation;
            ScopeId = scopeId;
        }

        public bool IsValid => Navigation != null && ScopeId > 0;

        public void Cancel() => Navigation?.Cancel(this);

        public bool Equals(UIFocusScopeToken other) => ReferenceEquals(Navigation, other.Navigation) && ScopeId == other.ScopeId;

        public override bool Equals(object obj) => obj is UIFocusScopeToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Navigation, ScopeId);
    }
}
