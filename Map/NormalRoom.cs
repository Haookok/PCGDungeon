using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ooparts.dungen
{
    public class NormalRoom : BaseRoom
    {
        [Header("Normal Room Specific")]
        
        
        [Header("Trap System")]
        public TrapSpawnSettings trapSettings;
        
        private GameObject _trapsObject;
        private List<GameObject> _traps = new List<GameObject>();
        private List<TeleporterTrap> _teleporters = new List<TeleporterTrap>();

        [Header("FX")]
        public GameObject dustEffectPrefab;
        
        public override void Start()
        {
            base.Start();
            isRoomCleared = false;
            roomType = RoomType.Normal;
        }

        public override IEnumerator Generate()
        {
            yield return CreateTiles();
            yield return CreateTraps();
            GenerateDustEffect();
        }

        public void GenerateDustEffect()
        {
            if (dustEffectPrefab != null)
            {
                GameObject dustEffect = Instantiate(dustEffectPrefab, transform);
                dustEffect.name = "DustEffect";
                dustEffect.transform.parent = transform;
                dustEffect.transform.localPosition = Vector3.zero;
            }
            
        }
        
        public override void Update()
        {
            CheckRoomClearStatus();
        }
        
        public override IEnumerator CreateMonsters()
        {
            _monstersObject = new GameObject("Monsters");
            _monstersObject.transform.parent = transform;
            _monstersObject.transform.localPosition = Vector3.zero;

            FindUICanvas();
            MonsterCount = Random.Range(4, 6);
            Monsters = new GameObject[MonsterCount];

            int marginFromWall = 3;
            float minX = marginFromWall;
            float maxX = Size.x - marginFromWall;
            float minZ = marginFromWall;
            float maxZ = Size.z - marginFromWall;

            for (int i = 0; i < MonsterCount; i++)
            {
                Vector3 monsterPosition;
                int attempts = 0;
                do
                {
                    float randomX = Random.Range(minX, maxX) - Size.x * 0.5f + 0.5f;
                    float randomZ = Random.Range(minZ, maxZ) - Size.z * 0.5f + 0.5f;
                    monsterPosition = new Vector3(randomX, 0, randomZ);
                    attempts++;
                } while (IsPositionOccupied(monsterPosition) && attempts < 50);

                GameObject newMonster = Instantiate(MonsterPrefab);
                newMonster.name = "Monster " + (i + 1);
                newMonster.transform.parent = _monstersObject.transform;
                newMonster.transform.localPosition = monsterPosition;
                newMonster.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                CreateMonsterHealthBarUI(newMonster);
                Monsters[i] = newMonster;
            }
            yield return null;
        }

        private bool IsPositionOccupied(Vector3 position)
        {
            foreach (GameObject monster in Monsters)
            {
                if (monster != null && Vector3.Distance(monster.transform.localPosition, position) < 1f)
                {
                    return true;
                }
            }
            return false;
        }

        public override IEnumerator CreatePlayer()
        {
            GameObject player = Instantiate(PlayerPrefab);
            player.name = "Player";
            player.transform.parent = transform.parent;
            player.transform.position = transform.position + 5 * Vector3.up;
            
            Player playerScript = player.GetComponent<Player>();
            playerInRoom = player;
            if (playerScript != null)
            {
                Slider healthBar = FindHealthBar();
                Slider magicBar = FindMagicBar();
                if (healthBar != null)
                {
                    playerScript.SetHealthBar(healthBar);
                }
                else
                {
                    Debug.LogWarning("未找到血条ui");
                }
                if(magicBar != null)
                {
                    playerScript.SetMagicBar(magicBar);
                }
                else
                {
                    Debug.LogWarning("未找到魔法条ui");
                }
            }
            
            StartCoroutine(SetupPlayerCamera(player));
            yield return null;
        }

        private IEnumerator CreateTraps()
        {
            if (trapSettings == null || !trapSettings.enableTraps)
                yield break;
                
            if (Random.value > trapSettings.trapSpawnChance)
            {
                Debug.Log("本房间不生成陷阱");
                yield break;
            }
            
            _trapsObject = new GameObject("Traps");
            _trapsObject.transform.parent = transform;
            _trapsObject.transform.localPosition = Vector3.zero;
            
            Vector2 roomSize = new Vector2(Size.x - trapSettings.wallMargin * 2, Size.z - trapSettings.wallMargin * 2);
            if (roomSize.x <= 0 || roomSize.y <= 0)
            {
                Debug.LogWarning("房间太小，无法生成陷阱");
                yield break;
            }
            
            yield return CreateTeleporters(roomSize);
            
            List<Vector2> trapPositions = PoissonDiskSampling.GeneratePoints(trapSettings.minDistanceBetweenTraps, roomSize, 30);
            trapPositions = FilterPositionsAroundTeleporters(trapPositions);
            
            int maxTraps = Mathf.Min(trapSettings.maxTrapsPerRoom, trapPositions.Count);

            for (int i = 0; i < maxTraps; i++)
            {
                Vector2 localPos = trapPositions[i];
                Vector3 worldPos = new Vector3(localPos.x - roomSize.x * 0.5f, 0.1f, localPos.y - roomSize.y * 0.5f);
                if (IsValidTrapPosition(worldPos))
                    CreateTrapAtPosition(worldPos);
                yield return null;
            }

            Debug.Log($"房间{Num}生成了{_traps.Count}个机关");
        }

        //传送门单独的方法
        private IEnumerator CreateTeleporters(Vector2 roomSize)
        {
            if (trapSettings.TeleporterTrapPrefab == null || trapSettings.teleporterWeight <= 0)
                yield break;
            
            if (Random.value > trapSettings.teleporterWeight)
                yield break;
            
            int teleporterCount = trapSettings.guaranteeTeleporterPairs ? 
                Random.Range(trapSettings.minTeleportersPerRoom / 2, trapSettings.maxTeleportersPerRoom / 2 + 1) * 2 : 
                Random.Range(trapSettings.minTeleportersPerRoom, trapSettings.maxTeleportersPerRoom + 1);
            
            List<Vector2> teleporterPositions = PoissonDiskSampling.GeneratePoints(
                trapSettings.minDistanceBetweenTeleporters, 
                roomSize, 
                30);
            
            teleporterCount = Mathf.Min(teleporterCount, teleporterPositions.Count);
            
            for (int i = 0; i < teleporterCount; i++)
            {
                Vector2 localPos = teleporterPositions[i];
                Vector3 worldPos = new Vector3(localPos.x - roomSize.x * 0.5f, 0.1f, localPos.y - roomSize.y * 0.5f);
                
                if (IsValidTeleporterPosition(worldPos))
                {
                    CreateTeleporterAtPosition(worldPos);
                }
                yield return null;
            }
            
            if (_teleporters.Count < 2)
            {
                foreach (TeleporterTrap teleporter in _teleporters)
                {
                    if (teleporter != null)
                    {
                        _traps.Remove(teleporter.gameObject);
                        DestroyImmediate(teleporter.gameObject);
                    }
                }
                _teleporters.Clear();
                Debug.Log($"房间{Num}传送门数量不足，已清除所有传送门");
            }
        }

        private void CreateTeleporterAtPosition(Vector3 position)
        {
            if (trapSettings.TeleporterTrapPrefab == null)
                return;
            
            GameObject newTeleporter = Instantiate(trapSettings.TeleporterTrapPrefab);
            newTeleporter.name = $"Teleporter_{_teleporters.Count + 1}";
            newTeleporter.transform.parent = _trapsObject.transform;
            newTeleporter.transform.localPosition = position + Vector3.up * 1.5f;
            newTeleporter.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            TeleporterTrap teleporterScript = newTeleporter.GetComponent<TeleporterTrap>();
            if (teleporterScript != null)
            {
                _teleporters.Add(teleporterScript);
            }
            
            _traps.Add(newTeleporter);
        }

        private bool IsValidTeleporterPosition(Vector3 position)
        {
            if (!IsValidTrapPosition(position))
                return false;
            
            foreach (TeleporterTrap existingTeleporter in _teleporters)
            {
                if (existingTeleporter != null && 
                    Vector3.Distance(existingTeleporter.transform.localPosition, position) < trapSettings.minDistanceBetweenTeleporters)
                {
                    return false;
                }
            }
            
            return true;
        }

        private List<Vector2> FilterPositionsAroundTeleporters(List<Vector2> positions)
        {
            List<Vector2> filteredPositions = new List<Vector2>();
            
            foreach (Vector2 pos in positions)
            {
                bool tooCloseToTeleporter = false;
                Vector3 worldPos = new Vector3(pos.x - (Size.x - trapSettings.wallMargin * 2) * 0.5f, 0, 
                                             pos.y - (Size.z - trapSettings.wallMargin * 2) * 0.5f);
                
                foreach (TeleporterTrap teleporter in _teleporters)
                {
                    if (teleporter != null && 
                        Vector3.Distance(teleporter.transform.localPosition, worldPos) < trapSettings.minDistanceBetweenTeleporters)
                    {
                        tooCloseToTeleporter = true;
                        break;
                    }
                }
                
                if (!tooCloseToTeleporter)
                {
                    filteredPositions.Add(pos);
                }
            }
            
            return filteredPositions;
        }

        private bool IsValidTrapPosition(Vector3 position)
        {
            if (Monsters != null)
            {
                foreach (GameObject monster in Monsters)
                {
                    if (monster != null && Vector3.Distance(monster.transform.localPosition, position) < trapSettings.monsterMargin)
                        return false;
                }
            }

            if (playerInRoom != null)
            {
                float playerDistance = Vector3.Distance(position, Vector3.zero);
                if (playerDistance < 3f)
                {
                    return false;
                }
            }
            
            float halfX = Size.x * 0.5f - trapSettings.wallMargin;
            float halfZ = Size.z * 0.5f - trapSettings.wallMargin;
            return Mathf.Abs(position.x) < halfX && Mathf.Abs(position.z) < halfZ;
        }

        private void CreateTrapAtPosition(Vector3 position)
        {
            GameObject trapPrefab = SelectRandomTrapPrefab();
            if (trapPrefab == null)
            {
                Debug.LogWarning("没有可用的陷阱预制件");
                return;
            }
            
            GameObject newTrap = Instantiate(trapPrefab);
            string trapTypeName = newTrap.GetComponent<TrapBase>()?.trapType.ToString() ?? "Unknown";
            newTrap.name = $"{trapTypeName}_Trap_{_traps.Count + 1}";
            newTrap.transform.parent = _trapsObject.transform;
            newTrap.transform.localPosition = position;
            if (newTrap.name.Contains("Arrow"))
                newTrap.transform.localPosition += Vector3.down * 0.09f;
            newTrap.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            _traps.Add(newTrap);
        }

        private GameObject SelectRandomTrapPrefab()
        {
            Dictionary<GameObject, float> trapWeights = new Dictionary<GameObject, float>
            {
                { trapSettings.bombTrapPrefab, trapSettings.bombWeight },
                { trapSettings.SpringboardTrapPrefab, trapSettings.springboardWeight },
                { trapSettings.HealRegenerationTrapPrefab, trapSettings.healRegenerationWeight },
                { trapSettings.FireTrapPrefab, trapSettings.fireTrapWeight },
                { trapSettings.ArrowTrapPrefab, trapSettings.arrowTrapWeight }
            };

            float totalWeight = 0f;
            foreach (var weight in trapWeights.Values)
            {
                totalWeight += weight;
            }

            float randomValue = Random.value * totalWeight;
            float accumulatedWeight = 0f;
            
            foreach (var kvp in trapWeights)
            {
                if (kvp.Key == null) continue;
                
                accumulatedWeight += kvp.Value;
                if (randomValue <= accumulatedWeight)
                {
                    return kvp.Key;
                }
            }

            return trapSettings.FireTrapPrefab;
        }

        public List<TeleporterTrap> GetTeleportersInRoom()
        {
            if (_teleporters == null || _teleporters.Count == 0 || _teleporters.Exists(t => t == null))
            {
                RefreshTeleportersList();
            }

            return new List<TeleporterTrap>(_teleporters);
        }

        private void RefreshTeleportersList()
        {
            _teleporters.Clear();
            if (_trapsObject != null)
            {
                TeleporterTrap[] teleporters = _trapsObject.GetComponentsInChildren<TeleporterTrap>();
                _teleporters.AddRange(teleporters);
            }

            _teleporters.RemoveAll(teleporter => teleporter == null);
        }
    }
}