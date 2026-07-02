using System.Collections.Generic;
using System;
using UnityEngine;
public class Point : MonoBehaviour
{
    [Header("Refrences")]
    public Transform Anchor;

    [Header("Chain")]
    public bool autoDistance;

    public List<segment> Segments;

    Vector3[] SegmentPlaces;
    Vector3 LastPlace;
    public enum RH { None, Up, Down, Left, Right, Forword, Backward}
    public RH RoationUp;

    public bool UseStapleUp;
    [ReadOnly("UseStapleUp")]public Vector3 StapleUpAxis;

    [Header("FABRIK")]
    public bool DoFABRIK;
    [ReadOnly("DoFABRIK")] public Transform Target;
    [ReadOnly("DoFABRIK")] public GameObject PoleObject;

    [Header("Collision")]
    public bool UseCollisions;
    [ReadOnly("UseCollisions")]public float tolerance;

    [Header("Gizmos")]
    public bool DrawPointDistance;
    public bool DrawEndEffectorMaxDistance;
    public bool DrawLimbs;
    public bool DrawPoleVector;
    bool started = false;

    private void Start()
    {
        started = true;
        SegmentPlaces = new Vector3[Segments.Count];
        for (int i = 0; i < SegmentPlaces.Length; i++)
        {
            SegmentPlaces[i] = Segments[i].transform.position;
        }
    }

