using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayableAd.Editor
{
    [InitializeOnLoad]
    public static class ShieldKnightSpriteSetup
    {
        private const string RawPath = "Assets/PlayableAd/Visuals/PlayerSprite/Source/ShieldKnight_Raw.png";
        private const string CleanPath = "Assets/PlayableAd/Visuals/PlayerSprite/Processed/ShieldKnight_Clean.png";
        private const string MaskPath = "Assets/PlayableAd/Visuals/PlayerSprite/Processed/ShieldKnight_MaskPreview.png";
        private const string ShadowPath = "Assets/PlayableAd/Visuals/PlayerSprite/Processed/ShieldKnight_GroundShadow.png";
        private const string AnimationFolder = "Assets/PlayableAd/Visuals/PlayerSprite/Animations";
        private const string ControllerPath = AnimationFolder + "/ShieldKnightSprite.controller";
        private const string PrefabPath = "Assets/PlayableAd/Visuals/PlayerSprite/ShieldKnightSpriteVisual.prefab";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SessionKey = "PlayableAd.ShieldKnightSpriteSetup.20260720.v2";
        private const int GridSize = 8;
        private const int ActiveRows = 5;
        private const float FramesPerSecond = 12f;

        private static readonly string[] RowNames =
        {
            "Run", "MoveLeft", "MoveRight", "ShieldCharge", "FallGray"
        };

        static ShieldKnightSpriteSetup()
        {
            if (!SessionState.GetBool(SessionKey, false) && File.Exists(RawPath))
                EditorApplication.delayCall += AutoBuild;
        }

        [MenuItem("Playable Ad/Build Shield Knight Sprite")]
        public static void Build()
        {
            EnsureFolder("Assets/PlayableAd/Visuals/PlayerSprite");
            EnsureFolder("Assets/PlayableAd/Visuals/PlayerSprite/Source");
            EnsureFolder("Assets/PlayableAd/Visuals/PlayerSprite/Processed");
            EnsureFolder(AnimationFolder);

            string audit;
            if (!File.Exists(CleanPath))
            {
                SpriteSheetAudit processed = ProcessSpriteSheet();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureTextureImporter();
                audit = processed.ToString();
            }
            else
            {
                audit = "Used existing processed sheet and existing 40 Sprite rects; no recut or background processing.";
            }
            CreateGroundShadowTexture();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureGroundShadowImporter();
            Dictionary<string, Sprite> sprites = LoadSprites();
            WriteFrameOrderReport(sprites);
            Dictionary<string, AnimationClip> clips = CreateAnimationClips(sprites);
            AnimatorController controller = CreateAnimatorController(clips);
            Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath);
            if (shadow == null) throw new InvalidOperationException("Ground shadow sprite failed to import.");
            GameObject prefab = CreateVisualPrefab(controller, sprites["ShieldKnight_Run_00"], shadow);
            UpdateMainScene(prefab);

            SessionState.SetBool(SessionKey, true);
            Debug.Log("Shield Knight sprite setup completed. " + audit);
            Selection.activeObject = prefab;
        }

        private static void AutoBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuild;
                return;
            }

            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static SpriteSheetAudit ProcessSpriteSheet()
        {
            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!sourceTexture.LoadImage(File.ReadAllBytes(RawPath), false))
                throw new InvalidOperationException("Unable to decode " + RawPath);

            int width = sourceTexture.width;
            int height = sourceTexture.height;
            if (width != height || width % GridSize != 0)
                throw new InvalidOperationException("Sprite sheet must be square and divisible by 8: " + width + "x" + height);

            int cell = width / GridSize;
            Color32[] source = sourceTexture.GetPixels32();
            int originalTransparentPixels = source.Count(pixel => pixel.a < 255);
            List<RowBand> bands = DetectContentBands(source, width, height);
            if (bands.Count != ActiveRows)
                throw new InvalidOperationException("Expected 5 content rows, detected " + bands.Count + ": " + string.Join(", ", bands));

            var output = new Color32[width * height];
            var mask = Enumerable.Repeat(new Color32(0, 0, 0, 255), width * height).ToArray();
            int foregroundPixels = 0;
            int softenedEdgePixels = 0;

            for (int row = 0; row < ActiveRows; row++)
            {
                RowBand band = bands[row].ExpandedToFit(3, cell, height);
                if (band.Height > cell)
                    throw new InvalidOperationException("Content row " + (row + 1) + " cannot fit a " + cell + "px cell: " + band);

                int targetTop = row * cell + cell - band.Height;
                for (int column = 0; column < GridSize; column++)
                {
                    int sourceLeft = column * cell;
                    bool[] background = FloodBackground(source, width, height, sourceLeft, band.Top, cell, band.Height);
                    CopyProcessedCell(source, output, mask, background, width, height,
                        sourceLeft, band.Top, column * cell, targetTop, cell, band.Height,
                        ref foregroundPixels, ref softenedEdgePixels);
                }
            }

            WriteTexture(CleanPath, output, width, height);
            WriteTexture(MaskPath, mask, width, height);
            UnityEngine.Object.DestroyImmediate(sourceTexture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return new SpriteSheetAudit(width, height, cell, originalTransparentPixels, bands,
                foregroundPixels, softenedEdgePixels);
        }

        private static List<RowBand> DetectContentBands(Color32[] pixels, int width, int height)
        {
            var active = new bool[height];
            for (int y = 0; y < height; y++)
            {
                int count = 0;
                for (int x = 0; x < width; x++)
                    if (IsForegroundCandidate(GetTopPixel(pixels, width, height, x, y))) count++;
                active[y] = count >= 12;
            }

            var rawBands = new List<RowBand>();
            int start = -1;
            for (int y = 0; y <= height; y++)
            {
                bool populated = y < height && active[y];
                if (populated && start < 0) start = y;
                if (!populated && start >= 0)
                {
                    rawBands.Add(new RowBand(start, y - 1));
                    start = -1;
                }
            }

            var merged = new List<RowBand>();
            foreach (RowBand band in rawBands)
            {
                if (merged.Count > 0 && band.Top - merged[merged.Count - 1].Bottom <= 8)
                {
                    RowBand previous = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new RowBand(previous.Top, band.Bottom);
                }
                else
                {
                    merged.Add(band);
                }
            }
            return merged;
        }

        private static bool[] FloodBackground(Color32[] pixels, int width, int height,
            int left, int top, int cellWidth, int cellHeight)
        {
            var background = new bool[cellWidth * cellHeight];
            var queue = new Queue<int>();

            Action<int, int> enqueue = (x, y) =>
            {
                int index = y * cellWidth + x;
                if (background[index]) return;
                if (!IsBackgroundLike(GetTopPixel(pixels, width, height, left + x, top + y))) return;
                background[index] = true;
                queue.Enqueue(index);
            };

            for (int x = 0; x < cellWidth; x++)
            {
                enqueue(x, 0);
                enqueue(x, cellHeight - 1);
            }
            for (int y = 1; y < cellHeight - 1; y++)
            {
                enqueue(0, y);
                enqueue(cellWidth - 1, y);
            }

            int[] offsets = { -1, 1, -cellWidth, cellWidth };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int x = current % cellWidth;
                int y = current / cellWidth;
                for (int i = 0; i < offsets.Length; i++)
                {
                    if ((i == 0 && x == 0) || (i == 1 && x == cellWidth - 1)
                        || (i == 2 && y == 0) || (i == 3 && y == cellHeight - 1)) continue;
                    int next = current + offsets[i];
                    if (background[next]) continue;
                    int nextX = next % cellWidth;
                    int nextY = next / cellWidth;
                    if (!IsBackgroundLike(GetTopPixel(pixels, width, height, left + nextX, top + nextY))) continue;
                    background[next] = true;
                    queue.Enqueue(next);
                }
            }
            return background;
        }

        private static void CopyProcessedCell(Color32[] source, Color32[] output, Color32[] mask,
            bool[] background, int width, int height, int sourceLeft, int sourceTop,
            int targetLeft, int targetTop, int cellWidth, int cellHeight,
            ref int foregroundPixels, ref int softenedEdgePixels)
        {
            for (int y = 0; y < cellHeight; y++)
            for (int x = 0; x < cellWidth; x++)
            {
                int local = y * cellWidth + x;
                int targetX = targetLeft + x;
                int targetY = targetTop + y;
                if (background[local]) continue;

                Color32 color = GetTopPixel(source, width, height, sourceLeft + x, sourceTop + y);
                byte alpha = 255;
                if (IsNearBackground(background, x, y, cellWidth, cellHeight, 3))
                {
                    int difference = 255 - (color.r + color.g + color.b) / 3;
                    alpha = (byte)Mathf.Clamp(Mathf.RoundToInt((difference - 1f) / 64f * 255f), 0, 255);
                    softenedEdgePixels++;
                }
                if (alpha == 0) continue;

                Color32 clean = alpha < 255 ? RemoveWhiteMatte(color, alpha) : color;
                clean.a = alpha;
                SetTopPixel(output, width, height, targetX, targetY, clean);
                SetTopPixel(mask, width, height, targetX, targetY, new Color32(alpha, alpha, alpha, 255));
                foregroundPixels++;
            }
        }

        private static bool IsNearBackground(bool[] background, int x, int y, int width, int height, int radius)
        {
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                int neighbourX = x + offsetX;
                int neighbourY = y + offsetY;
                if (neighbourX < 0 || neighbourX >= width || neighbourY < 0 || neighbourY >= height)
                    return true;
                if (background[neighbourY * width + neighbourX]) return true;
            }
            return false;
        }

        private static Color32 RemoveWhiteMatte(Color32 color, byte alphaByte)
        {
            float alpha = alphaByte / 255f;
            if (alpha <= 0.001f) return new Color32(0, 0, 0, 0);
            int red = Mathf.Clamp(Mathf.RoundToInt((color.r - 255f * (1f - alpha)) / alpha), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt((color.g - 255f * (1f - alpha)) / alpha), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt((color.b - 255f * (1f - alpha)) / alpha), 0, 255);
            return new Color32((byte)red, (byte)green, (byte)blue, alphaByte);
        }

        private static bool IsForegroundCandidate(Color32 color)
        {
            int min = Math.Min(color.r, Math.Min(color.g, color.b));
            int max = Math.Max(color.r, Math.Max(color.g, color.b));
            return min < 245 || max - min > 8;
        }

        private static bool IsBackgroundLike(Color32 color)
        {
            int min = Math.Min(color.r, Math.Min(color.g, color.b));
            int max = Math.Max(color.r, Math.Max(color.g, color.b));
            return min >= 245 && max - min <= 10;
        }

        private static Color32 GetTopPixel(Color32[] pixels, int width, int height, int x, int topY)
        {
            return pixels[(height - 1 - topY) * width + x];
        }

        private static void SetTopPixel(Color32[] pixels, int width, int height, int x, int topY, Color32 color)
        {
            pixels[(height - 1 - topY) * width + x] = color;
        }

        private static void WriteTexture(string path, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void CreateGroundShadowTexture()
        {
            const int width = 256;
            const int height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x + 0.5f) / width * 2f - 1f;
                float normalizedY = (y + 0.5f) / height * 2f - 1f;
                float distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                float alpha = 1f - Mathf.SmoothStep(0.18f, 1f, distance);
                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 1.35f);
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(ShadowPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ConfigureGroundShadowImporter()
        {
            var importer = AssetImporter.GetAtPath(ShadowPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter missing for " + ShadowPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static void ConfigureTextureImporter()
        {
            var importer = AssetImporter.GetAtPath(CleanPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter missing for " + CleanPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 90;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            int width;
            int height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            int cellWidth = width / GridSize;
            int cellHeight = height / GridSize;
            var metadata = new List<SpriteMetaData>(ActiveRows * GridSize);
            for (int row = 0; row < ActiveRows; row++)
            for (int column = 0; column < GridSize; column++)
            {
                metadata.Add(new SpriteMetaData
                {
                    name = "ShieldKnight_" + RowNames[row] + "_" + column.ToString("00"),
                    rect = new Rect(column * cellWidth, height - (row + 1) * cellHeight, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    border = Vector4.zero
                });
            }
            importer.spritesheet = metadata.ToArray();
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(CleanPath)
                .OfType<Sprite>().ToDictionary(sprite => sprite.name, sprite => sprite);
            if (sprites.Count != ActiveRows * GridSize)
                throw new InvalidOperationException("Expected 40 sprites, imported " + sprites.Count);
            return sprites;
        }

        private static Dictionary<string, AnimationClip> CreateAnimationClips(Dictionary<string, Sprite> sprites)
        {
            var clips = new Dictionary<string, AnimationClip>();
            for (int row = 0; row < ActiveRows; row++)
            {
                string clipName = "Player_" + RowNames[row];
                string path = AnimationFolder + "/" + clipName + ".anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);

                var clip = new AnimationClip { name = clipName, frameRate = FramesPerSecond };
                var frames = new List<ObjectReferenceKeyframe>();
                for (int frame = 0; frame < GridSize; frame++)
                {
                    frames.Add(new ObjectReferenceKeyframe
                    {
                        time = frame / FramesPerSecond,
                        value = sprites["ShieldKnight_" + RowNames[row] + "_" + frame.ToString("00")]
                    });
                }
                bool loop = row < 3;
                frames.Add(new ObjectReferenceKeyframe
                {
                    time = GridSize / FramesPerSecond,
                    value = loop
                        ? sprites["ShieldKnight_" + RowNames[row] + "_00"]
                        : sprites["ShieldKnight_" + RowNames[row] + "_07"]
                });
                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), frames.ToArray());
                AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
                {
                    loopTime = loop,
                    loopBlend = false
                });
                AssetDatabase.CreateAsset(clip, path);
                clips[clipName] = clip;
            }

            const string loopName = "Player_ShieldChargeLoop";
            string loopPath = AnimationFolder + "/" + loopName + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(loopPath) != null) AssetDatabase.DeleteAsset(loopPath);
            var shieldLoop = new AnimationClip { name = loopName, frameRate = FramesPerSecond };
            Sprite heldShield = sprites["ShieldKnight_ShieldCharge_07"];
            AnimationUtility.SetObjectReferenceCurve(shieldLoop,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = heldShield },
                    new ObjectReferenceKeyframe { time = 1f / FramesPerSecond, value = heldShield }
                });
            AnimationUtility.SetAnimationClipSettings(shieldLoop, new AnimationClipSettings { loopTime = true });
            AssetDatabase.CreateAsset(shieldLoop, loopPath);
            clips[loopName] = shieldLoop;
            AssetDatabase.SaveAssets();
            return clips;
        }

        private static void WriteFrameOrderReport(Dictionary<string, Sprite> sprites)
        {
            string reportPath = AnimationFolder + "/ShieldKnight_FrameOrderReport.txt";
            using (var writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("Shield Knight frame order report");
                writer.WriteLine("Basis: fixed 8x8 grid; visual rows top-to-bottom; frames left-to-right; natural numeric indices.");
                writer.WriteLine("Ambiguity: none after visual verification; left and right rows are distinct.");
                writer.WriteLine();
                for (int row = 0; row < ActiveRows; row++)
                {
                    writer.WriteLine(RowNames[row] + ":");
                    for (int frame = 0; frame < GridSize; frame++)
                    {
                        string name = "ShieldKnight_" + RowNames[row] + "_" + frame.ToString("00");
                        Rect rect = sprites[name].rect;
                        writer.WriteLine(frame + " -> " + name + " / rect=("
                            + rect.x + ", " + rect.y + ", " + rect.width + ", " + rect.height + ")");
                    }
                    writer.WriteLine();
                }
                writer.WriteLine("ShieldChargeLoop: ShieldKnight_ShieldCharge_07 held as a stable loop frame.");
            }
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static AnimatorController CreateAnimatorController(Dictionary<string, AnimationClip> clips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("HorizontalInput", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsShieldCharging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFallen", AnimatorControllerParameterType.Bool);
            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].defaultWeight = 1f;
            controller.layers = layers;

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState run = AddState(machine, "Player_Run", clips["Player_Run"], new Vector3(250f, 40f));
            AnimatorState left = AddState(machine, "Player_MoveLeft", clips["Player_MoveLeft"], new Vector3(60f, 160f));
            AnimatorState right = AddState(machine, "Player_MoveRight", clips["Player_MoveRight"], new Vector3(440f, 160f));
            AnimatorState shieldEnter = AddState(machine, "Player_ShieldChargeEnter", clips["Player_ShieldCharge"], new Vector3(210f, 260f));
            AnimatorState shieldLoop = AddState(machine, "Player_ShieldChargeLoop", clips["Player_ShieldChargeLoop"], new Vector3(410f, 260f));
            AnimatorState fall = AddState(machine, "Player_FallGray", clips["Player_FallGray"], new Vector3(250f, 380f));
            machine.defaultState = run;

            AddAnyTransition(machine, fall, Condition(AnimatorConditionMode.If, 0f, "IsFallen"));
            AddAnyTransition(machine, left,
                Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                Condition(AnimatorConditionMode.IfNot, 0f, "IsShieldCharging"),
                Condition(AnimatorConditionMode.Less, -0.1f, "HorizontalInput"));
            AddAnyTransition(machine, right,
                Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                Condition(AnimatorConditionMode.IfNot, 0f, "IsShieldCharging"),
                Condition(AnimatorConditionMode.Greater, 0.1f, "HorizontalInput"));
            AddAnyTransition(machine, run,
                Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                Condition(AnimatorConditionMode.IfNot, 0f, "IsShieldCharging"),
                Condition(AnimatorConditionMode.Greater, -0.1f, "HorizontalInput"),
                Condition(AnimatorConditionMode.Less, 0.1f, "HorizontalInput"));

            AddShieldEntryTransition(run, shieldEnter);
            AddShieldEntryTransition(left, shieldEnter);
            AddShieldEntryTransition(right, shieldEnter);
            AnimatorStateTransition enterLoop = shieldEnter.AddTransition(shieldLoop);
            enterLoop.hasExitTime = true;
            enterLoop.exitTime = 0.95f;
            enterLoop.hasFixedDuration = true;
            enterLoop.duration = 0.02f;
            enterLoop.canTransitionToSelf = false;
            enterLoop.AddCondition(AnimatorConditionMode.If, 0f, "IsShieldCharging");

            AddReturnTransition(left, run, Condition(AnimatorConditionMode.Greater, -0.1f, "HorizontalInput"));
            AddReturnTransition(right, run, Condition(AnimatorConditionMode.Less, 0.1f, "HorizontalInput"));
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorCondition Condition(AnimatorConditionMode mode, float threshold, string parameter)
        {
            return new AnimatorCondition { mode = mode, threshold = threshold, parameter = parameter };
        }

        private static void AddAnyTransition(AnimatorStateMachine machine, AnimatorState target,
            params AnimatorCondition[] conditions)
        {
            ConfigureTransition(machine.AddAnyStateTransition(target), conditions);
        }

        private static void AddReturnTransition(AnimatorState source, AnimatorState target,
            params AnimatorCondition[] conditions)
        {
            ConfigureTransition(source.AddTransition(target), conditions);
        }

        private static void AddShieldEntryTransition(AnimatorState source, AnimatorState target)
        {
            AnimatorStateTransition transition = source.AddTransition(target);
            ConfigureTransition(transition,
                new[]
                {
                    Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                    Condition(AnimatorConditionMode.If, 0f, "IsShieldCharging")
                });
        }

        private static void ConfigureTransition(AnimatorStateTransition transition, AnimatorCondition[] conditions)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            foreach (AnimatorCondition condition in conditions)
                transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static GameObject CreateVisualPrefab(AnimatorController controller, Sprite defaultSprite, Sprite shadowSprite)
        {
            GameObject root = new GameObject("ShieldKnightSpriteVisual");

            GameObject shadowObject = new GameObject("GroundShadow");
            shadowObject.transform.SetParent(root.transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, -1.02f, 0.02f);
            shadowObject.transform.localScale = new Vector3(0.62f, 0.34f, 1f);
            SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sortingOrder = 5;
            shadowRenderer.color = new Color(0.07f, 0.1f, 0.15f, 0.28f);
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;

            GameObject spriteObject = new GameObject("CharacterSprite");
            spriteObject.transform.SetParent(root.transform, false);
            spriteObject.transform.localPosition = new Vector3(0f, -1f, 0f);

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = defaultSprite;
            renderer.sortingOrder = 10;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Animator animator = spriteObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PlayerSpriteVisualController visualController = root.AddComponent<PlayerSpriteVisualController>();
            visualController.Configure(animator, renderer, shadowObject.transform, shadowRenderer);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void UpdateMainScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayableAdGame game = UnityEngine.Object.FindObjectOfType<PlayableAdGame>();
            if (game == null) throw new InvalidOperationException("PlayableAdGame not found in " + ScenePath);
            var serializedGame = new SerializedObject(game);
            SerializedProperty property = serializedGame.FindProperty("playerVisualPrefab");
            if (property == null) throw new InvalidOperationException("playerVisualPrefab field not found");
            property.objectReferenceValue = prefab;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private struct RowBand
        {
            public readonly int Top;
            public readonly int Bottom;
            public int Height { get { return Bottom - Top + 1; } }

            public RowBand(int top, int bottom)
            {
                Top = top;
                Bottom = bottom;
            }

            public RowBand ExpandedToFit(int desiredPadding, int maxHeight, int imageHeight)
            {
                int available = Math.Max(0, maxHeight - Height);
                int padding = Math.Min(desiredPadding, available);
                int topPadding = padding / 2;
                int bottomPadding = padding - topPadding;
                return new RowBand(Math.Max(0, Top - topPadding), Math.Min(imageHeight - 1, Bottom + bottomPadding));
            }

            public override string ToString()
            {
                return Top + "-" + Bottom + " (" + Height + "px)";
            }
        }

        private struct SpriteSheetAudit
        {
            private readonly int width;
            private readonly int height;
            private readonly int cell;
            private readonly int originalTransparentPixels;
            private readonly IList<RowBand> bands;
            private readonly int foregroundPixels;
            private readonly int softenedEdgePixels;

            public SpriteSheetAudit(int width, int height, int cell, int originalTransparentPixels,
                IList<RowBand> bands, int foregroundPixels, int softenedEdgePixels)
            {
                this.width = width;
                this.height = height;
                this.cell = cell;
                this.originalTransparentPixels = originalTransparentPixels;
                this.bands = bands;
                this.foregroundPixels = foregroundPixels;
                this.softenedEdgePixels = softenedEdgePixels;
            }

            public override string ToString()
            {
                return "Source=" + width + "x" + height
                    + ", originalAlphaPixels=" + originalTransparentPixels
                    + ", cell=" + cell + "x" + cell
                    + ", detectedRows=[" + string.Join(", ", bands) + "]"
                    + ", foregroundPixels=" + foregroundPixels
                    + ", softenedEdgePixels=" + softenedEdgePixels;
            }
        }
    }
}
