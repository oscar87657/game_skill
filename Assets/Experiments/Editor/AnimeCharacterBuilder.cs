// GOLDEN STANDARD
// 목적: CC0 애니풍 모델을 기존 JumpLab의 Humanoid 애니메이션에 연결한다.
// 책임: 명시적 본 매핑·기본 URP 재질·시각 프리팹만 생성하며 이동과 충돌은 변경하지 않는다.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameSkill.Experiments.Editor
{
    public static class AnimeCharacterBuilder
    {
        private const string Folder = "Assets/Art/ThirdParty/VRoid/SendagayaShino";
        private const string ModelPath = Folder + "/SendagayaShino.fbx";
        public const string PrefabPath = "Assets/Experiments/JumpLab/AnimeCharacter.prefab";

        [Serializable] private sealed class BoneMap { public string human; public string bone; }
        [Serializable] private sealed class MaterialMap
        {
            public string name;
            public string texture;
            public float[] color;
            public bool alpha;
        }
        [Serializable] private sealed class ImportMap { public BoneMap[] bones; public MaterialMap[] materials; }

        [MenuItem("Game Skill/Experiments/Apply Anime Character")]
        public static void Apply()
        {
            // Play 상태를 에셋에 저장하지 않도록 편집 중에만 실행한다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play 모드를 종료한 뒤 모델을 적용하세요.");
            AssetDatabase.Refresh();
            ImportMap map = JsonUtility.FromJson<ImportMap>(File.ReadAllText(Folder + "/ImportMap.json"));
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new InvalidOperationException("변환된 FBX가 없습니다.");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            var description = importer.humanDescription;
            description.human = map.bones.Select(item => new HumanBone
            {
                humanName = item.human,
                boneName = item.bone,
                limit = new HumanLimit { useDefaultValues = true }
            }).ToArray();
            importer.humanDescription = description;
            importer.SaveAndReimport();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Humanoid 본 매핑이 유효하지 않습니다.");

            // 원본 MToon 대신 기본 URP Unlit과 알파 컷아웃으로 색과 윤곽을 유지한다.
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            var materials = map.materials.ToDictionary(item => item.name, item => CreateMaterial(item));
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                visual.name = "Sendagaya Shino";
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    // FBX의 슬롯 순서를 유지해야 얼굴·눈·머리 텍스처가 다른 면에 붙지 않는다.
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => materials[material.name]).ToArray();
                }
                Animator animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Animations/HumanoidPlayer.controller");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // 실험의 충돌 크기는 고정하고 시각물의 머리~발 범위만 1.7m로 맞춘다.
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
                Bounds bounds = BoundsOf(renderers);
                if (bounds.size.y <= 0f) throw new InvalidOperationException("모델 크기가 유효하지 않습니다.");
                visual.transform.localScale *= 1.7f / bounds.size.y;
                bounds = BoundsOf(renderers);
                visual.transform.position = Vector3.up * -bounds.min.y;
                // 첫 화면에서는 얼굴을 보이고, 이동을 시작하면 실험 Controller가 좌우 방향을 정한다.
                visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                PrefabUtility.SaveAsPrefabAsset(visual, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(visual);
            }

            JumpLabBuilder.Create();
            var lab = UnityEngine.Object.FindAnyObjectByType<JumpExperiment>();
            if (lab == null) throw new InvalidOperationException("JumpLab Player가 없습니다.");
            Animator previous = lab.GetComponentInChildren<Animator>();
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), lab.transform);
            lab.ConfigureVisual(instance.GetComponent<Animator>());
            EditorUtility.SetDirty(lab);
            EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
            if (!EditorSceneManager.SaveScene(lab.gameObject.scene))
                throw new InvalidOperationException("교체한 씬을 저장하지 못했습니다.");
            AssetDatabase.SaveAssets();
            Debug.Log("JumpLab: Sendagaya Shino Humanoid 적용 완료");
        }

        private static Material CreateMaterial(MaterialMap item)
        {
            // 같은 경로를 재사용해 재적용할 때 프리팹의 재질 GUID가 바뀌지 않게 한다.
            string path = Folder + "/Materials/" + item.name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + item.texture));
            material.SetColor("_BaseColor", new Color(item.color[0], item.color[1], item.color[2], item.color[3]).gamma);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_AlphaClip", item.alpha ? 1f : 0f);
            material.SetFloat("_Cutoff", 0.1f);
            if (item.alpha)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = 2450;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            // 얼굴·몸·머리를 합친 경계로 캐릭터의 크기와 발 위치를 판단한다.
            if (renderers.Length == 0) throw new InvalidOperationException("모델 Renderer가 없습니다.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
