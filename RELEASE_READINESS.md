# Dinosaur RPS 통합 출시 준비 보고서

- 기준일: 2026-08-15
- 대상: `won` 브랜치 현재 워크트리
- Unity: 6000.2.10f1 / Windows Standalone 가정
- 최종 판정: **NO-GO — 공개 출시 금지, 내부 알파 수준**
- 이 문서가 출시 준비 상태의 **단일 기준 문서**다.

## 1. 결론

가위바위보 전투 루프와 아래 토너먼트 전환 배선은 만들어져 있다.

`00MainMenu → 01Start → 02Forest(튜토리얼) → 03End80 → QuarterFinals(8강) → 04End8 → 05Start4 → 06Lava(4강) → 07End4 → 08Start2 → FinalRound(결승) → 굿/배드 엔딩`

그러나 현재 상태로 출시하면 첫 플레이에서 다음 문제가 드러난다.

- 8강과 결승이 앞 스테이지의 복제본이다.
- 닭 패배 연출이 모든 스테이지 승리에 실행될 수 있다.
- 굿/배드 엔딩이 사실상 빈 막다른 씬이다.
- 전투 BGM이 없고 SFX 13종 중 2종만 연결되어 있다.
- 점수 UI 코드가 씬에 연결되지 않았다.
- 필수 컷신에서 프레임 드롭이 관측됐으며 영상 실패 복구가 없다.
- 4강과 결승에는 전투 중 설정/일시정지 UI가 없다.
- 릴리스 빌드와 전체 회귀 테스트 기록이 없다.

따라서 콘텐츠를 공개할 수 있는 상태가 아니며, 아래 P0를 모두 해결하기 전에는 외부 배포 또는 스토어 제출을 하면 안 된다.

## 2. 두 기존 감사 결과의 정정 사항

두 보고서의 중복을 합치면서 파일에서 다시 확인한 결과다.

| 기존 주장 | 재검증 결과 | 최종 분류 |
|---|---|---|
| BGM이 모든 전투 씬에 없음 | `SceneBgm` 배치 0건, 전투 씬 AudioClip 직접 연결도 0건 | 사실, P0 |
| SFX 13종 중 2종만 연결 | `SettingController.prefab`에 `CountdownChant`, `RoundWin`만 등록 | 사실, P0 |
| 모든 전투에서 설정/탈출 불가 | `02Forest`, `QuarterFinals`에는 `SettingCanvas`가 있음. `06Lava`, `FinalRound`에는 없음 | 부분 사실, P0 |
| 메인 메뉴 종료 버튼이 완전히 고장 | 이름이 `QuitBtn`인 빈 버튼은 있으나 별도 종료 버튼은 `OnClickExitGame`에 정상 연결 | 정리 대상, P1 |
| `.mov` 10개 모두 현재 컷신에 사용 | `.mov` 파일은 10개 존재. 활성 진행 경로에서는 6개 고유 MOV가 7개 씬 사용처에 연결 | 위험은 사실, 수량 표현 정정 |
| Unity 스플래시 ON은 무조건 BLOCKER | 라이선스 등급에 따라 제거 불가능할 수 있음 | 제품 설정 확인, P1 |
| 버전 `0.1.0` 자체가 BLOCKER | 내부 알파 버전으로는 자연스러우나 정식 출시 전 확정 필요 | P1 |
| 전체화면 Borderless는 문제 | 일반적인 PC 게임 설정이며 단독 문제 아님 | 선택 사항 |

## 3. 출시 차단 항목 — P0

### P0-01. 8강 콘텐츠가 튜토리얼 복제본

- [`QuarterFinals.unity`](Assets/Scenes/QuarterFinals.unity)는 [`02Forest.unity`](Assets/Scenes/02Forest.unity)와 `_currentStage` 외에는 동일하다.
- 닭 상대, 숲 배경, `ChickenHandCam`, `ChickenRawImage`, 닭 패배 설정이 그대로다.
- `_sealedHands: 01000000`도 남아 있어 8강 첫 라운드에 튜토리얼용 손 봉인이 적용된다.

해야 할 일:

- 8강 상대 프리팹, Animator, 손 카메라, 초상화, BGM, SFX를 실제 8강 구성으로 교체
- `_sealedHands` 제거
- 닭 전용 참조와 패배 연출 제거 또는 스테이지 설정으로 분리
- 8강 전용 승리·패배 Animator Trigger 검증

