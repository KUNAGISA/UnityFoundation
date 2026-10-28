using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class ActionSystem : SystemBase
    {
        private readonly struct PendingAction
        {
            public readonly int ActionId;
            public readonly Action<bool> OnExecuted;

            public PendingAction(int actionId, Action<bool> onExecuted)
            {
                ActionId = actionId;
                OnExecuted = onExecuted;
            }
        }

        private const int MaxExecutionCount = 1024;
        private readonly Queue<PendingAction> _pending = new();
        private ActionContext _context;
        private TablesModel _tablesModel;
        private bool _isExecuting;
        private bool _isRejecting;

        protected override void OnInitialize()
        {
            _context = BindContext(new ActionContext());
            _tablesModel = this.GetModel<TablesModel>();
        }

        protected override void OnShutdown()
        {
            FailPending();
            _context = null;
            _tablesModel = null;
        }

        public void Submit(int actionId, Action<bool> onExecuted = null)
        {
            if (_isRejecting)
            {
                Debug.LogError("[Action] 清理期间不能提交新的行为。");
                return;
            }
            _pending.Enqueue(new PendingAction(actionId, onExecuted));
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
                        FailPending();
                        break;
                    }

                    var pending = _pending.Dequeue();
                    var result = Execute(pending.ActionId);
                    // 回调也属于本次分发；它提交的行为继续排队，不能抢先执行。
                    NotifyResult(pending.OnExecuted, result);
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private bool Execute(int actionId)
        {
            try
            {
                if (!_tablesModel.ActionTable.TryGet(actionId, out var entry))
                {
                    Debug.LogError($"[Action] 行为配置不存在 [{actionId}]");
                    return false;
                }
                var result = ActionResolver.Execute(entry.Action, _context);
                if (!result)
                {
                    Debug.LogError($"[Action] 行为 [{actionId}] 发起失败。");
                }
                return result;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void FailPending()
        {
            var wasRejecting = _isRejecting;
            _isRejecting = true;
            try
            {
                while (_pending.Count > 0)
                {
                    NotifyResult(_pending.Dequeue().OnExecuted, false);
                }
            }
            finally
            {
                _isRejecting = wasRejecting;
            }
        }

        private static void NotifyResult(Action<bool> onExecuted, bool result)
        {
            try
            {
                onExecuted?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
