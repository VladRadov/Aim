using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aim.Views;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Aim.Editor
{
    public static class CharacterAnimationSetup
    {
        const string AnimationsFolder = "Assets/Aim/Animations";
        const string ControllerPath = AnimationsFolder + "/CharacterTarget.controller";
        const string CharacterModelPath = "Assets/Floreswa/Models/char01.fbx";
        const string CharacterPrefabPath = "Assets/Aim/Prefabs/char01_1.prefab";
        const string DefeatedState = "Defeated";
        const string IdleState = "Idle";
        const string RunClipFileName = "Drunk Run Forward";
        const string RunStateName = "Drunk_Run_Forward";

        sealed class ActionClipEntry
        {
            public string AssetPath;
            public string StateName;
            public AnimationClip Clip;
            public bool IsRun;
        }

        [MenuItem("Aim/Setup Character Animations")]
        public static void Setup()
        {
            RenameMixamoClipsByFileName();

            var actionClips = CollectActionClips();
            var defeated = CollectDefeatedClip();

            if (actionClips.Count == 0)
            {
                Debug.LogError("Aim: no action animation clips found in Assets/Aim/Animations.");
                return;
            }

            foreach (var entry in actionClips)
                EnsureLoopOnClipAsset(entry.AssetPath, true);

            if (defeated != null)
                EnsureLoopOnClipAsset(defeated.AssetPath, false);

            // Reload after possible reimports.
            actionClips = CollectActionClips();
            defeated = CollectDefeatedClip();

            var controller = CreateOrUpdateController(actionClips, defeated);
            var runState = actionClips.FirstOrDefault(entry => entry.IsRun)?.StateName ?? RunStateName;
            var idleStates = actionClips
                .Where(entry => !entry.IsRun)
                .Select(entry => entry.StateName)
                .ToArray();

            if (idleStates.Length == 0)
                Debug.LogWarning("Aim: no idle/in-place animations found. Stationary characters will use Idle.");

            if (!actionClips.Any(entry => entry.IsRun))
                Debug.LogWarning($"Aim: '{RunClipFileName}.fbx' not found. Moving characters need this run clip.");

            WireCharacterPrefabs(controller, runState, idleStates);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = controller;
            Debug.Log(
                $"Character animations wired. Run: {runState}. Idle pool ({idleStates.Length}): {string.Join(", ", idleStates)}");
        }

        static void RenameMixamoClipsByFileName()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { AnimationsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;

                if (clips == null || clips.Length == 0)
                    continue;

                var changed = false;
                for (var i = 0; i < clips.Length; i++)
                {
                    var desiredName = clips.Length == 1 ? fileName : $"{fileName}_{i}";
                    var shouldLoop = !string.Equals(fileName, "Defeated", StringComparison.OrdinalIgnoreCase);

                    if (clips[i].name != desiredName)
                    {
                        clips[i].name = desiredName;
                        changed = true;
                    }

                    if (clips[i].loopTime != shouldLoop || clips[i].loopPose != shouldLoop)
                    {
                        clips[i].loopTime = shouldLoop;
                        clips[i].loopPose = shouldLoop;
                        changed = true;
                    }
                }

                if (!changed)
                    continue;

                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        static List<ActionClipEntry> CollectActionClips()
        {
            var result = new List<ActionClipEntry>();
            var guids = AssetDatabase.FindAssets("t:Model", new[] { AnimationsFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(fileName, "Defeated", StringComparison.OrdinalIgnoreCase))
                    continue;

                var clip = LoadFirstClip(path);
                if (clip == null)
                    continue;

                result.Add(new ActionClipEntry
                {
                    AssetPath = path,
                    StateName = SanitizeStateName(fileName),
                    Clip = clip,
                    IsRun = string.Equals(fileName, RunClipFileName, StringComparison.OrdinalIgnoreCase)
                });
            }

            return result
                .OrderBy(entry => entry.StateName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static ActionClipEntry CollectDefeatedClip()
        {
            var path = AnimationsFolder + "/Defeated.fbx";
            var clip = LoadFirstClip(path);
            if (clip == null)
                return null;

            return new ActionClipEntry
            {
                AssetPath = path,
                StateName = DefeatedState,
                Clip = clip
            };
        }

        static AnimatorController CreateOrUpdateController(
            IReadOnlyList<ActionClipEntry> actionClips,
            ActionClipEntry defeated)
        {
            AnimatorController controller;
            if (File.Exists(Path.GetFullPath(ControllerPath)))
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                if (controller == null)
                    controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            else
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var root = controller.layers[0].stateMachine;
            ClearStates(root);

            var idle = root.AddState(IdleState);
            idle.motion = null;
            root.defaultState = idle;

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { IdleState, DefeatedState };

            foreach (var entry in actionClips)
            {
                var stateName = MakeUniqueName(entry.StateName, usedNames);
                entry.StateName = stateName;
                usedNames.Add(stateName);

                var state = root.AddState(stateName);
                state.motion = entry.Clip;
                state.speed = 1f;
            }

            if (defeated != null)
            {
                var defeatedState = root.AddState(DefeatedState);
                defeatedState.motion = defeated.Clip;
                defeatedState.speed = 1f;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static string MakeUniqueName(string desired, HashSet<string> used)
        {
            if (!used.Contains(desired))
                return desired;

            var index = 1;
            string candidate;
            do
            {
                candidate = $"{desired}_{index}";
                index++;
            } while (used.Contains(candidate));

            return candidate;
        }

        static void ClearStates(AnimatorStateMachine root)
        {
            foreach (var child in root.states.ToArray())
                root.RemoveState(child.state);

            root.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
            root.entryTransitions = Array.Empty<AnimatorTransition>();
        }

        static void WireCharacterPrefabs(
            RuntimeAnimatorController controller,
            string runStateName,
            string[] idleActionStateNames)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Aim/Prefabs" });
            var wired = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<CharacterTargetView>() == null)
                    continue;

                WireCharacterPrefab(path, controller, runStateName, idleActionStateNames);
                wired++;
            }

            if (wired == 0)
                WireCharacterPrefab(CharacterPrefabPath, controller, runStateName, idleActionStateNames);
        }

        static void WireCharacterPrefab(
            string prefabPath,
            RuntimeAnimatorController controller,
            string runStateName,
            string[] idleActionStateNames)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = prefabRoot.GetComponent<CharacterTargetView>();
                if (view == null)
                {
                    Debug.LogError($"Aim: {prefabPath} has no CharacterTargetView.");
                    return;
                }

                var animator = prefabRoot.GetComponent<Animator>();
                if (animator == null)
                    animator = prefabRoot.AddComponent<Animator>();

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;

                var avatar = LoadAvatar(CharacterModelPath);
                if (avatar != null)
                    animator.avatar = avatar;
                else
                    Debug.LogWarning("Aim: Avatar not found on char01.fbx. Humanoid retargeting may fail.");

                SetPrivateField(view, "animator", animator);
                SetStringField(view, "runStateName", runStateName);
                SetStringArrayField(view, "idleActionStateNames", idleActionStateNames);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log(
                    $"Aim: wired {prefabPath}. Run={runStateName}, idle pool={idleActionStateNames.Length}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        static void EnsureLoopOnClipAsset(string assetPath, bool loop)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            if (clips == null || clips.Length == 0)
                return;

            var changed = false;
            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime == loop && clips[i].loopPose == loop)
                    continue;

                clips[i].loopTime = loop;
                clips[i].loopPose = loop;
                changed = true;
            }

            if (!changed)
                return;

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static AnimationClip LoadFirstClip(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        static Avatar LoadAvatar(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        static string SanitizeStateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Action";

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
                else if (ch == ' ' || ch == '-' || ch == '_')
                    builder.Append('_');
            }

            var result = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "Action" : result;
        }

        static void SetPrivateField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                return;

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStringField(UnityEngine.Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                return;

            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStringArrayField(UnityEngine.Object target, string fieldName, string[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
                return;

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
