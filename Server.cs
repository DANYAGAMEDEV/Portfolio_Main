namespace Code.Infrastructure.Services
{
    using UnityEngine;
    using FishNet.Object;
    using FishNet.Connection;
    using VContainer;
    using Code.Gameplay.Behaviour.Units;
    using System.Collections.Generic;
    using Code.Infrastructure.Domain;
    using System;
    using Code.Gameplay.Behaviour.Space;
    using FishNet.Managing;
    using FishNet.Transporting;
    using FishNet.Object.Synchronizing;
    using System.Threading.Tasks;
    using Code.Infrastructure.Common;
    using TMPro;
    using UnityEngine.UI;

    public struct PlayerScore
    {
        public int kills;
        public int death;

        public PlayerScore(int kills, int death)
        {
            this.kills = kills;
            this.death = death;
        }
    }

    public class Server : NetworkBehaviour
    {
        public static Server Instance { get; private set; }
        public event Action<int, int, int> OnKill;

        public string clientPlayerName;

        [SerializeField] private NetworkManager networkManager;

        public UnitDataContainer unitDataContainer;

        [SerializeField] private GameObject botPrefab;

        private UnitDataEditor unitDataEditor;
        private SpaceArea spaceArea;

        public int spaceAreaSizeValue = 300;
        public int spaceAreaEdgeValue = 32;
        public int spawnPointsValue = 2;
        public int teamsCount = 2;
        public int safeZoneSizeValue = 38;

        public readonly SyncList<NetworkConnection> ConnectedClients = new SyncList<NetworkConnection>();
        public readonly SyncList<NetworkObject> ConnectedBots = new SyncList<NetworkObject>();
        public readonly SyncList<Unit> AllServerUnits = new SyncList<Unit>();
        private bool isRefreshingLists = false;

        public readonly SyncDictionary<Faction, List<int>> teams = new SyncDictionary<Faction, List<int>>();
        public readonly SyncDictionary<int, PlayerScore> playersScoreMap = new SyncDictionary<int, PlayerScore>();

        [SerializeField] private TextMeshProUGUI blueTeamNumber, redTeamNumber;
        [SerializeField] private Image teamTargetImage;
        [SerializeField] private Sprite teamRedSprite, teamBlueSprite;

        [SerializeField] private Transform itemsTransform;
        [SerializeField] private GameObject itemToSpawn;

        public Match match;
        public readonly IntSyncVar blueTeamScore = new();
        public readonly IntSyncVar redTeamScore = new();
        public readonly int maxScore = 30;

        [Inject]
        public void Construct(UnitDataEditor unitDataEditor, SpaceArea spaceArea)
        {
            this.unitDataEditor = unitDataEditor;
            this.spaceArea = spaceArea;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                Instance = this;
            }

            teams.OnChange += UpdateTeamPlayersCountUI;

            if (Application.isBatchMode)
            {
                Debug.Log("[ServerBootstrap] Headless mode detected. Starting server...");

                if (!networkManager.ServerManager.StartConnection())
                {
                    Debug.LogError("Failed to start server.");
                }
            }
        }

        private void OnDestroy()
        {
            teams.OnChange -= UpdateTeamPlayersCountUI;
        }

        [Server]
        public override void OnStartServer()
        {
            base.OnStartServer();

            networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;

            ConnectedClients.OnChange += (op, index, oldItem, newItem, asServer) => RefreshLists();
            ConnectedBots.OnChange += (op, index, oldItem, newItem, asServer) => RefreshLists();

            GenerateTeamsForPvP();
            GenerateItems();

            Debug.Log("[SERVER] Server started");
        }

        [Server]
        public override void OnStopServer()
        {
            base.OnStopServer();
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;

            ConnectedClients.OnChange -= (op, index, oldItem, newItem, asServer) => RefreshLists();
            ConnectedBots.OnChange -= (op, index, oldItem, newItem, asServer) => RefreshLists();
        }

        [Server]
        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                Debug.Log($"[SERVER] Client connected with Id: {connection.ClientId}");
                if (!ConnectedClients.Contains(connection))
                {
                    ConnectedClients.Add(connection);
                    AssignToTeam(connection.ClientId);
                    SetTeamScore();
                }
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                Debug.Log($"[SERVER]Client disconnected with Id: {connection.ClientId}");
                if (ConnectedClients.Contains(connection))
                {
                    ConnectedClients.Remove(connection);
                    RemoveFromTeam(connection.ClientId);
                }
                if(ConnectedClients.Collection.Count == 0)
                {
                    Debug.Log($"[SERVER] RESTARTED");
                    ConnectedClients.Clear();
                    ConnectedBots.Clear();
                    AllServerUnits.Clear();
                    isRefreshingLists = false;
                    playersScoreMap.Clear();
                    teams.Clear();
                    GenerateTeamsForPvP();
                    SetTeamScore();
                    GenerateItems();
                    Debug.Log($"[SERVER] ConnectedClients: {ConnectedClients.Collection.Count} ConnectedBots: {ConnectedBots.Collection.Count} AllServerUnits: {AllServerUnits.Collection.Count} playersScoreMap: {playersScoreMap.Collection.Count} teams: {teams.Collection.Count}");
                }
            }
        }

        [Server]
        private void SetTeamScore()
        {
            if (ConnectedClients.Collection.Count > 1) return;
            blueTeamScore.Value = 0;
            redTeamScore.Value = 0;
        }

        public UnitEntity GetDefaultPlayerData(int unitId)
        {
            Debug.Log("[SERVER] Sending default data to player");
            return unitDataContainer.units[unitId - 1].unitEntity;
        }

        [Server]
        private async void RefreshLists()
        {
            if (!isRefreshingLists)
            {
                isRefreshingLists = true;
            }

            AllServerUnits.Clear();

            foreach (var client in ConnectedClients)
            {
                if (client.FirstObject == null)
                {
                    while (client.FirstObject == null)
                    {
                        await Task.Yield();
                    }
                }

                Unit unit = client.FirstObject.GetComponent<Unit>();

                if (!AllServerUnits.Contains(unit))
                {
                    AllServerUnits.Add(unit);
                }
            }

            foreach (var bot in ConnectedBots)
            {
                Unit unit = bot.GetComponent<Unit>();
                if (!AllServerUnits.Contains(unit))
                {
                    AllServerUnits.Add(unit);
                }
            }

            Debug.Log("[SERVER] Refreshing Total Server Units list: " + AllServerUnits.Count + " ConnectedClients: " +
                      ConnectedClients.Count + " ConnectedBots: " + ConnectedBots.Count);

            isRefreshingLists = false;
        }

        [Server]
        private void GenerateTeamsForPvP()
        {
            for (int i = 0; i < teamsCount; i++)
            {
                Faction faction = (Faction)(i + 1);
                teams.Add(faction, new List<int>());
            }

            Debug.Log($"[SERVER] Generated {teams.Count} teams");
        }

        [Server]
        private void AssignToTeam(int clientId)
        {
            Faction factionToAdd = Faction.teamBlue;
            int minCount = teams[factionToAdd].Count;

            foreach (var pair in teams)
            {
                if (pair.Value.Contains(clientId))
                {
                    Debug.Log($"[SERVER ERROR] Can not assign team for {clientId}");
                    return;
                }
            }

            foreach (var pair in teams)
            {
                if (pair.Value.Count < minCount)
                {
                    factionToAdd = pair.Key;
                    minCount = pair.Value.Count;
                }
            }

            Debug.Log($"[SERVER] Client with Id: {clientId} was assigned to {factionToAdd}");

            List<int> updatedList = new List<int>(teams[factionToAdd]);
            updatedList.Add(clientId);
            teams[factionToAdd] = updatedList;
        }

        [Server]
        private void RemoveFromTeam(int clientId)
        {
            foreach (var pair in teams)
            {
                if (pair.Value.Contains(clientId))
                {
                    Debug.Log($"[SERVER] Client with Id: {clientId} was removed from {pair.Key}");
                    List<int> updatedList = new List<int>(teams[pair.Key]);
                    updatedList.Remove(clientId);
                    teams[pair.Key] = updatedList;
                    return;
                }
            }
        }

        private void UpdateTeamPlayersCountUI(SyncDictionaryOperation op, Faction key, List<int> value, bool asServer)
        {
            foreach (var team in teams)
            {
                if (team.Key == Faction.teamBlue) blueTeamNumber.text = team.Value.Count.ToString();
                else if (team.Key == Faction.teamRed) redTeamNumber.text = team.Value.Count.ToString();
            }
        }

        public void SetPlayerFactionUI(Faction f)
        {
            if (f == Faction.teamBlue) teamTargetImage.sprite = teamBlueSprite;
            else if (f == Faction.teamRed) teamTargetImage.sprite = teamRedSprite;
        }

        [Server]
        private void GenerateItems()
        {
            if (!spaceArea.isGenerated)
            {
                Invoke("GenerateItems", 1f);
            }
            else
            {
                int itemsCountToSpawn = 50;
                foreach(Transform item in itemsTransform.transform)
                {
                    ServerManager.Despawn(item.gameObject);
                }

                for(int i = 0; i< itemsCountToSpawn; i++)
                {
                    int randomPosX = UnityEngine.Random.Range(-300,301);
                    int randomPosZ = UnityEngine.Random.Range(-300, 301);
                    Vector3 pos = new Vector3(randomPosX, 0, randomPosZ);
                    bool isInsideArea = spaceArea.IsInsideArea(pos);
                    bool isoutOfSafeZone = !spaceArea.IsInsideSafeZone(pos);
                    if (isInsideArea && isoutOfSafeZone)
                    {
                        GameObject objToSpwn = Instantiate(itemToSpawn, itemsTransform);
                        objToSpwn.transform.position = new Vector3(randomPosX, 0, randomPosZ);
                        ServerManager.Spawn(objToSpwn);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerScore(int killerId, int deadId, Faction deadFaction)
        {
            Debug.Log($"[SERVER] Player Id:{killerId} killed player Id:{deadId} faction:{deadFaction}");

            SendKillFeedToClients(killerId, deadId, deadFaction);

            if (playersScoreMap.Collection.ContainsKey(killerId))
            {
                PlayerScore ps = new PlayerScore(playersScoreMap[killerId].kills, playersScoreMap[killerId].death);
                ps.kills += 1;
                playersScoreMap[killerId] = ps;
            }
            else
            {
                PlayerScore ps = new PlayerScore(1,0);
                playersScoreMap.Add(killerId, ps);
            }

            if (playersScoreMap.Collection.ContainsKey(deadId))
            {
                PlayerScore ps = new PlayerScore(playersScoreMap[deadId].kills, playersScoreMap[deadId].death);
                ps.death += 1;
                playersScoreMap[deadId] = ps;
            }
            else
            {
                PlayerScore ps = new PlayerScore(0, 1);
                playersScoreMap.Add(deadId, ps);
            }

            if(deadFaction == Faction.teamBlue)
            {
                if (redTeamScore.Value == maxScore) return;
                redTeamScore.Value += 1;
            }
            else if (deadFaction == Faction.teamRed)
            {
                if (blueTeamScore.Value == maxScore) return;
                blueTeamScore.Value += 1;
            }
        }
        [ObserversRpc]
        private void SendKillFeedToClients(int killerId, int deadId, Faction deadFaction)
        {
            match.UpdateKillFeed(killerId,deadId,deadFaction);
        }
    }
}