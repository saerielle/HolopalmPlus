using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Northway.Utils;
using System.Linq;

namespace HolopalmPlus;

[HarmonyPatch]
public class JobPatches
{
    private static Dictionary<string, GameObject> storyIcons = new Dictionary<string, GameObject>();

    [HarmonyPatch(typeof(NWButtonResults), "SetJobDisplay")]
    [HarmonyPostfix]
    public static void SetJobDisplayPostfix(NWButtonResults __instance)
    {
        try
        {
            Job job = (Job)AccessTools.Field(typeof(NWButtonResults), "job").GetValue(__instance);

            if (job == null)
            {
                return;
            }

            if (!HolopalmSave.GetSetting("showJobPreview", true))
            {
                CleanUpOtherLocationIcons("none");
                return;
            }

            string locationID = FixLocationID(job.location);
            CleanUpOtherLocationIcons(locationID);

            string iconKey = $"JobIcon_{locationID}_{job.jobID}";

            if (storyIcons.TryGetValue(iconKey, out GameObject existingIcon))
            {
                if (existingIcon != null)
                {
                    UnityEngine.Object.Destroy(existingIcon);
                    storyIcons.Remove(iconKey);
                }
                else
                {
                    storyIcons.Remove(iconKey);
                }
            }

            if (job.isExpedition)
            {
                GameObject icon = CreateExpeditionIcon(__instance, job, iconKey);
                storyIcons[iconKey] = icon;
            }
            else
            {
                Result result = new(job);
                Story story = Story.PickBeforeJobStory(result, false);

                if (story == null)
                {
                    return;
                }

                GameObject icon = CreateJobIcon(__instance, job, story, iconKey);

                storyIcons[iconKey] = icon;
            }
        }
        catch (Exception ex)
        {
            ModInstance.Log("Error in SetJobDisplayPostfix: " + ex);
        }
    }

    // We want to keep base game locations as they are, but need a special handling for
    // locations added by mods (namely via ExtraLoader)
    private static string FixLocationID(Location location)
    {
        if (location == null || string.IsNullOrEmpty(location.locationID))
        {
            return "unknown";
        }

        switch (location.locationID)
        {
            case "geoponics":
            case "quarters":
            case "engineering":
            case "garrison":
            case "command":
            case "expeditions":
                return location.locationID;

            default:
                if (location.locationName.ToLower().Contains("geoponics"))
                {
                    return "geoponics";
                }
                else if (location.locationName.ToLower().Contains("quarters"))
                {
                    return "quarters";
                }
                else if (location.locationName.ToLower().Contains("engineering"))
                {
                    return "engineering";
                }
                else if (location.locationName.ToLower().Contains("garrison"))
                {
                    return "garrison";
                }
                else if (location.locationName.ToLower().Contains("command"))
                {
                    return "command";
                }
                else if (location.locationName.ToLower().Contains("expeditions"))
                {
                    return "expeditions";
                }
                else
                {
                    return location.locationID;
                }
        }
    }

    private static readonly string[] unlockExpeditionJobs = [.. new[] {
        "surveyUnlockForage", "surveyUnlockArtifactsStart", "exploreUnlockHunt"
    }.Select(s => s.ToLower())];

    private static readonly string[] expeditionStarEvents = [.. new[] {
         "exploreSymMeet",
         "artifactsDysRunaway", "artifactsDysTransformed",
         "artifactsSocks", "forageShimmer",
        //  "forageQuestTangEggs",
         "forageQuestCookingSpice",
         "forageSpongecake", "huntTrack2",
         "huntTrack3", "huntTrack4",
         "surveyQuestFarming", "surveyQuestSocks",
         "surveyNomiLiarFlower", "surveyNomiDysCrystal",
         "surveyNomiDysFriends", "surveyNomiDysChoose",
         // ExtraColonist events down here
        "sneakFindEgg"
    }.Select(s => s.ToLower())];

    private static readonly string[] expeditionStartEvents = [.. new[] {
        "huntStart", "huntAnemone", "huntTrackStart",
        "artifactsStart", "artifactsDysMonitoring", "nearbyUtopiaTonin",
        "nearbyStart", "surveyStart", "surveyNomiStart",
        "artifactsQuestDysTransform", "forageStart", "sneakStart",
        "huntUltimateStart", "expeditionsMutiny"
    }.Select(s => s.ToLower())];

