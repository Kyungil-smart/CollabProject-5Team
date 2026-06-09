# 기존에 있던 내용은 그대로 놔두고 Player, NPC 프리팹을 생성하는 로직만 추가했습니다.

1. 변수 추가
   - `_playerPrefab`(GameObj): 플레이어 프리팹 참조
   - `_npcPrefabs`(List) : NPC 프리팹 참조
   - `_playerSpawnPoint`(transform) : 플레이어가 생성될 위치
   - `_npcSpawnPoint`(transform) : NPC가 생성될 위치
   - `_map`(transform) : 현재 사용중인 맵
   - `_sitPoint`(List<Transform>) : 앉을 자리 좌표 리스트
   - `_activeNpcs(List<NPCController)`: 활성화된 NPC를 담아두는 List

2. 함수 추가
   - `InitializeGameAsync()`: _playerSpawnPoint에 플레이어 프리팹을 생성한 뒤 UniTask로 1초 대기
   - `RefreshSitPoints()`: 사용 중인 맵의 자식 오브젝트 중 Seat.cs가 참조된 오브젝트를 찾아서 그 오브젝트에 붙어있는 SitPoint를 리스트에 담음
   - `SpawnNPCsAsync`: 생성할 NPC프리팹이나 의자의 수가 SitPoint 리스트의 수보다크면 생성중단(예외처리) NPC프리팹을 순서대로 생성해서 의자 좌표로 이동시킴, 유니테스크로 1초마다 생성되도록 제어
   - `LeaveWorkNPCs()`: NPC들을 `NPCLeave.cs`상태로 바꿔 퇴근 애니메이션을 재생하는 함수

## 유니티 에디터에서 설정할 것
- GameManager 오브젝트에서 Player Spawn Point, NPC Spawn Point 두 개 모두 Map Center 하위에 있는 PlayerSpawnPoint를 참조
- Map에는 MapCenter를 참조
- NPC가 앉을 의자에 `Seat.cs`컴포넌트 참조하고 `SitPoint`지정