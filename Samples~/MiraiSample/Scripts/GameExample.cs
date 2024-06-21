using System.Collections.Generic;
using System.Linq;
using Mirai;
using UnityEngine;

public class GameExample : MonoBehaviour
{
    [SerializeField] private GameObject authUI;
    [SerializeField] private GameObject loggedIn;
    private void Start()
    {
        GameAuth.OnLoginStateChanged += OnAuthStateChanged;
        OnAuthStateChanged(GameAuth.AccessToken);
    }
    private void OnAuthStateChanged(string token=null)
    {
        if (string.IsNullOrEmpty(token)) ShardsTech.Logout();
        else _ = ShardsTech.Login("Bearer " + token);
        
        authUI.gameObject.SetActive(string.IsNullOrEmpty(token));
        loggedIn.SetActive(!string.IsNullOrEmpty(token));
    }

    public async void Test(string newid)
    {
        await Block.ExecuteTask(ct => ShardsTech.ChangeGuildOwner(newid, ct));
    }
}
