using System;

namespace Gameplay.UI
{
    public readonly struct UIViewOptionMask : IEquatable<UIViewOptionMask>
    {
        public int Value { get; }
        public bool IsEmpty => Value == 0;

        /// <summary>以原始位值创建选项集合。</summary>
        public UIViewOptionMask(int value)
        {
            Value = value;
        }

        /// <summary>判断是否包含指定的全部选项位；空选项返回 false。</summary>
        public bool Contains(UIViewOptionMask option) => !option.IsEmpty && (Value & option.Value) == option.Value;
        /// <summary>按原始数值判断是否相等。</summary>
        public bool Equals(UIViewOptionMask other) => Value == other.Value;
        /// <summary>判断对象是否具有相同的类型和数值。</summary>
        public override bool Equals(object obj) => obj is UIViewOptionMask other && Equals(other);
        /// <summary>使用原始数值作为哈希值。</summary>
        public override int GetHashCode() => Value;
        /// <summary>返回选项位值的文本。</summary>
        public override string ToString() => Value.ToString();

        /// <summary>比较两个值是否相等。</summary>
        public static bool operator ==(UIViewOptionMask left, UIViewOptionMask right) => left.Equals(right);
        /// <summary>比较两个值是否不相等。</summary>
        public static bool operator !=(UIViewOptionMask left, UIViewOptionMask right) => !left.Equals(right);
    }
}
