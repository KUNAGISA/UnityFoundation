using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public interface IStartupSplash
    {
        UniTask WaitForSplashAsync(CancellationToken cancellationToken);
        void HideSplash();
    }
}
