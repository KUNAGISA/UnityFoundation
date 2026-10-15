using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public interface ISceneBeforeLeave
    {
        UniTask BeforeLeaveSceneAsync(CancellationToken cancellationToken);
    }
}
