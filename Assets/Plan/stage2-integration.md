# JinHo → HMM 스테이지 2 통합

2026-10-30. HMM `a0c96a1`, JinHo `23bce9f`, taehyeon `7b5cc11`을 기준으로 작업했다. HMM의 공통 런타임을 유지하고 JinHo 고유 맵과 선택적 정책을 통합했다. 스테이징·커밋·푸시는 하지 않았다.

## 결과

- 실제 의뢰 씬: `Assets/Scenes/Stages/Stage2.unity`. 제목 **두 경비의 보관실**, stageId `stage-02`. 원본 JinHo 씬은 `Assets/JinHo/Scenes/JinHo.unity`에 보존한다.
- Stage2Definition·Stage2Rules와 Stage2NavMesh 복사본을 분리했다. 규칙은 현재 HMM 저택 설정을 복제하며 원본 맵 배치·크기·이동망을 재생성하지 않는다.
- 길드에 두 번째 실제 의뢰를 등록하고 공유 빌드 씬 목록에 Stage2를 추가했다. 수락 후 출구 출발, 성공 후 길드 문 복귀, 완료 이력을 그대로 사용한다. Stage1은 재수락 불가, Stage2는 기본 재수락 가능이다.
- 기존 이동 발소리 스크립트가 참조하던 `footstep_concrete_000.ogg`가 이전 가져오기에서 누락돼 있었다. taehyeon의 원음·meta를 가져오고 길드·Stage1·Stage2 플레이어 각각 한 개의 PlayerFootsteps에 연결했다. 실제 이동 거리·웅크리기·운반에 따른 기존 피드백을 사용하며 경비 조사 Pulse를 추가하지 않는다.

## 공통 시스템에 통합한 부분

- SoundWorld: 같은 씬의 단일 소유자에게만 선택적 전달 정책 등록을 허용한다. Stage2의 방/통로 경로 정책이 AI 청각·지속음 청각 상실·재생 음량·소리 스캔의 기준이 된다. 정책이 없는 Stage1/길드는 기존 HMM 거리·벽/문 감쇠를 사용한다. 두 차폐 방식을 중첩하지 않는다.
- GuardAgent: HMM의 시야·이동·부드러운 회전·문 통과·추격 속도·기절·패닉을 유지한다. 선택적 조사 체류 정책과 최초 각성 후 각성 유지를 추가했다. `Patrolling`을 보존하고 `Watching`을 별도로 추가해 순찰과 제자리 각성을 구분한다.
- JinHoRoomStay: 유인방 체류·전신 입장·G2→G1 퇴장 순서와 같은 출력 재유인 억제를 유지한다. 특정 이름의 Renderer 대신 실제 Collider 범위와 NavMeshAgent 범위를 사용해 공통 NPC 표현과 분리했다. 플레이어 발견·기절·패닉이 체류보다 우선한다.
- JinHoMirror: 원본 거울을 유지한다. 플레이어 전신 모델은 추가하지 않았다.
- Stage2에 HMM 경비 발소리·관찰 시야와 카메라 소리 스캔을 연결했다. 기존 플레이어·HUD·인벤토리·미션 타입을 JinHo 구버전으로 교체하지 않았다.
- JinHoGeometrySync의 편집 대상에 Stage2 경로를 허용했다. 이 작업에서 메뉴 실행·재베이크는 하지 않았다.

## 확인 범위

사용자 요청대로 Play 진입, PlayMode/EditMode 테스트, 실제 동선·유인 타이밍·청취·화면 검증은 실행하지 않았다. 컴파일 확인과 편집을 위한 씬/에셋 연결만 수행했다. 원본 JinHo 및 Stage1의 공간 속성은 파일 비교로 확인했다.

HMM의 주입 지연·추격·회전·문 행동이 적용되므로 과거 JinHo 테스트의 시간·움직임 기대값을 현재 통과 기록으로 취급하지 않는다. 가져온 JinHo 검증 씬/테스트는 원본 참고 자료이며, 현재 의뢰의 실행 합격을 뜻하지 않는다. 완료 기록은 기존처럼 현재 플레이 세션 범위다.

근거는 `Artifacts/Stage2Integration/`의 import-manifest.json, scene-configuration.json, file-comparison.json, compile-status.json에 보관한다. before/에는 교체 전 파일을 보관했다.
