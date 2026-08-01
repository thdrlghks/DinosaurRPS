namespace Core.Enums
{
    /// <summary>
    /// 게임 안에서 소리가 나는 지점들. SFXManager의 라이브러리 키로 쓰인다.
    /// </summary>
    public enum SfxId
    {
        None = 0,

        // 입력
        HandSelect,
        HandSealed,

        // 카운트다운 — "안 내면 진다 가위바위보" 구호 한 덩어리.
        // 가위/바위/보 UI가 도는 동안 통째로 재생된다.
        CountdownChant,

        // 라운드 진행
        HandReveal,
        Impact,
        RoundWin,
        RoundLose,
        RoundDraw,
        HealthDamage,

        // 연출
        TyrannoRise,
        ChickenDefeat,

        // 매치 종료
        MatchVictory,
        MatchDefeat
    }
}