완료 기준: 튜토리얼과 8강이 상대, 규칙, UI, 사운드와 연출에서 명확히 다르다.

### P0-02. 결승 콘텐츠가 4강 복제본

- [`FinalRound.unity`](Assets/Scenes/FinalRound.unity)는 [`06Lava.unity`](Assets/Scenes/06Lava.unity)와 `_currentStage` 외에는 동일하다.
- 결승도 용암 맵, 같은 상대, 같은 카메라와 연출을 사용한다.

해야 할 일:

- `FinalRound`를 최종 결승 콘텐츠로 교체하거나 기존 `09Space`에 완성된 전투 시스템을 이식
- 결승 전용 상대, 맵, BGM, 난이도, 카메라와 승패 연출 적용
- 결승 승리 `10End2`, 패배 `BadAnimation` 분기를 실제 빌드에서 확인

완료 기준: 4강과 결승이 시각·청각·게임플레이 면에서 구분된다.

### P0-03. 닭 패배 연출이 모든 스테이지에서 발동 가능

- [`TournamentGameManager.IsChickenFinalDefeat`](Assets/Scripts/Managers/TournamentGameManager.cs)은 매치 승리만 확인하고 현재 스테이지를 확인하지 않는다.
- 8강·4강·결승 승리에서도 상대가 닭처럼 날아가고 Animator가 정지할 수 있다.

수정 기준:

```csharp
return _currentStage == TournamentStage.Qualifiers
       && result == GameResult.Win
       && _matchData.IsMatchOver()
       && _matchData.GetWinner() == GameResult.Win;
```

완료 기준: 닭 연출은 튜토리얼에서만 실행되고 나머지는 각 상대 전용 연출을 사용한다.

### P0-04. 굿/배드 엔딩이 막다른 씬

- [`VictoryAnimation.unity`](Assets/Scenes/EndingScenes/VictoryAnimation.unity)는 Sprite가 비어 있는 Image 중심이다.
- [`BadAnimation.unity`](Assets/Scenes/EndingScenes/BadScenes/BadAnimation.unity)는 기본 카메라와 조명만 있다.
- 두 씬 모두 엔딩 콘텐츠, 메인 메뉴 복귀, 다시 시작 또는 종료 수단이 없다.

해야 할 일:

- 실제 굿/배드 엔딩 영상 또는 Timeline 연결
- 엔딩 종료 후 `00MainMenu` 자동 복귀 또는 `메뉴 / 다시 시작 / 종료` 버튼 제공
- 엔딩 중 Space/ESC 정책과 중복 씬 전환 방지 구현

완료 기준: 두 엔딩이 정상 재생되고 플레이어가 게임을 종료하거나 메뉴로 돌아갈 수 있다.

### P0-05. 전투 오디오 대부분 미연결

직접 확인 결과:

- `SceneBgm`은 어느 씬/프리팹에도 배치되지 않았다.
- 전투 씬에 직접 연결된 일반 AudioClip도 확인되지 않았다.
- `SfxId`의 실사용 효과음 13종 중 `CountdownChant`, `RoundWin` 2종만 [`SettingController.prefab`](Assets/Resources/SettingController.prefab)에 등록돼 있다.
- `HandSelect`, `HandSealed`, `HandReveal`, `Impact`, `RoundLose`, `RoundDraw`, `HealthDamage`, `TyrannoRise`, `ChickenDefeat`, `MatchVictory`, `MatchDefeat` 등은 호출돼도 클립이 없어 재생되지 않는다.

해야 할 일:

- 스테이지별 BGM 선정 및 `SceneBgm` 배치
- 13개 SFX 전체에 클립 연결
- BGM/효과음/영상 음량을 설정 UI와 일관되게 연결
- 무승부, 타격, 체력 감소, 매치 승패를 실제 플레이로 청취 검증

완료 기준: 모든 전투와 컷신에서 의도된 소리가 나고 음량 설정·일시정지·Alt+Tab 후에도 정상이다.

### P0-06. 승리 점수 UI 미연결과 체력 규칙 중복

