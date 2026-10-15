using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Gameplay/Parallax/Layer Profile")]
    public class ParallaxLayerProfile : ScriptableObject
    {
        [SerializeField, LabelText("视差系数")]
        private Vector2 _factor = new(0.2f, 1f);
        public Vector2 Factor => _factor;
    }
}
