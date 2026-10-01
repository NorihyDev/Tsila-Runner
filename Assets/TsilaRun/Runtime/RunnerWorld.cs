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
            if (roads != null && roads.Length == RunnerRules.RoadCount && roads[0] != null && items != null && sections != null) return;
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
                    items[kind, i] = Instantiate(itemPrefabs[kind], transform);
                    items[kind, i].Release();
                }
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
            for (int pass = 0; pass < RunnerRules.ItemKindCount; pass++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                {
                    int kind = pass == 3 ? 4 : pass == 4 ? 3 : pass; // All obstacles before coins.
                    RunnerItem item = items[kind, i];
                    if (!item.InUse) continue;
                    float itemTravel = item.TravelThisTick(travel, travel / Mathf.Max(RunnerRules.StartSpeed, game.Speed));
                    bool hit = RunnerRules.SweptOverlap(previousPlayer, currentPlayer, item.HitBounds, itemTravel);
                    item.transform.position -= Vector3.forward * itemTravel;
                    if (hit && game.State == RunnerGame.RunState.Running)
                    {
                        if (item.kind == RunnerItemKind.Coin)
                        {
                            item.Release();
                            ActiveItemCount--;
                            game.CollectCoin();
                        }
                        else game.EndRun();
                    }
                    if (item.InUse && item.transform.position.z < -12f)
                    {
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
                    if (kind != (int)RunnerItemKind.Coin && items[kind, i].InUse && items[kind, i].HitBounds.Intersects(bounds)) return true;
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

                nextRow += RunnerRules.RowSpacing * (0.82f + (float)random.NextDouble() * 0.18f);
            }
        }

        void PlaceCoinAt(float x, float z, float y)
        {
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
