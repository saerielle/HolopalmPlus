using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Northway.Utils;

namespace HolopalmPlus
{
    [HarmonyPatch]
    public class MapStoryPatches
    {
        private static MapStoryOverlay overlayInstance;
        private static int lastMonth = 0;
        private static string lastScene = string.Empty;
        private static readonly string[] ignoredStoryIDEnds =
        [
            "repeat", "rebuilding", "mourning", "bffs", "secretadmirer"
        ];
        private static readonly List<string> ignoredStoryIDs =
        [
            "tammyWeek1", "tammyWeek1b"
        ];
        private static readonly List<string> socksStoryIDs =
        [
            "geoponicsCalPetStart", "geoponicsCalPetQuestStart", "geoponicsCalPetDiscovered"
        ];

        private static readonly List<string> exocomfortsSkillStoryIds =
            new List<string>
            {
                "anemoneSkillReminder1", "anemoneSkillReminder2", "anemoneSkillReminder3",
                "calSkillReminder1", "calSkillReminder2", "calSkillReminder3",
                "dysSkillReminder1", "dysSkillReminder2", "dysSkillReminder3",
                "marzSkillReminder1", "marzSkillReminder2", "marzSkillReminder3",
                "tammySkillReminder1", "tammySkillReminder2", "tammySkillReminder3",
                "tangSkillReminder1", "tangSkillReminder2", "tangSkillReminder3",
                "symSkillReminder1", "symSkillReminder2",
                "nomiSkillReminder1", "nomiSkillReminder2",
                "rexSkillReminder1", "rexSkillReminder2",
                "vaceSkillReminder1", "vaceSkillReminder2",
                "momSkillReminder1", "momSkillReminder2", "momSkillReminder3",
                "dadSkillReminder1", "dadSkillReminder2", "dadSkillReminder3"
            }
            .ConvertAll(id => id.ToLowerInvariant());

        private static readonly List<string> exocomfortsBirthdayStoryIds =
            new List<string>
            {
                // Vanilla
                "anemoneBirthday", "calBirthday", "dysBirthday", "marzBirthday",
                "tammyBirthday", "tangBirthday", "nomiBirthday",
                "rexBirthday", "vaceBirthday",
                // Gestalt
                "spellaltBirthday", "spellgestBirthday",
                // Extracolonist
                "avianBirthdayReminder", "felixBirthdayReminder", "geneBirthdayReminder",
                "graphBirthdayReminder", "komBirthdayReminder", "melBirthdayReminder",
                "periBirthdayReminder", "sacchiBirthdayReminder"
            }
            .ConvertAll(id => id.ToLowerInvariant());

        private static readonly List<string> mutinyStoryIds =
            new List<string>
            {
                "commandMutiny", "engineeringMutiny", "expeditionsMutiny",
                "garrisonMutiny", "geoponicsMutiny", "quartersMutiny"
            }
            .ConvertAll(id => id.ToLowerInvariant());

        private static bool hasBFFStory = false;
        private static bool hasSecretAdmirerStory = false;

        [HarmonyPatch(typeof(BillboardManager), nameof(BillboardManager.FillMapspots))]
        [HarmonyPostfix]
        public static void FillMapspotsPostfix()
        {
            if (lastMonth == Princess.monthOfGame && lastScene == MapManager.currentScene && overlayInstance != null)
            {
                return;
            }

            SetStories();
        }

