# 기존에 있던 내용은 그대로 놔두고 Player, NPC 프리팹을 생성하는 로직만 추가했습니다.

1. 변수 추가
   - `_playerPrefab`(GameObj): 플레이어 프리팹 참조
   - `_npcPrefabs`(List) : NPC 프리팹 참조
   - `_playerSpawnPoint`(transform) : 플레이어가 생성될 위치
   - `_npcSpawnPoint`(transform) : NPC가 생성될 위치
   - `_map`(transform) : 현재 사용중인 맵
   - `_sitPoints`(List<Transform>) : 앉을 자리 좌표 리스트
   - `_activeNpcs(List<NPCController>)`: 활성화된 NPC를 담아두는 List
   - `_officeMaps(List<GameObject>)`: 사무실 맵 프리팹을 담아둘 List
   - `_currentMapIndex`(int): 현재 활성화된 맵

2. 함수 추가
   1. `GenerateMap()`
     - `_officeMaps`의 첫 번째 맵만 활성화 시킴
   2. `InitializeGameAsync()`
     - _playerSpawnPoint에 플레이어 프리팹을 생성한 뒤 UniTask로 1초 대기
   3. `RefreshSitPoints()`
     - 사용 중인 맵의 자식 오브젝트 중 Seat.cs가 참조된 오브젝트를 찾아서 그 오브젝트에 붙어있는 SitPoint를 리스트에 담음
   4. `UpgradeOffice()`
     - 현재 인덱스 다음 맵을 활성화 시키고 현재 맵은 비활성화, 의자 좌표(`_sitPoints`)초기화 및 NPC가 떠났다가 맵에 맞게 새로 생성
   5. `SpawnNPCsAsync`
     - 고용 인원과(`_haveEmployees`) 의자 수(맵 프리팹 하위에 있는 `Seat.cs`가 붙은 의자 수) 중 적은 쪽을 기준으로 NPC 생성 후 의자 좌표로 이동시킴, 유니테스크로 1초마다 생성되도록 제어
     - NPC는 `_EmployeeManager.Instance.employeeList`에 있는 프리팹을 조회해서 생성함 
   6. `LeaveWorkNPCs()`
     - NPC들을 `NPCLeave.cs`상태로 바꿔 퇴근 애니메이션을 재생하는 함수
     - 퇴근하는 NPC의 MutableData를 `_EmployeeManager.cs`에 있는 원본 데이터 `haveEmployeeList`에 업데이트 시키고 사라짐

## 유니티 에디터에서 설정할 것
- GameManager 오브젝트에서 Player Spawn Point, NPC Spawn Point 두 개 모두 Map Center 하위에 있는 PlayerSpawnPoint를 할당
- Map에는 MapCenter를 할당
- NPC가 앉을 의자에 `Seat.cs`컴포넌트 참조하고 `SitPoint`지정