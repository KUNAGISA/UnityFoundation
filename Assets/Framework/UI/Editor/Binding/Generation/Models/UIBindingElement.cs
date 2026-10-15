namespace Gameplay.UI.Editor
{
    internal readonly struct UIBindingElement
    {
        public string NodePath { get; }
        public string CollectionRootPath { get; }
        public int Index { get; }
        public bool HasNodePath => !string.IsNullOrEmpty(NodePath);

        internal UIBindingElement(string nodePath, string collectionRootPath, int index)
        {
            NodePath = nodePath;
            CollectionRootPath = collectionRootPath;
            Index = index;
        }
    }
}