    private static Story GetPossibleExpeditionUnlockStory(Job job)
    {
        foreach (string storyID in unlockExpeditionJobs)
        {
            Story story = Story.FromID(storyID);
            if (story != null && !Princess.HasStory(storyID))
            {
                if (GetExecuteBlockingReq(job, story) == null)
                {
                    return story;
                }
            }
        }

        return null;
    }

    private static Story GetPossibleExpeditionStartStory(Job job)
    {
        foreach (string storyID in expeditionStartEvents)
        {
            Story story = Story.FromID(storyID);
            if (story != null && !Princess.HasStory(storyID))
            {
                if (GetExecuteBlockingReq(job, story) == null)
                {
                    return story;
                }
            }
        }

        return null;
    }

    private static Story GetPossibleExpeditionStarStory(Job job)
    {
        foreach (string storyID in expeditionStarEvents)
        {
            Story story = Story.FromID(storyID);
            if (story != null && !Princess.HasStory(storyID))
            {
                if (GetExecuteBlockingReq(job, story) == null)
                {
                    return story;
                }
            }
        }

        return null;
    }

    private static StoryReq GetExecuteBlockingReq(Job job, Story story)
    {
        foreach (StoryReq requirement in story.entryChoice.requirements)
        {
            if (requirement.type != StoryReqType.mapSpot && !requirement.Execute(new Result(job)))
            {
                return requirement;
            }
        }

        return null;
    }

    private static GameObject CreateExpeditionIcon(NWButtonResults instance, Job job, string iconKey)
    {
        GameObject icon = new GameObject(iconKey);
        icon.transform.SetParent(instance.transform, false);

        TMPro.TextMeshProUGUI textMesh = icon.AddComponent<TMPro.TextMeshProUGUI>();
        textMesh.font = instance.textField.font;
        textMesh.fontSize = 24;
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        icon.transform.localPosition = new Vector3(272, 2, 0);

        if (!job.isExpedition || job.expeditionBiome == null || !Singleton<MapManager>.instance.scenesByBiome.ContainsKey(job.expeditionBiome))
        {
            return icon;
        }

        if (!job.isRelax && Princess.stress >= 100)
        {
            return icon;
        }

        if (Princess.monthOfYear == 13)
        {
            // Glow season, only one expedition job available and it's a one-time thing
            return icon;
        }

        int doneTimes = job.timesWorked;
        if (doneTimes > 0)
        {
            instance.tooltipText += $"\nTimes worked: {doneTimes}";
        }

        string seed = "region" + (job.location?.GetTimesWorked() ?? Princess.month);
        string sceneName = Singleton<MapManager>.instance.scenesByBiome[job.expeditionBiome].PickRandom(seed).sceneName;
        sceneName = MapManager.ConvertToCurrentSceneName(sceneName);

        bool notPopulated = !Savegame.instance.mapSpotsByMap.ContainsKey(sceneName) ||
            (PrincessMonth.AgeForMonth(Princess.GetMemoryInt("visited_" + sceneName.ToLower())) < Princess.age) ||
            (job.expeditionBiome.isNearby && Job.FromID(Princess.GetMemory("visitedjob_" + sceneName.ToLower())) != job);

        if (notPopulated)
        {
            Story story = GetPossibleExpeditionUnlockStory(job);

            if (story != null)
            {
                textMesh.text = "☆";
                textMesh.color = Color.yellow;
                instance.tooltipText += "\nPossible unlock story";
                return icon;
            }

            story = GetPossibleExpeditionStarStory(job);
            if (story != null)
            {
                textMesh.text = "★";
                textMesh.color = Color.yellow;
                instance.tooltipText += $"\nPossible quest story";
            }

            story = GetPossibleExpeditionStartStory(job);
            if (story != null)
            {
                textMesh.text = "☆";
                textMesh.color = Color.white;
                instance.tooltipText += $"\nPossible start story";
                return icon;
            }
            return icon;
        }

        Dictionary<string, string> dictionary = Savegame.instance.mapSpotsByMap[sceneName];
        Dictionary<MapSpotType, int> dict = new Dictionary<MapSpotType, int>();

        int num = 0;
        bool hasUnlock = false;
        bool hasStar = false;
        bool hasStart = false;

        foreach (string key in dictionary.Keys)
        {
            if (dictionary[key] != null)
            {
                if (unlockExpeditionJobs.Contains(dictionary[key].ToLower()) && !Princess.HasStory(dictionary[key]))
                {
                    hasUnlock = true;
                }
                else if (expeditionStarEvents.Contains(dictionary[key].ToLower()) && !Princess.HasStory(dictionary[key]))
                {
                    hasStar = true;
                }
                else if (expeditionStartEvents.Contains(dictionary[key].ToLower()) && !Princess.HasStory(dictionary[key]))
                {
                    hasStart = true;
                }
            }

            string text = key.StripNonAlpha();
            MapSpotType mapSpotType = text.ParseEnum<MapSpotType>();

            if (mapSpotType == MapSpotType.none)
            {
                if (!text.Contains("cache"))
                {
                    continue;
                }

                mapSpotType = MapSpotType.collectible;
            }

            dict.IncrementInt(mapSpotType);

            num++;
        }

        bool bossRespawned = false;
        if (!hasUnlock && !hasStar && !hasStart)
        {
            if (dict.GetSafe(MapSpotType.boss, 0) == 0)
            {
                Result result = new Result(job)
                {
                    mapSpotType = MapSpotType.boss
                };
                int dateSeed = job.location?.GetTimesWorked() ?? Princess.month;
                if (Story.PickMapSpotStory(result, dateSeed, highPriorityOnly: true) != null)
                {
                    bossRespawned = true;
                }
            }
        }

        if (hasUnlock)
        {
            textMesh.text = "☆";
            textMesh.color = Color.yellow;
            instance.tooltipText += "\nHas unlock story";
        }
        else if (hasStar)
        {
            textMesh.text = "★";
            textMesh.color = Color.yellow;
            instance.tooltipText += "\nHas quest story";
        }
        else if (hasStart)
        {
            textMesh.text = "☆";
            textMesh.color = Color.white;
            instance.tooltipText += "\nHas start story";
        }
        else if (bossRespawned)
        {
            textMesh.text = "★";
            textMesh.color = Color.white;
        }
        else if (num > 0)
        {
            textMesh.text = $"[{num}]";
            textMesh.color = Color.white;
            textMesh.fontSize = 20;

            if (textMesh.text.Length > 3)
            {
                icon.transform.localPosition = new Vector3(266, 2, 0);
            }
        }
        else if (num == 0)
        {
            textMesh.text = "✓";
            textMesh.color = Color.green;
            textMesh.fontWeight = TMPro.FontWeight.Bold;
        }

        return icon;
    }

