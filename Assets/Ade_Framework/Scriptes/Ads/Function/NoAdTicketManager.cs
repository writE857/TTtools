using System;
using Ade_Framework;
using UnityEngine;

public static class NoAdTicketManager
{
    public const string CountKey = "NoAdTicketCount";

    public static event Action<int> CountChanged;

    public static int GetCount()
    {
        return Mathf.Max(0, AdeCloudPlayerPrefs.GetInt(CountKey, 0));
    }

    public static bool HasTicket(int amount = 1)
    {
        return GetCount() >= Mathf.Max(1, amount);
    }

    public static void SetCount(int count)
    {
        int safeCount = Mathf.Max(0, count);
        AdeCloudPlayerPrefs.SetInt(CountKey, safeCount);
        AdeCloudPlayerPrefs.Save();
        CountChanged?.Invoke(safeCount);
    }

    public static int Add(int amount = 1)
    {
        if (amount <= 0)
        {
            return GetCount();
        }

        int newCount = GetCount() + amount;
        SetCount(newCount);
        return newCount;
    }

    public static bool Consume(int amount = 1)
    {
        int safeAmount = Mathf.Max(1, amount);
        int currentCount = GetCount();
        if (currentCount < safeAmount)
        {
            return false;
        }

        SetCount(currentCount - safeAmount);
        return true;
    }

    public static void Refresh()
    {
        CountChanged?.Invoke(GetCount());
    }
}
