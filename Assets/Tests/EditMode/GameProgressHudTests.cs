// GOLDEN STANDARD
// 목적: 실제 Canvas 배치와 무관하게 진행 HUD의 이벤트 기반 표시 계약을 검증한다.
// 책임: 체력 변경, 능력 해금, 슬롯 조회와 초기 저장 문구의 갱신 결과를 확인한다.
// 불변식: 테스트는 로컬 저장 파일을 쓰지 않으며 생성한 Unity 객체와 정의를 모두 정리한다.
// 선택 이유: Presenter 단위 테스트로 UI 아트가 바뀌어도 상태와 표현 사이의 핵심 연결을 보호한다.
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill.Tests
{
    public sealed class GameProgressHudTests
    {
        [Test]
        public void Hud_RefreshesFromHealthAndAbilityEvents()
        {
            // 실제 플레이어와 같은 상태 컴포넌트를 준비해 이벤트 발행부터 HUD 반영까지 검증한다.
            var player =
                new GameObject("ProgressHudTestPlayer");
            var hudObject =
                new GameObject("ProgressHudTest");
            AbilityDefinition doubleJump =
                CreateAbility(
                    "double_jump",
                    "2단 점프");
            AbilityDefinition airDash =
                CreateAbility(
                    "air_dash",
                    "공중 대시");
            try
            {
                Health health =
                    player.AddComponent<Health>();
                health.Configure(5);
                PlayerAbilityState abilityState =
                    player.AddComponent<PlayerAbilityState>();
                player.AddComponent<PlayerCheckpointState>();
                player.AddComponent<PlayerWorldState>();
                GameProgressSaveController saveController =
                    player.AddComponent<GameProgressSaveController>();
                saveController.Configure(
                    new[]
                    {
                        doubleJump,
                        airDash
                    });

                Image healthFill =
                    CreateImage(
                        "HealthFill",
                        hudObject.transform);
                Text healthLabel =
                    CreateText(
                        "HealthLabel",
                        hudObject.transform,
                        string.Empty);
                Image doubleJumpBackground =
                    CreateImage(
                        "DoubleJumpSlot",
                        hudObject.transform);
                Text doubleJumpLabel =
                    CreateText(
                        "DoubleJumpLabel",
                        doubleJumpBackground.transform,
                        string.Empty);
                Image airDashBackground =
                    CreateImage(
                        "AirDashSlot",
                        hudObject.transform);
                Text airDashLabel =
                    CreateText(
                        "AirDashLabel",
                        airDashBackground.transform,
                        string.Empty);
                Button saveButton =
                    CreateButton(
                        "SaveButton",
                        hudObject.transform);
                Button loadButton =
                    CreateButton(
                        "LoadButton",
                        hudObject.transform);
                Text saveStatus =
                    CreateText(
                        "SaveStatus",
                        hudObject.transform,
                        "READY");
                GameProgressHud hud =
                    hudObject.AddComponent<GameProgressHud>();
                hud.Configure(
                    health,
                    abilityState,
                    saveController,
                    healthFill,
                    healthLabel,
                    new[]
                    {
                        new AbilityHudSlot(
                            doubleJump,
                            "DOUBLE JUMP",
                            doubleJumpBackground,
                            doubleJumpLabel),
                        new AbilityHudSlot(
                            airDash,
                            "AIR DASH",
                            airDashBackground,
                            airDashLabel)
                    },
                    saveButton,
                    loadButton,
                    saveStatus);

                Assert.That(
                    hud.HealthLabelText,
                    Is.EqualTo("HP 5 / 5"));
                Assert.That(
                    healthFill.fillAmount,
                    Is.EqualTo(1f).Within(0.001f));
                Assert.That(
                    hud.AbilitySlotCount,
                    Is.EqualTo(2));
                Assert.That(
                    doubleJumpLabel.text,
                    Is.EqualTo("?"));
                Assert.That(
                    hud.SaveStatusText,
                    Is.EqualTo("READY"));

                Assert.That(
                    health.TakeDamage(2),
                    Is.True);
                Assert.That(
                    hud.HealthLabelText,
                    Is.EqualTo("HP 3 / 5"));
                Assert.That(
                    healthFill.fillAmount,
                    Is.EqualTo(0.6f).Within(0.001f));

                Assert.That(
                    abilityState.TryUnlock(doubleJump),
                    Is.True);
                Assert.That(
                    hud.IsAbilityUnlocked(
                        doubleJump.Id),
                    Is.True);
                Assert.That(
                    hud.IsAbilityUnlocked(
                        airDash.Id),
                    Is.False);
                Assert.That(
                    doubleJumpLabel.text,
                    Is.EqualTo("DOUBLE JUMP"));
                Assert.That(
                    airDashLabel.text,
                    Is.EqualTo("?"));
            }
            finally
            {
                // 테스트 순서가 다음 테스트의 Unity 객체 조회와 능력 정의 상태에 영향을 주지 않게 정리한다.
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(doubleJump);
                Object.DestroyImmediate(airDash);
            }
        }

        private static AbilityDefinition CreateAbility(
            string id,
            string displayName)
        {
            // 반복되는 능력 정의 준비를 한곳에 모아 테스트 본문이 HUD 동작에 집중하게 한다.
            AbilityDefinition ability =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            ability.Configure(
                id,
                displayName,
                $"{displayName} HUD 테스트");
            return ability;
        }

        private static Image CreateImage(
            string objectName,
            Transform parent)
        {
            // UI Graphic에 필요한 RectTransform과 CanvasRenderer를 함께 만들어 실제 View 조건을 재현한다.
            var imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(
                parent,
                false);
            return imageObject.GetComponent<Image>();
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            string initialText)
        {
            // 레이블 초기값을 명시해 Configure가 바꾼 결과만 단언할 수 있게 한다.
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(
                parent,
                false);
            Text text = textObject.GetComponent<Text>();
            text.text = initialText;
            return text;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent)
        {
            // Button의 클릭 영역과 Target Graphic이 같은 Image를 사용하도록 런타임 씬 구성을 따른다.
            Image image =
                CreateImage(
                    objectName,
                    parent);
            Button button =
                image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