    private static GameObject CreateJobIcon(NWButtonResults instance, Job job, Story story, string iconKey)
    {
        GameObject icon = new GameObject(iconKey);
        icon.transform.SetParent(instance.transform, false);

        TMPro.TextMeshProUGUI textMesh = icon.AddComponent<TMPro.TextMeshProUGUI>();
        textMesh.font = instance.textField.font;
        textMesh.fontSize = 24;
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        icon.transform.localPosition = new Vector3(272, 2, 0);

        string storyID = story.storyID;

        if (!job.isRelax && Princess.stress >= 100)
        {
            return icon;
        }

        int doneTimes = job.timesWorked;
        if (doneTimes > 0)
        {
            if (job.isRelax)
            {
                instance.tooltipText += $"\nTimes relaxed: {doneTimes}";
            }
            else
            {
                instance.tooltipText += $"\nTimes worked: {doneTimes}";
            }
        }

        if (storyID.EndsWith("unlock"))
        {
            textMesh.text = "☆";
            textMesh.color = Color.yellow;
            instance.tooltipText += "\nUnlocking new job";
        }
        else if (job.timesWorked == 0)
        {
            textMesh.text = "☆";
            textMesh.color = Color.white;
            instance.tooltipText += "\nFirst time!";
        }
        else if (storyID.EndsWith("ultimate") || storyID.StartsWith("relaxparkultimate"))
        {
            textMesh.text = "★";
            textMesh.color = Color.yellow;
            instance.tooltipText += "\nUltimate story available";
        }
        else if (!storyID.EndsWith("repeat"))
        {
            textMesh.text = "★";
            textMesh.color = Color.white;
            instance.tooltipText += $"\nNew story available";
        }

        return icon;
    }

    private static void CleanUpOtherLocationIcons(string locationID)
    {
        List<string> keysToRemove = new List<string>();

        foreach (var kvp in storyIcons)
        {
            if (!kvp.Key.StartsWith($"JobIcon_{locationID}_"))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            if (storyIcons.TryGetValue(key, out GameObject icon))
            {
                if (icon != null)
                {
                    UnityEngine.Object.Destroy(icon);
                }
                storyIcons.Remove(key);
            }
        }
    }
}
