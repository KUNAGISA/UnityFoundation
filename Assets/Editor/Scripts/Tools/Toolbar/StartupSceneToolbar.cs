using System;
using System.Collections.Generic;
using System.Reflection;
using Gameplay.Tables;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Gameplay.Editor
{
    [InitializeOnLoad]
    internal static class StartupSceneToolbar
    {
        private const string ButtonName = "GameplayPlayFromStartupSceneButton";
        private const string PreviewButtonName = "GameplayPreviewCurrentSceneButton";
        private const string PlayModeToolbarZoneName = "ToolbarZonePlayMode";
        private const string ButtonText = "▶ 启动";
        private const string PreviewButtonText = "▶ 预览";
        private const string StartupScenePath = "Assets/Startup/StartupScene.unity";
        private const string RestorePendingKey = "Gameplay.Editor.StartupSceneToolbar.RestorePending";
        private const string PreviousSceneGuidKey = "Gameplay.Editor.StartupSceneToolbar.PreviousSceneGuid";
        private const string PreviewSceneIdKey = "Gameplay.Editor.StartupSceneToolbar.PreviewSceneId";

        private static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly FieldInfo ToolbarRootField = ToolbarType?.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);

        private static ScriptableObject _currentToolbar;
        private static EditorToolbarButton _button;
        private static EditorToolbarButton _previewButton;
        private static bool _hasLoggedInjectionWarning;

        static StartupSceneToolbar()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnEditorUpdate()
        {
            if (_currentToolbar == null)
            {
                _button = null;
                _previewButton = null;
                TryInjectButton();
            }

            UpdateButtonState();
        }

        private static void TryInjectButton()
        {
            if (ToolbarType == null || ToolbarRootField == null)
            {
                LogInjectionWarning("Unity toolbar type or root field was not found.");
                return;
            }

            try
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                if (toolbars.Length == 0)
                {
                    return;
                }

                var toolbar = toolbars[0] as ScriptableObject;
                var root = toolbar == null ? null : ToolbarRootField.GetValue(toolbar) as VisualElement;
                var playModeToolbarZone = root?.Q<VisualElement>(PlayModeToolbarZoneName);
                if (playModeToolbarZone == null)
                {
                    return;
                }

                playModeToolbarZone.Q<EditorToolbarButton>(ButtonName)?.RemoveFromHierarchy();
                playModeToolbarZone.Q<EditorToolbarButton>(PreviewButtonName)?.RemoveFromHierarchy();

                _button = new EditorToolbarButton(ButtonText, PlayFromStartupScene)
                {
                    name = ButtonName,
                    tooltip = "从启动场景进入主菜单"
                };
                _button.style.minWidth = 64;
                _button.style.flexShrink = 0;

                playModeToolbarZone.Add(_button);
                _previewButton = new EditorToolbarButton(PreviewButtonText, PreviewCurrentScene)
                {
                    name = PreviewButtonName,
                    tooltip = "从启动场景初始化后直接进入当前活动场景"
                };
                _previewButton.style.minWidth = 64;
                _previewButton.style.flexShrink = 0;
                playModeToolbarZone.Add(_previewButton);
                _currentToolbar = toolbar;
            }
            catch (Exception exception)
            {
                LogInjectionWarning($"Failed to inject the startup scene toolbar button: {exception.Message}");
            }
        }

        private static void PlayFromStartupScene()
        {
            StartFromStartupScene(false);
        }

        private static void PreviewCurrentScene()
        {
            StartFromStartupScene(true);
        }

        private static void StartFromStartupScene(bool preview)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var startupScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupScenePath);
            if (startupScene == null)
            {
                Debug.LogError($"Startup scene does not exist: {StartupScenePath}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!preview)
            {
                EnterPlayMode(startupScene, null);
                return;
            }

            if (!TryGetPreviewScenes(out var sceneGuid, out var matches))
            {
                return;
            }

            if (matches.Count == 1)
            {
                EnterPlayMode(startupScene, matches[0].Id);
                return;
            }

            var menu = new GenericMenu();
            foreach (var entry in matches)
            {
                var sceneId = entry.Id;
                menu.AddItem(new GUIContent($"{sceneId} · {entry.Name}"), false, () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode || GetActiveSceneGuid() != sceneGuid)
                    {
                        Debug.LogWarning("[Scene Preview] 选择期间播放状态或活动场景已变化，本次预览已取消。");
                        return;
                    }

                    EnterPlayMode(startupScene, sceneId);
                });
            }
            menu.ShowAsContext();
        }

        private static void EnterPlayMode(SceneAsset startupScene, int? previewSceneId)
        {
            StorePreviousPlayModeStartScene();
            EditorSceneManager.playModeStartScene = startupScene;

            try
            {
                SessionState.EraseInt(PreviewSceneIdKey);
                if (previewSceneId.HasValue)
                {
                    SessionState.SetInt(PreviewSceneIdKey, previewSceneId.Value);
                }
                EditorApplication.EnterPlaymode();
            }
            catch
            {
                SessionState.EraseInt(PreviewSceneIdKey);
                RestorePlayModeStartScene();
                throw;
            }
        }

        private static bool TryGetPreviewScenes(out string sceneGuid, out List<SceneEntry> matches)
        {
            matches = new List<SceneEntry>();
            var scenePath = SceneManager.GetActiveScene().path;
            sceneGuid = GetActiveSceneGuid();
            if (string.IsNullOrEmpty(sceneGuid))
            {
                Debug.LogError("[Scene Preview] 请先保存并激活要预览的场景。");
                return false;
            }

            var address = AddressableAssetSettingsDefaultObject.Settings?.FindAssetEntry(sceneGuid)?.address;
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[Scene Preview] 当前场景未注册为 Addressable：{scenePath}");
                return false;
            }

            var sceneTable = EditorArchitecture.Instance.GetModule<TablesModel>().SceneTable;
            foreach (var entry in sceneTable.DataMap.Values)
            {
                if (string.Equals(address, "Scenes/" + entry.Location, StringComparison.Ordinal))
                {
                    matches.Add(entry);
                }
            }

            if (matches.Count > 0)
            {
                matches.Sort((left, right) => left.Id.CompareTo(right.Id));
                return true;
            }

            Debug.LogError($"[Scene Preview] 场景表中没有地址为 {address} 的场景配置。");
            return false;
        }

        private static string GetActiveSceneGuid()
        {
            var scenePath = SceneManager.GetActiveScene().path;
            return string.IsNullOrEmpty(scenePath) ? string.Empty : AssetDatabase.AssetPathToGUID(scenePath);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseInt(PreviewSceneIdKey);
            }
            if (!SessionState.GetBool(RestorePendingKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                RestorePlayModeStartScene();
            }
        }

        private static void StorePreviousPlayModeStartScene()
        {
            var previousScene = EditorSceneManager.playModeStartScene;
            var previousScenePath = previousScene == null ? string.Empty : AssetDatabase.GetAssetPath(previousScene);
            var previousSceneGuid = string.IsNullOrEmpty(previousScenePath) ? string.Empty : AssetDatabase.AssetPathToGUID(previousScenePath);

            SessionState.SetString(PreviousSceneGuidKey, previousSceneGuid);
            SessionState.SetBool(RestorePendingKey, true);
        }

        private static void RestorePlayModeStartScene()
        {
            var previousSceneGuid = SessionState.GetString(PreviousSceneGuidKey, string.Empty);
            var previousScenePath = string.IsNullOrEmpty(previousSceneGuid) ? string.Empty : AssetDatabase.GUIDToAssetPath(previousSceneGuid);
            var previousScene = string.IsNullOrEmpty(previousScenePath) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousScenePath);

            EditorSceneManager.playModeStartScene = previousScene;
            SessionState.EraseString(PreviousSceneGuidKey);
            SessionState.EraseBool(RestorePendingKey);
        }

        private static void UpdateButtonState()
        {
            if (_button == null || _previewButton == null)
            {
                return;
            }

            var enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
            if (_button.enabledSelf != enabled)
            {
                _button.SetEnabled(enabled);
            }
            if (_previewButton.enabledSelf != enabled)
            {
                _previewButton.SetEnabled(enabled);
            }
        }

        private static void LogInjectionWarning(string message)
        {
            if (_hasLoggedInjectionWarning)
            {
                return;
            }

            _hasLoggedInjectionWarning = true;
            Debug.LogWarning(message);
        }
    }
}