- [`UIManager.UpdateVictoryScore`](Assets/Scripts/Managers/UIManager.cs)는 추가됐지만 네 점수 배열이 어느 전투 씬/프리팹에도 직렬화되지 않았다.
- 배열이 `null`이므로 현재 코드는 아무 UI도 변경하지 않는다.
- `UIManager.InitializeHealthBars`에는 예전 규칙인 튜토리얼 `1/1`, 8강 `999/1`이 남아 있다. `TournamentGameManager`가 다시 덮어써 우연히 보정되는 구조다.

해야 할 일:

- 플레이어/상대의 Empty/Filled 점수 배열을 네 전투 씬에 연결
- 튜토리얼 `999/1`, 8강·4강·결승 `2/2`로 정합화
- 체력과 필요 승수의 단일 출처를 `TournamentRules` 또는 Stage 설정 데이터로 통합

완료 기준: 0점 초기화, 1승 표시, 2승 종료와 씬 전환 후 초기화가 모두 정확하다.

### P0-07. 컷신 호환성과 오류 복구 부족

- 프로젝트에 MOV 파일 10개가 있다.
- 활성 흐름은 고유 MOV 6개를 사용하며 `DinosaurCrawling.mov`는 두 씬에서 재사용된다.
- 현재 Editor 로그에서 다음 프레임 드롭이 관측됐다.
  - 오프닝 MP4: 24프레임 드롭
  - 튜토리얼 MOV: 32프레임 드롭
- [`VideoSceneTransition`](Assets/Scripts/VideoSceneTransition.cs)은 준비 타임아웃이 없고 `errorReceived`에서 로그만 남긴다.

해야 할 일:

- 활성 MOV를 Windows 호환 H.264/AAC MP4로 변환 후 참조 교체
- `Prepare()` 타임아웃과 디코더 오류 fallback 구현
- 오류 시 다음 씬 이동 또는 재시도 UI 제공
- 저사양 Windows PC에서 모든 영상 재생·스킵 테스트

완료 기준: 정상·느린 디스크·손상 파일·지원하지 않는 코덱 상황에서도 진행이 멈추지 않는다.

### P0-08. 4강과 결승의 전투 중 설정/일시정지 부재

- `SettingCanvas` 프리팹은 `02Forest`와 그 복제본인 `QuarterFinals`에는 있다.
- `06Lava`와 `FinalRound`에는 없다.
- 따라서 4강·결승에서는 ESC로 설정하거나 정상적으로 전투를 빠져나갈 수 없다.

해야 할 일:

- 공통 UI 프리팹 또는 부트스트랩 방식으로 모든 전투 씬에 설정/일시정지 제공
- `계속`, `재시작`, `메인 메뉴`, `종료` 정책 확정
- 연출 도중 설정창을 열고 닫을 때 `Time.timeScale`, 오디오, 입력 잠금 복구 검증

완료 기준: 모든 전투 씬에서 ESC 동작과 복귀가 일관된다.

### P0-09. 피격·반피·사망 연출 미완성

- [`TournamentGameManager.cs`](Assets/Scripts/Managers/TournamentGameManager.cs)에 `Stagger`, `Death` Trigger가 TODO로 남아 있다.
- 닭이 아닌 상대는 HP 0에서도 전용 사망 연출 없이 서 있을 수 있다.

해야 할 일:

- 8강·4강·결승 상대와 플레이어 Animator에 Stagger/Death 상태 연결
- 고정 1초 대기 대신 Animator 상태 종료 또는 Animation Event 사용

완료 기준: 모든 스테이지의 반피·최종 패배 연출과 씬 전환 타이밍이 일치한다.

### P0-10. 릴리스 빌드와 전체 테스트 부재

- 저장소에 Windows 빌드 산출물 또는 검증 기록이 없다.
- EditMode/PlayMode 테스트와 테스트 Assembly Definition이 없다.
- 굿·배드 엔딩을 포함한 처음부터 끝까지의 빌드 플레이 결과가 없다.

완료 기준:

- Windows Development Build 전체 흐름 통과
- 오류 수정 후 Release Build 생성
- 깨끗한 PC에서 굿/배드 엔딩 각각 2회 연속 통과
- Player 로그의 처리되지 않은 예외 0건
- 최소 사양에서 합의한 FPS·메모리 목표 충족

## 4. 출시 전 필수 항목 — P1

### P1-01. 제품 설정 확정

현재 값:

