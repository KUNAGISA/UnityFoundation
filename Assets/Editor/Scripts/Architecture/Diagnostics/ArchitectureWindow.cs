using System.Collections.Generic;
using Aoiro;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using System.Reflection;

namespace Gameplay.Editor
{
    internal class ArchitectureWindow<TArchitecture> : OdinMenuEditorWindow, IEditorController where TArchitecture : Architecture<TArchitecture>, new()
    {
        private SubscriptionToken _initializeSignalToken = default;

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            var architecture = Architecture<TArchitecture>.Instance;
            
            Push(tree, "System", architecture.EnumerateModules<ISystem>());
            Push(tree, "Model", architecture.EnumerateModules<IModel>());
            Push(tree, "Service", architecture.EnumerateModules<IService>());

            tree.Config.DrawSearchToolbar = true;
            return tree;
        }

        private static void Push(OdinMenuTree tree, string path, IEnumerable<IArchitectureModule> modules)
        {
            foreach (var module in modules.OrderBy(x => x.GetType().FullName))
            {
                var type = module.GetType();
                var name = type.GetCustomAttribute<LabelTextAttribute>()?.Text ?? type.Name;
                tree.Add($"{path}/{name}", module);
            }
        }

        protected override void Initialize()
        {
            _initializeSignalToken.Cancel();
            _initializeSignalToken = this.SubscribeEvent<EnteredPlayModeEvent>(_ => ForceMenuTreeRebuild());
        }

        protected override void OnDestroy()
        {
            _initializeSignalToken.Cancel();
            _initializeSignalToken = default;
            base.OnDestroy();
        }
    }
}
