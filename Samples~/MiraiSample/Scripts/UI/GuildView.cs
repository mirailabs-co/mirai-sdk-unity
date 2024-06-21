using System.Collections;
using System.Collections.Generic;
using Mirai;
using UnityEngine;
using UnityEngine.UI;

public class GuildView : MonoBehaviour
{
    [SerializeField] private Text txtGuildName;
    [SerializeField] private Text txtGuildDescription;
    [SerializeField] private Text txtGuildOwner;
    [SerializeField] private Text txtMember;
    private GuildData _guildData;
    public void SetData(GuildData guildData)
    {
        _guildData = guildData;
    }
}
