# 우편 분류소 HMM 통합

- 원본: origin/gganwoor 1ae53e882f5f770e54df0367e6d819b4fd570fc9. 해당 스테이지 폴더 60개 파일만 가져왔다. LFS 원본은 SHA-256과 크기를 확인했다.
- 번호: 스테이지 3. 경로: Assets/Scenes/Stages/Stage3/PostalDepot.unity. 기존 Stage1·Stage2는 유지한다.
- 길드 세 번째 준비 중 항목을 실제 우편 의뢰로 교체했다. 수락 후 출구 F 출발, 목표 완료 후 기존 길드 문 앞 복귀, 완료 표시를 연결했다. Stage1 nonRepeatable=true 유지, Stage2/3=false.
- HMM ManorStealthRules를 복제한 우편 전용 규칙을 사용한다. 주입 대기는 3초이며 Stage2의 즉시 주입을 변경하지 않는다.
- HMM 발소리·경비 모델/시야/발소리·조준 윤곽선·관계 음원/앰비언스·소리 스캔을 바인딩했다. 원본 맵과 구운 NavMesh를 다시 만들지 않았다.
- HMM 공통 코드 변경: ThiefPlayer의 minMoveDistance=0; ThiefInteractable에 IThiefInteractionHandler 선택 위임. 인터페이스가 없는 기존 오브젝트는 기존 분기를 사용한다. NPC AI/소리 코어/기존 조준 로직을 이전 버전으로 덮어쓰지 않았다.
- 편지/하역문은 공통 F 대상을 사용한다. 원본의 별도 F 입력과 프레임마다 플레이어 비활성/복구 방식은 제거했다. 읽기 중 전체 일시정지와 편지 교체는 유지한다. 분류표 안내는 해당 mission의 편지 reader를 참조한다.
- 원본 테스트의 수락·F 경로·주입 지연 가정은 갱신했으나 테스트는 실행하지 않았다.
- 사용자 Pull의 Stage2 비주얼/거울 변경(52b8bd6)을 유지했다. stage1/Stage2/공유 프리팹 변경 여부와 원본 공간 보존은 Artifacts/PostalIntegration/static-preservation.json 참조.
- 이번 확인은 컴파일만 수행한다. Play Mode, 키 입력, 동선·AI·편지·귀환 실제 실행, NavMesh 재생성은 하지 않았다. 커밋하지 않았다.
- 소스·원본 백업·설정·컴파일 기록: Artifacts/PostalIntegration.
