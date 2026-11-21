using System;

namespace Gameplay.UI
{
    public readonly struct UILayerId : IEquatable<UILayerId>
    {
        private const int UnspecifiedValue = int.MinValue;

        public static UILayerId Unspecified => new UILayerId(UnspecifiedValue);

        public int Value { get; }
        public bool IsSpecified => Value != UnspecifiedValue;

        /// <summary>以项目定义的数值创建逻辑层标识；保留值 int.MinValue 表示未指定。</summary>
        public UILayerId(int value)
        {
            Value = value;
        }

        /// <summary>按原始数值判断是否相等。</summary>
        public bool Equals(UILayerId other) => Value == other.Value;
        /// <summary>判断对象是否具有相同的类型和数值。</summary>
        public override bool Equals(object obj) => obj is UILayerId other && Equals(other);
        /// <summary>使用原始数值作为哈希值。</summary>
        public override int GetHashCode() => Value;
        /// <summary>返回层数值的文本；未指定时返回 Unspecified。</summary>
        public override string ToString() => IsSpecified ? Value.ToString() : nameof(Unspecified);

        /// <summary>比较两个值是否相等。</summary>
        public static bool operator ==(UILayerId left, UILayerId right) => left.Equals(right);
        /// <summary>比较两个值是否不相等。</summary>
        public static bool operator !=(UILayerId left, UILayerId right) => !left.Equals(right);
    }
}
