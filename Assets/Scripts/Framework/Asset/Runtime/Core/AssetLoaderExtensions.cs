namespace Gameplay.Asset
{
    public static class AssetLoaderExtensions
    {
        /// <summary>
        /// 创建资源加载器
        /// </summary>
        /// <param name="service">资源管理器</param>
        /// <returns>资源加载器</returns>
        public static AssetLoader CreateLoader(this IAssetService service)
        {
            return new AssetLoader(service);
        }
    }
}
