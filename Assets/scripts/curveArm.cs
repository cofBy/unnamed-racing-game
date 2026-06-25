using System.Collections;
using UnityEngine;

public class curveArm : MonoBehaviour
{
    [Header("Points")]
    public Transform[] points;

    [Header("rendering the curve")]
    public LineRenderer curveRender;
    public int steps;
    public float timer;

    Vector2[] linears;

    Vector2[] segment;
    private void Awake()
    {
        linears = new Vector2[points.Length - 1];
        segment = new Vector2[points.Length - 1];
    }
    private void Update()
    {
        linears = new Vector2[points.Length - 1];
        segment = new Vector2[points.Length - 1];

        curveRender.positionCount = steps;
        for (int j = 0; j < steps; j++)
        {
            timer = (float)j / (steps - 1);

            for (int i = 0; i < linears.Length; i++)
            {
                linears[i] = Vector3.Lerp(points[i].position, points[i + 1].position, timer);
                segment[i] = linears[i];
            }
            curve(linears.Length - 1, timer);

            curveRender.SetPosition(j, segment[0]);
        }
    }
    void curve(int length, float t)
    {
        if (length <= 0) return;

        for (int i = 0; i < length; i++)
        {
            segment[i] = Vector2.Lerp(segment[i], segment[i + 1], t);
        }

        curve(length - 1, t);
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (i <  points.Length - 1)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }
}