        private static void SetStories()
        {
            try
            {
                if (overlayInstance == null)
                {
                    GameObject overlayGO = new GameObject("MapStoryOverlayManager");
                    overlayInstance = overlayGO.AddComponent<MapStoryOverlay>();
                    GameObject.DontDestroyOnLoad(overlayGO);
                }

                lastMonth = Princess.monthOfGame;
                lastScene = MapManager.currentScene;
                hasBFFStory = false;
                hasSecretAdmirerStory = false;

                List<MapStoryOverlay.StoryItem> items = [];

                if (Princess.monthOfGame < 2)
                {
                    return;
                }

                if (Princess.seasonAndMonth.ToLowerInvariant() == "dust-1")
                {
                    items.Add(new MapStoryOverlay.StoryItem
                    {
                        id = $"vertumnalia_{Princess.year}",
                        type = "custom",
                        icon = "★",
                        description = "Vertumnalia this month!"
                    });
                }

                if (Princess.HasMemory("mutiny"))
                {
                    int votes = Princess.GetMemoryInt("mutinyVotes");

                    if (votes < 6)
                    {
                        List<string> missingStories = mutinyStoryIds.FindAll(id => !Princess.HasStory(id));
                        List<string> locations = missingStories.ConvertAll(id => id.Replace("mutiny", ""));

                        if (locations.Contains("quarters") && votes < 3)
                        {
                            locations.Remove("quarters");
                        }

                        if (!locations.Contains("quarters") && votes >= 3 && !Princess.HasMemory("mutinyQuarters"))
                        {
                            locations.Add("quarters");
                        }

                        if (locations.Count > 0)
                        {
                            if (locations.Count == mutinyStoryIds.Count)
                            {
                                items.Add(new MapStoryOverlay.StoryItem
                                {
                                    id = "mutinyAll",
                                    type = "custom",
                                    icon = "★",
                                    description = "Get votes from the council members"
                                });
                            }
                            else
                            {
                                string locationsString = string.Join(", ", locations.ConvertAll(loc => char.ToUpper(loc[0]) + loc.Substring(1)));
                                if (locations.Count > 1)
                                {
                                    locationsString = locationsString.Substring(0, locationsString.LastIndexOf(", ")) + " and " + locationsString.Substring(locationsString.LastIndexOf(", ") + 2);
                                }

                                string start = locations.Count > 1 ? "Get votes from the" : "Get a vote from the";
                                string ending = locations.Count > 1 ? "council members" : "council member";

                                items.Add(new MapStoryOverlay.StoryItem
                                {
                                    id = $"mutiny_{votes}",
                                    type = "custom",
                                    icon = "★",
                                    description = $"{start} {locationsString} {ending}"
                                });
                            }
                        }
                    }
                }

                foreach (Chara chara in Chara.allCharas)
                {
                    MapStoryOverlay.StoryItem charaStory = GetCharaStory(chara);
                    if (charaStory != null)
                    {
                        items.Add(charaStory);
                    }
                }

                if (hasBFFStory)
                {
                    items.Insert(0, new MapStoryOverlay.StoryItem
                    {
                        id = "bffs",
                        type = "custom",
                        icon = "★",
                        description = "Choose your BFF!"
                    });
                }

                if (hasSecretAdmirerStory)
                {
                    items.Insert(0, new MapStoryOverlay.StoryItem
                    {
                        id = "secretadmirer",
                        type = "custom",
                        icon = "★",
                        description = "Find your Secret Admirer!"
                    });
                }

                if (MapManager.IsColonyScene(MapManager.currentScene) && Princess.monthOfYear != 13)
                {
                    Chara sym = Chara.FromID("sym");
                    if (sym != null && sym.hasMet)
                    {
                        Result result = new Result();
                        WHHelper.SetReadonlyField(result, "chara", sym);
                        Story symStory = Story.PickCharaStory(result);
                        if (symStory != null && ShouldShowStory(symStory))
                        {
                            items.Add(new MapStoryOverlay.StoryItem
                            {
                                id = $"story_{symStory.storyID}",
                                type = "charaStory",
                                charaID = sym.charaID,
                                description = $"{sym.nickname} may have a new story"
                            });
                        }
                    }

                    // MapStoryOverlay.StoryItem socksStory = CheckSocksStory();
                    // if (socksStory != null)
                    // {
                    //     items.Add(socksStory);
                    // }
                }

                overlayInstance.UpdateStoryItems([.. items]);

                bool isActive = HolopalmSave.GetSetting("showMapStoryOverlay") && items.Count > 0;
                overlayInstance.SetOverlayActive(isActive, isActive);
            }
            catch (System.Exception ex)
            {
                ModInstance.Log($"Error in SetStories: {ex.Message}");
            }
        }

        private static MapStoryOverlay.StoryItem CheckSocksStory()
        {
            Location location = Location.FromID("geoponics");
            if (location == null)
            {
                return null;
            }

            List<Job> jobs = Job.FromLocation(location);

            foreach (Job job in jobs)
            {
                if (job == null)
                {
                    continue;
                }

                Result result = new Result(job);
                Story story = Story.PickBeforeJobStory(result);
                if (story != null && story.storyID != null && socksStoryIDs.Contains(story.storyID))
                {
                    return new MapStoryOverlay.StoryItem
                    {
                        id = $"socks_{story.storyID}",
                        type = "socksStory",
                        charaID = "socks",
                        description = $"Socks has a new story in {job.jobName}"
                    };
                }
            }

            return null;
        }

