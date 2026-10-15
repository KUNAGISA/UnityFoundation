using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    internal sealed class UIBindingSettingsProvider : SettingsProvider
    {
        private UIBindingProjectSettings _settings;
        private PropertyTree _propertyTree;

        private UIBindingSettingsProvider() : base("Project/UI Binding", SettingsScope.Project, new[] { "UI", "Binding", "Template", "View", "Widget" })
        {
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new UIBindingSettingsProvider();
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            CreatePropertyTree();
        }

        public override void OnDeactivate()
        {
            _propertyTree?.ApplyChanges();
            _settings?.SaveSettings();
            DisposePropertyTree();
        }

        public override void OnGUI(string searchContext)
        {
            if (_propertyTree == null)
            {
                CreatePropertyTree();
            }

            EditorGUI.BeginChangeCheck();
            _propertyTree.Draw(false);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.SaveSettings();
            }

            DrawActions();
            DrawValidationErrors();
        }

        private void CreatePropertyTree()
        {
            DisposePropertyTree();
            _settings = UIBindingProjectSettings.GetOrCreate();
            _propertyTree = PropertyTree.Create(new SerializedObject(_settings));
            _propertyTree.DrawMonoScriptObjectField = false;
        }

        private void DisposePropertyTree()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
        }

        private void DrawActions()
        {
            SirenixEditorGUI.BeginHorizontalToolbar(24f, 4);
            GUILayout.FlexibleSpace();
            if (SirenixEditorGUI.ToolbarButton("Validate", true))
            {
                ShowValidationResult();
            }
            if (SirenixEditorGUI.ToolbarButton("Reset to Defaults", true))
            {
                ResetToDefaults();
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog("Reset UI Binding Settings", "Reset templates and all binding rules to defaults?", "Reset", "Cancel"))
            {
                return;
            }

            _settings.ResetToDefaults();
            _settings.SaveSettings();
            CreatePropertyTree();
        }

        private void ShowValidationResult()
        {
            var errors = _settings.CollectValidationErrors();
            EditorUtility.DisplayDialog("UI Binding Settings", errors.Length == 0 ? "Settings are valid." : string.Join(Environment.NewLine, errors), "OK");
        }

        private void DrawValidationErrors()
        {
            var errors = _settings.CollectValidationErrors();
            for (var index = 0; index < errors.Length; index++)
            {
                SirenixEditorGUI.ErrorMessageBox(errors[index]);
            }
        }
    }
}
