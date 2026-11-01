# 엔딩 영상과 영상 오류 복구

## 엔딩 영상 교체

현재는 요청대로 기존 영상을 임시 연결했다. 원본 `Assets/Graphics/Animations/Ending.mp4`, `Ending2.mp4`는 그대로 보존했다. 재생용 사본은 `Assets/Graphics/Videos/Endings/`에 있으며 H.264 MP4, 1280×720, 약 3초다. 원본에 음성이 없어 사본도 무음이다.

| 엔딩 | 씬 | 임시 영상 | 씬에서 선택할 오브젝트 |
| --- | --- | --- | --- |
| 굿 엔딩 | `Assets/Scenes/EndingScenes/VictoryAnimation.unity` | `GoodEnding.mp4` (숲) | `Good Ending Video` |
| 배드 엔딩 | `Assets/Scenes/EndingScenes/BadScenes/BadAnimation.unity` | `BadEnding.mp4` (용암) | `Bad Ending Video` |

1. 원하는 새 영상을 Project 창에 넣고 해당 엔딩 씬을 연다.
2. 위 표의 오브젝트를 선택한다.
3. **Video Player → Video Clip**에 새 영상을 넣고 씬을 저장한다.

새 클립을 지정하면 이전 영상의 첫 장면을 표시하지 않고 검은 화면에서 새 영상의 첫 프레임을 기다린다. 준비 중에도 이미지를 표시하려면 같은 오브젝트의 **Video First Frame Display → First Frame**에 새 이미지, **First Frame Clip**에 새 클립을 함께 지정한다. 기본 화면은 16:9이며 다른 비율의 영상은 여백을 두고 맞춘다.

영상 완료 후 결과 문구와 **메인 메뉴** 버튼이 나타난다. 버튼 또는 Enter/Space로 `00MainMenu`에 돌아간다. 재생 도중에는 정상 영상을 건너뛰지 않는다.

## 오류 처리

모든 활성 컷신과 두 엔딩에서 영상 파일 누락, 디코더 오류, 준비 지연, 프레임 정지를 감지한다. 각 씬의 **Video Scene Transition → Playback Recovery**에서 시간을 조절할 수 있다.

- **Prepare Timeout**: 영상 준비 제한 시간, 기본 15초.
- **Playback Stall Timeout**: 실제 프레임이 진행되지 않는 시간, 기본 15초.
- 일시정지 중에는 두 타이머가 진행되지 않는다.
- 오류 화면의 **다시 시도 [R]**는 현재 영상을 처음부터 다시 준비한다.
- **계속 [Enter]** 또는 Space는 해당 컷신의 원래 다음 씬으로 이동한다. 엔딩에서는 메인 메뉴로 이동한다.
- **메인 메뉴 [M]**는 `00MainMenu`로 돌아간다.
- 연속 입력은 한 번만 처리하며, 설정창이 열린 동안 영상과 화면 이동 입력은 멈춘다.

일반 컷신은 기존처럼 영상 완료 후 다음 버튼을 표시하고, 선택된 버튼 그림을 0.5초 보여 준 뒤 이동한다. 진단 로그 옵션을 꺼도 오류 복구는 작동한다.

메인 메뉴의 배경 영상도 실패하거나 15초 동안 진행되지 않으면 메뉴 UI를 표시한다. 메뉴를 빠르게 오가더라도 이전 영상의 대기 작업이 남지 않는다.

## 재생 화면을 처음부터 다시 만드는 도구

`Tools → Dinosaur RPS → Build Endings And Video Recovery`는 두 엔딩 씬을 기본 임시 영상으로 다시 생성하고 활성 컷신에 복구 설정을 지정하는 개발용 도구다. **영상만 교체할 때는 실행할 필요가 없다.** 이미 바꾼 엔딩 콘텐츠도 기본값으로 다시 생성되므로 일반적인 교체는 위의 Inspector 절차를 사용한다.

## 검증

Unity 6000.2.10f1의 별도 프로젝트에서 실제 PlayMode 재생으로 검증했다. 두 엔딩의 프레임 출력·자연 완료·메뉴 버튼 이동, 정상 컷신의 완료 전 건너뛰기 방지와 0.5초 전환, 파일 오류 후 재시도, 응답 없는 영상 URL의 준비 타임아웃과 계속 진행, 멈춘 디코더의 복구와 메뉴 복귀를 통과했다. 일시정지 시간 제외, 중복 이동 방지, 메인 메뉴 배경 영상 정지 시 UI 표시도 확인했으며 검사 중 처리되지 않은 런타임 예외는 없었다.

검사에서는 타임아웃을 짧게 주입했으며 실제 씬의 기본값은 15초다. 결과와 캡처는 `.utmp/quarter-setup/EndingRecoveryFrames/`, 전체 로그는 `.utmp/ending-recovery-final.log`에 있다. 전체 게임 출시 빌드 검증을 대신하는 검사는 아니다.
