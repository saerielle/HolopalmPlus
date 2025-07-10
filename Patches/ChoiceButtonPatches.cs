using HarmonyLib;
using System;
using System.Collections.Generic;
using Northway.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace HolopalmPlus;

[HarmonyPatch]
public class ChoiceButtonPatches
{
    [HarmonyPatch(typeof(NWButtonResults), "SetRequirements")]
    [HarmonyPostfix]
    public static void ShowSeenTextPatch(NWButtonResults __instance, Choice choice, Result result)
    {
        try
        {
            CleanupButtonOverlays(__instance);

            if (Settings.instance.skipSeenText || !HolopalmSave.GetSetting("showSeenText") || !WHHelper.HasSeenChoice(choice))
            {
                return;
            }

            NWButtonResults button = __instance;

            Image icon = button.iconLeft1?.gameObject.activeSelf == false ? button.iconLeft1 : button.iconLeft2?.gameObject.activeSelf == false ? button.iconLeft2 : null;

            if (icon == null)
            {
                return;
            }


            var SetIconMethod = typeof(NWButtonResults).GetMethod("SetIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (SetIconMethod == null)
            {
                return;
            }

            string tooltip = TextLocalized.Localize("button_icon_seen");
            string iconName = "checkmark";
            Color iconColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            bool hasLove = false;
            bool hasMemory = false;
            bool hasSkills = false;

            List<StorySet> showableLoveSet = new List<StorySet>();
            List<StorySet> showableMemorySet = new List<StorySet>();
            List<StorySet> showableSkillSets = new List<StorySet>();

            if (HolopalmSave.GetSetting("extendedOutcomePreview"))
            {
                List<StorySet> allSets = new List<StorySet>();
                bool hasJumpParent = false;

                PopulateAllSetsForChoice(ref allSets, choice);

                foreach (var set in allSets)
                {
                    switch (set.type)
                    {
                        case StorySetType.love:
                            AddSetConditional(ref showableLoveSet, set, choice.requirements);
                            break;
                        case StorySetType.memory:
                            AddSetConditional(ref showableMemorySet, set, choice.requirements);
                            break;
                        case StorySetType.skill:
                            AddSetConditional(ref showableSkillSets, set, choice.requirements);
                            break;
                        case StorySetType.jump:
                            if (set.stringID != null && choice.parent != null && set.stringID == choice.parent.choiceID)
                            {
                                hasJumpParent = true;
                            }
                            break;
                    }
                }

                if (hasJumpParent)
                {
                    showableLoveSet.Clear();
                    showableMemorySet.Clear();
                    showableSkillSets.Clear();

                    iconName = "arrow";
                    tooltip += "\nDoes not advance the story";
                }

                hasLove = showableLoveSet.Count > 0;
                hasMemory = showableMemorySet.Count > 0;
                hasSkills = showableSkillSets.Count > 0;

                List<string> uniqueSkillTypes = new List<string>();
                foreach (var set in showableSkillSets)
                {
                    uniqueSkillTypes.Add(GetSkillIcon(set.stringID, set.intValue));
                }

                if (hasLove || hasMemory || hasSkills)
                {
                    if (hasLove && !hasMemory && !hasSkills)
                    {
                        iconName = "love";
                    }
                    else if (!hasLove && !hasMemory && uniqueSkillTypes.Count == 1)
                    {
                        iconName = uniqueSkillTypes[0];
                    }
                    else
                    {
                        iconName = "other";
                    }

                    foreach (var set in showableLoveSet)
                    {
                        Chara chara = Chara.FromID(set.stringID);
                        if (chara != null)
                        {
                            tooltip += "\n" + GetSignString(set.intValue) + " Friendship with " + chara.nickname;
                        }
                    }

                    foreach (var set in showableMemorySet)
                    {
                        if (set.stringID.ParseEnum<MemoryChange>() != MemoryChange.none)
                        {
                            tooltip += "\n" +
                                (TextLocalized.CanLocalize("memchange_" + set.stringID)
                                    ? (set.intValue.ToSignedString() + " " + TextLocalized.Localize("memchange_" + set.stringID))
                                    : (set.intValue.ToSignedString() + " " + set.stringID.ToUpperCaseCapitals())
                                );
                        }
                    }

                    foreach (var set in showableSkillSets)
                    {
                        Skill skill = Skill.FromID(set.stringID);
                        if (skill != null)
                        {
                            if (set.intValue < 0 && skill.hasNegativeSkillName)
                            {
                                tooltip += "\n+ " + skill.skillNameNegative;
                            }
                            else
                            {
                                tooltip += "\n" + GetSignString(set.intValue) + " " + skill.skillName;
                            }
                        }
                    }
                }
            }

            SetIconMethod.Invoke(null, [icon, iconName, tooltip, iconColor]);

            if (HolopalmSave.GetSetting("extendedOutcomePreview") && hasLove && !hasMemory && !hasSkills)
            {
                // Add the + or - sign to the icon, but only if all results have the same sign
                bool hasPositive = false;
                bool hasNegative = false;

                foreach (var set in showableLoveSet)
                {
                    if (set.intValue > 0)
                    {
                        hasPositive = true;
                    }
                    else if (set.intValue < 0)
                    {
                        hasNegative = true;
                    }
                }

                if (hasPositive && !hasNegative)
                {
                    AddTMPSignOverlay(icon, "+");
                }
                else if (!hasPositive && hasNegative)
                {
                    AddTMPSignOverlay(icon, "-");
                }
                else if (hasPositive && hasNegative)
                {
                    AddTMPSignOverlay(icon, "+/-");
                }
            }
        }
        catch (Exception e)
        {
            ModInstance.Log($"ChoiceButtonPatches Error: {e.Message}");
        }
    }

    private static void PopulateAllSetsForChoice(ref List<StorySet> allSets, Choice choice, int level = 0)
    {
        allSets.AddRange(choice.sets);

        if (level >= 10)
        {
            return;
        }

        if (choice.choices.Count == 1 && WHHelper.HasSeenChoice(choice.choices[0]))
        {
            PopulateAllSetsForChoice(ref allSets, choice.choices[0]);
        }

        foreach (StorySet storySet in choice.sets)
        {
            if (storySet.type == StorySetType.jump)
            {
                Choice choiceById = choice.story.GetChoiceById(storySet.stringID, warnInvalid: false);
                if (choiceById != null)
                {
                    if (choice.parent != null && choice.parent.choiceID == choiceById.choiceID)
                    {
                        continue;
                    }
                    PopulateAllSetsForChoice(ref allSets, choiceById);
                }
            }
        }
    }

    private static void AddTMPSignOverlay(Image targetIcon, string text)
    {
        GameObject signObject = new GameObject("TMPSignOverlay");
        signObject.transform.SetParent(targetIcon.transform, false);

        var tmpText = signObject.AddComponent<TMPro.TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = text.Length < 2 ? 24 : 20;
        tmpText.color = Color.white;
        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
        tmpText.fontWeight = TMPro.FontWeight.Heavy;
    }

    private static void CleanupButtonOverlays(NWButtonResults button)
    {
        CleanupIconOverlays(button.iconLeft1);
        CleanupIconOverlays(button.iconLeft2);
    }

    private static void CleanupIconOverlays(Image icon)
    {
        if (icon == null) return;

        for (int i = icon.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = icon.transform.GetChild(i);
            if (child.name == "TMPSignOverlay")
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static string GetSignString(int number)
    {
        string result = "+";
        if (number > 0)
        {
            result = "+";
        }

        if (number < 0)
        {
            result = "-";
        }

        return result;
    }

    private static string GetSkillIcon(string skillID, int value)
    {
        Skill skill = Skill.FromID(skillID);
        if (skill != null)
        {
            if (skill.skillID == "empathy" || skill.skillID == "persuasion" || skill.skillID == "creativity" || skill.skillID == "bravery")
            {
                return "social";
            }
            else if (skill.skillID == "reasoning" || skill.skillID == "organization" || skill.skillID == "engineering" || skill.skillID == "biology")
            {
                return "mental";
            }
            else if (skill.skillID == "toughness" || skill.skillID == "perception" || skill.skillID == "combat" || skill.skillID == "animals")
            {
                return "physical";
            }
            else
            {
                return skill.skillID + ((value < 0) ? "_down" : "_up");
            }
        }

        return "other";
    }

    private static bool IsShowableSet(StorySet set, List<StoryReq> reqs = null)
    {
        try
        {
            if (set.type == StorySetType.love)
            {
                Chara chara = Chara.FromID(set.stringID);
                return chara != null;
            }

            if (set.type == StorySetType.memory && set.stringID.ParseEnum<MemoryChange>() != MemoryChange.none)
            {
                return true;
            }

            if (set.type == StorySetType.skill)
            {
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            ModInstance.Log($"Error in IsShowableSet: {e.Message}");
            return false;
        }
    }

    private static void AddSetConditional(ref List<StorySet> list, StorySet set, List<StoryReq> reqs = null)
    {
        if (IsShowableSet(set, reqs))
        {
            if (set.requirement == null)
            {
                list.Add(set);
            }
            else
            {
                if (set.requirement.Execute(new Result()))
                {
                    list.Add(set);
                }
                else if (set.elseSet != null)
                {
                    AddSetConditional(ref list, set.elseSet, reqs);
                }
            }
        }
    }
}
