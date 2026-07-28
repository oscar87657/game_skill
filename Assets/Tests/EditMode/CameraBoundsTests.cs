// GOLDEN STANDARD
// 목적: Scene과 프레임 타이밍 없이 카메라 중심점 제한과 구역 데이터 연결을 검증한다.
// 책임: 범위 내부·외부·뒤집힌 입력과 동일 ID 구역 매칭의 정상·경계 흐름을 확인한다.
// 불변식: 각 테스트는 생성한 ScriptableObject를 종료 전에 정리하며 Z 깊이 보존을 함께 검사한다.
// 선택 이유: 카메라 손맛 테스트와 별개로 수학적 경계 회귀를 빠른 EditMode 테스트에서 막을 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class CameraBoundsTests
    {
        [Test]
        public void PerspectiveDistance_PreservesOrthographicFraming()
        {
            // 기존 정사영 반높이를 원근 거리로 바꾼 뒤 다시 계산해 같은 세로 구도가 유지되는지 검증한다.
            float distance =
                CameraPerspectiveMath
                    .DistanceForVerticalFraming(
                        5.2f,
                        35f);
            float restoredHalfExtent =
                CameraPerspectiveMath
                    .VerticalHalfExtent(
                        distance,
                        35f);

            Assert.That(
                distance,
                Is.EqualTo(16.4923f)
                    .Within(0.001f));
            Assert.That(
                restoredHalfExtent,
                Is.EqualTo(5.2f)
                    .Within(0.0001f));
        }

        [TestCase(0f, 3f, 0f, 3f)]
        [TestCase(-9f, 3f, -4f, 3f)]
        [TestCase(9f, 9f, 2f, 4f)]
        public void ClampCenter_ConstrainsEachAxis(
            float desiredX,
            float desiredY,
            float expectedX,
            float expectedY)
        {
            // X/Y는 허용 범위로 제한하고 횡스크롤 카메라 깊이 Z는 그대로 보존하는지 검증한다.
            Vector3 result = CameraBoundsMath.ClampCenter(
                new Vector3(desiredX, desiredY, -9f),
                new Vector2(-4f, 2f),
                new Vector2(2f, 4f));

            Assert.That(result.x, Is.EqualTo(expectedX));
            Assert.That(result.y, Is.EqualTo(expectedY));
            Assert.That(result.z, Is.EqualTo(-9f));
        }

        [Test]
        public void ClampCenter_NormalizesReversedBounds()
        {
            // Inspector에서 최소·최대가 뒤집혀도 같은 유효 범위를 사용하는지 확인한다.
            Vector3 result = CameraBoundsMath.ClampCenter(
                new Vector3(9f, -3f, -7f),
                new Vector2(2f, 4f),
                new Vector2(-4f, 2f));

            Assert.That(result, Is.EqualTo(new Vector3(2f, 2f, -7f)));
        }

        [Test]
        public void CameraZoneBounds_MatchesSameIdAndConstrains()
        {
            // 에셋 참조가 달라도 영구 ID가 같으면 같은 카메라 방으로 처리하는지 검증한다.
            WorldZoneDefinition zone =
                CreateZone("start_hall", "시작 홀");
            WorldZoneDefinition sameIdZone =
                CreateZone("start_hall", "같은 ID");
            try
            {
                var bounds = new CameraZoneBounds(
                    zone,
                    new Vector2(-4f, 2.4f),
                    new Vector2(2f, 3.4f));

                Assert.That(bounds.IsConfigured, Is.True);
                Assert.That(bounds.Matches(sameIdZone), Is.True);
                Assert.That(
                    bounds.Constrain(
                        new Vector3(30f, -5f, -9f)),
                    Is.EqualTo(
                        new Vector3(2f, 2.4f, -9f)));
            }
            finally
            {
                Object.DestroyImmediate(zone);
                Object.DestroyImmediate(sameIdZone);
            }
        }

        private static WorldZoneDefinition CreateZone(
            string id,
            string displayName)
        {
            // 반복되는 ScriptableObject 준비를 한곳에 두어 테스트가 카메라 경계 규칙에 집중하게 한다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, displayName, $"{displayName} 카메라 테스트");
            return zone;
        }
    }
}
