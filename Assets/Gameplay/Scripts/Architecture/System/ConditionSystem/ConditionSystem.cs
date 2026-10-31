using System;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class ConditionSystem : SystemBase
    {
        private ConditionContext _context;
        private TablesModel _tablesModel;

        protected override void OnInitialize()
        {
            _context = BindContext(new ConditionContext());
            _tablesModel = this.GetModel<TablesModel>();
        }

        protected override void OnShutdown()
        {
            _context = null;
            _tablesModel = null;
        }

        public bool Evaluate(int conditionId)
        {
            try
            {
                if (!_tablesModel.ConditionTable.TryGet(conditionId, out var entry))
                {
                    Debug.LogError($"[Condition] 条件配置不存在 [{conditionId}]");
                    return false;
                }
                return ConditionResolver.Evaluate(entry.Condition, _context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }
    }
}
