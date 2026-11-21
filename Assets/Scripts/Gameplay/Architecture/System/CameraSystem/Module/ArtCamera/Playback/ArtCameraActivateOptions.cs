namespace Gameplay
{
    public readonly struct ArtCameraActivateOptions
    {
        public readonly string Location;
        public readonly ArtCameraLayer Layer;
        public readonly ArtCameraTimeMode TimeMode;
        public readonly ArtCameraCoveredPolicy CoveredPolicy;
        public readonly ArtCameraCompletionPolicy CompletionPolicy;
        public readonly ArtCameraBinding[] Bindings;

        public ArtCameraActivateOptions(string location, ArtCameraLayer layer, ArtCameraTimeMode timeMode, ArtCameraCoveredPolicy coveredPolicy, ArtCameraCompletionPolicy completionPolicy, ArtCameraBinding[] bindings = null)
        {
            Location = location;
            Layer = layer;
            TimeMode = timeMode;
            CoveredPolicy = coveredPolicy;
            CompletionPolicy = completionPolicy;
            Bindings = bindings;
        }
    }
}
