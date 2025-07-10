using HarmonyLib;
using System;

namespace HolopalmPlus;

[HarmonyPatch]
public class StoryPatches
{
    [HarmonyPatch(typeof(Story), "PickBeforeJobStory")]
    [HarmonyPostfix]
    public static void StoryPickBeforeJobStoryPostfix(ref Story __result, Result result, bool highPriorityOnly = false)
    {
        try
        {
            if (highPriorityOnly)
            {
                return;
            }

            if (Status.inMourning && (result.job == null || !result.job.isRebuilding))
            {
                return;
            }

            if (Princess.season == Season.glow && !HolopalmSave.GetSetting("glowJobStories"))
            {
                return;
            }

            if (Princess.season != Season.glow && !HolopalmSave.GetSetting("frequentStories"))
            {
                return;
            }

            if (__result != null && !__result.storyID.EndsWith("repeat"))
            {
                return;
            }

            if (result.job == null || result.job.isExpedition)
            {
                return;
            }

            if (Status.inMourning)
            {
                return;
            }

            Story story = Story.PickStoryFromDict(result.job, Story.storiesByJobHigh, result);
            if (story != null)
            {
                return;
            }

            story = Story.PickStoryFromDict(result.location, Story.storiesByLocationHigh, result);
            if (story != null)
            {
                return;
            }

            if (Princess.GetMemoryInt("work_" + result.location.locationID) % 2 == 0)
            {
                story = Story.PickStoryFromDict(result.job, Story.storiesByJobReg, result);
                if (story != null)
                {
                    __result = story;
                    return;
                }

                story = Story.PickStoryFromDict(result.location, Story.storiesByLocationReg, result);
                if (story != null)
                {
                    __result = story;
                    return;
                }
            }
            else
            {
                story = Story.PickStoryFromDict(result.location, Story.storiesByLocationReg, result);
                if (story != null)
                {
                    __result = story;
                    return;
                }

                story = Story.PickStoryFromDict(result.job, Story.storiesByJobReg, result);
                if (story != null)
                {
                    __result = story;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ModInstance.Log("Error in StoryPickBeforeJobStoryPostfix: " + ex);
        }
    }
}
