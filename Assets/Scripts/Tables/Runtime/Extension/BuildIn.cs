using UnityEngine;

using Gameplay.Tags;

namespace Gameplay.Tables
{
    internal static class BuildIn
    {
        public static Vector2 ToVector2(in vector2 v) => new Vector2(v.X, v.Y);
        public static Vector3 ToVector3(in vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static Vector4 ToVector4(in vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
        public static Quaternion ToQuaternion(in rotation rot) => Quaternion.Euler(rot.X, rot.Y, rot.Z);

        public static GameplayTag ToGameplayTag(in gameplaytag value)
        {
            if (value.Id == 0)
            {
                return default;
            }
            var tag = new GameplayTag(value.Id);
            if (!GameplayTags.Catalog.Contains(tag))
            {
                throw new System.ArgumentException($"Unknown configured gameplay tag [{value.Id}].", nameof(value));
            }
            return tag;
        }
    }
}
