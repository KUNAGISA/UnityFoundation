using Aoiro.Sequence;
using Aoiro;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Gameplay.Common;
using LiteLua;
using UnityEngine;

namespace Gameplay.Sequence
{
    public static class SequenceLuaAPI
    {
        public static void Register(IntPtr l)
        {
            LuaAPI.lua_pushcfunction(l, Print);
            LuaAPI.lua_setglobal(l, "print");
            
            LuaAPI.lua_pushcfunction(l, Wait);
            LuaAPI.lua_setglobal(l, "wait");
            
            LuaAPI.lua_pushcfunction(l, Options);
            LuaAPI.lua_setglobal(l, "options");
            
            LuaAPI.lua_pushcfunction(l, Message);
            LuaAPI.lua_setglobal(l, "message");
            
            LuaAPI.lua_pushcfunction(l, SetCharacter);
            LuaAPI.lua_setglobal(l, "set_chara");
            
            LuaAPI.lua_pushcfunction(l, MoveCharacter);
            LuaAPI.lua_setglobal(l, "move_chara");
            
            LuaAPI.lua_pushcfunction(l, ZoomCharacter);
            LuaAPI.lua_setglobal(l, "zoom_chara");
            
            LuaAPI.lua_pushcfunction(l, ChangeCharacterOpacity);
            LuaAPI.lua_setglobal(l, "change_chara_opacity");
            
            LuaAPI.lua_pushcfunction(l, SetCharacterZOrder);
            LuaAPI.lua_setglobal(l, "set_chara_z");
            
            LuaAPI.lua_pushcfunction(l, ChangeCharacterTint);
            LuaAPI.lua_setglobal(l, "change_chara_tint");
            
            LuaAPI.lua_pushcfunction(l, RotateCharacter);
            LuaAPI.lua_setglobal(l, "rotate_chara");
            
            LuaAPI.lua_pushcfunction(l, SetCharacterOffset);
            LuaAPI.lua_setglobal(l, "set_chara_offset");
            
            LuaAPI.lua_pushcfunction(l, SetCharacterFlip);
            LuaAPI.lua_setglobal(l, "set_chara_flip");
        }
        
        public static void SetCurrentPlayer(IntPtr l, IntPtr playerPtr)
        {
            LuaAPI.lua_pushthread(l);
            LuaAPI.lua_pushlightuserdata(l, playerPtr);
            LuaAPI.lua_settable(l, LuaConst.RegistryIndex);
        }

        private static ISequencePlayer<ISequenceContext> GetCurrentPlayer(IntPtr l)
        {
            LuaAPI.lua_pushthread(l);
            LuaAPI.lua_gettable(l, LuaConst.RegistryIndex);
            var playerPtr = LuaAPI.lua_touserdata(l, -1);
            return (ISequencePlayer<ISequenceContext>)GCHandle.FromIntPtr(playerPtr).Target;
        }

        private static int Print(IntPtr l)
        {
            var n = LuaAPI.lua_gettop(l);
            var sb = new StringBuilder();
            for (var index = 1; index <= n; index++)
            {
                var str = LuaAPI.lua_tostring(l, index);
                sb.Append(str);
                sb.Append(' ');
            }
            Debug.Log($"[Lua] {sb}");
            return 0;
        }

        private static int Wait(IntPtr l)
        {
            var player = GetCurrentPlayer(l);
            
            if (LuaAPI.lua_isnumber(l, 1))
            {
                var time = LuaAPI.lua_tonumber(l, 1);
                player.RequestYield(new WaitTimeYield(time));
            }
            else
            {
                player.RequestYield(new WaitActionYield());
            }
            
            LuaAPI.lua_yield(l);
            return 0;
        }

