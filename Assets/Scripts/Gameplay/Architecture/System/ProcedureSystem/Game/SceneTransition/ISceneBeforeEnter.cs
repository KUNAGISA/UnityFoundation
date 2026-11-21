using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public interface ISceneBeforeEnter
    {
        UniTask BeforeEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken);
    }
}
