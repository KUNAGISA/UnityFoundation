using System;
using System.Linq;
using UnityEngine.LowLevel;

namespace Gameplay.Common
{
    public static class PlayerLoopBootstrap
    {
        public static void InsertTo<T>(ref PlayerLoopSystem playerLoopSystem, Type type, PlayerLoopSystem.UpdateFunction callback, Type insertAfterType = null)
        {
            var index = FindLoopSystemIndex(playerLoopSystem.subSystemList, typeof(T));
            
            var source = RemoveRunner(playerLoopSystem.subSystemList[index], type);
            var dest = new PlayerLoopSystem[source.Length + 1];

            var insertIndex = FindInsertIndex(source, insertAfterType);
            Array.Copy(source, 0, dest, 0, insertIndex);
            if (insertIndex < source.Length)
            {
                Array.Copy(source, insertIndex, dest, insertIndex + 1, source.Length - insertIndex);
            }
            dest[insertIndex] = new PlayerLoopSystem { type = type, updateDelegate = callback };
            playerLoopSystem.subSystemList[index].subSystemList = dest;
        }
        
        public static void RemoveFrom<T>(ref PlayerLoopSystem playerLoopSystem, Type type)
        {
            var index = FindLoopSystemIndex(playerLoopSystem.subSystemList, typeof(T));
            playerLoopSystem.subSystemList[index].subSystemList = RemoveRunner(playerLoopSystem.subSystemList[index], type);
        }

        private static int FindLoopSystemIndex(PlayerLoopSystem[] playerLoopList, Type systemType)
        {
            for (var index = 0; index < playerLoopList.Length; index++)
            {
                if (playerLoopList[index].type == systemType)
                {
                    return index;
                }
            }

            throw new Exception("Target PlayerLoopSystem does not found. Type:" + systemType.FullName);
        }

        private static int FindInsertIndex(PlayerLoopSystem[] playerLoopList, Type systemType)
        {
            if (systemType == null)
            {
                return playerLoopList.Length;
            }
            for (var index = 0; index < playerLoopList.Length; index++)
            {
                if (playerLoopList[index].type == systemType)
                {
                    return index + 1;
                }
            }
            return playerLoopList.Length;
        }
        
        private static PlayerLoopSystem[] RemoveRunner(in PlayerLoopSystem playerLoopSystem, Type type)
        {
            return playerLoopSystem.subSystemList.Where(x => x.type != type).ToArray();
        }
    }
}