        private static int Options(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入选项Id", out var choiceId, out var errorCode))
            {
                return errorCode;
            }

            var count = LuaAPI.lua_gettop(l) - 1;
            if (count <= 0)
            {
                return LuaAPI.luaL_error(l, "需要传入选项");
            }

            var player = GetCurrentPlayer(l);
            var choices = new string[count];
            for (var index = 0; index < count; ++index)
            {
                choices[index] = LuaAPI.lua_tostring(l, index + 2);
            }
            
            player.RequestYield(new ChoiceYield(choiceId, choices));
            
            LuaAPI.lua_yield(l);
            return 0;
        }
        
        private static int Message(IntPtr l)
        {
            var player = GetCurrentPlayer(l);

            var text = LuaAPI.lua_tostring(l, 1);
            var name = string.Empty;
            var auto = false;
            int? audioId = null;
            
            if (LuaAPI.lua_type(l, 2) == LuaType.Table)
            {
                if (LuaAPI.lua_getfield(l, 2, "actor") == LuaType.String)
                {
                    name = LuaAPI.lua_tostring(l, -1);
                }
                LuaAPI.lua_pop(l, 1);

                if (LuaAPI.lua_getfield(l, 2, "auto") == LuaType.Boolean)
                {
                    auto = LuaAPI.lua_toboolean(l, -1);
                }
                LuaAPI.lua_pop(l, 1);

                if (LuaAPI.lua_getfield(l, 2, "cv_id") == LuaType.Number)
                {
                    audioId = (int)LuaAPI.lua_tointeger(l, -1);
                }
                LuaAPI.lua_pop(l, 1);
            }
            
            player.EnqueueAction(new DialogueAction(text, name, audioId, auto));
            return 0;
        }

        private static int SetCharacter(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            var advSession = player.GetModule<AdvSession>();
            
            var location = LuaAPI.lua_tostring(l, 2);
            var position = advSession.GetPortraitVisualState(portraitId).Position;
            
            if (LuaAPI.lua_type(l, 3) == LuaType.Table)
            {
                if (LuaAPI.lua_getfield(l, 3, "x") == LuaType.Number)
                {
                    position.x = (float)LuaAPI.lua_tonumber(l, -1);
                }
                LuaAPI.lua_pop(l, 1);

                if (LuaAPI.lua_getfield(l, 3, "y") == LuaType.Number)
                {
                    position.y = (float)LuaAPI.lua_tonumber(l, -1);
                }
                LuaAPI.lua_pop(l, 1);
            }
            
            advSession.SetPortraitAsset(portraitId, location, position);
            return 0;
        }

        private static int MoveCharacter(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetVector2Arg(l, 2, "需要传入坐标", out var position, out errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            var duration = ReadDuration(l, 4);
            
            player.EnqueueAction(new PortraitMoveAction(portraitId, duration, position));
            return 0;
        }
        
        private static int ZoomCharacter(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetVector2Arg(l, 2, "需要传入缩放", out var scale, out errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            var duration = ReadDuration(l, 4);
            
            player.EnqueueAction(new PortraitScaleAction(portraitId, duration, scale));
            return 0;
        }
        
        private static int ChangeCharacterOpacity(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetSingleArg(l, 2, "需要传入透明度", out var opacity, out errorCode))
            {
                return errorCode;
            }

            var player = GetCurrentPlayer(l);
            var duration = ReadDuration(l, 3);
            
            player.EnqueueAction(new PortraitOpacityAction(portraitId, duration, opacity));
            return 0;
        }

        private static int SetCharacterZOrder(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetIntegerArg(l, 2, "需要传入ZOrder", out var order, out errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            player.GetModule<AdvSession>().SetPortraitSortingOrder(portraitId, order);
            return 0;
        }
        
        private static int ChangeCharacterTint(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode))
            {
                return errorCode;
            }
            
            var tint = ColorRGB.Default;
            if (!TryGetByteArg(l, 2, "需要传入颜色值", out tint.R, out errorCode) || !TryGetByteArg(l, 3, "需要传入颜色值", out tint.G, out errorCode) || !TryGetByteArg(l, 4, "需要传入颜色值", out tint.B, out errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            var duration = ReadDuration(l, 5);
            
            player.EnqueueAction(new PortraitTintAction(portraitId, duration, tint));
            return 0;
        }

        private static int RotateCharacter(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetSingleArg(l, 2, "需要传入角度", out var degree, out errorCode))
            {
                return errorCode;
            }

            var player = GetCurrentPlayer(l);
            var duration = ReadDuration(l, 3);
            
            player.EnqueueAction(new PortraitRotateAction(portraitId, duration, degree));
            return 0;
        }

        private static int SetCharacterOffset(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode) || !TryGetVector2Arg(l, 2, "需要传入偏移", out var origin, out errorCode))
            {
                return errorCode;
            }
            
