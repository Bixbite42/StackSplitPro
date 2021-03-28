using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Bixbite.StackSplitPro
{
    [BepInProcess("DSPGAME.exe"), BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class StackSplitPro : BaseUnityPlugin
    {
        private const string PluginGUID = "com.Bixbite.StackSplitPro";
        private const string PluginName = "Stack Split Pro";
        private const string PluginVersion = "1.0.0.0";

        private static readonly FieldInfo _uiGridSplit_sliderImg = AccessTools.Field(typeof(UIGridSplit), "sliderImg");
        private static readonly FieldInfo _uiGridSplit_valueText = AccessTools.Field(typeof(UIGridSplit), "valueText");

        private static StackSplitPro _instance;

        private Harmony _harmony;

        private bool _isGridSplitOpen;
        private int? _typedValue;
        private float _typingRestartTimer;

        private void Awake()
        {
            Assert.Null(_instance, $"An instance of {nameof(StackSplitPro)} has already been created!");
            _instance = this;

            _harmony = new Harmony(PluginGUID);
            try { _harmony.PatchAll(typeof(StackSplitPro)); }
            catch (Exception e) { Logger.LogError($"Harmony patching failed: {e.Message}"); }
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
            _instance = null;
        }

        public void OnUIGridSplitOpen()
        {
            _isGridSplitOpen = true;
        }

        public bool OnUIGridSplitUpdate(UIGridSplit uiGridSplit)
        {
            for (KeyCode key = KeyCode.Keypad0; key <= KeyCode.Keypad9; key++)
            {
                CheckKey(key, value: key - KeyCode.Keypad0);
            }

            for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
            {
                CheckKey(key, value: key - KeyCode.Alpha0);
            }

            if (_typingRestartTimer > 0)
            {
                _typingRestartTimer = Mathf.Max(0, _typingRestartTimer - Time.deltaTime);
            }

            // Skip the original function when overriding with a manually typed value
            return _typedValue == null;

            void CheckKey(KeyCode key, int value)
            {
                if (Input.GetKeyDown(key))
                {
                    // If the first number pressed is zero, ignore it
                    if (value == 0 && _typedValue == null)
                    {
                        return;
                    }

                    if (_typingRestartTimer == 0)
                    {
                        _typedValue = 0;
                    }

                    _typingRestartTimer = 1.0f;

                    // Add the pressed key to the typed value, clamping to the valid range
                    _typedValue = Mathf.Clamp(_typedValue.Value * 10 + value, 1, uiGridSplit.maxCount);

                    uiGridSplit.value = _typedValue.Value;
                    ((Image)_uiGridSplit_sliderImg.GetValue(uiGridSplit)).fillAmount = (float)_typedValue / uiGridSplit.maxCount;
                    ((Text)_uiGridSplit_valueText.GetValue(uiGridSplit)).text = $"{_typedValue}";
                }
            }
        }

        public void OnUIGridSplitClose()
        {
            _typedValue = null;
            _typingRestartTimer = 0;
            _isGridSplitOpen = false;
        }

        public void OnUIBuildMenuUpdate()
        {
            // Prevent the number keys from opening build menus
            if (_isGridSplitOpen)
            {
                VFInput.inputing = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGridSplit), "_OnOpen")]
        public static void UIGridSplit_OnOpen_Postfix() => _instance.OnUIGridSplitOpen();

        [HarmonyPrefix, HarmonyPatch(typeof(UIGridSplit), "_OnUpdate")]
        public static bool UIGridSplit_OnUpdate_Prefix(UIGridSplit __instance) => _instance.OnUIGridSplitUpdate(__instance);
        
        [HarmonyPostfix, HarmonyPatch(typeof(UIGridSplit), "_OnClose")]
        public static void UIGridSplit_OnClose_Postfix() => _instance.OnUIGridSplitClose();

        [HarmonyPrefix, HarmonyPatch(typeof(UIBuildMenu), "_OnUpdate")]
        public static void UIBuildMenu_OnUpdate_Prefix() => _instance.OnUIBuildMenuUpdate();
    }
}
