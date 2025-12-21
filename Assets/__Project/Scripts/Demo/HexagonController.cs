using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HexagonController : MonoBehaviour
{
    private float size = 1f;        // радиус шестиугольника
    private LineRenderer lr;

    // нормализованные точки правильного шестиугольника
    private static readonly Vector3[] basePoints = new Vector3[]
    {
        new Vector3( 1f, 0f, 0f ),
        new Vector3( 0.5f, 0f, 0.866f ),
        new Vector3( -0.5f, 0f, 0.866f ),
        new Vector3( -1f, 0f, 0f ),
        new Vector3( -0.5f, 0f, -0.866f ),
        new Vector3( 0.5f, 0f, -0.866f ),
        new Vector3( 1f, 0f, 0f )   // замыкаем
    };

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = basePoints.Length;
        UpdateHex();
    }

    private void OnValidate()
    {
        if (lr != null)
            UpdateHex();
    }

    public void UpdateHex()
    {
        for (int i = 0; i < basePoints.Length; i++)
            lr.SetPosition(i, basePoints[i] * size);
    }

    public void SetColor(Color c)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        lr.startColor = c;
        lr.endColor = c;
    }
    
    public void SetSize(float newSize)
    {
        size = newSize;
        UpdateHex(); // Update hex shape when size changes
    }
}