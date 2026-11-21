using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public class SceneTransitionSystem : SystemBase
    {
        private SceneTransitionRequest? _request = null;
        private TablesModel _tables = null;
        
        protected override void OnInitialize()
        {
            _tables = this.GetModel<TablesModel>();
        }

        protected override void OnShutdown()
        {
            _tables = null;
            _request = null;
        }

        public bool RequestTransition(in SceneTransitionRequest request)
        {
            if ((_request?.Reason ?? SceneTransitionReason.Unknow) > request.Reason)
            {
                return false;
            }

            if (!_tables.SceneTable.TryGet(request.SceneId, out var sceneCfg))
            {
                Debug.LogError($"[Scene] Scene [{request.SceneId}] is not found.");
                return false;
            }
            
            _request = request;
            return true;
        }

        public bool TryConsumeTransition(out SceneTransitionRequest request)
        {
            if (_request != null)
            {
                (request, _request) = (_request.Value, null);
                return true;
            }
            
            request = default;
            return false;
        }

        internal void ClearPending()
        {
            _request = null;
        }
    }
}
