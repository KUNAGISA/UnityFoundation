using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public interface IScenePreload
    {
        UniTask PreloadSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken);
    }
}
