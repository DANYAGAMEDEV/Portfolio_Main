namespace Code.Gameplay.Behaviour.Space
{
    using Code.Infrastructure.Domain;
    using Code.Infrastructure.Services;
    using System.Collections.Generic;
    using UnityEngine;
    public class SpaceArea : MonoBehaviour
    {
        [Header("Префаб космического фона")]
        [SerializeField]
        private GameObject spaceBackground;

        [Header("Префаб визуальной границы космоса")]
        [SerializeField]
        private GameObject edgePrefab;

        [Header("Префаб визуальной границы безопасной зоны синей команды")]
        [SerializeField]
        private GameObject safeZonePrefab_blue;

        [Header("Префаб визуальной границы безопасной зоны красной команды")]
        [SerializeField]
        private GameObject safeZonePrefab_red;

        [Header("Префаб визуальной границы безопасной зоны зеленой команды")]
        [SerializeField]
        private GameObject safeZonePrefab_green;

        [Header("Префаб спавна")]
        [SerializeField]
        private GameObject spawnPrefab;

        private int spaceAreaSize;
        private int safeZoneSize;
        private int edgeCount;
        private int spawnPoints;

        private Vector3[] polygonPoints;

        private bool gotSize, gotPolygons, gotSpawn, gotSafeZone;

        public Dictionary<Faction, Vector3[]> factionSpawns = new Dictionary<Faction, Vector3[]>();

        private List<GameObject> spaceInnerAreaEdges = new List<GameObject>();

        public bool isGenerated;

        private void Start()
        {
            this.spaceAreaSize = Server.Instance.spaceAreaSizeValue;
            this.edgeCount = Server.Instance.spaceAreaEdgeValue;
            this.spawnPoints = Server.Instance.spawnPointsValue;
            this.safeZoneSize = Server.Instance.safeZoneSizeValue;
            Generate();
        }

        public void Generate()
        {
            transform.position = Vector3.zero;
            GenerateOuterArea();
            GenerateInnerArea();
            GenerateSpaceBorder();
            GenerateSpawnZones();

            void GenerateSpaceBorder()
            {
                GenerateEdges(edgeCount, polygonPoints, edgePrefab, true);

                foreach(var edge in spaceInnerAreaEdges)
                {
                    ParticleSystem ps = edge.GetComponentInChildren<ParticleSystem>();
                    var shape = ps.shape; 
                    shape.scale = new Vector3(1, edge.transform.localScale.z * 2, 300);
                }
            }
            void GenerateOuterArea()
            {
                float outerSize = spaceAreaSize * 2f;
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(outerSize * 2, 0.01f, outerSize * 2);
            }
            void GenerateInnerArea()
            {
                GeneratePolygon(ref polygonPoints, edgeCount, spaceAreaSize, Vector3.zero);
            }

            isGenerated = true;
            Debug.Log($"[CLIENT] Space generated with spaceAreaSize = {spaceAreaSize}, edgeCount = {edgeCount}, spawnPoints = {spawnPoints}, safeZoneSize = {safeZoneSize}");
        }
        private void Update()
        {
            spaceBackground.transform.localPosition = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.z);
        }

        private void GeneratePolygon(ref Vector3[] polygonPointsArray, int edges, int areaSize, Vector3 centerPosition)
        {
            polygonPointsArray = new Vector3[edges];
            float angleStep = 360f / edges;
            float angleOffset = 90f + (180f / edges);

            for (int i = 0; i < edges; i++)
            {
                float angle = Mathf.Deg2Rad * (angleStep * i - angleOffset);
                float x = Mathf.Cos(angle) * areaSize;
                float z = Mathf.Sin(angle) * areaSize;
                polygonPointsArray[i] = new Vector3(x, 0, z) + centerPosition;
            }
        }
        private void GenerateEdges(int edges, Vector3[] points, GameObject visualPrefab, bool createList = false)
        {
            for (int i = 0; i < edges; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[(i + 1) % edges];

                Vector3 midPoint = (start + end) / 2f;
                Vector3 direction = end - start;
                float length = direction.magnitude;

                GameObject edge = Instantiate(visualPrefab, transform);
                edge.transform.localPosition = midPoint;
                edge.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                edge.transform.localScale = new Vector3(edge.transform.localScale.x, edge.transform.localScale.y, length);

                if (createList)
                {
                    spaceInnerAreaEdges.Add(edge);
                }
            }
        }
        public bool IsInsideArea(Vector3 worldPosition)
        {
            Vector3 localPoint = worldPosition - transform.position;

            int crossings = 0;
            for (int i = 0; i < polygonPoints.Length; i++)
            {
                Vector3 a = polygonPoints[i];
                Vector3 b = polygonPoints[(i + 1) % polygonPoints.Length];

                if (((a.z > localPoint.z) != (b.z > localPoint.z)) && (localPoint.x < (b.x - a.x) * (localPoint.z - a.z) / (b.z - a.z) + a.x))
                {
                    crossings++;
                }
            }

            return (crossings % 2) == 1;
        }
        public bool IsInsideSafeZone(Vector3 worldPosition, Faction playerFaction = Faction.bot)
        {
            bool isInsideSafeZone = false;
            foreach(var safeZone in factionSpawns)
            {
                if(safeZone.Key == playerFaction || safeZone.Key == Faction.bot)
                {
                    Vector3 localPoint = worldPosition - transform.position;

                    int crossings = 0;
                    for (int i = 0; i < safeZone.Value.Length; i++)
                    {
                        Vector3 a = safeZone.Value[i];
                        Vector3 b = safeZone.Value[(i + 1) % safeZone.Value.Length];

                        if (((a.z > localPoint.z) != (b.z > localPoint.z)) && (localPoint.x < (b.x - a.x) * (localPoint.z - a.z) / (b.z - a.z) + a.x))
                        {
                            crossings++;
                        }
                    }
                    isInsideSafeZone = (crossings % 2) == 1;
                    if (isInsideSafeZone) break;
                }
            }
            return isInsideSafeZone;
        }

        private void GenerateSpawnZones()
        {
            Vector3 center = GetPolygonCenter(polygonPoints);

            int totalPoints = polygonPoints.Length;
            int count = Mathf.Min(spawnPoints, totalPoints);

            int step = totalPoints / count;

            for (int i = 0; i < count; i++)
            {
                int index = (i * step) % totalPoints;
                Vector3 corner = polygonPoints[index];

                Vector3 directionToCenter = (center - corner).normalized;

                int offsetToCenter = safeZoneSize * 3;

                Vector3 spawnPos = corner + directionToCenter * offsetToCenter;


                Faction faction = (Faction)(i + 1);
                factionSpawns.Add(faction, new Vector3[0]);

                Vector3[] value = null;
                GeneratePolygon(ref value, 42, safeZoneSize, spawnPos);
                factionSpawns[faction] = value;

                GameObject prefab = default;
                if (faction == Faction.teamBlue)
                {
                    prefab = safeZonePrefab_blue;
                }
                else if (faction == Faction.teamRed)
                {
                    prefab = safeZonePrefab_red;
                }
                else if (faction == Faction.teamGreen)
                {
                    prefab = safeZonePrefab_green;
                }

                GenerateEdges(42, factionSpawns[faction], prefab);

                GameObject spawnObj = Instantiate(spawnPrefab, transform);
                spawnObj.transform.localPosition = spawnPos;
            }
        }

        private Vector3 GetPolygonCenter(Vector3[] points)
        {
            Vector3 sum = Vector3.zero;
            foreach (var point in points)
                sum += point;

            return sum / points.Length;
        }

        public Vector3 GetLocalClientSpawnPosition(Faction faction)
        {
            var playerSafeZone = factionSpawns[faction];
            return GetPolygonCenter(playerSafeZone);
        }
    }
}