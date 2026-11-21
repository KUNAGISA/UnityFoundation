using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct SceneTransitionContext
    {
        public IProcedureContext Procedure { get; }
        public SceneTransitionRequest Request { get; }
        public SceneEntry Scene { get; }

        public SceneTransitionContext(IProcedureContext procedure, in SceneTransitionRequest request, SceneEntry scene)
        {
            Procedure = procedure;
            Request = request;
            Scene = scene;
        }
    }
}
