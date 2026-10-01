using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerWorld : MonoBehaviour
    {
        public GameObject roadPrefab;
        public RunnerItem[] itemPrefabs;
        public RunnerPlayer player;
        public RunnerGame game;
        [System.NonSerialized] Transform[] roads;
        [System.NonSerialized] RunnerRoadSection[] sections;
        [System.NonSerialized] RunnerItem[,] items;
        System.Random random;
        float nextRow;
        int safeLane;
        double travelled;
        public int ActiveItemCount { get; private set; }

        public void Initialize()
        {
            if (roads != null && roads.Length == RunnerRules.RoadCount && roads[0] != null &&
                items != null && items.GetLength(0) == RunnerRules.ItemKindCount &&
                items.GetLength(1) == RunnerRules.PoolPerKind && sections != null) return;
            // Unity's fast Enter Play Mode can retain managed fields with destroyed scene objects.
            // Recover once here; normal restarts simply reuse the existing valid pool.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            roads = new Transform[RunnerRules.RoadCount];
            sections = new RunnerRoadSection[roads.Length];
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i] = Instantiate(roadPrefab, transform).transform;
                sections[i] = roads[i].GetComponent<RunnerRoadSection>();
            }
            items = new RunnerItem[RunnerRules.ItemKindCount, RunnerRules.PoolPerKind];
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                {
                    if (itemPrefabs != null && kind < itemPrefabs.Length && itemPrefabs[kind] != null)
                        items[kind, i] = Instantiate(itemPrefabs[kind], transform);
                    else if (RunnerRules.IsPowerUp((RunnerItemKind)kind))
                        items[kind, i] = CreateFallbackPowerUp((RunnerItemKind)kind);
                    else
                        throw new System.InvalidOperationException("Missing runner item prefab: " + (RunnerItemKind)kind);
                    items[kind, i].Release();
                }
        }

        RunnerItem CreateFallbackPowerUp(RunnerItemKind kind)
        {
            var root = new GameObject(kind.ToString());
            root.transform.SetParent(transform, false);
            var item = root.AddComponent<RunnerItem>();
            item.kind = kind;
            item.hitbox = root.AddComponent<BoxCollider>();
            item.hitbox.isTrigger = true;
            item.hitbox.center = Vector3.up * 0.9f;
            item.hitbox.size = Vector3.one * 0.9f;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Power Up";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 0.9f;
            visual.transform.localScale = Vector3.one * 0.72f;
            if (Application.isPlaying) Destroy(visual.GetComponent<Collider>());
            else DestroyImmediate(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            Color color = kind == RunnerItemKind.CoinMagnet ? new Color(1f, 0.72f, 0.12f) :
                kind == RunnerItemKind.Shield ? new Color(0.2f, 0.78f, 1f) : new Color(1f, 0.42f, 0.12f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
            return item;
        }

        public void ResetWorld(int seed)
        {
            Initialize();
            random = new System.Random(seed);
            safeLane = 1;
            nextRow = RunnerRules.FirstRow;
            ActiveItemCount = 0;
            travelled = 0d;
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i].position = new Vector3(0f, 0f, (i - 1) * RunnerRules.RoadLength);
                if (sections[i] != null) sections[i].SetLocation(roads[i].position.z);
            }
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++) items[kind, i].Release();
            FillAhead();
        }

        public void Simulate(float travel, Bounds previousPlayer, Bounds currentPlayer)
        {
            travelled += travel;
            for (int i = 0; i < roads.Length; i++)
            {
                Vector3 p = roads[i].position - Vector3.forward * travel;
                if (p.z < -RunnerRules.RoadLength * 1.5f)
                {
                    p.z += roads.Length * RunnerRules.RoadLength;
                    if (sections[i] != null) sections[i].SetLocation(travelled + p.z);
                }
                roads[i].position = p;
            }
            // Obstacles before coins, so a fatal contact cannot also award a coin.
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                {
                    if (RunnerRules.IsObstacle((RunnerItemKind)kind) != (pass == 0)) continue;
                    RunnerItem item = items[kind, i];
                    if (!item.InUse) continue;
                    Bounds itemFrom = item.HitBounds;
                    float itemTravel = item.TravelThisTick(travel, travel / Mathf.Max(RunnerRules.StartSpeed, game.TravelSpeed));
                    item.transform.position -= Vector3.forward * itemTravel;
                    float dt = travel / Mathf.Max(RunnerRules.StartSpeed, game.TravelSpeed);
                    Vector3 target = player.transform.position + Vector3.up * player.Height * 0.5f;
                    float magnetDistance = Vector3.Distance(item.transform.position, target);
                    if (item.kind == RunnerItemKind.Coin && game.MagnetRemaining > 0f &&
                        item.transform.position.z >= target.z && magnetDistance <= RunnerRules.MagnetRadius)
                    {
                        float closeInFactor = 1f + Mathf.InverseLerp(RunnerRules.MagnetRadius, 0f, magnetDistance);
                        item.transform.position = Vector3.MoveTowards(item.transform.position, target,
                            RunnerRules.MagnetPullSpeed * closeInFactor * dt);
                    }
                    Bounds itemTo = item.HitBounds;
                    bool hit = RunnerRules.SweptOverlap(previousPlayer, currentPlayer, itemFrom, itemTo);
                    if (hit && game.State == RunnerGame.RunState.Running)
                    {
                        if (item.kind == RunnerItemKind.Coin)
                        {
                            item.Release();
                            ActiveItemCount--;
                            game.CollectCoin();
                        }
                        else if (RunnerRules.IsPowerUp(item.kind))
                        {
                            item.Release();
                            ActiveItemCount--;
                            game.CollectPowerUp(item.kind);
                        }
                        else if (game.TryAbsorbObstacle())
                        {
                            item.Release();
                            ActiveItemCount--;
                        }
                        else game.EndRun();
                    }
                    if (item.InUse && item.transform.position.z < -12f)
                    {
                        if (RunnerRules.IsObstacle(item.kind)) game.ObstacleCleared();
                        item.Release();
                        ActiveItemCount--;
                    }
                }
            nextRow -= travel;
            FillAhead();
        }

        public bool IntersectsObstacle(Bounds bounds)
        {
            if (items == null) return false;
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                    if (RunnerRules.IsObstacle((RunnerItemKind)kind) && items[kind, i].InUse && items[kind, i].HitBounds.Intersects(bounds)) return true;
            return false;
        }

        void FillAhead()
        {
            while (nextRow <= RunnerRules.Horizon)
            {
                safeLane = RunnerRules.NextSafeLane(safeLane, random);
                int obstacleCount = RunnerRules.RecommendedObstacleCount(game.Distance);
                int[] lanes = { 0, 1, 2 };
                Shuffle(lanes);

                for (int i = 0; i < lanes.Length; i++)
                {
                    int lane = lanes[i];
                    if (lane == safeLane || obstacleCount <= 0) continue;
                    RunnerItemKind obstacle = PickObstacleKind();
                    float z = nextRow + (i * 1.5f);
                    Place(obstacle, lane, z);
                    if (RunnerRules.CanCoinRideObstacle(obstacle) && random.NextDouble() < 0.75d)
                    {
                        float y = RunnerRules.ObstacleCoinHeight(obstacle);
                        int coinCount = 2 + random.Next(0, 2);
                        for (int coin = 0; coin < coinCount; coin++)
                            PlaceCoinAt((lane - 1) * RunnerRules.LaneWidth, z + (coin - 1) * 1.8f, y);
                    }
                    obstacleCount--;
                }

                int coinTrailLength = RunnerRules.RecommendedCoinTrailLength(game.Distance);
                for (int i = 0; i < coinTrailLength; i++)
                    PlaceCoinAt((safeLane - 1) * RunnerRules.LaneWidth, nextRow - 6f + i * 2.6f, 0.9f);

                if (random.NextDouble() < 0.65d && coinTrailLength >= 5)
                {
                    int wideLane = lanes[random.Next(0, lanes.Length)];
                    if (wideLane != safeLane)
                    {
                        RunnerItemKind coinRide = PickObstacleKind();
                        if (RunnerRules.CanCoinRideObstacle(coinRide))
                        {
                            float y = RunnerRules.ObstacleCoinHeight(coinRide);
                            for (int i = 0; i < 3; i++)
                                PlaceCoinAt((wideLane - 1) * RunnerRules.LaneWidth, nextRow + 5f + i * 2.3f, y);
                        }
                    }
                }

                if (random.NextDouble() < 0.28d)
                {
                    var powerUp = (RunnerItemKind)((int)RunnerItemKind.CoinMagnet + random.Next(0, 3));
                    Place(powerUp, safeLane, nextRow - 8f);
                }

                nextRow += RunnerRules.RowSpacing * (0.82f + (float)random.NextDouble() * 0.18f);
            }
        }

        void PlaceCoinAt(float x, float z, float y)
        {
            RunnerItem coinPrefab = items[(int)RunnerItemKind.Coin, 0];
            Bounds coinBounds = new Bounds(new Vector3(x, y, z) + coinPrefab.hitbox.center, coinPrefab.hitbox.size);
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
            {
                if (!RunnerRules.IsObstacle((RunnerItemKind)kind)) continue;
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                    if (items[kind, i].InUse && coinBounds.Intersects(items[kind, i].HitBounds)) return;
            }
            for (int i = 0; i < RunnerRules.PoolPerKind; i++)
            {
                RunnerItem item = items[(int)RunnerItemKind.Coin, i];
                if (item.InUse) continue;
                item.Place(x, z, y);
                ActiveItemCount++;
                return;
            }
        }

        RunnerItemKind PickObstacleKind()
        {
            int roll = random.Next(0, 100);
            if (roll < 36) return RunnerItemKind.Barrier;
            if (roll < 68) return RunnerItemKind.Overhead;
            if (roll < 88) return RunnerItemKind.Tower;
            return RunnerItemKind.RunningPerson;
        }

        void Shuffle(int[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                int tmp = values[i];
                values[i] = values[j];
                values[j] = tmp;
            }
        }

        void Place(RunnerItemKind kind, int lane, float z)
        {
            for (int i = 0; i < RunnerRules.PoolPerKind; i++)
            {
                RunnerItem item = items[(int)kind, i];
                if (item.InUse) continue;
                item.Place((lane - 1) * RunnerRules.LaneWidth, z);
                ActiveItemCount++;
                return;
            }
            // Bounded pools: if tuning ever exhausts one, omit the item rather than allocate.
        }
    }
}
