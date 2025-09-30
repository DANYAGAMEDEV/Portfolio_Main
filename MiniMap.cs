namespace Code.Gameplay.Behaviour.Space
{
    using Code.Gameplay.Behaviour.Units;
    using Code.Infrastructure.Domain;
    using Code.Infrastructure.Services;
    using FishNet.Connection;
    using System.Collections.Generic;
    using UnityEngine;
    using VContainer;

    public class MiniMap : MonoBehaviour
    {
        [SerializeField]
        private SpaceArea spaceArea;

        [SerializeField]
        private RectTransform minimapFrame;

        [SerializeField] 
        private RectTransform enemyIconPrefab;

        [SerializeField]
        private RectTransform allyIconPrefab;

        [SerializeField]
        private RectTransform blueSafeZoneIconPrefab;
        [SerializeField]
        private RectTransform redSafeZoneIconPrefab;
        private Dictionary<RectTransform, Vector3> safeZoneIcons = new Dictionary<RectTransform, Vector3>();


        [SerializeField]
        private RectTransform playerIcon;
        private Transform playerTransform;
        private RectTransform minimapRectTransform;
        private Dictionary<Unit, RectTransform> unitIconDictionary = new Dictionary<Unit, RectTransform>();
        private float minimapRange = 600;
        private float minimapWidth;
        private float minimapHeight;
        private float scaleX;
        private float scaleY;

        private bool isScaled;

        private Faction playerFaction;

        private void Start()
        {
            this.minimapRectTransform = GetComponent<RectTransform>();

            minimapWidth = minimapRectTransform.rect.width;
            minimapHeight = minimapRectTransform.rect.height;

            scaleX = minimapWidth / (minimapRange * 2f);
            scaleY = minimapHeight / (minimapRange * 2f);
        }
        void Update()
        {
            var serverUnits = Server.Instance.AllServerUnits;


            if (serverUnits.Collection.Count == 0)
            {
                return;
            }

            if (playerTransform == null)
            {
                foreach (var unit in serverUnits.Collection)
                {
                    if (unit is PlayerShip && unit.IsLocal)
                    {
                        playerTransform = unit.transform;
                        playerFaction = unit.unitFaction;
                    }
                }
                return;
            }
            else
            {
                playerIcon.localEulerAngles = new Vector3(0, 0, -playerTransform.eulerAngles.y);

                Vector3 playerPos = playerTransform.position;

                List<Unit> toRemove = new();

                foreach (var pair in unitIconDictionary)
                {
                    if (pair.Key == null || !serverUnits.Collection.Contains(pair.Key) || !pair.Key.IsAlive.Value)
                    {
                        Destroy(pair.Value.gameObject);
                        toRemove.Add(pair.Key);
                    }
                }

                foreach (var unit in toRemove)
                {
                    unitIconDictionary.Remove(unit);
                }

                foreach (var unit in Server.Instance.AllServerUnits.Collection)
                {
                    if (unit == null || unitIconDictionary.ContainsKey(unit) || !unit.IsAlive.Value) continue;
                    else
                    {
                        if (!unit.IsLocal)
                        {
                            if (unit.unitFaction == playerFaction)
                            {
                                RectTransform icon = Instantiate(allyIconPrefab, transform);
                                unitIconDictionary[unit] = icon;
                            }
                            else
                            {
                                RectTransform icon = Instantiate(enemyIconPrefab, transform);
                                unitIconDictionary[unit] = icon;
                            }
                        }
                    }
                }

                foreach (var pair in unitIconDictionary)
                {
                    Unit unit = pair.Key;
                    RectTransform icon = pair.Value;

                    Vector3 offset = unit.transform.position - playerPos;
                    Vector2 offset2D = new Vector2(offset.x, offset.z);
                    Vector2 uiPos = new Vector2(offset2D.x * scaleX, offset2D.y * scaleY);

                    if (offset2D.magnitude > minimapRange)
                    {
                        icon.gameObject.SetActive(false);
                    }
                    else
                    {
                        icon.gameObject.SetActive(true);
                        icon.anchoredPosition = uiPos;
                    }
                }

                if(spaceArea.factionSpawns.Count == 2)
                {
                    if(safeZoneIcons.Count == 0)
                    {
                        float iconWorldSize = 76f;

                        Vector3 blueSafeZoneCenter = spaceArea.GetLocalClientSpawnPosition(Faction.teamBlue);
                        RectTransform blueIcon = Instantiate(blueSafeZoneIconPrefab, transform);
                        blueIcon.sizeDelta = new Vector2(iconWorldSize * scaleX, iconWorldSize * scaleY);
                        safeZoneIcons.Add(blueIcon, blueSafeZoneCenter);

                        Vector3 redSafeZoneCenter = spaceArea.GetLocalClientSpawnPosition(Faction.teamRed);
                        RectTransform redIcon = Instantiate(redSafeZoneIconPrefab, transform);
                        redIcon.sizeDelta = new Vector2(iconWorldSize * scaleX, iconWorldSize * scaleY);
                        safeZoneIcons.Add(redIcon, redSafeZoneCenter);
                    }
                    else
                    {
                        foreach (var zone in safeZoneIcons)
                        {
                            RectTransform rect = zone.Key;
                            Vector3 pos = zone.Value;

                            Vector3 offset = pos - playerPos;
                            Vector2 offset2D = new Vector2(offset.x, offset.z);
                            Vector2 uiPos = new Vector2(offset2D.x * scaleX, offset2D.y * scaleY);

                            if (offset2D.magnitude > minimapRange)
                            {
                                rect.gameObject.SetActive(false);
                            }
                            else
                            {
                                rect.gameObject.SetActive(true);
                                rect.anchoredPosition = uiPos;
                            }
                        }
                    }
                }
            }
        }
    }
}