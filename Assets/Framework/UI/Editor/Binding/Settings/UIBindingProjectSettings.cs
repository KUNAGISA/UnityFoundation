using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Editor
{
    [UnityEditor.FilePath("ProjectSettings/UIBindingSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UIBindingProjectSettings : ScriptableSingleton<UIBindingProjectSettings>
    {
        internal const int AutoBindingId = 0;
        internal const string DefaultBindingTemplatePath = "Assets/Framework/UI/Editor/Binding/Templates/Bindings/UIBind.sbn";
        internal const string DefaultViewTemplatePath = "Assets/Framework/UI/Editor/Binding/Templates/Scripts/View.cs.txt";
        internal const string DefaultWidgetTemplatePath = "Assets/Framework/UI/Editor/Binding/Templates/Scripts/Widget.cs.txt";

        private const int CurrentVersion = 4;

        [SerializeField, UnityEngine.HideInInspector]
        private int _version;

        [SerializeField, BoxGroup("Code Templates"), LabelText("Binding Template")]
        private DefaultAsset _bindingTemplate;

        [SerializeField, BoxGroup("Code Templates"), LabelText("View Script Template")]
        private TextAsset _viewScriptTemplate;

        [SerializeField, BoxGroup("Code Templates"), LabelText("Widget Script Template")]
        private TextAsset _widgetScriptTemplate;

        [SerializeReference]
        [TitleGroup("Binding Rules")]
        [InfoBox("Auto binding checks enabled rules from top to bottom. Changing a rule key changes its computed id and invalidates existing prefab references.")]
        [ListDrawerSettings(DraggableItems = true, ShowPaging = false, ListElementLabelName = nameof(UIBindingRuleConfig.DisplayName), ShowFoldout = false)]
        [TypeDrawerSettings(BaseType = typeof(UIBindingRuleConfig), Filter = TypeInclusionFilter.IncludeConcreteTypes)]
        private List<UIBindingRuleConfig> _bindingRules = new();

        internal IReadOnlyList<UIBindingRuleConfig> BindingRules => _bindingRules;

        internal static UIBindingProjectSettings GetOrCreate()
        {
            var settings = instance;
            if (settings._version != CurrentVersion)
            {
                settings.ResetToDefaults();
                settings.SaveSettings();
            }
            return settings;
        }

        [InitializeOnLoadMethod]
        private static void EnsureInitialized()
        {
            GetOrCreate();
        }

        internal string GetBindingTemplatePath()
        {
            return GetTemplatePath(_bindingTemplate, DefaultBindingTemplatePath, "binding");
        }

        internal string GetViewScriptTemplatePath()
        {
            return GetTemplatePath(_viewScriptTemplate, DefaultViewTemplatePath, "view script");
        }

        internal string GetWidgetScriptTemplatePath()
        {
            return GetTemplatePath(_widgetScriptTemplate, DefaultWidgetTemplatePath, "widget script");
        }

        internal UIBindingRuleConfig FindRule(int id)
        {
            for (var index = 0; index < _bindingRules.Count; index++)
            {
                var rule = _bindingRules[index];
                if (rule != null && rule.Id == id)
                {
                    return rule;
                }
            }
            return null;
        }

        internal string[] CollectValidationErrors()
        {
            var errors = new List<string>();
            var ids = new HashSet<int>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < _bindingRules.Count; index++)
            {
                var rule = _bindingRules[index];
                if (rule == null)
                {
                    errors.Add($"Binding rule [{index}] is missing its managed-reference type.");
                    continue;
                }
                if (!keys.Add(rule.Key))
                {
                    errors.Add($"Binding rule key [{rule.Key}] is duplicated.");
                }
                else if (!ids.Add(rule.Id))
                {
                    errors.Add($"Binding rule id [{rule.Id}] has a stable-key hash collision.");
                }
                rule.Validate(index, errors);
            }

            ValidateTemplate(_bindingTemplate, DefaultBindingTemplatePath, "Binding", errors);
            ValidateTemplate(_viewScriptTemplate, DefaultViewTemplatePath, "View script", errors);
            ValidateTemplate(_widgetScriptTemplate, DefaultWidgetTemplatePath, "Widget script", errors);
            return errors.ToArray();
        }

        internal void ValidateOrThrow()
        {
            var errors = CollectValidationErrors();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Invalid UI binding project settings:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }

        internal void ResetToDefaults()
        {
            _version = CurrentVersion;
            _bindingTemplate = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultBindingTemplatePath);
            _viewScriptTemplate = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultViewTemplatePath);
            _widgetScriptTemplate = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultWidgetTemplatePath);
            _bindingRules.Clear();
            _bindingRules.Add(new ComponentBindingRuleConfig("unity.button", "Button", typeof(Button), "btn", UIBindingEventConfig.Create("onClick", "Click")));
            _bindingRules.Add(new ComponentBindingRuleConfig("unity.toggle", "Toggle", typeof(Toggle), "toggle", UIBindingEventConfig.Create("onValueChanged", "ValueChanged")));
            _bindingRules.Add(new ComponentBindingRuleConfig("unity.image", "Image", typeof(Image), "img"));
            _bindingRules.Add(new ComponentBindingRuleConfig("tmp.text", "Text Mesh Pro UGUI", typeof(TextMeshProUGUI), "txt"));
            _bindingRules.Add(new UIWidgetBindingRuleConfig("framework.ui-widget"));
            _bindingRules.Add(new TransformBindingRuleConfig("framework.transform"));
            _bindingRules.Add(new GameObjectBindingRuleConfig("framework.game-object"));
        }

        internal void SaveSettings()
        {
            Save(true);
        }

        private static string GetTemplatePath(UnityEngine.Object template, string defaultPath, string templateName)
        {
            var path = template == null ? defaultPath : AssetDatabase.GetAssetPath(template);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException($"Cannot resolve UI {templateName} template path.");
            }
            return path;
        }

        private static void ValidateTemplate(UnityEngine.Object template, string defaultPath, string displayName, List<string> errors)
        {
            var path = template == null ? defaultPath : AssetDatabase.GetAssetPath(template);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                errors.Add($"{displayName} template [{path}] is missing.");
            }
        }
    }
}
