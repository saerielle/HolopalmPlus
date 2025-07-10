using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HolopalmPlus
{
    public class MapStoryOverlay : MonoBehaviour
    {
        public class StoryItem
        {
            public string id;
            public string type;
            public string charaID;
            public string icon;
            public string description;
        }

        private GameObject overlayPanel;
        private Transform itemContainer => overlayPanel?.transform.Find("ItemContainer");
        private GameObject itemPrefab;
        // private GameObject moreIndicator;

        private GameObject filterPanel;
        private GameObject filterToggleButton;
        private Transform typeFilterContainer => filterPanel?.transform.Find("FilterList");

        private List<StoryItem> storyItems = [];
        private readonly List<StoryItem> filteredItems = [];
        private readonly List<string> ignoredIds = [];
        private readonly Dictionary<string, GameObject> itemGameObjects = [];
        private readonly Dictionary<string, Sprite> cachedPortraits = [];

        // Available types
        private readonly string[] availableTypes =
        [
            "charaStory",
            "charaInteraction",
            "charaBirthday",
            "custom",
            // "socksStory",
            "ignoredItems"
        ];

        // Constants
        private const int MAX_VISIBLE_ITEMS = 5;
        private const float ITEM_HEIGHT = 40f;
        private const float ITEM_SPACING = 5f;
        private const float FILTER_PANEL_WIDTH = 350f;
        private const float FILTER_PANEL_HEIGHT = 300f;

        private void Awake()
        {
            CreateOverlayUI();
        }

        private void OnDestroy()
        {
            CleanupCachedPortraits();
        }

        private void CreateOverlayUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("MapStoryCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            overlayPanel = new GameObject("MapStoryOverlay");
            overlayPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = overlayPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            panelRect.sizeDelta = new Vector2(300, 280);

            Image bg = overlayPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = true;

            CreateFilterToggleButton();

            GameObject container = new GameObject("ItemContainer");
            container.transform.SetParent(overlayPanel.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 30);
            containerRect.offsetMax = new Vector2(-10, -40);

            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = ITEM_SPACING;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            CreateItemPrefab();
            // CreateMoreIndicator();
            CreateFilterPanel();

            overlayPanel.SetActive(false);
        }

        private void CreateFilterToggleButton()
        {
            filterToggleButton = new GameObject("FilterToggleButton");
            filterToggleButton.transform.SetParent(overlayPanel.transform, false);

            RectTransform btnRect = filterToggleButton.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-10, -5);
            btnRect.sizeDelta = new Vector2(80, 30);

            Image btnBg = filterToggleButton.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.3f, 0.4f, 0.8f);

            Button btn = filterToggleButton.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(ToggleFilterPanel);

            GameObject btnText = new GameObject("Text");
            btnText.transform.SetParent(filterToggleButton.transform, false);
            TextMeshProUGUI text = btnText.AddComponent<TextMeshProUGUI>();
            text.text = "Filters";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void CreateFilterPanel()
        {
            filterPanel = new GameObject("FilterPanel");
            filterPanel.transform.SetParent(overlayPanel.transform.parent, false);
            filterPanel.SetActive(false);

            RectTransform filterRect = filterPanel.AddComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(1, 1);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.pivot = new Vector2(1, 1);
            filterRect.anchoredPosition = new Vector2(-320, -10);
            filterRect.sizeDelta = new Vector2(200, 180);

            Image filterBg = filterPanel.AddComponent<Image>();
            filterBg.color = new Color(0.2f, 0.3f, 0.4f, 0.8f);
            filterBg.raycastTarget = true;

            VerticalLayoutGroup panelLayout = filterPanel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 8;
            panelLayout.padding = new RectOffset(10, 10, 10, 10);
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childAlignment = TextAnchor.UpperCenter;

            GameObject header = new GameObject("Header");
            header.transform.SetParent(filterPanel.transform, false);

            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = "Notification Filters";
            headerText.fontSize = 16;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = Color.white;
            headerText.fontStyle = FontStyles.Bold;

            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.minHeight = 25;
            headerLayout.preferredHeight = 25;
            headerLayout.flexibleHeight = 0;

            GameObject filterList = new GameObject("FilterList");
            filterList.transform.SetParent(filterPanel.transform, false);

            LayoutElement filterListLayout = filterList.AddComponent<LayoutElement>();
            filterListLayout.flexibleHeight = 1;
            filterListLayout.minHeight = 120;

            VerticalLayoutGroup listLayout = filterList.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 2;
            listLayout.padding = new RectOffset(0, 0, 5, 0);
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childAlignment = TextAnchor.UpperLeft;

            CreateFilterCheckboxes();
        }
        private void CreateFilterCheckboxes()
        {
            foreach (var type in availableTypes)
            {
                GameObject checkbox = new GameObject($"Filter_{type}");
                checkbox.transform.SetParent(typeFilterContainer, false);

                RectTransform checkRect = checkbox.AddComponent<RectTransform>();
                checkRect.sizeDelta = new Vector2(170, 25);

                HorizontalLayoutGroup hlg = checkbox.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;

                GameObject toggleObj = new GameObject("Toggle");
                toggleObj.transform.SetParent(checkbox.transform, false);

                Toggle toggle = toggleObj.AddComponent<Toggle>();
                RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
                toggleRect.sizeDelta = new Vector2(20, 20);

                GameObject toggleBg = new GameObject("Background");
                toggleBg.transform.SetParent(toggleObj.transform, false);
                Image bgImage = toggleBg.AddComponent<Image>();
                bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1);
                RectTransform bgRect = toggleBg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;

                GameObject checkmark = new GameObject("Checkmark");
                checkmark.transform.SetParent(toggleObj.transform, false);
                Image checkImage = checkmark.AddComponent<Image>();
                checkImage.color = Color.white;
                RectTransform checkRect2 = checkmark.GetComponent<RectTransform>();
                checkRect2.anchorMin = new Vector2(0.1f, 0.1f);
                checkRect2.anchorMax = new Vector2(0.9f, 0.9f);
                checkRect2.sizeDelta = Vector2.zero;
                checkRect2.anchoredPosition = Vector2.zero;

                toggle.targetGraphic = bgImage;
                toggle.graphic = checkImage;
                toggle.isOn = HolopalmSave.GetOverlayFilter(type, true);

                GameObject label = new GameObject("Label");
                label.transform.SetParent(checkbox.transform, false);

                TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
                labelText.text = GetFriendlyTypeName(type);
                labelText.fontSize = 14;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;

                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(130, 20);

                string typeId = type;
                toggle.onValueChanged.AddListener((bool value) =>
                {
                    OnTypeFilterToggled(typeId, value);
                });
            }
        }

        private string GetFriendlyTypeName(string type)
        {
            return type switch
            {
                "charaStory" => "Character Stories",
                "charaInteraction" => "Interactions",
                "charaBirthday" => "Birthdays",
                "custom" => "Colony Events",
                // "socksStory" => "Socks Stories",
                "ignoredItems" => "Ignored",
                _ => type,
            };
        }

        private void OnTypeFilterToggled(string filterId, bool isActive)
        {
            HolopalmSave.SetOverlayFilter(filterId, isActive);

            int activeCount = 0;
            int totalCount = availableTypes.Length - 1;
            foreach (var type in availableTypes)
            {
                if (type != "ignoredItems" && HolopalmSave.GetOverlayFilter(type, true))
                {
                    activeCount++;
                }
            }

            TextMeshProUGUI btnText = filterToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (activeCount == totalCount)
                {
                    btnText.text = "Filters";
                }
                else
                {
                    btnText.text = $"Filters ({activeCount})";
                }
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredItems.Clear();

            foreach (var item in storyItems)
            {
                if (!HolopalmSave.GetOverlayFilter("ignoredItems", false) && ignoredIds.Contains(item.id))
                {
                    continue;
                }

                if (!HolopalmSave.GetOverlayFilter(item.type, true))
                {
                    continue;
                }

                filteredItems.Add(item);
            }

            RefreshDisplay();
        }

        private void ToggleFilterPanel()
        {
            if (filterPanel != null)
            {
                bool newState = !filterPanel.activeSelf;
                filterPanel.SetActive(newState);
            }
            else
            {
            }
        }

        private void CreateItemPrefab()
        {
            itemPrefab = new GameObject("StoryItemPrefab");
            itemPrefab.SetActive(false);
            itemPrefab.transform.SetParent(transform);

            RectTransform itemRect = itemPrefab.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(280, ITEM_HEIGHT);

            Image itemBg = itemPrefab.AddComponent<Image>();
            itemBg.color = new Color(0.27f, 0.42f, 0.53f, 0.6f);

            HorizontalLayoutGroup hlg = itemPrefab.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5, 5, 5, 5);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            GameObject iconHolder = new GameObject("StoryItemIconHolder");
            iconHolder.transform.SetParent(itemPrefab.transform, false);
            RectTransform iconRect = iconHolder.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(30, 30);

            LayoutElement iconLayout = iconHolder.AddComponent<LayoutElement>();
            iconLayout.minWidth = 30;
            iconLayout.minHeight = 30;
            iconLayout.preferredWidth = 30;
            iconLayout.preferredHeight = 30;

            Image iconImage = iconHolder.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = true;
            iconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            GameObject iconTextGO = new GameObject("IconText");
            iconTextGO.transform.SetParent(iconHolder.transform, false);
            TextMeshProUGUI iconText = iconTextGO.AddComponent<TextMeshProUGUI>();
            iconText.text = "?";
            iconText.fontSize = 20;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.raycastTarget = true;
            iconText.color = Color.white;

            RectTransform iconTextRect = iconText.GetComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;

            GameObject descHolder = new GameObject("Description");
            descHolder.transform.SetParent(itemPrefab.transform, false);
            RectTransform descRect = descHolder.AddComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(200, 30);

            // TextMeshProUGUI descText = descHolder.AddComponent<TextMeshProUGUI>();
            // descText.text = "";
            // descText.fontSize = 14;
            // descText.color = Color.white;
            // descText.raycastTarget = true;
            // descText.alignment = TextAlignmentOptions.MidlineLeft;

            // LayoutElement descLayout = descHolder.AddComponent<LayoutElement>();
            // descLayout.flexibleWidth = 1;
            // descLayout.minHeight = 30;
            // descLayout.preferredHeight = 30;
            ContentSizeFitter itemSizeFitter = itemPrefab.AddComponent<ContentSizeFitter>();
            itemSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI descText = descHolder.AddComponent<TextMeshProUGUI>();
            descText.text = "";
            descText.fontSize = 14;
            descText.color = Color.white;
            descText.raycastTarget = true;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.enableWordWrapping = true; // Add this line

            LayoutElement descLayout = descHolder.AddComponent<LayoutElement>();
            descLayout.flexibleWidth = 1;
            descLayout.minHeight = 30;
            descLayout.flexibleHeight = 1; // Add this line
            // Remove preferredHeight to allow flexible sizing

            GameObject xButton = new GameObject("CloseButton");
            xButton.transform.SetParent(itemPrefab.transform, false);
            RectTransform xRect = xButton.AddComponent<RectTransform>();
            xRect.sizeDelta = new Vector2(20, 20);

            LayoutElement xLayout = xButton.AddComponent<LayoutElement>();
            xLayout.minWidth = 20;
            xLayout.minHeight = 20;
            xLayout.preferredWidth = 20;
            xLayout.preferredHeight = 20;

            Image xBg = xButton.AddComponent<Image>();
            xBg.color = new Color(0.8f, 0.2f, 0.2f, 0f);

            Button btn = xButton.AddComponent<Button>();
            btn.targetGraphic = xBg;
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() => { });

            GameObject xTextGO = new GameObject("X");
            xTextGO.transform.SetParent(xButton.transform, false);
            TextMeshProUGUI xText = xTextGO.AddComponent<TextMeshProUGUI>();
            xText.text = "×";
            xText.fontSize = 16;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = Color.white;
            xText.raycastTarget = true;

            RectTransform xTextRect = xText.GetComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.offsetMin = Vector2.zero;
            xTextRect.offsetMax = Vector2.zero;
        }

        // private void CreateMoreIndicator()
        // {
        //     moreIndicator = new GameObject("MoreIndicator");
        //     moreIndicator.transform.SetParent(overlayPanel.transform, false);
        //     moreIndicator.SetActive(false);

        //     RectTransform moreRect = moreIndicator.AddComponent<RectTransform>();
        //     moreRect.anchorMin = new Vector2(0.5f, 0);
        //     moreRect.anchorMax = new Vector2(0.5f, 0);
        //     moreRect.pivot = new Vector2(0.5f, 0);
        //     moreRect.anchoredPosition = new Vector2(0, 10);
        //     moreRect.sizeDelta = new Vector2(50, 20);

        //     TextMeshProUGUI moreText = moreIndicator.AddComponent<TextMeshProUGUI>();
        //     moreText.text = "...";
        //     moreText.fontSize = 16;
        //     moreText.alignment = TextAlignmentOptions.Center;
        //     moreText.color = Color.white;
        //     moreText.raycastTarget = true;
        // }

        public void UpdateStoryItems(StoryItem[] items)
        {
            // items = items.Where(item => !ignoredIds.Contains(item.id)).ToArray();

            ClearItems();

            storyItems = [.. items];

            ApplyFilters();
        }

        public void RemoveItem(string id, bool ignore = false)
        {
            // storyItems.RemoveAll(item => item.id == id);
            if (ignore)
            {
                ignoredIds.Add(id);
            }
            ApplyFilters();
        }

        public void ClearIgnoredItems()
        {
            ignoredIds.Clear();
        }

        public void RemoveCharaItems(string charaID)
        {
            storyItems.RemoveAll(item => item.charaID == charaID);
            ApplyFilters();
        }

        private void ClearItems()
        {
            foreach (var kvp in itemGameObjects)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            itemGameObjects.Clear();
            storyItems.Clear();
            filteredItems.Clear();
        }

        private void RefreshDisplay()
        {
            if (itemContainer == null)
            {
                return;
            }

            foreach (var kvp in itemGameObjects)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            itemGameObjects.Clear();

            if (filteredItems.Count == 0)
            {
                // moreIndicator.SetActive(false);
                return;
            }

            int visibleCount = Mathf.Min(filteredItems.Count, MAX_VISIBLE_ITEMS);
            for (int i = 0; i < visibleCount; i++)
            {
                CreateItemGameObject(filteredItems[i]);
            }

            // moreIndicator.SetActive(filteredItems.Count > MAX_VISIBLE_ITEMS);
        }

        private void CreateItemGameObject(StoryItem item)
        {
            if (itemPrefab == null)
            {
                return;
            }

            if (itemContainer == null)
            {
                return;
            }

            GameObject itemGO = Instantiate(itemPrefab);
            itemGO.transform.SetParent(itemContainer, false);
            itemGO.SetActive(true);
            itemGO.name = $"StoryItem_{item.id}";

            RectTransform rt = itemGO.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = itemGO.AddComponent<RectTransform>();
            }
            rt.sizeDelta = new Vector2(280, ITEM_HEIGHT);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Transform iconHolder = itemGO.transform.Find("StoryItemIconHolder");
            if (iconHolder == null)
            {
                return;
            }

            Image iconImage = iconHolder.GetComponent<Image>();
            TextMeshProUGUI iconText = iconHolder.Find("IconText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = itemGO.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            Button closeBtn = itemGO.transform.Find("CloseButton")?.GetComponent<Button>();

            if (descText != null)
            {
                descText.text = item.description;
            }

            bool hasPortrait = false;
            if (!string.IsNullOrEmpty(item.charaID))
            {
                try
                {
                    Sprite portrait = LoadCharacterPortrait(item.charaID);
                    if (portrait != null && iconImage != null)
                    {
                        iconImage.sprite = portrait;
                        iconImage.color = Color.white;
                        if (iconText != null) iconText.text = "";
                        hasPortrait = true;
                    }
                }
                catch (Exception e)
                {
                    ModInstance.Log($"[MapStoryOverlay] Error loading portrait: {e.Message}");
                }
            }

            if (!hasPortrait && iconText != null)
            {
                if (iconImage != null)
                    iconImage.color = new Color(0, 0, 0, 0);

                if (!string.IsNullOrEmpty(item.icon))
                {
                    iconText.text = item.icon;
                }
                else
                {
                    iconText.text = "★";
                }
            }

            if (closeBtn != null)
            {
                string itemId = item.id;
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => RemoveItem(itemId, true));
            }

            itemGameObjects[item.id] = itemGO;

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemContainer as RectTransform);
        }

        private Sprite LoadCharacterPortrait(string charaID)
        {
            Chara chara = Chara.FromID(charaID);

            if (chara == null)
            {
                return null;
            }

            string portraitName = chara.charaID + Princess.artStage.ToString();
            Sprite sprite = AssetManagerLoadWrapper(portraitName);

            if (sprite == null)
            {
                portraitName = chara.charaID;
                sprite = AssetManagerLoadWrapper(portraitName);
            }

            return sprite;
        }

        private Sprite AssetManagerLoadWrapper(string portraitName)
        {
            if (cachedPortraits.ContainsKey(portraitName) && cachedPortraits[portraitName] != null)
            {
                return cachedPortraits[portraitName];
            }

            Sprite originalSprite = AssetManager.LoadCharaPortrait(portraitName);
            if (originalSprite != null)
            {
                Sprite copiedSprite = CreateSpriteCopy(originalSprite);
                cachedPortraits[portraitName] = copiedSprite;
                return copiedSprite;
            }

            return null;
        }

        private Sprite CreateSpriteCopy(Sprite originalSprite)
        {
            Texture2D originalTexture = originalSprite.texture;

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                originalTexture.width,
                originalTexture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );

            Graphics.Blit(originalTexture, renderTexture);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.ARGB32, false);
            newTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            Sprite newSprite = Sprite.Create(
                newTexture,
                originalSprite.rect,
                originalSprite.pivot,
                originalSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            return newSprite;
        }

        private void CleanupCachedPortraits()
        {
            foreach (var kvp in cachedPortraits)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.texture != null)
                    {
                        DestroyImmediate(kvp.Value.texture);
                    }
                    DestroyImmediate(kvp.Value);
                }
            }
            cachedPortraits.Clear();
        }

        private System.Collections.IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = 0.5f)
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut(CanvasGroup canvasGroup, float duration = 0.5f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            overlayPanel.SetActive(false);
            yield return null;
        }

        public void SetOverlayActive(bool active, bool force = false)
        {
            if (!active)
            {
                filterPanel?.SetActive(false);
            }

            if (overlayPanel != null)
            {
                if (force)
                {
                    CanvasGroup canvasGroup = overlayPanel.GetComponent<CanvasGroup>() ?? overlayPanel.AddComponent<CanvasGroup>();
                    if (active)
                    {
                        canvasGroup.alpha = 0f;
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                        StartCoroutine(FadeIn(canvasGroup));
                        overlayPanel.SetActive(true);
                    }
                    else
                    {
                        canvasGroup.alpha = 1f;
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                        StartCoroutine(FadeOut(canvasGroup));
                        filterPanel?.SetActive(false);
                    }
                }
                else
                {
                    overlayPanel.SetActive(active);
                }
            }
            else if (force)
            {
                ModInstance.Log("[MapStoryOverlay] Overlay panel not found, creating new one.");
                CreateOverlayUI();
                SetOverlayActive(active, false);
            }
        }
    }
}
