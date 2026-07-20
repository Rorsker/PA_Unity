using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    public enum RoutePreviewStepType { Obstacle, SetLevelReward, AddSpeedReward }
    public enum RouteState { SpecialBoost, StrongGain, Gain, Neutral, Risk, HeavyRisk }

    [Serializable]
    public struct RoutePreviewStep
    {
        [InspectorName("Type（类型）")] public RoutePreviewStepType type;
        [Range(1, PlayerSpeedSettings.RequiredLevelCount), InspectorName("Required Level（要求等级）")] public int requiredLevel;
        [Min(0f), InspectorName("Boost Amount（增益数值）")] public float boostAmount;
        [Min(0f), InspectorName("Soft Cap（软上限）")] public float softCap;

        public static RoutePreviewStep Obstacle(int requiredLevel, float boostAmount, float softCap)
        {
            return new RoutePreviewStep
            {
                type = RoutePreviewStepType.Obstacle,
                requiredLevel = requiredLevel,
                boostAmount = boostAmount,
                softCap = softCap
            };
        }

        public static RoutePreviewStep SetLevelReward(int targetLevel)
        {
            return new RoutePreviewStep { type = RoutePreviewStepType.SetLevelReward, requiredLevel = targetLevel };
        }
    }

    [Serializable]
    public sealed class RoutePreviewSettings
    {
        [Header("Classification（分类判定）")]
        [Min(0.01f), InspectorName("Gain Threshold（增益阈值）")] public float gainThreshold = 0.15f;
        [Min(0.01f), InspectorName("Heavy Risk Threshold（高风险阈值）")] public float heavyRiskThreshold = 0.9f;
        [Min(0.01f), InspectorName("Recommend Min Score Difference（推荐最低分差）")] public float recommendMinScoreDifference = 0.22f;

        [Header("Choice Window（选择窗口）")]
        [Range(1.3f, 2f), InspectorName("Preview Time（预览时间）")] public float previewTime = 1.6f;
        [Range(0.1f, 0.3f), InspectorName("Fade In Duration（淡入时长）")] public float fadeInDuration = 0.2f;
        [Range(0.1f, 0.3f), InspectorName("Fade Out Duration（淡出时长）")] public float fadeOutDuration = 0.22f;
        [Range(6f, 24f), InspectorName("Minimum Preview Distance（最小预览距离）")] public float minimumPreviewDistance = 10f;
        [Range(30f, 80f), InspectorName("Maximum Preview Distance（最大预览距离）")] public float maximumPreviewDistance = 52f;
        [Range(3f, 16f), InspectorName("Ribbon Length（引导带长度）")] public float ribbonLength = 7.5f;

        [Header("Four-state visual language（四状态视觉语言）")]
        [InspectorName("Special Color（特殊颜色）")] public Color specialColor = new Color(1f, 0.68f, 0.08f, 1f);
        [InspectorName("Gain Color（增益颜色）")] public Color gainColor = new Color(0.04f, 1f, 0.48f, 1f);
        [InspectorName("Neutral Color（中性颜色）")] public Color neutralColor = new Color(0.68f, 0.86f, 1f, 1f);
        [InspectorName("Risk Color（风险颜色）")] public Color riskColor = new Color(0.46f, 0.018f, 0.025f, 1f);
        [Range(0.08f, 0.5f), InspectorName("Ribbon Width（引导带宽度）")] public float ribbonWidth = 0.12f;
        [Range(0.15f, 0.3f), InspectorName("Normal Ribbon Alpha（普通引导带透明度）")] public float normalRibbonAlpha = 0.22f;
        [Range(0.3f, 0.45f), InspectorName("Recommended Ribbon Alpha（推荐引导带透明度）")] public float recommendedRibbonAlpha = 0.34f;
        [Range(0.35f, 0.75f), InspectorName("Entry Icon Alpha（入口图标透明度）")] public float entryIconAlpha = 0.58f;
        [Range(0.2f, 0.8f), InspectorName("Icon Scale（图标缩放）")] public float iconScale = 0.34f;
    }

    public readonly struct RouteEvaluation
    {
        public readonly float StartSpeed;
        public readonly float ExpectedEndSpeed;
        public readonly float ExpectedSpeedDelta;
        public readonly int StartLevel;
        public readonly int ExpectedEndLevel;
        public readonly int GainTargetCount;
        public readonly int NeutralTargetCount;
        public readonly int LossTargetCount;
        public readonly int SpecialRewardCount;
        public readonly bool HasForcedSpeedLoss;
        public readonly RouteState State;
        public readonly float RecommendationScore;

        public RouteEvaluation(float startSpeed, float endSpeed, int startLevel, int endLevel,
            int gainCount, int neutralCount, int lossCount, int specialCount, bool forcedLoss,
            RouteState state, float score)
        {
            StartSpeed = startSpeed;
            ExpectedEndSpeed = endSpeed;
            ExpectedSpeedDelta = endSpeed - startSpeed;
            StartLevel = startLevel;
            ExpectedEndLevel = endLevel;
            GainTargetCount = gainCount;
            NeutralTargetCount = neutralCount;
            LossTargetCount = lossCount;
            SpecialRewardCount = specialCount;
            HasForcedSpeedLoss = forcedLoss;
            State = state;
            RecommendationScore = score;
        }
    }

    public static class RoutePreviewEvaluator
    {
        public static RouteEvaluation Evaluate(float startSpeed, IReadOnlyList<RoutePreviewStep> steps,
            PlayerSpeedSettings speedSettings, RoutePreviewSettings previewSettings)
        {
            float previewSpeed = Mathf.Clamp(startSpeed, speedSettings.minimumSpeed, speedSettings.maximumSpeed);
            int startLevel = GetLevel(previewSpeed, speedSettings);
            int gain = 0;
            int neutral = 0;
            int loss = 0;
            int special = 0;
            bool forcedLoss = false;
            bool crossedNormalSoftCap = false;

            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    RoutePreviewStep step = steps[i];
                    if (step.type == RoutePreviewStepType.Obstacle)
                    {
                        int currentLevel = GetLevel(previewSpeed, speedSettings);
                        CollisionOutcome outcome = ObstacleController.EvaluateCollisionOutcome(currentLevel, step.requiredLevel);
                        if (outcome == CollisionOutcome.SpeedGain)
                        {
                            gain++;
                            float cap = Mathf.Clamp(step.softCap, speedSettings.minimumSpeed, speedSettings.maximumSpeed);
                            previewSpeed = Mathf.Min(previewSpeed + Mathf.Max(0f, step.boostAmount), cap);
                        }
                        else if (outcome == CollisionOutcome.SpeedLoss)
                        {
                            loss++;
                            forcedLoss = true;
                            int targetLevel = Mathf.Max(1, currentLevel - 1);
                            previewSpeed = GetLevelStart(targetLevel, speedSettings);
                        }
                        else neutral++;
                    }
                    else if (step.type == RoutePreviewStepType.SetLevelReward)
                    {
                        special++;
                        float old = previewSpeed;
                        previewSpeed = GetLevelStart(step.requiredLevel, speedSettings);
                        crossedNormalSoftCap |= old <= speedSettings.normalImpactSoftCap && previewSpeed > speedSettings.normalImpactSoftCap;
                    }
                    else
                    {
                        special++;
                        float old = previewSpeed;
                        previewSpeed = Mathf.Min(previewSpeed + Mathf.Max(0f, step.boostAmount),
                            Mathf.Clamp(step.softCap, speedSettings.minimumSpeed, speedSettings.maximumSpeed));
                        crossedNormalSoftCap |= old <= speedSettings.normalImpactSoftCap && previewSpeed > speedSettings.normalImpactSoftCap;
                    }
                }
            }

            float delta = previewSpeed - startSpeed;
            RouteState state;
            if (special > 0 && delta > previewSettings.gainThreshold &&
                (crossedNormalSoftCap || previewSpeed > startSpeed))
                state = RouteState.SpecialBoost;
            else if (delta < -previewSettings.heavyRiskThreshold || loss > 1)
                state = RouteState.HeavyRisk;
            else if (delta < -previewSettings.gainThreshold || forcedLoss && delta <= previewSettings.gainThreshold)
                state = RouteState.Risk;
            else if (delta > previewSettings.gainThreshold * 3f)
                state = RouteState.StrongGain;
            else if (delta > previewSettings.gainThreshold)
                state = RouteState.Gain;
            else
                state = RouteState.Neutral;

            float score = delta + special * 0.35f - loss * 0.45f;
            return new RouteEvaluation(startSpeed, previewSpeed, startLevel, GetLevel(previewSpeed, speedSettings),
                gain, neutral, loss, special, forcedLoss, state, score);
        }

        public static int GetLevel(float speed, PlayerSpeedSettings settings)
        {
            int level = 1;
            for (int i = 1; i < settings.levelStartSpeeds.Length; i++)
            {
                if (speed < settings.levelStartSpeeds[i]) break;
                level = i + 1;
            }
            return level;
        }

        private static float GetLevelStart(int level, PlayerSpeedSettings settings)
        {
            return settings.levelStartSpeeds[Mathf.Clamp(level, 1, settings.levelStartSpeeds.Length) - 1];
        }
    }
}
