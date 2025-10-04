using HarmonyLib;
using System;
using System.Collections.Generic;
using Northway.Utils;

namespace HolopalmPlus;

[HarmonyPatch]
public class ExtendedBioPatches
{
    [HarmonyPatch(typeof(CharasMenu), "UpdateCurrentChara")]
    [HarmonyPostfix]
    public static void CharasMenuUpdateCurrentCharaPostfix(CharasMenu __instance)
    {
        try
        {
            if (!HolopalmSave.GetSetting("extendedCharaBio", true))
            {
                return;
            }

            if (__instance == null)
            {
                return;
            }

            Chara chara = WHHelper.GetReadonlyField<Chara, CharasMenu>(__instance, "chara");

            if (chara == null)
            {
                return;
            }

            if (!chara.canLove)
            {
                __instance.justBirthday.SetActiveMaybe();
                string fact4 = chara.GetFact(CharaFact.birthday, true);
                __instance.ageBirthday.GetChildByNameRecursive<NWText>("RightTitle").text = TextLocalized.Localize("charas_birthday");
                __instance.ageBirthday.GetChildByNameRecursive<NWText>("RightText").text = fact4.IsNullOrEmptyOrWhitespace() ? "?" : fact4;

                string text = chara.nickname;
                string fact = chara.GetFact(CharaFact.name, true);
                if (!fact.IsNullOrEmptyOrWhitespace() && chara.nickname != fact)
                {
                    text = chara.nickname + " (" + fact + ")";
                }

                __instance.charaName.text = text;
            }
        }
        catch (Exception e)
        {
            ModInstance.Log($"Error in ExtendedBioPatches.CharasMenuUpdateCurrentCharaPostfix: {e.Message}");
        }
    }

    public static readonly string princessEndingText = "On a path to become";

    [HarmonyPatch(typeof(CharasMenu), nameof(CharasMenu.UpdatePrincess))]
    [HarmonyPostfix]
    public static void CharasMenuUpdatePrincessPostfix(CharasMenu __instance)
    {
        try
        {
            if (!HolopalmSave.GetSetting("extendedCharaBio", true))
            {
                return;
            }

            if (__instance == null || __instance.princessParagraphText == null || __instance.princessParagraphText.text == null)
            {
                return;
            }

            Chara chara = WHHelper.GetReadonlyField<Chara, CharasMenu>(__instance, "chara");
            if (!Singleton<CharasMenu>.isOpen || chara != null)
            {
                return;
            }

            __instance.princessParagraphText.text += "\n";

            Ending ending = Ending.PickEnding();

            if (ending != null && !ending.endingName.IsNullOrEmptyOrWhitespace())
            {
                string currentEndingArticle = ending.endingName.ToLowerInvariant().StartsWithAny("a", "e", "i", "o", "u") ? "an" : "a";

                __instance.princessParagraphText.text += $"\n{princessEndingText} {currentEndingArticle} {ending.endingName}.";
            }

            List<string> dating = [];

            foreach (Chara chara2 in Chara.allCharas)
            {
                if (chara2 == null)
                {
                    continue;
                }

                if (chara2.isDatingYou)
                {
                    dating.Add(chara2.nicknameIncludingNemmie);
                }
            }

            if (dating.Count > 0)
            {
                string datingText = string.Join(", ", dating);
                if (dating.Count > 1)
                {
                    int lastCommaIndex = datingText.LastIndexOf(',');
                    if (lastCommaIndex != -1)
                    {
                        datingText = datingText.Remove(lastCommaIndex, 1).Insert(lastCommaIndex, " and");
                    }
                }

                if (!datingText.IsNullOrEmptyOrWhitespace())
                {
                    __instance.princessParagraphText.text += $"\nDating {datingText}.";
                }
            }

            if (__instance.princessParagraphText.text.EndsWith("\n"))
            {
                __instance.princessParagraphText.text = __instance.princessParagraphText.text.RemoveEnding("\n");
            }
        }
        catch (Exception e)
        {
            ModInstance.Log($"Error in ExtendedBioPatches.CharasMenuUpdatePrincessPostfix: {e.Message}");
        }
    }

    // Patch to still enable having Marz and Rex as secret admirers, even if they are dating each other
    [HarmonyPatch(typeof(Chara), "isDatingSomeone", MethodType.Getter)]
    [HarmonyPostfix]
    public static void CharaIsDatingSomeonePostfix(Chara __instance, ref bool __result)
    {
        try
        {
            if (!HolopalmSave.GetSetting("extendedCharaBio", true))
            {
                return;
            }

            if (__instance == null)
            {
                return;
            }

            if (__instance.charaID == "marz" || __instance.charaID == "rex")
            {
                if (Princess.HasMemory("marzRexDate"))
                {
                    __result = false;
                }
            }

            if (__instance.charaID == "dys" || __instance.charaID == "sym")
            {
                if (Princess.HasMemory("dysSymDate"))
                {
                    __result = false;
                }
            }
        }
        catch (Exception e)
        {
            ModInstance.Log($"Error in ExtendedBioPatches.CharaIsDatingSomeonePostfix: {e.Message}");
        }
    }

    [HarmonyPatch(typeof(Chara), "GetFact", [typeof(CharaFact), typeof(bool)])]
    [HarmonyPostfix]
    public static void CharaGetFactPostfix(Chara __instance, ref string __result, CharaFact fact, bool force)
    {
        try
        {
            if (!HolopalmSave.GetSetting("extendedCharaBio", true))
            {
                return;
            }

            Chara chara = __instance;

            if (chara == null)
            {
                return;
            }

            if (fact == CharaFact.date)
            {
                Chara otherChara = null;

                switch (chara.charaID)
                {
                    case "dys":
                        if (Princess.HasMemory("dysSymDate"))
                        {
                            otherChara = Chara.FromID("sym");
                        }
                        break;
                    case "sym":
                        if (Princess.HasMemory("dysSymDate"))
                        {
                            otherChara = Chara.FromID("dys");
                        }
                        break;
                    case "marz":
                        if (Princess.HasMemory("marzRexDate"))
                        {
                            otherChara = Chara.FromID("rex");
                        }
                        break;
                    case "rex":
                        if (Princess.HasMemory("marzRexDate"))
                        {
                            otherChara = Chara.FromID("marz");
                        }
                        break;
                    default:
                        return;
                }

                if (otherChara != null)
                {
                    string additionalText = TextLocalized.Localize("charas_datingChara", otherChara.nickname);

                    if (additionalText.IsNullOrEmptyOrWhitespace())
                    {
                        return;
                    }

                    if (__result.IsNullOrEmptyOrWhitespace())
                    {
                        __result = additionalText.RemoveEnding("\n");
                        return;
                    }
                    else if (!__result.Contains(additionalText))
                    {
                        __result += $"\n{additionalText}".RemoveEnding("\n");
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            ModInstance.Log($"Error in ExtendedBioPatches.CharaGetFactPostfix: {e.Message}");
        }
    }
}
