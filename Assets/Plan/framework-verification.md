# 공통 스테이지 기반 검증 기록

검증일: 2026-10-28. Unity 6000.3.19f1. 기획 기준: D:/wiki main 595095c 및 사용자의 팀 공통 기반 범위 정정.

## 확인한 결과

- 공통 프리팹 8종: PlayerRig, StageSystems, Guard, Door, Key, Collectible, Resonator, Exit. 예제 씬에서 해당 프리팹을 참조한다.
- StageDefinition/StageInventory/StageContext로 씬별 제목·목표·아이템·참조를 분리했다. Runtime에는 특정 저택 제목이나 씬 경로가 없다.
- FrameworkExample은 copper_key, relic_a, relic_b를 사용한다. 저택의 key/objective와 별개이며 같은 공통 코드로 실행된다.
- PlayMode **18/18 통과**, 18.17초. 기존 저택 13개 + 공통 기반 5개. 원본은 framework-test-results.json.
- 공통 검사: 씬 참조 연결·설정 검증, 다른 키로 문 개방 차단, 목표 2개 중 일부만 소지한 상태에서 완료 차단, 전부 소지 후 탈출, 능력 사용 이력과 완료 조건 분리, 같은 씬 재시작과 소지품·문·획득 상태 초기화, 작성한 문 회전 기준 유지, 중복 아이템 ID 거부, 잘못된 목표·중복 컨텍스트·HUD 누락 탐지.
- 빈 씬에 Install 메뉴 실행 → StageTemplate 저장 → 닫고 다시 열기 후 오류 0, definition=StageTemplate, 목표물 위치=(0,1,4) 유지. 도구 중복 설치도 거부됨을 확인했다.
- 실제 실행 프레임 1577 → 1604, wait 조건 met=true. 한글 목표 안내·소지품·조작 표시와 공통 예제 화면을 확인했다. Images/framework-example.png.
- 실행 콘솔 오류 0 / 경고 0. 최종 예제 설정 검증 오류 0. 편집 모드로 종료, 씬 dirty=false. 임시 백그라운드 실행 설정 복구.

## 함께 개선한 문제

프리팹 인스턴스의 설정·위치 변경을 저장하지 않던 설치 도구 문제를 발견해 RecordPrefabInstancePropertyModifications로 보완했다. 재로드와 별도 빈 씬 설치로 재검증했다. 문 회전은 배치된 닫힘 회전을 기준으로 하며, 의뢰 재시작은 담당 씬 경로를 사용한다. 중복 설치·잘못된 참조·필수 아이템 누락·출구 누락·Collider 누락·잘못된 소리 수치·중복 AudioListener·NavMesh 밖 경비를 검사한다.

## 현재 범위와 이력

길드·훈련장은 다른 팀원 담당이다. 공통 실행 예제를 빌드 목록의 첫 씬으로 설정했다. 이후 2026-10-28 사용자의 명시적 삭제 요청에 따라 길드 씬과 빌드 참조를 제거했다. 원래 제거돼 있던 sound_thief_wiki 파일과 사용자 변경을 복원하지 않았다.

과거 길드 전용 생성기와 테스트는 공통 구현에서 사용하지 않는다. 길드 전용 기획서는 삭제했다. 팀원용 작업 절차는 stage-authoring.md, 계획은 framework-development.md, 실행 프롬프트는 development-prompt.md다.

이번 검증은 PC 단일 플레이어·단일 활성 스테이지·수집 후 탈출 의뢰에 대한 것이다. 정식 미술/음향/글꼴, 저장, 네트워크, 특수 목표 유형 및 완성 스테이지 제작은 후속 범위다. Unity가 직렬화한 YAML에는 빈 값 뒤 공백이 있어 전체 git diff --check는 경고를 출력하며, C#·문서에는 해당 공백 오류가 없다.
최종 추가 검사: startImmediately=false가 이전 startsAccepted=true보다 우선하며 외부 Accept 호출로 정상 시작하는 것을 확인했다. 최종 18개 전체 통과.