- 회사명: `DefaultCompany`
- 앱 식별자: Unity URP 템플릿 기본값
- 앱 아이콘: 없음
- 기본 해상도: 1024×768
- 창 크기 조절: 비활성화
- 버전: 0.1.0
- Unity 스플래시: 활성화

해야 할 일:

- 실제 팀/회사명과 고유 앱 식별자 설정
- Windows 아이콘 제작·지정
- 기본 16:9 해상도와 창 크기 조절 정책 확정
- 정식 출시 버전 설정
- Unity 라이선스 등급에 맞춰 스플래시 정책 확인

### P1-02. 사용하지 않는 씬과 빌드 크기 정리

- 현재 흐름에서 사용하지 않는 `09Space`가 Build Settings에 활성화되어 있다.
- Space 에셋에는 약 45.59MB와 34.92MB의 대형 지구 텍스처가 있다.
- `망작`, `CAM3`, 구형 `SemiFinals`, `_Recovery` 씬도 프로젝트에 남아 있다.

해야 할 일:

- `09Space`를 결승에 사용하지 않으면 Build Settings에서 비활성화
- 스크랩 씬은 먼저 별도 백업 브랜치/태그에 보존한 뒤 삭제 여부 결정
- 사용하지 않는 에셋이 실제 Player 빌드에 포함되는지 Build Report로 확인

### P1-03. 메뉴의 중복/죽은 종료 버튼 정리

- 정상적으로 `SettingUI.OnClickExitGame`에 연결된 종료 버튼이 있다.
- 별도로 이름이 `QuitBtn`인 버튼은 OnClick이 비어 있다.

해야 할 일:

- `QuitBtn`이 화면에 노출되면 정상 종료에 연결
- 사용하지 않는 중복 오브젝트라면 삭제

### P1-04. 패배 UX 결정

- 결승 외 패배는 짧은 대기 후 메인 메뉴로 이동한다.
- 결과 화면, 재시도, 진행 확인이 없다.

최소 권장: `다시 도전 / 메인 메뉴` 선택과 패배 결과 표시.

### P1-05. 저장/전적 시스템 범위 결정

- `BattleHistoryManager` 구현은 있지만 어느 씬/프리팹에도 배치되지 않았다.
- 호출부가 null 조건부라 현재 모든 기록 호출은 no-op이다.
- 진행 저장 UI도 없다.

결정: 실제 전적 기능으로 연결하거나 코드와 PlayerPrefs 데이터 모델을 제거한다.

### P1-06. 로그와 경고 정리

- 정상 Animator 전환도 `Debug.LogError`로 기록한다.
- Unity 6에서 폐기 예정인 `FindObjectOfType` 경고와 사용하지 않는 필드 경고가 있다.

해야 할 일:

- 정상 로그를 제거하거나 Editor/Development Build로 제한
- Error는 실제 복구 불가능한 문제에만 사용
- 폐기 API와 사용하지 않는 직렬화 필드 정리

### P1-07. 성능 측정과 에셋 최적화

- `Assets` 원본 총량: 약 364MB
- 영상·음원: 약 57MB
- 큰 Space 텍스처와 여러 FBX/URP 효과가 존재한다.

측정 기준 예시:

- 목표 60 FPS, 평균 프레임 16.7ms 이하
- 1% low 45 FPS 이상
- 반복 전투 중 프레임당 관리 힙 할당 0B 목표
- 최소 사양 PC 메모리 피크 2GB 이하를 1차 기준으로 측정

실제 수치는 최소/권장 사양을 확정한 뒤 조정한다.

### P1-08. 입력과 접근성 안내

- 전투 Q/W/E, 컷신 Space, 설정 ESC를 사용한다.
- 키 재매핑, 게임패드, 자막, 색각 보조가 없다.

최소 요건:

- 튜토리얼과 메뉴에서 모든 키 표시
- 색상 외 아이콘·텍스트로 승패와 점수 전달
- 스킵 가능한 컷신에 안내 표시
- 키보드 전용이면 스토어 페이지에 명시

### P1-09. 라이선스와 크레딧 감사

- DOTween/DOTween Pro, TextMesh Pro 폰트, Lightning Bolt, Simple Nature Pack, RPGPP_LT, Lava/Space 에셋 등이 존재한다.
- 일부 라이선스 파일은 있지만 전체 출처·구매 증빙·상업 배포 권리를 정리한 문서가 없다.

