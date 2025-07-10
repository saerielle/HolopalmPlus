using System;
using System.Collections.Generic;
using System.Reflection;

namespace HolopalmPlus;

public static class WHHelper
{
    // public static void SetReadonlyField<T>(object instance, string fieldName, T value)
    // {
    //     Type type = instance.GetType();
    //     FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

    //     if (field == null)
    //     {
    //         throw new ArgumentException($"Field '{fieldName}' not found on type '{type.Name}'");
    //     }

    //     field.SetValue(instance, value);
    // }

    public static void SetReadonlyField<TInstance, TValue>(TInstance instance, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field?.SetValue(instance, value);
    }

    public static T GetReadonlyField<T, TInstance>(TInstance instance, string fieldName)
    {
        FieldInfo field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            return default;
        }
        return (T)field.GetValue(instance);
    }

    // Slightly modified versiono of the original ResultsMenu.HasSeenChoice
    public static bool HasSeenChoice(Choice choice)
    {
        if (choice == null || choice.story == null || choice.story.storyID == "gamestartintro" || choice.story.storyID.StartsWith("ending"))
        {
            return false;
        }

        if (choice.isDone)
        {
            return false;
        }

        if (choice.hasJob)
        {
            return false;
        }

        if (choice.hasBattle)
        {
            return false;
        }

        if (Groundhogs.instance.seenChoices.GetList(choice.story.storyID).ContainsSafe(choice.choiceID))
        {
            return true;
        }

        return false;
    }
}
