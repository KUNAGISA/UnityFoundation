using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public interface ISceneAfterEnter
    {
        UniTask AfterEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken);
    }
}
