using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.UI;

namespace HolopalmPlus
{
    [HarmonyPatch]
    public class CorePatches
    {
        public static Dictionary<string, string> settingNames = new()
        {
            { "showMapStoryOverlay", "Notification overlay" },
            { "showJobPreview", "Job story preview" },
            { "frequentStories", "Frequent job stories" },
            { "glowJobStories", "Glow job stories" },
            { "showSeenText", "Highlight seen options" },
            { "extendedOutcomePreview", "Extended choice preview" },
            { "extendedCharaBio", "Extended character bio" },
            { "giftInfo", "Show gift icons" }
        };

        public static Dictionary<string, string> settingDescriptions = new()
        {
            { "showMapStoryOverlay", "Show a notification overlay for new character stories and birthdays (only shows up to 5 notifications at a time)" },
            { "showJobPreview", "Show if performing a specific job would result in an event-type story" },
            { "frequentStories", "Special event-type job stories can occur more often" },
            { "glowJobStories", "Special event-type job stories can occur during Glow season" },
            { "showSeenText", "Highlight options that have been seen before" },
            { "extendedOutcomePreview", "Show more information about the outcome of a choice such as friendship and skill gain/loss for seen choices (only works when 'Highlight seen options' is on)" },
            { "extendedCharaBio", "Show current ending and dating status in the bio; Show birthdays and full names for all non-friend characters, as well as hidden dating flags" },
            { "giftInfo", "Show icons for liked, disliked, neutral and unknown collectibles when gifting" }
        };

        [HarmonyPatch(typeof(Groundhogs), "Load")]
        [HarmonyPostfix]
        public static void GroundhogsLoadPostfix()
        {
            HolopalmSave.instance.Load();
        }

        [HarmonyPatch(typeof(SettingsMenu), "CreateButton")]
        [HarmonyPostfix]
        public static void SettingsMenuCreateButtonPostfix(SettingsMenu __instance, Selectable __result, string settingName, Selectable aboveButton)
        {
            if (__instance == null || __result == null || settingName != "hardwareMouseCursor")
            {
                return;
            }

            Selectable currentAboveButton = __result;

            NWButton spacerButton = CreateSeparator(__instance);
            if (spacerButton != null)
            {
                ConnectNavigation(currentAboveButton, spacerButton);
                currentAboveButton = spacerButton;
            }

            NWButton headerButton = CreateModHeader(__instance);
            if (headerButton != null)
            {
                ConnectNavigation(currentAboveButton, headerButton);
                currentAboveButton = headerButton;
            }

            foreach (var setting in HolopalmSave.instance.settings)
            {
                string key = setting.Key;
                bool value = setting.Value;

                string text = GetButtonText(key);
                NWButton button = __instance.AddButton(text, null);

                string tooltip = GetButtonDescription(key);
                if (tooltip != null)
                {
                    button.tooltipText = tooltip;
                }

                Action listener2 = delegate
                {
                    SetSetting(key, !value, button);
                };
                button.onClick.ReplaceListener(delegate
                {
                    listener2();
                });

                ConnectNavigation(currentAboveButton, button);
                currentAboveButton = button;
            }
        }

        private static NWButton CreateSeparator(SettingsMenu settingsMenu)
        {
            try
            {
                NWButton separatorButton = settingsMenu.AddButton("", null);
                separatorButton.interactable = false;

                var buttonImage = separatorButton.GetComponent<UnityEngine.UI.Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new UnityEngine.Color(0, 0, 0, 0);
                }

                var textComponent = separatorButton.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = "";
                    textComponent.color = new UnityEngine.Color(0, 0, 0, 0);
                }

                var rectTransform = separatorButton.GetComponent<UnityEngine.RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new UnityEngine.Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y * 1.5f);
                }

                return separatorButton;
            }
            catch (Exception ex)
            {
                ModInstance.Log($"Failed to create separator: {ex.Message}");
                return null;
            }
        }

        private static NWButton CreateModHeader(SettingsMenu settingsMenu)
        {
            try
            {
                NWButton headerButton = settingsMenu.AddButton("Holopalm Plus", null);

                headerButton.interactable = false;

                if (headerButton.GetComponent<Text>() != null)
                {
                    var textComponent = headerButton.GetComponent<Text>();
                    textComponent.fontStyle = UnityEngine.FontStyle.Bold;
                    textComponent.color = new UnityEngine.Color(1f, 0.8f, 0.2f, 1f);
                }

                return headerButton;
            }
            catch (Exception ex)
            {
                ModInstance.Log($"Failed to create mod header: {ex.Message}");
                return null;
            }
        }

        private static string GetButtonText(string settingName)
        {
            if (HolopalmSave.instance == null)
            {
                return settingName;
            }

            if (settingNames.TryGetValue(settingName, out string name))
            {
                return name + " " + (HolopalmSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
            }

            return settingName + " " + (HolopalmSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
        }

        private static string GetButtonDescription(string settingName)
        {
            if (HolopalmSave.instance == null)
            {
                return null;
            }

            if (settingDescriptions.TryGetValue(settingName, out string description))
            {
                return description;
            }

            return null;
        }

        private static void ConnectNavigation(Selectable top, Selectable bottom)
        {
            if (!(top == null) || !(bottom == null))
            {
                if (bottom is NWSelectable nWSelectable)
                {
                    nWSelectable.selectOverrideOnUp = top;
                }
                else if (bottom is NWButton nWButton)
                {
                    nWButton.selectOverrideOnUp = top;
                }
                else if (bottom is NWDropdown nWDropdown)
                {
                    nWDropdown.selectOverrideOnUp = top;
                }

                if (top is NWSelectable nWSelectable2)
                {
                    nWSelectable2.selectOverrideOnDown = bottom;
                }
                else if (top is NWButton nWButton2)
                {
                    nWButton2.selectOverrideOnDown = bottom;
                }
                else if (top is NWDropdown nWDropdown2)
                {
                    nWDropdown2.selectOverrideOnDown = bottom;
                }
            }
        }

        private static void SetSetting(string settingName, object value, NWButton buttonToUpdate = null)
        {
            if (HolopalmSave.instance == null || HolopalmSave.instance.settings == null)
            {
                return;
            }

            HolopalmSave.UpdateSettings(settingName, (bool)value);

            if (!(buttonToUpdate != null))
            {
                return;
            }

            buttonToUpdate.text = GetButtonText(settingName);

            if (value is bool)
            {
                Action listener = delegate
                {
                    SetSetting(settingName, !(bool)value, buttonToUpdate);
                };
                buttonToUpdate.onClick.ReplaceListener(delegate
                {
                    listener();
                });
            }
        }
    }
}
