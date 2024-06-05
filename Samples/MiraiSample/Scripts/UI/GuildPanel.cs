using System;
using System.Collections;
using System.Collections.Generic;
using Mirai;
using UnityEngine;
using UnityEngine.UI;

public class GuildPanel : MonoBehaviour
{
    [SerializeField] private Toggle tgLeaderboardTab;
    [SerializeField] private Toggle tgMyGuildTab;
    private void OnEnable()
    {
        StartCoroutine(MyGuildUpdated());
    }
    public IEnumerator MyGuildUpdated()
    {
        var userInGuild = ShardsTech.MyGuild != null;
        tgMyGuildTab.gameObject.SetActive(userInGuild);
        tgMyGuildTab.isOn = userInGuild;
        yield return null;
        if (userInGuild) tgMyGuildTab.isOn = true;
        else tgLeaderboardTab.isOn = true;
    }
}
