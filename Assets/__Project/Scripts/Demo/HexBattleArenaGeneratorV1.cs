using System.Collections.Generic;
using UnityEngine;

public class HexBattleArenaGenerator : MonoBehaviour
{
    [Header("Hex Parameters")]
    public float hexSize = 2f;  
    public bool arenaEnabled = true;

    [Header("Visual")]
    public GameObject hexCellPrefab;

    List<GameObject> spawnedCells = new List<GameObject>();

    List<Vector3> platformBoundary;   // filled from platform
    Vector3 platformCenter;

    public void Init(List<Vector3> boundary, Vector3 worldCenter)
    {
        platformBoundary = boundary;
        platformCenter  = worldCenter;
        Regenerate();
    }

    public void SetEnabled(bool v)
    {
        arenaEnabled = v;
        if (spawnedCells != null)
            foreach (var c in spawnedCells)
                c.SetActive(v);
    }

    public void Regenerate()
    {
        Clear();

        if (!arenaEnabled)
            return;

        if (hexCellPrefab == null)
        {
            Debug.LogError("HexBattleArenaGenerator: missing hexCellPrefab!");
            return;
        }

        float hexWidth = hexSize * 2f;
        float hexHeight = Mathf.Sqrt(3f) * hexSize;

        // rough bounding box around platform
        float minX=float.MaxValue, maxX=float.MinValue, minZ=float.MaxValue, maxZ=float.MinValue;
        foreach (var p in platformBoundary)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);
        }

        // iterate hex grid covering bounding rectangle
        for(float x=minX-hexWidth; x<=maxX+hexWidth; x+=hexWidth*0.75f)
        {
            for(float z=minZ-hexHeight; z<=maxZ+hexHeight; z+=hexHeight)
            {
                Vector3 cell = new Vector3(x, platformCenter.y, z);

                // offset each second column for hex staggering
                if (((int)((x - minX)/ (hexWidth*0.75f))) % 2 == 1)
                    cell.z += hexHeight * 0.5f;

                // check if cell center is inside polygon
                if (!PointInPolygonXZ(cell, platformBoundary))
                    continue;

                // Spawn visual cell
                var hexGO = Instantiate(hexCellPrefab, this.transform);
                HexagonController hexController = hexGO.GetComponent<HexagonController>();
                hexGO.transform.localPosition = new Vector3(cell.x, 0.1f, cell.z); // 0.1f to avoid blicking/intersection with ground0
                hexController.SetSize(hexSize);
                hexController.SetColor(Color.black);
                hexController.UpdateHex();
                spawnedCells.Add(hexGO);
            }
        }
    }

    public void Clear()
    {
        if (spawnedCells == null) return;
        for (int i=0;i<spawnedCells.Count;i++)
            if (spawnedCells[i]!=null)
                DestroyImmediate(spawnedCells[i]);
        spawnedCells.Clear();
    }

    // =============================
    // Point in polygon (XZ)
    // =============================
    bool PointInPolygonXZ(Vector3 point, List<Vector3> poly)
    {
        bool inside = false;
        for(int i=0, j=poly.Count-1; i<poly.Count; j=i++)
        {
            Vector3 pi = poly[i];
            Vector3 pj = poly[j];
            if (((pi.z > point.z) != (pj.z > point.z)) &&
                (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
                inside = !inside;
        }
        return inside;
    }
}
