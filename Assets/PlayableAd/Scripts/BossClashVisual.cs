using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    public enum BossClashPhase
    {
        None,
        Approach,
        Contact,
        Struggle,
        Stagger,
        Finish
    }

    [Serializable]
    public sealed class BossClashSettings
    {
        [Header("Boss Phase Timing（Boss 阶段时序）")]
        [Range(0.4f, 0.6f), InspectorName("Approach Duration（接近阶段时长）")] public float approachDuration = 0.5f;
        [Range(0.15f, 0.25f), InspectorName("Contact Duration（接触阶段时长）")] public float contactDuration = 0.2f;
        [Range(1f, 1.5f), InspectorName("Low-Level Struggle Duration（未满10级角力时长）")] public float struggleDuration = 1.25f;
        [Range(0.3f, 0.5f), InspectorName("Stagger Duration（踉跄阶段时长）")] public float staggerDuration = 0.4f;
        [Range(0.3f, 0.5f), InspectorName("Finish Duration（终结阶段时长）")] public float finishDuration = 0.4f;

        [Header("Interactive Tap Struggle（交互连点角力）")]
        [Range(1, 20), InspectorName("Required Tap Count（胜利所需点击数）")] public int requiredTapCount = 8;
        [Min(0f), InspectorName("Z-Axis Tug Amplitude（Z轴拉扯幅度）")] public float zAxisTugAmplitude = 0.24f;
        [Min(0.1f), InspectorName("Z-Axis Tug Frequency（Z轴拉扯频率）")] public float zAxisTugFrequency = 2.4f;
        [Min(0f), InspectorName("Z-Axis Tap Push Distance（Z轴单次点击推进距离）")] public float tapPushDistance = 0.18f;
        [Min(0.02f), InspectorName("Tap Push Return Duration（点击推进回弹时长）")] public float tapPushReturnDuration = 0.12f;

        [Header("Rapid Tap Hand Hint（快速连点手指提示）")]
        [InspectorName("Hint Screen Anchor（提示屏幕锚点）")] public Vector2 tapHintScreenAnchor = new Vector2(0.72f, 0.34f);
        [InspectorName("Hint Screen Offset（提示屏幕偏移）")] public Vector2 tapHintScreenOffset = Vector2.zero;
        [InspectorName("Hint Size（提示尺寸）")] public Vector2 tapHintSize = new Vector2(170f, 255f);
        [Min(0f), InspectorName("Hint Bob Distance（提示浮动距离）")] public float tapHintBobDistance = 12f;
        [Min(0.1f), InspectorName("Hint Pulse Speed（提示脉冲速度）")] public float tapHintPulseSpeed = 2.8f;
        [Range(0f, 0.25f), InspectorName("Hint Pulse Scale（提示脉冲幅度）")] public float tapHintPulseScale = 0.055f;
        [Range(0f, 0.5f), InspectorName("Tap Compression（点击压缩幅度）")] public float tapHintTapCompression = 0.18f;
        [Min(0f), InspectorName("Tap Drop Distance（点击下压距离）")] public float tapHintTapDrop = 16f;
        [Min(0.02f), InspectorName("Tap Response Duration（点击回弹时长）")] public float tapHintTapResponse = 0.12f;

        [Header("Camera Hierarchy（镜头反馈层级）")]
        [Range(0.1f, 0.55f), InspectorName("Contact Shake（接触抖动）")] public float contactShake = 0.3f;
        [Range(0.01f, 0.14f), InspectorName("Struggle Shake（对抗抖动）")] public float struggleShake = 0.065f;
        [Range(0.35f, 0.9f), InspectorName("Finish Shake（终结抖动）")] public float finishShake = 0.62f;
        [Range(0f, 8f), InspectorName("Contact FOV Punch（接触视场角冲击）")] public float contactFovPunch = 3f;
        [Range(0f, 12f), InspectorName("Finish FOV Punch（终结视场角冲击）")] public float finishFovPunch = 8f;

        [Header("Clash Readability（对抗可读性）")]
        [InspectorName("Player Energy（玩家能量颜色）")] public Color playerEnergy = new Color(1f, 0.36f, 0.04f, 0.9f);
        [InspectorName("Boss Energy（Boss 能量颜色）")] public Color bossEnergy = new Color(0.38f, 0.03f, 0.24f, 0.86f);
        [Range(0.15f, 0.55f), InspectorName("Beam Width（光束宽度）")] public float beamWidth = 0.28f;
        [Range(0.3f, 1f), InspectorName("Center Brightness（中心亮度）")] public float centerBrightness = 0.78f;
    }

    public sealed class BossClashVisual : MonoBehaviour
    {
        private BossClashSettings settings;
        private Transform runner;
        private Transform boss;
        private Transform centerRoot;
        private LineRenderer playerBeam;
        private LineRenderer bossBeam;
        private LineRenderer[] rings;
        private LineRenderer[] bossTracks;
        private LineRenderer[] cracks;
        private readonly List<Material> ownedMaterials = new List<Material>(4);
        private BossClashPhase phase;
        private bool playerWins;

        public BossClashPhase Phase => phase;

        public void Initialize(Transform runnerTransform, Transform bossTransform, BossClashSettings clashSettings)
        {
            runner = runnerTransform;
            boss = bossTransform;
            settings = clashSettings;
            BuildEnergyBeams();
            BuildCenterRings();
            BuildGroundMarks();
            SetVisible(false);
        }

        public void Begin(bool wins)
        {
            playerWins = wins;
            SetVisible(true);
            SetPhase(BossClashPhase.Approach);
        }

        public void SetPhase(BossClashPhase nextPhase)
        {
            phase = nextPhase;
            bool showForwardTracks = phase == BossClashPhase.Struggle
                || phase == BossClashPhase.Stagger || phase == BossClashPhase.Finish;
            for (int i = 0; i < bossTracks.Length; i++) bossTracks[i].enabled = showForwardTracks;
            bool cracked = phase == BossClashPhase.Stagger || phase == BossClashPhase.Finish;
            for (int i = 0; i < cracks.Length; i++) cracks[i].enabled = cracked;
        }

        public void UpdatePresentation(float phaseProgress, Vector3 clashCenter)
        {
            centerRoot.position = clashCenter;
            playerBeam.SetPosition(0, runner.position + Vector3.up * 0.8f);
            playerBeam.SetPosition(1, clashCenter);
            bossBeam.SetPosition(0, boss.position + Vector3.up * 1.1f);
            bossBeam.SetPosition(1, clashCenter);

            float peak = phase == BossClashPhase.Contact || phase == BossClashPhase.Finish ? 1f : 0.72f;
            float pulse = 0.88f + Mathf.Sin(Time.unscaledTime * 22f) * 0.12f;
            playerBeam.startWidth = settings.beamWidth * peak;
            playerBeam.endWidth = settings.beamWidth * 0.58f * peak;
            bossBeam.startWidth = settings.beamWidth * peak;
            bossBeam.endWidth = settings.beamWidth * 0.58f * peak;

            for (int i = 0; i < rings.Length; i++)
            {
                float scale = (0.65f + i * 0.28f) * pulse * (phase == BossClashPhase.Finish ? 1.35f : 1f);
                rings[i].transform.localScale = Vector3.one * scale;
                rings[i].transform.Rotate(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (90f + i * 35f) * Time.unscaledDeltaTime);
                Color centerColor = Color.Lerp(settings.playerEnergy, settings.bossEnergy, playerWins ? 0.35f : 0.68f);
                centerColor.a = settings.centerBrightness * peak * (1f - i * 0.16f);
                rings[i].startColor = centerColor;
                rings[i].endColor = centerColor;
            }

            UpdateGroundMarks(phaseProgress);
        }

        public void SetVisible(bool visible)
        {
            if (playerBeam != null) playerBeam.enabled = visible;
            if (bossBeam != null) bossBeam.enabled = visible;
            if (rings != null) for (int i = 0; i < rings.Length; i++) rings[i].enabled = visible;
            if (!visible)
            {
                if (bossTracks != null) for (int i = 0; i < bossTracks.Length; i++) bossTracks[i].enabled = false;
                if (cracks != null) for (int i = 0; i < cracks.Length; i++) cracks[i].enabled = false;
                phase = BossClashPhase.None;
            }
        }

        private void BuildEnergyBeams()
        {
            Material playerMaterial = CreateOwnedMaterial("SharedBossPlayerEnergy");
            Material bossMaterial = CreateOwnedMaterial("SharedBossDarkEnergy");
            playerBeam = CreateLine("PlayerClashEnergy", playerMaterial, settings.playerEnergy, true);
            bossBeam = CreateLine("BossClashEnergy", bossMaterial, settings.bossEnergy, true);
        }

        private void BuildCenterRings()
        {
            centerRoot = new GameObject("ClashCenter").transform;
            centerRoot.SetParent(transform, false);
            Material material = CreateOwnedMaterial("SharedClashRing");
            rings = new LineRenderer[3];
            for (int ringIndex = 0; ringIndex < rings.Length; ringIndex++)
            {
                LineRenderer ring = CreateLine("ClashRing_" + ringIndex, material, Color.white, false);
                ring.transform.SetParent(centerRoot, false);
                ring.loop = true;
                ring.positionCount = 40;
                ring.startWidth = 0.06f + ringIndex * 0.025f;
                ring.endWidth = ring.startWidth;
                for (int i = 0; i < 40; i++)
                {
                    float angle = i / 40f * Mathf.PI * 2f;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
                }
                rings[ringIndex] = ring;
            }
        }

        private void BuildGroundMarks()
        {
            Material material = CreateOwnedMaterial("SharedBossGroundMarks");
            bossTracks = new LineRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                bossTracks[i] = CreateLine("BossRetreatTrack_" + i, material, new Color(0.15f, 0.08f, 0.06f, 0.8f), true);
                bossTracks[i].startWidth = 0.12f;
                bossTracks[i].endWidth = 0.05f;
            }

            cracks = new LineRenderer[5];
            for (int i = 0; i < cracks.Length; i++)
            {
                cracks[i] = CreateLine("GroundCrack_" + i, material, new Color(0.12f, 0.08f, 0.07f, 0.82f), true);
                cracks[i].startWidth = 0.07f;
                cracks[i].endWidth = 0.015f;
            }
        }

        private void UpdateGroundMarks(float progress)
        {
            Vector3 bossFoot = boss.position + Vector3.down * 1.35f;
            for (int i = 0; i < bossTracks.Length; i++)
            {
                float x = i == 0 ? -0.55f : 0.55f;
                bossTracks[i].SetPosition(0, bossFoot + new Vector3(x, 0.03f, 0f));
                bossTracks[i].SetPosition(1, bossFoot + new Vector3(x, 0.03f, 1.2f + progress * 2f));
            }
            for (int i = 0; i < cracks.Length; i++)
            {
                float angle = (-70f + i * 35f) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                cracks[i].SetPosition(0, bossFoot + Vector3.up * 0.035f);
                cracks[i].SetPosition(1, bossFoot + direction * (0.7f + i * 0.16f) + Vector3.up * 0.035f);
            }
        }

        private LineRenderer CreateLine(string name, Material material, Color color, bool worldSpace)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(transform, false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = worldSpace;
            line.positionCount = 2;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            line.numCapVertices = 0;
            return line;
        }

        private Material CreateOwnedMaterial(string materialName)
        {
            Material material = new Material(Shader.Find("Sprites/Default")) { name = materialName };
            ownedMaterials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < ownedMaterials.Count; i++)
            {
                Material material = ownedMaterials[i];
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            ownedMaterials.Clear();
        }
    }
}
