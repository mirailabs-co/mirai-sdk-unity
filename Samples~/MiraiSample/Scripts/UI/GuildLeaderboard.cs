using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirai;
using UnityEngine;
using UnityEngine.UI;

public class GuildLeaderboard : MonoBehaviour
{
    [SerializeField] private Dropdown drLbs;
    [SerializeField] private InputField ipSearchGuildName;
    [SerializeField] private Transform container;
    
    private List<LeaderboardData> _leaderboards;
    
    private void Awake()
    {
        drLbs.onValueChanged.AddListener(_ => RefreshLeaderboard());
        ipSearchGuildName.onSubmit.AddListener(_ => RefreshLeaderboard());
    }

    private async void OnEnable()
    {
        if (_leaderboards == null)
        {
            _leaderboards = await ShardsTech.GetLeaderboards();
            drLbs.options = _leaderboards.Select(lb => new Dropdown.OptionData(lb.name)).ToList();
        }
        RefreshLeaderboard();
    }
    public async void RefreshLeaderboard()
    {
        for (var i = 0; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(false);
        var guildScoresDto = await ShardsTech.GetGuildScores(_leaderboards[drLbs.value]._id, ipSearchGuildName.text);
        for (var i = 0; i < Mathf.Max(container.childCount,guildScoresDto.data.Count); i++)
        {
            if (i == container.childCount) Instantiate(container.GetChild(0), container);
            var item = container.GetChild(i).GetComponent<ItemGuildLeaderboard>();
            item.gameObject.SetActive(i < guildScoresDto.data.Count);
            if (i < guildScoresDto.data.Count)
                item.SetData(i+1, guildScoresDto.data[i]);
        }
    }
}
