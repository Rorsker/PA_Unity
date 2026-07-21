using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlayableAd.EditorTools
{
    public static class PlayerFourStateSetup
    {
        private const string Folder = "Assets/PlayableAd/Visuals/PlayerSprite";
        private const string SheetPath = Folder + "/20260720113048_72277c48.png";
        private const string ControllerPath = Folder + "/player.controller";
        private const string LeftClipPath = Folder + "/222_Left.anim";
        private const string RightClipPath = Folder + "/222_Right.anim";
        private const string SourcePrefabPath = Folder + "/ShieldKnightSpriteVisual.prefab";
        private const string PrefabPath = Folder + "/PlayerFourStateVisual.prefab";

        [MenuItem("Playable Ad/Player/Build Four-State Visual")]
        public static void Build()
        {
            Dictionary<string, Sprite> sprites = LoadSprites();
            AnimationClip runClip = RequireAsset<AnimationClip>(Folder + "/111.anim");
            AnimationClip shieldClip = RequireAsset<AnimationClip>(Folder + "/333.anim");
            AnimationClip fallClip = RequireAsset<AnimationClip>(Folder + "/444.anim");
            AnimationClip leftClip = BuildLateralClip(LeftClipPath, sprites, 8);
            AnimationClip rightClip = BuildLateralClip(RightClipPath, sprites, 16);

            SetLoop(runClip, true);
            SetLoop(shieldClip, false);
            SetLoop(fallClip, false);

            AnimatorController controller = ConfigureController(runClip, leftClip, rightClip, shieldClip, fallClip);
            GameObject prefab = CreateVisualPrefab(controller, sprites[SpriteName(0)]);
            BindScene(prefab, controller);

            AssetDatabase.SaveAssets();
            Debug.Log("Four-state player visual built: 111=run, 222=lateral, 333=shield, 444=fallen.");
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var sprites = new Dictionary<string, Sprite>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null) sprites[sprite.name] = sprite;
            }

            if (sprites.Count < 40)
                throw new InvalidOperationException("Expected at least 40 sprites in " + SheetPath + ", found " + sprites.Count);
            return sprites;
        }

        private static AnimationClip BuildLateralClip(string path, Dictionary<string, Sprite> sprites, int firstIndex)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.name = Path.GetFileNameWithoutExtension(path);
            clip.frameRate = 10f;
            var keys = new ObjectReferenceKeyframe[9];
            for (int i = 0; i < 8; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / 10f,
                    value = sprites[SpriteName(firstIndex + i)]
                };
            }
            keys[8] = new ObjectReferenceKeyframe { time = 0.8f, value = keys[0].value };

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            SetLoop(clip, true);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController ConfigureController(AnimationClip runClip, AnimationClip leftClip,
            AnimationClip rightClip, AnimationClip shieldClip, AnimationClip fallClip)
        {
            AnimatorController controller = RequireAsset<AnimatorController>(ControllerPath);
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
                controller.RemoveParameter(i);
            controller.AddParameter("HorizontalInput", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsShieldCharging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFallen", AnimatorControllerParameterType.Bool);

            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].defaultWeight = 1f;
            controller.layers = layers;
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);
            foreach (ChildAnimatorState child in machine.states)
                machine.RemoveState(child.state);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
                machine.RemoveStateMachine(child.stateMachine);

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            {
                BlendTree oldTree = asset as BlendTree;
                if (oldTree != null) UnityEngine.Object.DestroyImmediate(oldTree, true);
            }

            var lateralTree = new BlendTree
            {
                name = "222_LeftRight",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "HorizontalInput",
                useAutomaticThresholds = false
            };
            lateralTree.AddChild(leftClip, -1f);
            lateralTree.AddChild(rightClip, 1f);
            AssetDatabase.AddObjectToAsset(lateralTree, controller);

            AnimatorState run = AddState(machine, "111", runClip, new Vector3(250f, 40f));
            AnimatorState lateral = AddState(machine, "222", lateralTree, new Vector3(250f, 150f));
            AnimatorState shield = AddState(machine, "333", shieldClip, new Vector3(80f, 270f));
            AnimatorState fallen = AddState(machine, "444", fallClip, new Vector3(420f, 270f));
            machine.defaultState = run;

            AddTransition(machine.AddAnyStateTransition(fallen),
                Condition(AnimatorConditionMode.If, 0f, "IsFallen"));
            AddTransition(machine.AddAnyStateTransition(shield),
                Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                Condition(AnimatorConditionMode.If, 0f, "IsShieldCharging"));

            AddTransition(run.AddTransition(lateral), LocomotionConditions(
                Condition(AnimatorConditionMode.Less, -0.12f, "HorizontalInput")));
            AddTransition(run.AddTransition(lateral), LocomotionConditions(
                Condition(AnimatorConditionMode.Greater, 0.12f, "HorizontalInput")));
            AddTransition(lateral.AddTransition(run), LocomotionConditions(
                Condition(AnimatorConditionMode.Greater, -0.12f, "HorizontalInput"),
                Condition(AnimatorConditionMode.Less, 0.12f, "HorizontalInput")));

            AddTransition(shield.AddTransition(run), LocomotionConditions(
                Condition(AnimatorConditionMode.Greater, -0.12f, "HorizontalInput"),
                Condition(AnimatorConditionMode.Less, 0.12f, "HorizontalInput")));
            AddTransition(shield.AddTransition(lateral), LocomotionConditions(
                Condition(AnimatorConditionMode.Less, -0.12f, "HorizontalInput")));
            AddTransition(shield.AddTransition(lateral), LocomotionConditions(
                Condition(AnimatorConditionMode.Greater, 0.12f, "HorizontalInput")));

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorCondition[] LocomotionConditions(params AnimatorCondition[] directional)
        {
            var result = new List<AnimatorCondition>
            {
                Condition(AnimatorConditionMode.IfNot, 0f, "IsFallen"),
                Condition(AnimatorConditionMode.IfNot, 0f, "IsShieldCharging")
            };
            result.AddRange(directional);
            return result.ToArray();
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

        private static void AddTransition(AnimatorStateTransition transition, params AnimatorCondition[] conditions)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.04f;
            transition.canTransitionToSelf = false;
            foreach (AnimatorCondition condition in conditions)
                transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static GameObject CreateVisualPrefab(AnimatorController controller, Sprite defaultSprite)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
            try
            {
                root.name = "PlayerFourStateVisual";
                Transform character = root.transform.Find("CharacterSprite");
                if (character == null) throw new InvalidOperationException("CharacterSprite missing in source prefab");

                character.localPosition = new Vector3(0f, -0.62f, 0f);
                character.localScale = Vector3.one * 0.56f;
                SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
                Animator animator = character.GetComponent<Animator>();
                renderer.sprite = defaultSprite;
                renderer.sortingOrder = 10;
                animator.runtimeAnimatorController = controller;
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BindScene(GameObject prefab, AnimatorController controller)
        {
            PlayableAdGame game = UnityEngine.Object.FindObjectOfType<PlayableAdGame>();
            if (game == null) throw new InvalidOperationException("PlayableAdGame missing in active scene");

            var serializedGame = new SerializedObject(game);
            SerializedProperty visualProperty = serializedGame.FindProperty("playerVisualPrefab");
            visualProperty.objectReferenceValue = prefab;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);

            foreach (GameObject candidate in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
            {
                if (candidate.name != "player" || candidate.scene != game.gameObject.scene) continue;
                Animator animator = candidate.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.enabled = true;
                }
                candidate.SetActive(false);
                EditorUtility.SetDirty(candidate);
                break;
            }

            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }

        private static void SetLoop(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            RemoveEmptyAnimationEvents(clip);
            EditorUtility.SetDirty(clip);
        }

        private static void RemoveEmptyAnimationEvents(AnimationClip clip)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            var validEvents = new List<AnimationEvent>();
            foreach (AnimationEvent animationEvent in events)
            {
                if (!string.IsNullOrEmpty(animationEvent.functionName))
                    validEvents.Add(animationEvent);
            }
            if (validEvents.Count != events.Length)
                AnimationUtility.SetAnimationEvents(clip, validEvents.ToArray());
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException(typeof(T).Name + " missing at " + path);
            return asset;
        }

        private static string SpriteName(int index)
        {
            return "20260720113048_72277c48_" + index;
        }
    }
}
