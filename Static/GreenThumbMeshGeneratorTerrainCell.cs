using UnityEngine;
using System.Collections.Generic;

namespace FishTacoGames
{
    public static class GreenThumbMeshGeneratorTerrainCell
    {
        public static Transform GenerateMeshColliderObjectFromBoundsold(Bounds bounds, TerrainData data, Vector3 terrainPosition, int cellID, bool convex = false, Transform parentT = null, bool useMesh = true)
        {
            GameObject segmentCollider = new("Cell" + cellID);
            segmentCollider.transform.parent = parentT;
            if (useMesh)
            {
                Mesh mesh = new();
                List<Vector3> vertices = new();
                Vector3 terrainSize = data.size;

                for (float x = bounds.min.x; x < bounds.max.x + 1; x++)
                {
                    for (float z = bounds.min.z; z < bounds.max.z + 1; z++)
                    {
                        int sampleX = Mathf.FloorToInt((x - terrainPosition.x) / terrainSize.x * data.heightmapResolution);
                        int sampleZ = Mathf.FloorToInt((z - terrainPosition.z) / terrainSize.z * data.heightmapResolution);
                        float height = data.GetHeight(sampleX, sampleZ);
                        Vector3 vertexPosition = new(x, height, z);
                        vertices.Add(vertexPosition);
                    }
                }

                // Change vertices to fit within the bounds
                for (int i = 0; i < vertices.Count; i++)
                {
                    // If the vertex is outside the bounds, adjust it
                    if (!bounds.Contains(vertices[i]))
                    {
                        Vector3 adjustedVertex = vertices[i];
                        adjustedVertex.x = Mathf.Clamp(adjustedVertex.x, bounds.min.x, bounds.max.x);
                        adjustedVertex.z = Mathf.Clamp(adjustedVertex.z, bounds.min.z, bounds.max.z);

                        if (vertices.Contains(adjustedVertex))
                        {
                            vertices.RemoveAt(i);
                            i--; // Adjust index since we removed one vertex
                        }
                        else
                        {
                            vertices[i] = adjustedVertex;
                        }
                    }
                }
                List<int> triangles = new();
                int width = Mathf.CeilToInt(bounds.size.x + 1);

                for (int i = 0; i < vertices.Count - width; i++)
                {
                    if ((i + 1) % width == 0) continue;

                    triangles.Add(i);
                    triangles.Add(i + 1);
                    triangles.Add(i + width);

                    triangles.Add(i + 1);
                    triangles.Add(i + width + 1);
                    triangles.Add(i + width);
                }

                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                MeshCollider meshCollider = segmentCollider.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                if (convex)
                    meshCollider.convex = true;
                //Debug.Log($"Generated {vertices.Count} vertices for collider mesh.");
            }
            return segmentCollider.transform;
        }
    }
}