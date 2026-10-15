using System;
using Aoiro.Sequence;
using LiteLua;

namespace Gameplay.Sequence
{
    internal sealed class LuaSequenceRoutine : ISequenceRoutine
    {
        private readonly IntPtr _mainThread;
        private readonly IntPtr _thread;

        public LuaSequenceRoutine(IntPtr mainThread, IntPtr thread)
        {
            _mainThread = mainThread;
            _thread = thread;
        }

        public SequenceScriptStatus Resume(int? result)
        {
            var argumentCount = 0;
            if (result.HasValue)
            {
                LuaAPI.lua_pushinteger(_thread, result.Value);
                argumentCount = 1;
            }

            return (SequenceScriptStatus)LuaAPI.lua_resume(_thread, _mainThread, argumentCount, out _);
        }

        public string GetLastError()
        {
            return LuaAPI.lua_tostring(_thread, -1);
        }
    }
}
