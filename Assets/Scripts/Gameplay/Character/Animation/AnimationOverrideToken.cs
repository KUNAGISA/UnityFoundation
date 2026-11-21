namespace Gameplay
{
    public sealed class AnimationOverrideToken
    {
        public static AnimationOverrideToken None { get; } = new(null, 0);

        private CharacterAnimation _animation;
        private readonly int _id;

        public bool IsValid => _animation != null && _animation.IsOverrideActive(_id);

        internal AnimationOverrideToken(CharacterAnimation animation, int id)
        {
            _animation = animation;
            _id = id;
        }

        public void Cancel()
        {
            var animation = _animation;
            _animation = null;
            if (animation != null)
            {
                animation.CancelOverride(_id);
            }
        }
    }
}
