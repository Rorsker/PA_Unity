using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class RouteLanePreviewData
    {
        [InspectorName("Route ID（路线 ID）")] public string routeId;
        [InspectorName("Lane Index（路线索引）")] public int laneIndex;
        [InspectorName("Lane X（路线横向坐标）")] public float laneX;
        public readonly List<RoutePreviewStep> steps = new List<RoutePreviewStep>(4);
    }

    public sealed class RouteChoiceZoneData
    {
        public string zoneId;
        public float choiceZ;
        public bool consumed;
        public readonly RouteLanePreviewData[] routes = new RouteLanePreviewData[3];
        public readonly RouteEvaluation[] evaluations = new RouteEvaluation[3];
    }

    public sealed class RouteChoicePreview : MonoBehaviour
    {
        private readonly LineRenderer[] ribbons = new LineRenderer[3];
        private readonly LineRenderer[] icons = new LineRenderer[3];
        private readonly LineRenderer[] riskBadges = new LineRenderer[3];
        private readonly Color[] ribbonColors = new Color[3];
        private readonly Color[] iconColors = new Color[3];
        private readonly Color[] riskColors = new Color[3];
        private RoutePreviewSettings settings;
        private RouteChoiceZoneData zone;
        private float visibility;
        private bool targetVisible;
        private int recommendedLane = -1;

        public bool IsVisible => visibility > 0.001f;
        public RouteChoiceZoneData CurrentZone => zone;

        public void Initialize(RoutePreviewSettings previewSettings, Material sharedMaterial)
        {
            settings = previewSettings ?? new RoutePreviewSettings();
            for (int i = 0; i < 3; i++)
            {
                ribbons[i] = CreateLine("RouteRibbon_" + i, sharedMaterial, 2);
                icons[i] = CreateLine("RouteEntryIcon_" + i, sharedMaterial, 5);
                riskBadges[i] = CreateLine("RouteRiskBadge_" + i, sharedMaterial, 4);
            }
            ApplyVisibility(0f);
        }

        public void Show(RouteChoiceZoneData targetZone, float playerSpeed, PlayerSpeedSettings speedSettings)
        {
            zone = targetZone;
            transform.position = new Vector3(0f, 0.055f, zone.choiceZ);
            Reevaluate(playerSpeed, speedSettings);
            targetVisible = true;
        }

        public void Reevaluate(float playerSpeed, PlayerSpeedSettings speedSettings)
        {
            if (zone == null) return;
            float best = float.NegativeInfinity;
            float second = float.NegativeInfinity;
            int bestLane = -1;
            for (int i = 0; i < 3; i++)
            {
                RouteLanePreviewData route = zone.routes[i];
                zone.evaluations[i] = RoutePreviewEvaluator.Evaluate(playerSpeed, route.steps, speedSettings, settings);
                float score = zone.evaluations[i].RecommendationScore;
                if (score > best)
                {
                    second = best;
                    best = score;
                    bestLane = i;
                }
                else if (score > second) second = score;
            }
            recommendedLane = best - second >= settings.recommendMinScoreDifference ? bestLane : -1;
            ConfigureLines();
        }

        public void Hide()
        {
            targetVisible = false;
        }

        public void Tick(float unscaledDeltaTime)
        {
            float duration = targetVisible ? settings.fadeInDuration : settings.fadeOutDuration;
            visibility = Mathf.MoveTowards(visibility, targetVisible ? 1f : 0f,
                unscaledDeltaTime / Mathf.Max(0.01f, duration));
            ApplyVisibility(visibility);
            if (!targetVisible && visibility <= 0f) zone = null;
        }

        private void ConfigureLines()
        {
            for (int i = 0; i < 3; i++)
            {
                RouteLanePreviewData route = zone.routes[i];
                RouteEvaluation evaluation = zone.evaluations[i];
                Color color = GetColor(evaluation.State);
                bool recommended = i == recommendedLane;
                Color ribbonColor = color;
                ribbonColor.a = recommended ? settings.recommendedRibbonAlpha : settings.normalRibbonAlpha;
                Color iconColor = color;
                iconColor.a = settings.entryIconAlpha * (recommended ? 1f : 0.82f);
                float nearZ = -settings.ribbonLength;
                ribbons[i].SetPosition(0, new Vector3(route.laneX * 0.48f, 0f, nearZ));
                ribbons[i].SetPosition(1, new Vector3(route.laneX, 0f, -0.8f));
                ribbons[i].startWidth = settings.ribbonWidth * 0.78f;
                ribbons[i].endWidth = settings.ribbonWidth * (recommended ? 1.18f : 0.92f);
                ribbonColors[i] = ribbonColor;
                iconColors[i] = iconColor;
                SetColor(ribbons[i], ribbonColor);

                ConfigureIcon(icons[i], evaluation.State, route.laneX, iconColor, recommended);
                riskBadges[i].enabled = evaluation.HasForcedSpeedLoss;
                if (evaluation.HasForcedSpeedLoss)
                {
                    float s = settings.iconScale * 0.32f;
                    riskBadges[i].SetPosition(0, new Vector3(route.laneX + s, 0.02f, -0.65f));
                    riskBadges[i].SetPosition(1, new Vector3(route.laneX + s * 2f, 0.02f, -0.2f));
                    riskBadges[i].SetPosition(2, new Vector3(route.laneX + s, 0.02f, 0.25f));
                    riskBadges[i].SetPosition(3, new Vector3(route.laneX, 0.02f, -0.2f));
                    Color riskColor = settings.riskColor;
                    riskColor.a = settings.entryIconAlpha * 0.72f;
                    riskColors[i] = riskColor;
                    SetColor(riskBadges[i], riskColor);
                }
            }
        }

        private void ConfigureIcon(LineRenderer line, RouteState state, float x, Color color, bool recommended)
        {
            float s = settings.iconScale * (recommended ? 1.18f : 1f);
            line.startWidth = settings.ribbonWidth * 0.34f;
            line.endWidth = line.startWidth;
            SetColor(line, color);
            if (state == RouteState.Neutral)
            {
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(x - s, 0.02f, 0.15f));
                line.SetPosition(1, new Vector3(x + s, 0.02f, 0.15f));
            }
            else if (state == RouteState.SpecialBoost)
            {
                line.positionCount = 5;
                line.SetPosition(0, new Vector3(x - s * 0.2f, 0.02f, -0.5f));
                line.SetPosition(1, new Vector3(x + s * 0.45f, 0.02f, 0f));
                line.SetPosition(2, new Vector3(x, 0.02f, 0.05f));
                line.SetPosition(3, new Vector3(x + s * 0.2f, 0.02f, 0.65f));
                line.SetPosition(4, new Vector3(x - s * 0.45f, 0.02f, 0.12f));
            }
            else
            {
                bool risk = state == RouteState.Risk || state == RouteState.HeavyRisk;
                float direction = risk ? -1f : 1f;
                line.positionCount = 5;
                line.SetPosition(0, new Vector3(x, 0.02f, -s * direction));
                line.SetPosition(1, new Vector3(x, 0.02f, s * direction));
                line.SetPosition(2, new Vector3(x - s * 0.52f, 0.02f, s * 0.42f * direction));
                line.SetPosition(3, new Vector3(x, 0.02f, s * direction));
                line.SetPosition(4, new Vector3(x + s * 0.52f, 0.02f, s * 0.42f * direction));
            }
        }

        private LineRenderer CreateLine(string objectName, Material material, int positions)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = positions;
            line.sharedMaterial = material;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private Color GetColor(RouteState state)
        {
            if (state == RouteState.SpecialBoost) return settings.specialColor;
            if (state == RouteState.Gain || state == RouteState.StrongGain) return settings.gainColor;
            if (state == RouteState.Risk || state == RouteState.HeavyRisk) return settings.riskColor;
            return settings.neutralColor;
        }

        private static void SetColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, color.a * 0.42f);
        }

        private void ApplyVisibility(float alpha)
        {
            bool enabled = alpha > 0.001f;
            for (int i = 0; i < 3; i++)
            {
                ribbons[i].enabled = enabled;
                icons[i].enabled = enabled;
                if (!enabled) riskBadges[i].enabled = false;
                ApplyColor(ribbons[i], ribbonColors[i], alpha);
                ApplyColor(icons[i], iconColors[i], alpha);
                if (riskBadges[i].enabled) ApplyColor(riskBadges[i], riskColors[i], alpha);
            }
        }

        private static void ApplyColor(LineRenderer line, Color baseColor, float visibility)
        {
            Color start = baseColor;
            Color end = baseColor;
            start.a *= visibility;
            end.a *= visibility * 0.46f;
            line.startColor = start;
            line.endColor = end;
        }
    }
}
