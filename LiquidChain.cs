using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARYKEI.LiquidChain
{
    [RequireComponent(typeof(LineRenderer))]
    public class LiquidChain : MonoBehaviour
    {
        public LiquidChainSolver solver;
        public LineRenderSetting lineRenderSettings;
        public TargetSetting targetSettings;
        private Transform target;

        private void Start()
        {
            solver.Init();
            lineRenderSettings.Init(GetComponent<LineRenderer>(), solver.points.Count);
        }

        private void FixedUpdate()
        {
            if (solver.isConnected)
            {
                solver.CalcRestDistance(transform.position, target.position);
                lineRenderSettings.CalcLengthMultiplier(solver.currentRestDistance);
                if ((transform.position - target.position).sqrMagnitude < targetSettings.touchDistance * targetSettings.touchDistance)
                    InitChain();
            }
            else
            {
                solver.ConnectState--;
                lineRenderSettings.UpdateBrokenChainMultiplier();

                if (solver.isChainDead)
                {
                    lineRenderSettings.Line.enabled = false;
                    target = targetSettings.FindTarget(transform.position);
                    if (target != null)
                        InitChain();
                    return;
                }
            }

            solver.Predict();
            solver.MakeStraight(transform.position, target.position);
            solver.DistanceConstraint();
            solver.MoveLiquid();
            solver.CheckBreak(lineRenderSettings.LengthMultiplier);
            solver.UpdateVelocity();

            lineRenderSettings.UpdateLine(solver.points);
        }

        private void InitChain()
        {
            solver.SetupChain(transform, target);
            lineRenderSettings.BrokenChainWidthMultiplier = 1;
        }

        [System.Serializable]
        public class LiquidChainSolver
        {
            public List<ChainPoint> points;
            public int IterationCount = 3;
            public int numOfPoints = 15;

            public float BreakThreshold = 0.01f;
            public float StraightRange = 0.03f;
            public float GravityMultiplier = 1.0f;

            [HideInInspector]
            public float currentTotalDistance;
            [HideInInspector]
            public float currentRestDistance;
            public float LiquidQuantity = 5;

            public int LifeTimeFramesOfBrokenChain = 60;
            [HideInInspector]
            public int ConnectState;
            public bool isConnected
            {
                get { return ConnectState == 1; }
            }
            public bool isChainDead
            {
                get { return ConnectState <= -LifeTimeFramesOfBrokenChain; }
            }


            public void Init()
            {
                points = new List<ChainPoint>();
                for (int i = 0; i < numOfPoints + 2; i++)
                    points.Add(new ChainPoint());
                ConnectState = -LifeTimeFramesOfBrokenChain;
            }

            public void SetupChain(Transform src, Transform dst)
            {
                points[0].Set(src, LiquidQuantity);
                points[points.Count - 1].Set(dst, LiquidQuantity);

                float restDistance = (src.position - dst.position).magnitude / (numOfPoints + 2 - 1);
                var direction = (dst.position - src.position).normalized;
                for (int i = 1; i < numOfPoints + 1; i++)
                    points[i].Set(src.position + direction * restDistance * (i + 1), LiquidQuantity);
                ConnectState = 1;
            }

            public void CalcRestDistance(Vector3 srcPos, Vector3 dstPos)
            {
                currentTotalDistance = (srcPos - dstPos).magnitude;
                currentRestDistance = currentTotalDistance / (float)(points.Count - 1);
            }
            public void MakeStraight(Vector3 p0, Vector3 p1)
            {
                if (!isConnected) return;

                var dir = p1 - p0;
                var length = dir.magnitude;

                float Straightness = Mathf.Pow(Mathf.Clamp01(StraightRange / length), 3);

                for (int i = 1; i < points.Count - 1; i++)
                {
                    float alpha = i / (float)(points.Count - 1);
                    var linearPos = p0 + dir * alpha;

                    points[i].predict = Vector3.Lerp(points[i].predict, linearPos, Straightness);
                }
            }
            public void CheckBreak(float LengthMultiplier)
            {
                if (!isConnected) return;
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].mass * LengthMultiplier < BreakThreshold)
                    {
                        ConnectState = 0;
                        return;
                    }
                }
            }
            public void MoveLiquid()
            {
                if (!isConnected) return;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    var p0 = points[i];
                    var p1 = points[i + 1];

                    var flowDir = ((p1.predict - p0.predict).normalized.y + 1) * 0.5f;

                    float totalmass = p0.mass + p1.mass;

                    float a0 = flowDir;
                    float a1 = 1 - flowDir;

                    float flowSpeed = (p0.velocity + p1.velocity).magnitude * 100;

                    p0.mass = Mathf.Lerp(p0.mass, totalmass * a0, Time.fixedDeltaTime * flowSpeed);
                    p1.mass = Mathf.Lerp(p1.mass, totalmass * a1, Time.fixedDeltaTime * flowSpeed);
                }
            }
            public void Predict()
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];

                    p.position = p.predict;
                    p.velocity += new Vector3(0, -9.8f * GravityMultiplier, 0) * Time.fixedDeltaTime;
                    p.predict += p.velocity * Time.fixedDeltaTime;
                }
            }
            public void DistanceConstraint()
            {
                for (int it = 0; it < IterationCount; it++)
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var p0 = points[i];
                        var p1 = points[i + 1];

                        float mass0 = p0.mass;
                        float mass1 = p1.mass;

                        if (i == 0) mass0 = 10000;
                        if (i + 1 == points.Count - 1) mass1 = 10000;

                        mass0 = 1.0f / mass0;
                        mass1 = 1.0f / mass1;

                        var mass = mass0 + mass1;
                        var a0 = mass0 / mass;
                        var a1 = mass1 / mass;

                        var n = p1.predict - p0.predict;
                        float d = n.magnitude;
                        n = n / d;

                        var correction = n * (d - currentRestDistance);
                        p0.predict = p0.predict + correction * a0;
                        p1.predict = p1.predict - correction * a1;
                    }
                }
            }
            public void UpdateVelocity()
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    if (isConnected && p.PinTransform != null) p.predict = p.PinTransform.position;
                    p.velocity = (p.predict - p.position) / Time.fixedDeltaTime;
                }
            }
        }

        [System.Serializable]
        public class LineRenderSetting
        {
            public float Width = 0.001f;
            public float MinWidth = 0.1f;

            public float baseLength = 0.1f;
            [HideInInspector]
            public LineRenderer Line;
            private AnimationCurve widthCurve;

            [HideInInspector]
            public float BrokenChainWidthMultiplier = 1;
            [HideInInspector]
            public float LengthMultiplier;

            public void CalcLengthMultiplier(float restDistance)
            {
                LengthMultiplier = Mathf.Min(1, Mathf.Pow(baseLength/ restDistance, 3));
            }
            public void UpdateBrokenChainMultiplier()
            {
                BrokenChainWidthMultiplier = Mathf.Lerp(BrokenChainWidthMultiplier, 0, Time.fixedDeltaTime * 15);
            }

            public void Init(LineRenderer _Line, int numOfPoints)
            {
                Line = _Line;
                Line.positionCount = numOfPoints;
                Line.useWorldSpace = true;
            }

            public void UpdateLine(List<ChainPoint> points)
            {
                Line.enabled = true;
                UpdateLineRendererPoints(points);
                UpdateLineRendererWidth(points);
            }

            private Keyframe GetLineWidthKeyframeOfPointIndex(List<ChainPoint> points, int idx)
            {
                float t = (idx + 1) / (float)(points.Count - 1);

                float width = Mathf.Max(MinWidth, points[idx].mass * points[idx].randomWidth);
                width *= LengthMultiplier;
                width *= BrokenChainWidthMultiplier;

                return new Keyframe(t, width);
            }

            public void UpdateLineRendererPoints(List<ChainPoint> points)
            {
                for (int i = 0; i < points.Count; i++) Line.SetPosition(i, points[i].predict);
            }
            public void UpdateLineRendererWidth(List<ChainPoint> points)
            {
                Line.widthMultiplier = Width;
                if (widthCurve == null)
                {
                    widthCurve = new AnimationCurve();
                    for (int i = 0; i < points.Count; i++) widthCurve.AddKey(GetLineWidthKeyframeOfPointIndex(points, i));
                }
                for (int i = 0; i < points.Count; i++)
                    widthCurve.MoveKey(i, GetLineWidthKeyframeOfPointIndex(points, i));
                Line.widthCurve = widthCurve;
            }
        }

        [System.Serializable]
        public class TargetSetting
        {
            public float touchDistance = 0.03f;
            public string targetTag = "LiquidChainTarget";

            public Transform FindTarget(Vector3 pos)
            {
                float sqRange = touchDistance * touchDistance;
                var targets = GameObject.FindGameObjectsWithTag(targetTag);

                for (int i = 0; i < targets.Length; i++)
                {
                    if ((pos - targets[i].transform.position).sqrMagnitude < sqRange)
                    {
                        return targets[i].transform;
                    }
                }
                return null;
            }
        }

        public class ChainPoint
        {
            public Vector3 predict;
            public Vector3 position;
            public Vector3 velocity;
            public Transform PinTransform;
            public float mass;
            public float randomWidth;

            public void Set(Transform _PinTransform, float _StartLiquid)
            {
                PinTransform = _PinTransform;
                predict = PinTransform.position;
                position = PinTransform.position;
                velocity = Vector3.zero;
                mass = _StartLiquid;
            }
            public void Set(Vector3 pos, float StartLiquid)
            {
                PinTransform = null;
                predict = pos;
                position = pos;
                velocity = Vector3.zero;
                mass = StartLiquid * 0.1f;
                randomWidth = Random.Range(0.3f, 1f);
            }
        }
    }
}