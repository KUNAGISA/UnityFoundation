namespace Gameplay
{
    public sealed class AnimationOverrideToken
    {
        private AnimationAbility _ability;
        private readonly int _id;

        public bool IsValid => _ability != null;

        internal AnimationOverrideToken(AnimationAbility ability, int id)
        {
            _ability = ability;
            _id = id;
        }

        public void Cancel()
        {
            var ability = _ability;
            _ability = null;
            ability?.CancelOverride(_id);
        }
    }
}
