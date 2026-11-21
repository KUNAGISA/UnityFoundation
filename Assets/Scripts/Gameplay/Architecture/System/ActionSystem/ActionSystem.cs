using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class ActionSystem : SystemBase
    {
        private const int MaxExecutionCount = 1024;
        private readonly Queue<int> _pending = new();
        private ActionContext _context;
        private TablesModel _tablesModel;
        private bool _isExecuting;

        protected override void OnInitialize()
        {
            _context = BindContext(new ActionContext());
            _tablesModel = this.GetModel<TablesModel>();
        }

        protected override void OnShutdown()
        {
            _pending.Clear();
            _context = null;
            _tablesModel = null;
        }

        public void Submit(int actionId)
        {
            _pending.Enqueue(actionId);
            if (_isExecuting)
            {
                return;
            }

            _isExecuting = true;
            try
            {
                var executionCount = 0;
                while (_pending.Count > 0)
                {
                    if (++executionCount > MaxExecutionCount)
                    {
                        Debug.LogError($"[Action] 单轮行为超过 {MaxExecutionCount} 次，可能存在触发循环。");
                        _pending.Clear();
                        break;
                    }

                    Execute(_pending.Dequeue());
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void Execute(int actionId)
        {
            try
            {
                if (!_tablesModel.ActionTable.TryGet(actionId, out var entry))
                {
                    Debug.LogError($"[Action] 行为配置不存在 [{actionId}]");
                    return;
                }
                var result = ActionResolver.Execute(entry.Action, _context);
                if (!result)
                {
                    Debug.LogError($"[Action] 行为 [{actionId}] 发起失败。");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
