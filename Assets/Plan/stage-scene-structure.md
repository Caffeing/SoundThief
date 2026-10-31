# 스테이지 씬·파일 구조

## 실제 게임 씬

- 스테이지 2: `Assets/Scenes/Stages/Stage2/Stage2.unity`
- 스테이지 3 우체국: `Assets/Scenes/Stages/Stage3/PostalDepot.unity`

씬 파일명과 GUID는 유지한다. Build Settings와 문자열로 지정한 경로는 새 위치로 갱신했다.

## 스테이지 전용 파일

`Assets/Game/Stages/Stage2` 및 `Assets/Game/Stages/Stage3` 아래에서 용도별로 분리한다.

- Runtime: 스테이지 전용 실행 코드
- Editor: 생성·배치·통합 도구
- Settings: 의뢰 정의, 규칙, 로컬 빌드 프로필
- Materials: 스테이지 전용 재질
- Navigation: 베이크된 NavMesh 데이터
- Documentation: 설명·검증 자료
- Tests: 해당 스테이지 테스트
- Shaders·Scenes: 필요한 스테이지에만 추가하는 전용 셰이더와 별도 검증 씬

기존 네임스페이스와 어셈블리 이름은 호환성을 위해 유지한다. 공통 기능은 Assets/Game의 공통 코드에 둔다.

## 적용 범위

이번 정리는 Project 창의 파일·폴더 구조만 대상으로 한다. 씬 내부 Hierarchy 정리는 사용자 의도와 달라 되돌렸다. 파일 이동에 따른 씬 경로와 Build Settings 갱신은 유지한다.


팀원별 기존/새 경로 및 병합 안내: [stage-file-migration-guide.md](stage-file-migration-guide.md).