            var player = GetCurrentPlayer(l);
            player.GetModule<AdvSession>().SetPortraitOrigin(portraitId, origin);
            return 0;
        }

        private static int SetCharacterFlip(IntPtr l)
        {
            if (!TryGetIntegerArg(l, 1, "需要传入立绘Id", out var portraitId, out var errorCode))
            {
                return errorCode;
            }

            var flip = LuaAPI.lua_toboolean(l, 2);
            var isHorizontal = LuaAPI.lua_toboolean(l, 3);

            var player = GetCurrentPlayer(l);
            var advSession = player.GetModule<AdvSession>();
            
            if (isHorizontal)
            {
                advSession.SetPortraitFlipHorizontal(portraitId, flip);
            }
            else
            {
                advSession.SetPortraitFlipVertical(portraitId, flip);
            }

            return 0;
        }
        
        private static bool TryGetIntegerArg(IntPtr l, int argIndex, string errorMessage, out int value, out int errorCode)
        {
            if (LuaAPI.lua_type(l, argIndex) != LuaType.Number)
            {
                value = 0;
                errorCode = LuaAPI.luaL_argerror(l, argIndex, errorMessage);
                return false;
            }

            value = (int)LuaAPI.lua_tointeger(l, argIndex);
            errorCode = 0;
            return true;
        }

        private static float ReadDuration(IntPtr l, int argIndex)
        {
            if (LuaAPI.lua_type(l, argIndex) != LuaType.Table)
            {
                return 0f;
            }

            var duration = LuaAPI.lua_getfield(l, argIndex, "time") == LuaType.Number ? (float)LuaAPI.lua_tonumber(l, -1) : 0f;
            LuaAPI.lua_pop(l, 1);
            return duration;
        }
        
        private static bool TryGetByteArg(IntPtr l, int argIndex, string errorMessage, out byte value, out int errorCode)
        {
            if (LuaAPI.lua_type(l, argIndex) != LuaType.Number)
            {
                value = 0;
                errorCode = LuaAPI.luaL_argerror(l, argIndex, errorMessage);
                return false;
            }

            value = (byte)LuaAPI.lua_tointeger(l, argIndex);
            errorCode = 0;
            return true;
        }

        private static bool TryGetVector2Arg(IntPtr l, int argStartIndex, string errorMessage, out Vector2 value, out int errorCode)
        {
            if (!TryGetSingleArg(l, argStartIndex, errorMessage, out var x, out errorCode) || !TryGetSingleArg(l, argStartIndex + 1, errorMessage, out var y, out errorCode))
            {
                value = default;
                return false;
            }
            value = new Vector2(x, y);
            return true;
        }
        
        private static bool TryGetSingleArg(IntPtr l, int argIndex, string errorMessage, out float value, out int errorCode)
        {
            if (LuaAPI.lua_type(l, argIndex) != LuaType.Number)
            {
                value = 0f;
                errorCode = LuaAPI.luaL_argerror(l, argIndex, errorMessage);
                return false;
            }

            value = (float)LuaAPI.lua_tonumber(l, argIndex);
            errorCode = 0;
            return true;
        }
    }
}
