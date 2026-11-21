using Gameplay.UI;

namespace Gameplay
{
    public partial class LaunchLoadingView : UIView<LaunchLoadingView.Data>
    {
        public class Data : UIViewData
        {
            public override string Location => "Views/" + nameof(LaunchLoadingView);
            public override UILayerId LayerId => UILayer.Loading.ToLayerId();

            public IReadOnlyProgress Progress { get; }
			
			public Data(IReadOnlyProgress progress)
			{
                Progress = progress;
            }
        }

        private Data _data;
        
        protected override void OnInitialize(Data data)
        {
	        _data = data;
	        _txtLoading.text = $"Loading... {_data.Progress.Value:P2}";
        }

        private void Update()
        {
	        _txtLoading.text = $"Loading... {_data.Progress.Value:P2}";
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class LaunchLoadingView
{

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtLoading = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_txtLoading = transform.Find("Loading").GetComponent<TMPro.TextMeshProUGUI>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
