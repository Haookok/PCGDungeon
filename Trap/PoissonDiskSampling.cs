using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ooparts.dungen
{
    public static class PoissonDiskSampling
    {
        public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize,
            int numSamplesBeforeRejection = 30)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            
            int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2>();

            spawnPoints.Add(sampleRegionSize / 2);

            while (spawnPoints.Count > 0)
            {
                //随机从备选队列中选一个点
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];//当作中心来向外生成
                bool candidateAccepted = false;
                for (int i = 0; i < numSamplesBeforeRejection; i++)
                {
                    float angle = Random.value * Mathf.PI * 2;
                    Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));//随机方向
                    Vector2 candidate = spawnCenter + dir * Random.Range(radius, radius * 2);//现在的中心点坐标。加上这个方向上这个距离的向量

                    if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                        candidateAccepted = true;
                        break;
                    }
                }
                if(!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex); //如果没有找到合适的点，就从备选队列中移除
                }
            }

            return points;
        }

        static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius,
            List<Vector2> points, int[,] grid)
        {
            if (candidate.x < 0 || candidate.x >= sampleRegionSize.x || candidate.y < 0 || candidate.y >= sampleRegionSize.y)
                return false;

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);

            //从当前点旁边找四个格子，也就是形成一个5x5的格子区域，检查距离够不够
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    int checkX = cellX + x;
                    int checkY = cellY + y;

                    if (checkX >= 0 && checkX < grid.GetLength(0) && checkY >= 0 && checkY < grid.GetLength(1))
                    {
                        int pointIndex = grid[checkX, checkY];
                        if (pointIndex > 0)
                        {
                            Vector2 point = points[pointIndex - 1];
                            if (Vector2.Distance(candidate, point) < radius)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        
        
    }
}

