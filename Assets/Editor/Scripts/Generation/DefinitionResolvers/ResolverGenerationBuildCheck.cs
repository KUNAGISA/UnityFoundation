using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Gameplay.Editor
{
    public sealed class ResolverGenerationBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ActionGenerator.CheckGenerated();
            ConditionGenerator.CheckGenerated();
        }
    }
}
