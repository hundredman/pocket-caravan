using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 지역 담당 시험 세션 설정. Resources/RegionSessions/{regionId}.asset이 있으면, 그 지역 씬에서 Play를 시작했을 때
    /// 본편 저장 파일 대신 전용 저장 파일·시작 자금·출신으로 진행한다 (GameManager.TryBeginRegionSession).
    /// 본편 시작 맵(빌드 0번)과 World/settings.json의 시작 설정은 바꾸지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Caravan/Region Test Session", fileName = "RegionTestSession")]
    public class RegionTestSession : ScriptableObject
    {
        public const string ResourceFolder = "RegionSessions";

        [Tooltip("RegionData.id와 같아야 한다")] public string regionId;
        [Tooltip("'처음부터 시작'으로 돌아갈 씬 이름 (빌드 목록에 있어야 함)")] public string startScene;
        [Tooltip("persistentDataPath 안의 전용 저장 파일 이름. 본편 저장 파일 이름은 허용하지 않는다")] public string saveFileName;
        public int startMoney = 100;
        public OriginData startOrigin;
        public VehicleData startVehicle;
        [TextArea] public string note;

        /// <summary>이 지역의 시험 세션 설정. 없거나 잘못되었으면 null (기존 비시작 씬 정책 유지).</summary>
        public static RegionTestSession For(RegionData region)
        {
            if (region == null || string.IsNullOrEmpty(region.id)) return null;
            var session = Resources.Load<RegionTestSession>(ResourceFolder + "/" + region.id);
            if (session == null) return null;
            if (session.regionId != region.id || !SaveSystem.IsValidSlotName(session.saveFileName))
            {
                Debug.LogWarning($"[저장] 지역 시험 세션 설정이 올바르지 않아 사용하지 않습니다: {region.id}");
                return null;
            }
            return session;
        }
    }
}
