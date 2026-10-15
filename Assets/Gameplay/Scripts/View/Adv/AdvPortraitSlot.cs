using Gameplay.Sequence;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public partial class AdvPortraitSlot : UIWidget
    {
	    private RectTransform _transform = null;
	    private AdvPortrait _portrait = null;
	    
	    public int SortingOrder { get; private set; }
	    
	    partial void OnAwake()
	    {
		    _transform = GetComponent<RectTransform>();
	    }

	    public void SetPortrait(AdvPortrait portrait)
	    {
		    ClearPortrait();
		    _portrait = portrait;
		    _portrait.transform.SetParent(_transform, false);
		    _portrait.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		    _portrait.transform.localScale = Vector3.one;
	    }
	    
	    public void ClearPortrait()
	    {
		    if (_portrait != null)
		    {
			    Destroy(_portrait.gameObject);
			    _portrait = null;
		    }
	    }
	    
	    public void ApplyState(in AdvPortraitVisualState state)
	    {
		    _transform.anchoredPosition = state.Position - state.Origin;
		    _transform.localScale = new Vector3(state.Scale.x, state.Scale.y, 1f);
		    _transform.localRotation = Quaternion.Euler(0f, 0f, state.RotationDegrees);

		    if (_portrait != null)
		    {
			    _portrait.ApplyState(state);
		    }
	    }

	    public void SetSortingOrder(int order)
	    {
		    SortingOrder = order;
	    }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class AdvPortraitSlot
{
	private void Awake()
	{
		OnAwake();
	}

	partial void OnAwake();
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
