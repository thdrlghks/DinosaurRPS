# 인간 우주 결승 맵

`Assets/Scenes/FinalRound.unity`를 열고 Play로 실행합니다. 결승은 2승제이며 Q=가위, W=보, E=바위입니다. 세 손 모두 처음부터 사용할 수 있습니다.

- 지구와 별 배경, 금속 원형 경기장, 청록/자홍색 발판, 회전하는 궤도 구조물과 위성을 배치했습니다.
- 기존 `hm_R.fbx`, `hm_P.fbx`, `hm_S.fbx`의 실제 팔/손가락 애니메이션을 사용합니다. 원본 FBX는 수정하지 않았습니다.
- 원본의 Maya 손가락 스케일 보정(`InheritType=2`)이 Unity에서 누적 축소되는 문제를 파생 `HumanRock/Paper/Scissors.anim`에서 보정했습니다. 원래 위치/회전 곡선은 유지합니다.
- 기존에 없던 대기, 승리, 패배, 무승부 반응은 원본 골격을 활용한 간단한 동작으로 추가했습니다.
- 인간 등장 그림/초상화와 손 확대 카메라를 연결했습니다. 기존 결승 진행 경로 `08Start2 → FinalRound → 10End2`를 그대로 사용합니다.
- 라운드 도입은 가위에서 티라노 이동 샷, 바위에서 인간 이동 샷을 각각 2초 재생합니다. 보는 경기장 구도를 유지하고, 구호가 끝난 뒤 1.5초 동안 전체 경기장으로 빠집니다. 세 경로는 매 라운드 처음부터 재생합니다.

`Men.unity`는 기존 미완성 시제품이며, 실제 결승은 `FinalRound.unity`입니다.

## 편집

배치와 카메라는 저장된 씬에서 바로 수정할 수 있습니다. 환경 메시/재질은 `Assets/MapAssets/HumanSpace`, 애니메이션과 전용 재질은 `Assets/Graphics/Characters/Men/Human*`에 있습니다.

`Tools > Dinosaur RPS > Build Human Space Final Round`는 결승 씬을 새로 저작하는 명령입니다. 기존 `FinalRound`의 수동 편집을 덮어쓰므로 재생에 사용할 필요는 없습니다. 파일 임포트만으로 실행되지 않습니다.

`Tools > Dinosaur RPS > Restore Human Space Intro Cameras`는 결승의 세 인트로 카메라 경로와 타깃만 다시 배치합니다. 경로는 씬의 `Human Space Intro Camera Paths` 아래에 저장됩니다.

## 검증

Unity 6000.2.10f1의 별도 검증 프로젝트에서 저장 후 다시 연 씬으로 Play Mode 테스트와 1600×900 렌더 검사를 수행했습니다. 실행 중인 사용자 Unity 창은 직접 확인하지 못했습니다.

무승부 → 패배 → 승리 → 승리의 실제 4라운드를 실행해 2승 1무 1패 결과와 `10End2` 이동을 확인했습니다. 세 손동작의 정지 상태, 양손 손가락 스케일 보정, 배경 회전, 선택 잠금 해제 검사를 통과했고 런타임 예외는 발생하지 않았습니다.

검증 로그와 캡처는 `.utmp/human-battle.log`, `.utmp/quarter-setup/HumanFrames/`에 있습니다. 배포용 실행 파일 빌드는 수행하지 않았습니다.

인트로 카메라는 별도 Play Mode 검사에서 세 카메라의 실제 위치 이동, 구호 종료 전 줌아웃 대기, 이동 중 일시정지/재개, 첫 손 확대 화면, 다음 라운드의 경로 초기화를 확인했습니다. 로그는 `.utmp/human-intro-motion-functional-pass.log`, 캡처는 `.utmp/quarter-setup/HumanIntroFrames/`에 있습니다.
