# JinHo 구현 설명

현재 기준은 [공용 협업 안내](JinHo_독립%20시스템%20범위와%20협업%20안내.md)다. 이 문서는 구현된 연결을 설명하며 전체 플레이 합격을 뜻하지 않는다.

## 유지하는 자산

| 자산 | 역할 |
| --- | --- |
| `Assets/JinHo/Scenes/JinHo.unity` | 현재 사용자 편집 맵과 기존 게임 시스템 인스턴스 |
| `Assets/JinHo/Scenes/JinHoSystemsValidation.unity` | 소리 전달·각성 기능을 분리한 작은 검증 환경 |
| `Assets/JinHo/Navigation/`의 2개 NavMesh | 위 두 씬의 전용 이동 데이터 |
| `Assets/JinHo/Settings/JinHo Rules.asset` | 원본 규칙을 복제한 스테이지 설정. 경비 속도만 2.2→3 |
| `Assets/JinHo/Settings/JinHo Validation.asset` | 공유 5개 씬과 현재 JinHo·작은 검증 씬을 포함하는 전용 Build Profile |
| `Runtime/JinHoStageBindings.cs` | 원본 SoundWorld에 씬 전용 전달 정책 등록, G1/G2 각성 설정 |
| `Runtime/JinHoSoundPropagation.cs` | 벽·문·열린 통로를 반영한 청각 전달과 플레이어 음량 감쇠 |
| `Runtime/JinHoRoomStay.cs` | 유인1 추가 유지와 유인2 전신 진입·역순 퇴장 정책 |
| `Runtime/JinHoMirror.cs`, `Shaders/PlanarMirror.shader`, `Materials/G2 Mirror.mat` | 실제 씬을 반사하는 거울. Player 카메라 교체 없음 |
| `Editor/JinHoGeometrySync.cs` | 현재 벽에서 음향/방 판정 갱신 및 전용 NavMesh 재구성 |
| `Editor/JinHoStageBuilder.cs` | 작은 검증 환경 생성/열기, 전용 프로필 연결 |
| `Tests/EditMode/PropagationTests.cs` | 소리 전달 계산·정책 등록 검사 |
| `Tests/PlayMode/JinHoSystemsTests.cs` | 작은 검증 환경의 기능 검사 |
| `Tests/PlayMode/JinHoCurrentRevisionTests.cs` | 현재 맵의 관찰·유인·퇴장 검사. 알려진 실패 포함 |

표의 Runtime/Editor/Tests 등 생략된 경로는 `Assets/JinHo/` 기준이다. `.meta`와 어셈블리 정의 `.asmdef`도 필요하므로 함께 보존한다.

## 실제 호출과 기존 참조

원본 `StageContext.Bind()`가 `ThiefPlayer`, `ThiefHud`, `MissionDirector`, `StageInventory`, `SoundWorld`, `GuardAgent`의 기존 타입과 참조를 연결한다. JinHo 바인딩은 이 타입을 교체하거나 복사하지 않는다.

기존 추출/주입 → 기존 SoundEmitter 출력 → `SoundWorld.Pulse()` → 선택적 `ISoundPropagationPolicy.AllowsHearing()` → 원본 `GuardAgent.Hear()` 순서다. 전달 정책은 기존 호출을 허용하거나 차단하며 직접 Hear를 다시 호출하지 않는다. 원본 Hear의 등급별 직선 거리 제한과 각성/우선순위 조건도 남는다.

같은 Pulse에서 원래 AudioSource를 한 번 생성하고 `AudioGain()`의 배율을 음량에 적용한다. 원래 3D 거리 감쇠·출력 시간·시각 효과는 유지한다. 다른 소리를 만들어 감쇠를 흉내 내지 않는다.

경비는 원래 Update에서 시야·기절·패닉 처리를 먼저 수행한다. 소리 조사 상태에만 `IGuardInvestigationPolicy.Evaluate()`가 연결된다. Original은 원래 조사, Hold는 기존 목표로 계속 이동/도착 시 정지, ReturnHome은 저장된 원위치로 기존 복귀 처리를 뜻한다. 정책은 비공개 상태를 강제로 변경하지 않는다.

두 정책 모두 같은 씬의 소유자 하나만 등록할 수 있고 해당 소유자만 해제한다. 정책 미연결 시 기존 흐름을 사용한다. 공통 코드 변경은 승인된 SoundWorld/GuardAgent 선택적 연결부와 각성 설정이며 코드 사본/기존 Player·HUD·미션 대체는 없다.

## 수치와 주의할 의미

- 청각 경로는 XZ 공간 구역과 연결점의 최단 비용으로 계산한다. 닫힌 경계 하나당 경로 비용 12m와 재생 배율 0.25를 누적한다. 열린 우회 경로의 추가 청각 여유는 9m이며 원본 직선 거리 한도를 제거하지 않는다.
- G1은 처음 수면 후 각성을 유지하고 G2는 초기·복귀 후 각성을 유지한다. 별도 순찰 동선은 없다.
- 유인1 `firstExtraSeconds=2`는 원본 출력 종료 후 조사 목표를 추가 유지하는 시간이다. 원본 Alert 4초와 실제 이동을 포함한 전체 방 체류를 2초로 보장하는 값이 아니다.
- 유인2 `staySeconds=3`은 각 경비의 전신 입장부터 계산한다. G1은 최소 시간을 채워도 G2의 전신 퇴장을 기다린다. 퇴장 중 같은 출력에 재유인되는 처리는 억제한다.
- 오른쪽 소리방 상·하단 벽 가로는 5.423265, 중심 X=-4.2117, 문4/문틀 X=-6.923332다. 기기·경비·Player 위치는 이 폭 조정에서 유지했다.
- 오른쪽 열쇠방 구획벽 두께 2m, 단일 틈 범위 X[-9.30,-8.40], Y[1.76,1.90], Z=15.8이다. 다른 틈/투명 관찰창은 폐기했다. 거울의 문3 이전 관찰 목적은 아직 충족 여부를 입증하지 못했다.

## 편집·실행 절차

현재 씬은 저장된 파일을 직접 사용한다. 삭제된 역사적 맵 빌더로 재생성하지 않는다. 벽을 바꿀 때 외부 원본 평면도와 대조한 뒤 `Sound Thief > JinHo > Sync Systems From Current Walls`로 음향·방 판정을 갱신하고, `JinHoGeometrySync.Bake()`로 전용 NavMesh를 재구성한다. 실제 참조·시야·동선 검증은 별도로 수행해야 한다. 이 정리 작업에서는 위 갱신/베이크를 새로 실행하지 않았다.

재시작에는 현재 JinHo가 씬 목록에 포함돼 있어야 한다. 전용 `JinHo Validation` 프로필을 활성화해 사용한다. 공유 씬 목록은 수정하지 않는다. 전용 프로필은 현재 씬/검증 씬을 포함하도록 정리했으나 그 후 실제 재시작·빌드는 검사하지 않았다.

테스트는 사용자 지시가 있을 때만 실행한다. 현재 맵 테스트의 원시 출력과 이미지는 프로젝트 `Temp/JinHoValidation/`에 생성되어 Git에 올라가지 않는다. 구현/테스트 소스는 보존하고 일회성 출력은 커밋하지 않는다.
