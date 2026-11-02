using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 사용하는 배경음악, 효과음, 환경음 데이터를 관리하는 오디오 데이터베이스입니다.
/// </summary>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Game Data/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    [Header("# 배경음악 ====")]
    [SerializeField] private List<BgmData> bgmDatas = new();

    [Header("# 효과음 ====")]
    [SerializeField] private List<SfxData> sfxDatas = new();

    [Header("# 환경음 ====")]
    [SerializeField] private List<EnvData> envDatas = new();

    /// <summary>
    /// 등록된 배경음악 데이터 목록을 읽기 전용으로 반환합니다.
    /// </summary>
    public IReadOnlyList<BgmData> BgmDatas => bgmDatas;

    /// <summary>
    /// 등록된 효과음 데이터 목록을 읽기 전용으로 반환합니다.
    /// </summary>
    public IReadOnlyList<SfxData> SfxDatas => sfxDatas;

    /// <summary>
    /// 등록된 환경음 데이터 목록을 읽기 전용으로 반환합니다.
    /// </summary>
    public IReadOnlyList<EnvData> EnvDatas => envDatas;

    /// <summary>
    /// 지정된 배경음악 타입에 해당하는 데이터를 반환합니다.
    /// </summary>
    /// <param name="bgm">조회할 배경음악 타입입니다.</param>
    /// <returns>일치하는 배경음악 데이터이며, 존재하지 않으면 null을 반환합니다.</returns>
    public BgmData GetBgmData(BgmType bgm)
    {
        foreach (BgmData data in bgmDatas)
        {
            if (data.bgm == bgm)
            {
                return data;
            }
        }
        return null;
    }

    /// <summary>
    /// 지정된 효과음 타입에 해당하는 데이터를 반환합니다.
    /// </summary>
    /// <param name="sfx">조회할 효과음 타입입니다.</param>
    /// <returns>일치하는 효과음 데이터이며, 존재하지 않으면 null을 반환합니다.</returns>
    public SfxData GetSfxData(SfxType sfx)
    {
        foreach (SfxData data in sfxDatas)
        {
            if (data.sfx == sfx)
            {
                return data;
            }
        }
        return null;
    }

    /// <summary>
    /// 지정된 환경음 타입에 해당하는 데이터를 반환합니다.
    /// </summary>
    /// <param name="env">조회할 환경음 타입입니다.</param>
    /// <returns>일치하는 환경음 데이터이며, 존재하지 않으면 null을 반환합니다.</returns>
    public EnvData GetEnvData(EnvType env)
    {
        foreach (EnvData data in envDatas)
        {
            if (data.env == env)
            {
                return data;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 인스펙터 값이 바뀔 때마다 id 중복 정리와 이름 자동 채우기를 수행합니다.
    /// </summary>
    private void OnValidate()
    {
        FixEntries(bgmDatas, typeof(BgmType));
        FixEntries(sfxDatas, typeof(SfxType));
        FixEntries(envDatas, typeof(EnvType));
    }

    // + 버튼으로 복제된 항목의 중복 id를 새 번호로 바꾸고, 기존 enum 항목은 이름을 채움
    private static void FixEntries<T>(List<T> list, System.Type enumType) where T : AudioEntry
    {
        int nextId = 1;
        foreach (T e in list)
        {
            if (e != null && e.id >= nextId) nextId = e.id + 1;
        }

        var used = new HashSet<int>();
        foreach (T e in list)
        {
            if (e == null) continue;

            // 중복 id → 새 번호 부여
            if (!used.Add(e.id))
            {
                e.id = nextId++;
                used.Add(e.id);
            }

            // 기존 enum 값에서 넘어온 항목은 enum 이름을 그대로 가져옴
            if (string.IsNullOrEmpty(e.name) && System.Enum.IsDefined(enumType, e.id))
            {
                e.name = System.Enum.GetName(enumType, e.id);
            }
        }
    }
#endif
}