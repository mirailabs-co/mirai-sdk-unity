# MiraiSDK
## Mirai Sign Provider
A SDK which is used for all apps to sign. 
### Install

Will update later...maybe need use upm package

### How to use:
An example application using the library
#### Inititalize SignProvider
After login with MiraiID you will receive miraiAccessToken call Init
```cs
MiraiSignProvider.Init(MiraiAccessToken);
```
#### GetUriConnectToWallet
If after init MiraiSignProvider.UserProviderInfo is null or MiraiSignProvider.Topic is null you need call 
```cs
//default chainIds=["eip155:1"]
MiraiSignProvider.GetUriConnectToWallet();
```
Register event MiraiSignProvider.OnReceiveUri you can show QR code or open wallet deeplink
```cs
void Start()
{
    MiraiSignProvider.OnReceiveUri += MiraiSignProviderOnReceiveUri;
}
void MiraiSignProviderOnReceiveUri(string uri)
{
    var qrTexture = MiraiSignProvider.GenerateQr(uri);
    //or
    var deeplink=$"metamask://wc?uri={UnityWebRequest.EscapeURL(uri)}";
    Application.OpenURL(deeplink);
}
```

#### Send Request
if already have Topic now you can call MiraiSignProvider.SendRequest
```cs
MiraiSignProvider.SendRequest("personal_sign",new []{"testSign", "0x12ab...address"});
```
Register event MiraiSignProvider.OnReceiveResponse you can receive Response of Request
```cs
void Start()
{
    MiraiSignProvider.OnReceiveResponse += MiraiSignProviderOnReceiveResponse;
}
private void MiraiSignProviderOnReceiveResponse(string response)
{
    Debug.Log(response);
}
```

### public method
```cs
public static async void Init(string miraiAccessToken);
public static async Task GetUriConnectToWallet(params string[] chainIds);
public static async Task<UserProviderInfo> GetUserProvider();
public static async Task<string> GetTopic();
public static async Task<string> SendRequest(string method, params string[] @params);
public static async Task SwitchChain(int chainId);
public static async Task SetConfigRPCMap(Dictionary<string,string[]> rpcMaps);
public static async Task DisconnectWallet();
```
### public event
```cs
public static event Action OnSocketConnected;
public static event Action OnSocketDisconnected;
public static event Action<string> OnReceiveResponse;
public static event Action<string> OnReceiveUri;
public static event Action<string> OnTopicChanged;
```