해야 할 일: `THIRD_PARTY_NOTICES.md`에 에셋명, 제작자, 출처, 라이선스, 구매 증빙, 크레딧 의무를 기록한다.

### P1-10. 문서와 재현 가능한 릴리스

- README는 프로젝트명만 있다.
- 현재 워크트리는 여러 수정 파일과 추적되지 않은 이미지/메타를 포함한다.
- Git LFS 사용 흔적이 없고 저장소 pack은 약 225MB다.

해야 할 일:

- 실행법, 조작법, 지원 OS, 최소 사양, 빌드 절차, 알려진 문제 문서화
- 모든 필수 에셋과 `.meta` 커밋
- 대용량 바이너리의 Git LFS 정책 결정
- 깨끗한 clone에서 임포트·빌드 재현
- 릴리스 커밋과 태그 생성

## 5. 제품 결정 항목 — P2

다음은 결함이라기보다 출시 범위를 정해야 하는 항목이다.

- AI를 현재의 연출형 확률 AI로 유지할지 난이도/적응형 시스템을 추가할지
- 전적과 진행 저장을 제품 기능으로 제공할지
- 패배 후 재시도와 스테이지 선택을 제공할지
- 게임패드와 키 재매핑을 1.0 범위에 포함할지
- 엔딩 이후 자유 플레이 또는 통계 화면을 제공할지
- 구형 카메라·빈 `GameManager`·미사용 메서드를 삭제할지

## 6. 실행 순서

### 1단계 — 코드·설정 빠른 수정

- [ ] 닭 패배 연출에 `Qualifiers` 조건 추가
- [ ] 8강 `_sealedHands` 제거
- [ ] 점수 UI 배열 연결 및 체력 규칙 정합화
- [ ] 4강·결승에 공통 설정/일시정지 UI 배치
- [ ] 엔딩에 임시 메뉴 복귀 버튼 또는 자동 복귀 추가
- [ ] `09Space` 빌드 제외
- [ ] 죽은 `QuitBtn` 정리
- [ ] 정상 경로의 `Debug.LogError` 정리
- [ ] 회사명·앱 ID·해상도·아이콘·버전 정책 확정

### 2단계 — 에셋과 콘텐츠 작업

- [ ] SFX 13종 연결
- [ ] 스테이지별 BGM 연결
- [ ] 활성 MOV를 H.264/AAC MP4로 변환
- [ ] 영상 오류·타임아웃 fallback 구현
- [ ] 8강 상대·배경·UI·연출 교체
- [ ] 결승 상대·배경·UI·연출 교체
- [ ] Stagger/Death 애니메이션 연결
- [ ] 굿/배드 엔딩 콘텐츠 완성

### 3단계 — 출시 검증

- [ ] 라이선스/크레딧 문서 완성
- [ ] 최소/권장 사양 확정
- [ ] Development Build 전체 플레이
- [ ] 성능·메모리·영상 프로파일링
- [ ] Release Build 굿/배드 엔딩 각각 2회 통과
- [ ] 720p, 1080p, 1440p, 울트라와이드 UI 확인
- [ ] 설정 저장, Alt+Tab, 영상 스킵 연타, 손상 영상 fallback 확인
- [ ] 깨끗한 clone에서 빌드 재현
- [ ] 릴리스 태그 생성

## 7. 최종 출시 승인 조건

다음을 모두 만족해야 `GO`로 변경한다.

1. P0 10건 전부 해결
2. 실제 8강·결승·굿 엔딩·배드 엔딩 콘텐츠 완성
3. 모든 필수 BGM/SFX 연결
4. 컷신 실패 시 진행 복구 가능
5. Windows Release Build 생성
6. 굿/배드 엔딩 각각 2회 연속 전체 플레이 통과
7. Player 로그의 처리되지 않은 예외 0건
8. 합의한 최소 사양에서 FPS·메모리 목표 통과
9. 전체 에셋의 상업적 배포 권리 확인
10. 제품 메타데이터, 문서, 릴리스 커밋과 태그 완성

현재 판정은 **NO-GO**다. 작업 순서는 `빠른 코드/설정 수정 → 오디오·영상 → 8강/결승/엔딩 콘텐츠 → 빌드 QA`가 가장 효율적이다.
