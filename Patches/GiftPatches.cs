using HarmonyLib;
using System;
using System.Collections.Generic;
using Northway.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace HolopalmPlus;

[HarmonyPatch]
public class GiftPatches
{
    private static string lastCharaID = null;
    private static Dictionary<string, GameObject> cardIcons = new Dictionary<string, GameObject>();

    [HarmonyPatch(typeof(StoryCalls), "choosegift", [typeof(string), typeof(bool)])]
    [HarmonyPrefix]
    public static bool StoryCallsChooseGiftPrefix(string charaID, bool takeGift)
    {
        lastCharaID = charaID;
        return true;
    }

    [HarmonyPatch(typeof(PickerMenu), "PopulateCollectibles")]
    [HarmonyPostfix]
    public static void PickerMenuPopulateCollectiblesPostfix(PickerMenu __instance)
    {
        try
        {
            if (cardIcons.Count > 0)
            {
                foreach (var icon in cardIcons.Values)
                {
                    if (icon != null)
                    {
                        UnityEngine.Object.Destroy(icon);
                    }
                }
                cardIcons.Clear();
            }

            if (lastCharaID == null)
            {
                return;
            }

            Chara chara = Chara.FromID(lastCharaID);

            if (chara == null)
            {
                lastCharaID = null;
                return;
            }

            __instance.cardsContainer.ClearAndRecycleChildren();
            NWUtils.LayoutGroupOnThenOff(__instance.cardsContainer);

            List<string> addedTracker = new List<string>();
            List<CardData> likedCards = new List<CardData>();
            List<CardData> neutralCards = new List<CardData>();
            List<CardData> unknownCards = new List<CardData>();
            List<CardData> dislikedCards = new List<CardData>();

            foreach (string card in Princess.cards)
            {
                if (!addedTracker.Contains(card))
                {
                    CardData cardData = CardData.FromID(card);
                    if (cardData == null)
                    {
                        ModInstance.Log("PopulateCollectibles found invalid cardID " + card + ", maybe cards have changed");
                    }
                    else if (cardData.collectible != null)
                    {
                        addedTracker.Add(card);

                        if (chara.likedCards.Contains(cardData))
                        {
                            if (Princess.HasGroundhog("fact_" + chara.charaID + "_likes_" + card, evenIfDisabled: true))
                            {
                                likedCards.Add(cardData);
                            }
                            else
                            {
                                unknownCards.Add(cardData);
                            }
                        }
                        else if (chara.dislikedCards.Contains(cardData))
                        {
                            if (Princess.HasGroundhog("fact_" + chara.charaID + "_dislikes_" + card, evenIfDisabled: true))
                            {
                                dislikedCards.Add(cardData);
                            }
                            else
                            {
                                unknownCards.Add(cardData);
                            }
                        }
                        else if (HolopalmSave.HasGiftedCard(chara.charaID, cardData.cardID))
                        {
                            neutralCards.Add(cardData);
                        }
                        else
                        {
                            unknownCards.Add(cardData);
                        }
                    }
                }
            }

            foreach (CardData item in likedCards)
            {
                AddCardToContainer(item, __instance, "liked");
            }

            foreach (CardData item in unknownCards)
            {
                AddCardToContainer(item, __instance, "unknown");
            }

            foreach (CardData item in neutralCards)
            {
                AddCardToContainer(item, __instance, "neutral");
            }

            foreach (CardData item in dislikedCards)
            {
                AddCardToContainer(item, __instance, "disliked");
            }

            if (addedTracker.Count > 0)
            {
                Singleton<PickerMenu>.panel.firstSelectedButton = __instance.cardsContainer.GetComponentInChildren<CardSelectable>();
            }
            else
            {
                Singleton<PickerMenu>.panel.firstSelectedButton = __instance.cancelButton;
            }

            lastCharaID = null;
        }
        catch (Exception ex)
        {
            ModInstance.Log($"Error in PickerMenuPopulateCollectiblesPostfix: {ex.Message}");
        }
    }



    private static void AddCardToContainer(CardData item, PickerMenu menu, string type = "unknown")
    {
        try
        {
            Card obj = BattleManager.CreateSingleCardContainer(item, menu.cardsContainer);

            if (!HolopalmSave.GetSetting("giftInfo"))
            {
                return;
            }

            string iconName = null;
            switch (type)
            {
                case "liked":
                    iconName = "love";
                    break;
                case "unknown":
                    iconName = "other";
                    break;
                case "disliked":
                    iconName = "kudos_down";
                    break;
            }

            if (iconName == null)
            {
                return;
            }

            Sprite iconSprite = AssetManager.GetRequirementIcon(iconName, true);
            if (iconSprite == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("CardIcon_" + item.cardID);
            iconObject.transform.SetParent(obj.transform, false);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;

            if (type == "liked")
            {
                Color color = new Color(0.8f, 0.2f, 0.2f, 1f);
                iconImage.color = color;
                iconObject.transform.localPosition = new Vector3(-79, -129, 0);
            }
            else if (type == "disliked")
            {
                Color color = new Color(0.3f, 0.3f, 0.3f, 1f);
                iconImage.color = color;
                iconObject.transform.localPosition = new Vector3(-82, -129, 0);
            }
            else
            {
                iconImage.color = Color.white;
                iconObject.transform.localPosition = new Vector3(-84, -130, 0);
            }

            iconImage.rectTransform.sizeDelta = new Vector2(32, 32);
            cardIcons[item.cardID] = iconObject;
        }
        catch (Exception ex)
        {
            ModInstance.Log($"Error in AddCardToContainer: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(StoryCalls), "choosegiftDone")]
    [HarmonyPostfix]
    public static void StoryCallsChooseGiftDonePostfix(string charaID, CardData data, bool tookGift)
    {
        try
        {
            Chara chara = Chara.FromID(charaID);
            if (chara == null)
            {
                return;
            }

            HolopalmSave.AddGiftedCard(charaID, data.cardID);

            // Clear all the icons
            if (cardIcons.Count > 0)
            {
                foreach (var icon in cardIcons.Values)
                {
                    if (icon != null)
                    {
                        UnityEngine.Object.Destroy(icon);
                    }
                }
                cardIcons.Clear();
            }
        }
        catch (Exception ex)
        {
            ModInstance.Log($"Error in StoryCallsChooseGiftDonePostfix: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(PickerMenu), "CancelPicker")]
    [HarmonyPostfix]
    public static void PickerMenuCancelPickerPostfix(PickerMenu __instance)
    {
        try
        {
            lastCharaID = null;
            if (cardIcons.Count > 0)
            {
                foreach (var icon in cardIcons.Values)
                {
                    if (icon != null)
                    {
                        UnityEngine.Object.Destroy(icon);
                    }
                }
                cardIcons.Clear();
            }
        }
        catch (Exception ex)
        {
            ModInstance.Log($"Error in PickerMenuCancelPickerPostfix: {ex.Message}");
        }
    }
}
