// GOLDEN STANDARD
// 목적: 입력 학습과 메트로배니아 진행 상태를 현재 안내 단계로 결정적으로 변환한다.
// 책임: 튜토리얼 우선순위, 능력·월드 진행 분기와 단계별 표시 문구·표식 ID를 제공한다.
// 불변식: 같은 상태는 프레임·Scene·UI와 무관하게 항상 같은 안내 결과를 반환한다.
// 선택 이유: 진행 판단을 순수 함수로 분리하면 HUD 아트와 월드 배치를 바꿔도 규칙을 빠르게 테스트할 수 있다.
namespace GameSkill
{
    public enum GuidanceStage
    {
        LearnMove,
        LearnJump,
        LearnDash,
        LearnAttack,
        ReachDoubleJump,
        ReachAirDash,
        ReachWallTraversal,
        ReturnToShaft,
        ClimbShaft,
        ActivateShortcut,
        ReachBossRoom,
        DefeatBoss,
        Complete
    }

    public readonly struct GuidanceContent
    {
        public GuidanceContent(
            GuidanceStage stage,
            string objective,
            string hint,
            string markerId)
        {
            // 불변 값 객체로 한 단계의 HUD 문구와 월드 표식 키를 함께 전달한다.
            Stage = stage;
            Objective = objective;
            Hint = hint;
            MarkerId = markerId;
        }

        public GuidanceStage Stage { get; }
        public string Objective { get; }
        public string Hint { get; }
        public string MarkerId { get; }
        public bool HasMarker =>
            !string.IsNullOrWhiteSpace(
                MarkerId);
    }

    public static class GuidanceProgression
    {
        public const string BacktrackShaftZoneId =
            "backtrack_shaft";
        public const string BossRoomZoneId =
            "boss_room";

        public static GuidanceStage Resolve(
            bool moved,
            bool jumped,
            bool dashed,
            bool attacked,
            bool hasDoubleJump,
            bool hasAirDash,
            bool hasWallTraversal,
            string currentZoneId,
            bool rewardCollected,
            bool shortcutUnlocked,
            bool bossDefeated)
        {
            // 완료 상태를 최우선으로 두어 이전 단계 데이터가 일부 누락돼도 엔딩 목표가 되돌아가지 않게 한다.
            if (bossDefeated)
            {
                return GuidanceStage.Complete;
            }

            bool hasProgressAbility =
                hasDoubleJump
                || hasAirDash
                || hasWallTraversal;
            // 첫 능력을 얻기 전까지만 실제 성공 입력을 순서대로 안내하고, 기존 세이브는 기본 조작을 건너뛴다.
            if (!hasProgressAbility)
            {
                if (!moved)
                {
                    return GuidanceStage.LearnMove;
                }

                if (!jumped)
                {
                    return GuidanceStage.LearnJump;
                }

                if (!dashed)
                {
                    return GuidanceStage.LearnDash;
                }

                if (!attacked)
                {
                    return GuidanceStage.LearnAttack;
                }
            }

            if (!hasDoubleJump)
            {
                return GuidanceStage.ReachDoubleJump;
            }

            if (!hasAirDash)
            {
                return GuidanceStage.ReachAirDash;
            }

            if (!hasWallTraversal)
            {
                return GuidanceStage.ReachWallTraversal;
            }

            if (!rewardCollected)
            {
                // 샤프트에 진입한 뒤에는 입구 대신 정상 보상을 가리켜 같은 목표 안에서도 동선을 구체화한다.
                return currentZoneId
                    == BacktrackShaftZoneId
                    ? GuidanceStage.ClimbShaft
                    : GuidanceStage.ReturnToShaft;
            }

            if (!shortcutUnlocked)
            {
                return GuidanceStage.ActivateShortcut;
            }

            return currentZoneId
                == BossRoomZoneId
                ? GuidanceStage.DefeatBoss
                : GuidanceStage.ReachBossRoom;
        }

        public static GuidanceContent ContentFor(
            GuidanceStage stage)
        {
            // 단계와 화면 문구를 한곳에서 매핑해 Presenter와 테스트가 같은 사전을 사용하게 한다.
            return stage switch
            {
                GuidanceStage.LearnMove =>
                    new GuidanceContent(
                        stage,
                        "MOVE RIGHT",
                        "[A / D] MOVE",
                        string.Empty),
                GuidanceStage.LearnJump =>
                    new GuidanceContent(
                        stage,
                        "TRY A JUMP",
                        "[SPACE] JUMP",
                        string.Empty),
                GuidanceStage.LearnDash =>
                    new GuidanceContent(
                        stage,
                        "TRY A DASH",
                        "[LEFT SHIFT] DASH / HOLD TO RUN",
                        string.Empty),
                GuidanceStage.LearnAttack =>
                    new GuidanceContent(
                        stage,
                        "TRY AN ATTACK",
                        "[ENTER] ATTACK",
                        string.Empty),
                GuidanceStage.ReachDoubleJump =>
                    new GuidanceContent(
                        stage,
                        "REACH THE FIRST ABILITY",
                        "DASH THROUGH THE RED HAZARD",
                        "double_jump"),
                GuidanceStage.ReachAirDash =>
                    new GuidanceContent(
                        stage,
                        "CLIMB TO THE HIGH PLATFORM",
                        "[SPACE] DOUBLE JUMP",
                        "air_dash"),
                GuidanceStage.ReachWallTraversal =>
                    new GuidanceContent(
                        stage,
                        "PASS THE AIR-DASH GATE",
                        "[LEFT SHIFT] AIR DASH",
                        "wall_traversal"),
                GuidanceStage.ReturnToShaft =>
                    new GuidanceContent(
                        stage,
                        "RETURN TO THE LEFT SHAFT",
                        "FOLLOW THE GOLD BEACON",
                        "shaft_entrance"),
                GuidanceStage.ClimbShaft =>
                    new GuidanceContent(
                        stage,
                        "CLAIM THE HEALTH FRAGMENT",
                        "HOLD TOWARD WALL + [SPACE]",
                        "shaft_reward"),
                GuidanceStage.ActivateShortcut =>
                    new GuidanceContent(
                        stage,
                        "ACTIVATE THE RETURN SHORTCUT",
                        "TOUCH THE GOLD DEVICE",
                        "shaft_shortcut"),
                GuidanceStage.ReachBossRoom =>
                    new GuidanceContent(
                        stage,
                        "ENTER THE ABILITY TRIAL",
                        "FOLLOW THE GOLD BEACON TO THE RIGHT",
                        "boss_entrance"),
                GuidanceStage.DefeatBoss =>
                    new GuidanceContent(
                        stage,
                        "DEFEAT THE ABILITY WARDEN",
                        "COMBINE JUMP, DASH AND WALL GRAB",
                        "boss_target"),
                _ =>
                    new GuidanceContent(
                        GuidanceStage.Complete,
                        "VERTICAL SLICE COMPLETE",
                        "ALL CORE SYSTEMS CLEARED",
                        string.Empty)
            };
        }
    }
}