        private static bool ShouldShowStory(Story story)
        {
            if (story == null)
            {
                return false;
            }

            if (story.hasExecuted && HasSeenStoryThisMonth(story.storyID))
            {
                return false;
            }

            if (ignoredStoryIDs.Contains(story.storyID))
            {
                return false;
            }

            foreach (string end in ignoredStoryIDEnds)
            {
                if (story.storyID.EndsWith(end))
                {
                    if (end == "bffs" && !hasBFFStory)
                    {
                        hasBFFStory = true;
                    }
                    else if (end == "secretadmirer" && !hasSecretAdmirerStory)
                    {
                        hasSecretAdmirerStory = true;
                    }
                    return false;
                }
            }

            return true;
        }

        private static bool IsSkillStoryReq(StoryReq req)
        {
            if (req.type == StoryReqType.skill)
            {
                Skill skill = Skill.FromID(req.stringID);
                if (skill != null && !skill.isSpecial && (skill.suit == CardSuit.mental || skill.suit == CardSuit.physical || skill.suit == CardSuit.social))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasSeenStoryThisMonth(string storyID)
        {
            int storyMonth = Princess.GetStoryMonth(storyID);

            if (storyMonth == -1)
            {
                return false;
            }

            return Princess.monthOfGame == storyMonth;
        }

        private static bool IsBirthday(Chara chara)
        {
            if (chara == null || !chara.onMap || chara.isDead || !chara.hasMet)
            {
                return false;
            }

            if (!chara.isBirthday || chara.love >= 100 || !StoryCalls.cangift(chara.charaID))
            {
                return false;
            }

            if (HasSeenStoryThisMonth(chara.charaID + "repeat"))
            {
                return false;
            }

            string exocomfortsStoryId = exocomfortsBirthdayStoryIds.Find(id => id.StartsWith(chara.charaID));

            if (exocomfortsStoryId != null && HasSeenStoryThisMonth(exocomfortsStoryId))
            {
                return false;
            }

            return Princess.HasGroundhog("fact_" + chara.charaID + "_" + CharaFact.birthday, evenIfDisabled: true) && Princess.HasMemory("foundCollectible");
        }

        private static string GetCharaName(Chara chara)
        {
            if (chara == null)
            {
                return "";
            }

            if (chara.charaID == "mom")
            {
                return "Mom";
            }
            else if (chara.charaID == "dad")
            {
                return "Dad";
            }
            else
            {
                return chara.nicknameIncludingNemmie;
            }
        }

        private static MapStoryOverlay.StoryItem GetCharaStory(Chara chara)
        {
            try
            {
                if (chara == null)
                {
                    return null;
                }

                if (chara.charaID != "sym" && (!chara.onMap || chara.isDead))
                {
                    return null;
                }

                MapSpot charaSpot = MapSpot.allMapSpots.Find(spot => spot.type == MapSpotType.chara && spot.charaID == chara.charaID);

                if (charaSpot == null || !charaSpot.isActiveAndEnabled)
                {
                    return null;
                }

                if (IsBirthday(chara))
                {
                    return new MapStoryOverlay.StoryItem
                    {
                        id = $"bday_{chara.charaID}_{Princess.year}",
                        type = "charaBirthday",
                        charaID = chara.charaID,
                        description = $"{GetCharaName(chara)}'s birthday!"
                    };
                }

                if (charaSpot.story != null)
                {
                    // if (!chara.hasMet && chara.charaID != "sym")
                    // {
                    //     return new MapStoryOverlay.StoryItem
                    //     {
                    //         id = $"meet_{chara.charaID}",
                    //         type = "charaStory",
                    //         charaID = chara.charaID,
                    //         description = $"{GetCharaName(chara)} is waiting to be met"
                    //     };
                    // }

                    if (ShouldShowStory(charaSpot.story))
                    {
                        if (exocomfortsSkillStoryIds.Contains(charaSpot.story.storyID.ToLowerInvariant()))
                        {
                            if (HasSeenStoryThisMonth(charaSpot.story.storyID))
                            {
                                return null;
                            }
                            return new MapStoryOverlay.StoryItem
                            {
                                id = $"skill_{charaSpot.storyName}",
                                type = "charaInteraction",
                                charaID = chara.charaID,
                                description = $"New interaction with {GetCharaName(chara)}"
                            };
                        }

                        if (charaSpot.story.storyID.ToLowerInvariant() == "rexbarreminder")
                        {
                            return new MapStoryOverlay.StoryItem
                            {
                                id = $"rex_bar_{Princess.year}",
                                type = "charaInteraction",
                                charaID = chara.charaID,
                                description = $"{GetCharaName(chara)} needs logs for the bar"
                            };
                        }

                        return new MapStoryOverlay.StoryItem
                        {
                            id = $"story_{charaSpot.storyName}",
                            type = "charaStory",
                            charaID = chara.charaID,
                            description = $"{GetCharaName(chara)} has a new story"
                        };
                    }

                    if (charaSpot.storyName.ToLower() == $"{chara.charaID}repeat".ToLower())
                    {
                        Result result = new Result();
                        WHHelper.SetReadonlyField(result, "chara", chara);
                        foreach (Choice choice in charaSpot.story.allChoices)
                        {
                            if (choice.level != 1 || choice.isDone)
                            {
                                continue;
                            }

                            if (choice.CanShow(result) && choice.CanExecuteShown(result))
                            {
                                string buttonText = choice.GetButtonText();
                                if (buttonText == "...")
                                {
                                    continue;
                                }

                                if (chara.isDatingYou && choice.hasStory && choice.isFlirt)
                                {
                                    return new MapStoryOverlay.StoryItem
                                    {
                                        id = $"date_{chara.charaID}_{Princess.year}",
                                        type = "charaInteraction",
                                        charaID = chara.charaID,
                                        description = $"{GetCharaName(chara)} wants to go on a date!"
                                    };
                                }

                                if (!HasSeenStoryThisMonth(chara.charaID + "skillreminder1") &&
                                    !HasSeenStoryThisMonth(chara.charaID + "skillreminder2") &&
                                    !HasSeenStoryThisMonth(chara.charaID + "skillreminder3") &&
                                    !HasSeenStoryThisMonth(chara.charaID + "repeat"))
                                {
                                    StoryReq req = choice.requirements.Find(IsSkillStoryReq);

                                    if (req != null)
                                    {
                                        return new MapStoryOverlay.StoryItem
                                        {
                                            id = $"skill_{chara.charaID}_{req.stringID}",
                                            type = "charaInteraction",
                                            charaID = chara.charaID,
                                            description = $"New interaction with {GetCharaName(chara)}"
                                        };
                                    }
                                }

                                if (chara.charaID == "rex")
                                {
                                    if (buttonText == "\"Hugs incoming!\"" && chara.love < 100)
                                    {
                                        return new MapStoryOverlay.StoryItem
                                        {
                                            id = $"hug_{chara.charaID}_{Princess.year}",
                                            type = "charaInteraction",
                                            charaID = chara.charaID,
                                            description = $"{GetCharaName(chara)} could use a hug"
                                        };
                                    }
                                }

                                if (chara.charaID == "graph" && buttonText == "\"Are you open for commissions?\"")
                                {
                                    return new MapStoryOverlay.StoryItem
                                    {
                                        id = $"commission_{chara.charaID}_{Princess.year}",
                                        type = "charaInteraction",
                                        charaID = chara.charaID,
                                        description = $"{GetCharaName(chara)} is open for commissions"
                                    };
                                }
                            }
                        }
                    }
                }

                if (chara.charaID == "rex")
                {
                    if (Princess.HasStory("rex2BuildStart") && !Princess.HasMemory("loungeBar") && StoryCalls.cangift("rex"))
                    {
                        return new MapStoryOverlay.StoryItem
                        {
                            id = $"rex_lounge_{Princess.year}",
                            type = "charaInteraction",
                            charaID = chara.charaID,
                            description = $"{chara.nickname} needs logs for the bar"
                        };
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModInstance.Log($"Error getting character story for {chara?.charaID}: {ex.Message}");
            }

            return null;
        }

        [HarmonyPatch(typeof(LoadingMenu), "ShowLoading")]
        [HarmonyPrefix]
        public static void LoadingMenuShowLoadingPrefix()
        {
            lastMonth = 0;
            lastScene = string.Empty;
            overlayInstance?.SetOverlayActive(false);
        }

        [HarmonyPatch(typeof(Savegame), nameof(Savegame.Load))]
        [HarmonyPostfix]
        public static void SavegameLoadPostfix()
        {
            lastMonth = 0;
            lastScene = string.Empty;
            overlayInstance?.SetOverlayActive(false);
            overlayInstance?.ClearIgnoredItems();
        }

        // public static void RemoveStoryItem(string itemId)
        // {
        //     overlayInstance?.RemoveItem(itemId);
        // }

        public static MapStoryOverlay GetOverlayInstance()
        {
            return overlayInstance;
        }

        [HarmonyPatch(typeof(Story), nameof(Story.Execute))]
        [HarmonyPrefix]
        public static bool StoryExecutePrefix(Story __instance)
        {
            Story story = __instance;
            if (story == null)
            {
                return true;
            }

            if (story.mapSpotType == MapSpotType.collectible)
            {
                return true;
            }

            overlayInstance?.SetOverlayActive(false);
            return true;
        }

        [HarmonyPatch(typeof(EndingMenu), nameof(EndingMenu.ShowEnding))]
        [HarmonyPostfix]
        public static void EndingMenuShowEndingPostfix(Ending __instance)
        {
            overlayInstance?.SetOverlayActive(false);
        }

        [HarmonyPatch(typeof(Result), nameof(Result.FinishStory))]
        [HarmonyPostfix]
        public static void ResultFinishStoryPostfix(Result __instance)
        {

            if (Singleton<BattleMenu>.instance.currentBattle != null && (Singleton<BattleHeaderMenu>.isOpen || Singleton<BattleHeaderMenu>.isOpening))
            {
                return;
            }
            
            if (Singleton<EndingMenu>.isOpen)
            {
                return;
            }

            // if (Singleton<ResultsMenu>.isOpen)
            // {
            //     ModInstance.Log("[MapStoryPatches] ResultsMenu is active, skipping story update");
            //     return;
            // }

            if (__instance != null && (__instance.mapSpot?.type == MapSpotType.collectible || __instance.mapSpotType == MapSpotType.collectible))
            {
                return;
            }

            SetStories();

            // if (__instance == null || __instance.story == null)
            // {
            //     return;
            // }

            // if (__instance.mapSpot != null && __instance.mapSpot.type == MapSpotType.chara)
            // {
            //     SetStories();
            // }
        }

        [HarmonyPatch(typeof(ResultsMenu), nameof(ResultsMenu.ShowResult))]
        [HarmonyPostfix]
        public static void ResultsMenuShowResultPostfix(ResultsMenu __instance)
        {
            overlayInstance?.SetOverlayActive(false);
        }

        // [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.ShowMenu))]
        // [HarmonyPrefix]
        // public static bool MenuManagerShowMenuPrefix(MenuType type, bool force = false, bool immediate = false)
        // {
        //     overlayInstance?.SetOverlayActive(false);
        //     return true;
        // }

        [HarmonyPatch(typeof(BattleMenu), nameof(BattleMenu.StartBattle))]
        [HarmonyPrefix]
        public static bool BattleMenuStartBattlePrefix()
        {
            overlayInstance?.SetOverlayActive(false);
            return true;
        }

        [HarmonyPatch(typeof(StoryCalls), nameof(StoryCalls.hideallmenus))]
        [HarmonyPostfix]
        public static void StoryCallsHideAllMenusPostfix()
        {
            overlayInstance?.SetOverlayActive(false);
        }

        // [HarmonyPatch(typeof(BattleMenu), nameof(BattleMenu.WinBattle))]
        // [HarmonyPostfix]
        // public static void BattleMenuWinBattlePostfix()
        // {
        //     overlayInstance?.SetOverlayActive(false);
        // }

        // [HarmonyPatch(typeof(BattleMenu), nameof(BattleMenu.LoseBattle))]
        // [HarmonyPostfix]
        // public static void BattleMenuLoseBattlePostfix()
        // {
        //     overlayInstance?.SetOverlayActive(false);
        // }

        // [HarmonyPatch(typeof(ResultsMenu), "OnOpen")]
        // [HarmonyPostfix]
        // public static void ResultsMenuOnOpenPostfix(ResultsMenu __instance)
        // {
        //     if (__instance == null || overlayInstance == null)
        //     {
        //         return;
        //     }

        //     overlayInstance.SetOverlayActive(false);
        // }

        // [HarmonyPatch(typeof(ResultsMenu), "DoneClicked")]
        // [HarmonyPostfix]
        // public static void ResultsMenuDoneClickedPostfix(ResultsMenu __instance)
        // {
        //     if (__instance == null || overlayInstance == null)
        //     {
        //         return;
        //     }

        //     SetStories();
        // }

        // [HarmonyPatch(typeof(ResultsMenu), "OnStartClose")]
        // [HarmonyPostfix]
        // public static void ResultsMenuOnStartClosePostfix(ResultsMenu __instance)
        // {
        //     if (__instance == null || overlayInstance == null)
        //     {
        //         return;
        //     }

        //     SetStories();
        // }
    }
}
