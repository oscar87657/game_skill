// GOLDEN STANDARD
// 목적: 저장된 실험 씬의 실제 CharacterController로 세 후보·천장·초기화 계약을 확인한다.
// 책임: 동일 입력을 주입해 실행 준비를 검증하며 손맛과 애니메이션 적합성은 주장하지 않는다.
#if UNITY_EDITOR
using System.Collections;
using GameSkill.Experiments;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameSkill.Tests
{
    public sealed class JumpLabPlayModeTests
    {
        [UnityTest]
        public IEnumerator SavedLab_ExecutesVariantsCeilingAndReset()
        {
            // 빌드 목록과 무관하게 디스크의 실험 씬을 실행해 생성 결과까지 검증한다.
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Experiments/JumpLab/JumpLab.unity", new LoadSceneParameters(LoadSceneMode.Single));
            JumpExperiment lab = Object.FindAnyObjectByType<JumpExperiment>();
            Assert.That(lab, Is.Not.Null);
            lab.enabled = false;
            var heights = new float[3];
            try
            {
                // 프레임마다 동일한 입력을 직접 전달해 사람의 입력 편차를 제외한다.
                for (int mode = 0; mode < 3; mode++)
                {
                    lab.SelectVariant((JumpVariant)mode);
                    for (int frame = 0; frame < 180 && !lab.HasResult; frame++)
                        lab.Simulate(0f, frame == 0, true, 1f / 120f);
                    Assert.That(lab.HasResult, Is.True, $"mode {mode} did not land");
                    heights[mode] = lab.LastHeight;
                    Assert.That(heights[mode], Is.EqualTo(3f).Within(0.12f));
                }
                lab.SelectVariant(JumpVariant.ReleaseCut);
                for (int frame = 0; frame < 180 && !lab.HasResult; frame++)
                    lab.Simulate(0f, frame == 0, frame < 6, 1f / 120f);
                Assert.That(lab.HasResult, Is.True);
                Assert.That(lab.LastHeight, Is.LessThan(heights[1] * 0.5f));

                // 모든 후보가 천장 접촉 시 상승을 취소하고 실제 바닥으로 돌아와야 한다.
                for (int mode = 0; mode < 3; mode++)
                {
                    lab.SelectVariant((JumpVariant)mode);
                    CharacterController body = lab.GetComponent<CharacterController>();
                    body.enabled = false;
                    lab.transform.position = new Vector3(-7f, 0.05f, 0f);
                    body.enabled = true;
                    Physics.SyncTransforms();
                    // 순간 이동 뒤에는 실제 접지가 확인된 후 점프 입력을 시작한다.
                    for (int frame = 0; frame < 30 && !lab.IsGrounded; frame++)
                        lab.Simulate(0f, false, false, 1f / 120f);
                    Assert.That(lab.IsGrounded, Is.True, "Ceiling trial must start grounded");
                    for (int frame = 0; frame < 240 && !lab.HasResult; frame++)
                        lab.Simulate(0f, frame == 0, true, 1f / 120f);
                    Assert.That(lab.HasResult, Is.True);
                    Assert.That(lab.LastCeilingHit, Is.True);
                    Assert.That(lab.LastHeight, Is.LessThan(1.4f));
                }
                lab.SelectVariant(JumpVariant.Gravity);
                Assert.That(lab.HasResult, Is.False);
                Assert.That(lab.transform.position.x, Is.EqualTo(0f).Within(0.01f));
                Vector3 position = lab.transform.position;
                lab.Simulate(0f, true, true, float.NaN);
                Assert.That(lab.transform.position, Is.EqualTo(position));

                // 저장된 모델과 애니메이션 연결이 실제 실행에서도 남아 있는지 확인한다.
                lab.enabled = true;
                yield return null;
                yield return null;
                Animator animator = lab.GetComponentInChildren<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                Assert.That(animator.avatar, Is.Not.Null);
                Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
                Assert.That(animator.GetBoneTransform(HumanBodyBones.Head), Is.Not.Null);
                Assert.That(animator.GetBoneTransform(HumanBodyBones.LeftFoot), Is.Not.Null);
                Assert.That(animator.applyRootMotion, Is.False);
            }
            finally
            {
                if (lab != null) lab.enabled = false;
            }
        }
    }
}
#endif