    private void LateUpdate()
    {
        SegmentPlaces = new Vector3[Segments.Count];
        for (int i = 0; i < SegmentPlaces.Length; i++)
        {
            SegmentPlaces[i] = Segments[i].transform.position;
        }
        // sets the shoulder - head
        SegmentPlaces[0] = Anchor.position;
        Segments[0].transform.position = SegmentPlaces[0];

        Rotate(Segments[0].transform, SegmentPlaces[1]);

        // Chain DC
        for (int i = 1; i < Segments.Count; i++)
        {
            SegmentPlaces[i] = ConstraintDistance(SegmentPlaces[i], SegmentPlaces[i - 1], Segments[i].distance);
            Segments[i].transform.position += SegmentPlaces[i] - Segments[i].transform.position;
            if (i < Segments.Count - 1)
            {
                Rotate(Segments[i].transform, SegmentPlaces[i + 1]);
            }

            if (UseCollisions == true)
            {
                for (int j = 0; j < SegmentPlaces.Length; j++)
                {
                    if (Vector3.Distance(SegmentPlaces[j], SegmentPlaces[i]) < Segments[j].distance - tolerance & i != j)
                    {
                        SegmentPlaces[i] = ConstraintDistance(SegmentPlaces[i], SegmentPlaces[j], Segments[j].distance + tolerance);
                    }
                }
                if (Vector3.Distance(LastPlace, SegmentPlaces[i]) < Segments[i].distance - tolerance)
                {
                    SegmentPlaces[i] = ConstraintDistance(SegmentPlaces[i], LastPlace, Segments[i].distance + tolerance);
                }
            }
        }


        // FABRIK
        if (DoFABRIK == true)
        {
            SegmentPlaces[SegmentPlaces.Length - 1] = EndEffector();
            Segments[Segments.Count - 1].transform.position = SegmentPlaces[SegmentPlaces.Length - 1];

            for (int i = Segments.Count - 1; i > 0; i--)
            {
                SegmentPlaces[i - 1] = ConstraintDistance(SegmentPlaces[i - 1], SegmentPlaces[i], Segments[i].distance);
                Segments[i - 1].transform.position = SegmentPlaces[i - 1];
            }
            if (PoleObject != null)
            {
                SolvePoleVector(PoleObject.transform.position);
            }
        }
        else
        {
            LastPlace = ConstraintDistance(LastPlace, SegmentPlaces[SegmentPlaces.Length - 1], Segments[Segments.Count - 1].distance);
            Rotate(Segments[Segments.Count - 1].transform, LastPlace);
        }

    }
    void SolvePoleVector(Vector3 polePosition)
    {
        for (int i = 1; i < SegmentPlaces.Length - 1; i++)
        {
            Vector3 prev = SegmentPlaces[i - 1];
            Vector3 current = SegmentPlaces[i];
            Vector3 next = SegmentPlaces[i + 1];

            Vector3 planeNormal = (next - prev).normalized;
            Plane plane = new Plane(planeNormal, prev);

            Vector3 projectedPole = plane.ClosestPointOnPlane(polePosition);
            Vector3 projectedJoint = plane.ClosestPointOnPlane(current);

            Vector3 jointDir = (projectedJoint - prev).normalized;
            Vector3 poleDir = (projectedPole - prev).normalized;

            float angle = Vector3.SignedAngle(jointDir, poleDir, planeNormal);
            SegmentPlaces[i] = Quaternion.AngleAxis(angle, planeNormal) * (current - prev) + prev;
        }
    }
    void Rotate(Transform obj, Vector3 to)
    {
        Vector3 dir = (to - obj.position).normalized;

        if (dir == Vector3.zero)
            return;
        
        Vector3 up = StapleUpAxis;

        if (UseStapleUp == true)
        {
            switch (RoationUp)
            {
                case RH.Forword:
                    obj.rotation = Quaternion.LookRotation(dir, up);
                    break;

                case RH.Backward:
                    obj.rotation = Quaternion.LookRotation(-dir, up);
                    break;

                case RH.Up:
                    obj.rotation = Quaternion.LookRotation(up, dir);
                    break;

                case RH.Down:
                    obj.rotation = Quaternion.LookRotation(up, -dir);
                    break;

                case RH.Right:
                    obj.rotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(0, 0, -90);
                    break;

                case RH.Left:
                    obj.rotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(0, 0, 90);
                    break;
            }
        }
        else
        {
            switch (RoationUp)
            {
                case RH.Forword:
                    obj.forward = dir;
                    break;

                case RH.Backward:
                    obj.forward = -dir;
                    break;

                case RH.Up:
                    obj.up = dir;
                    break;

                case RH.Down:
                    obj.up = -dir;
                    break;

                case RH.Right:
                    obj.right = dir;
                    break;

                case RH.Left:
                    obj.right = -dir;
                    break;
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float distanceSum = 0;
        if (DrawPointDistance == true)
        {
            for (int i = 1; i < Segments.Count; i++)
            {
                distanceSum += Segments[i].distance;
                Gizmos.DrawWireSphere(Segments[i].transform.position, Segments[i].distance);
            }
        }

        if (DoFABRIK == true)
        {
            if (DrawEndEffectorMaxDistance == true)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(Anchor.position, distanceSum);
            }

            for (int i = 1; i < Segments.Count; i++)
            {
                if (DrawLimbs == true && started == true)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(SegmentPlaces[i - 1], SegmentPlaces[i]);
                }
            }
            if (PoleObject != null & DrawPoleVector == true)
            {
                Gizmos.color = Color.yellow;
                Vector3 Dir = Segments[Segments.Count - 1].transform.position - Segments[0].transform.position;

                Vector3 med = Vector3.Lerp(Segments[Segments.Count - 1].transform.position, Segments[0].transform.position, 0.5f);

                Gizmos.DrawRay(transform.position, Dir);
                Gizmos.DrawRay(med, (PoleObject.transform.position - med).normalized);
            }
        }
    }
    Vector3 EndEffector()
    {
        float MaxDistance = 0;
        for (int i = 0; i < Segments.Count -1; i++)
        {
            MaxDistance += Segments[i].distance;
        }

        return Vector3.ClampMagnitude(Target.position - Anchor.position, MaxDistance) + Anchor.position;
    }

    Vector3 ConstraintDistance(Vector3 point, Vector3 anchor, float dist)
    {
        Vector3 dir = (point - anchor).normalized;

        return (dir * dist) + anchor;
    }

    [Serializable] public struct segment
    {
        public Transform transform;
        public float distance;
    }

    //private void OnValidate()
    //{
    //    if (autoDistance == true && started == false)
    //    {
    //        for (int i = 0; i < Segments.Count - 1; i++)
    //        {
    //            Segments[i].distance = Vector3.Distance(Segments[i].transform.position, Segments[i + 1].transform.position);
    //        }
    //        Segments[Segments.Count - 1].distance = Vector3.Distance(Segments[Segments.Count - 1].transform.position, Segments[Segments.Count - 2].transform.position);
    //    }
    //}
}
