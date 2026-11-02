using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 오디오 데이터 공통 항목입니다. name으로 enum이 자동 생성됩니다.
/// </summary>
[System.Serializable]
public abstract class AudioEntry
{
    // 첫 번째 string 필드라서 리스트 항목 제목으로도 표시됩니다
    [Tooltip("enum 이름으로 사용됩니다. 영문, 숫자, _ 만 사용하십시오.")]
    public string name;

    // enum 숫자 값 (자동 부여). 기존 bgm/sfx/env 필드에 저장된 값을 그대로 이어받습니다
    [FormerlySerializedAs("bgm")]
    [FormerlySerializedAs("sfx")]
    [FormerlySerializedAs("env")]
    [HideInInspector] public int id;

    public AudioClip audioClip;
}

[System.Serializable]
public class SfxData : AudioEntry
{
    [Min(1)] public int maxPlayCount = 1;
    [Tooltip("중첩 상한에서는 기존 소리를 재시작하고, 채널이 가득 차면 낮은 우선순위 소리를 교체합니다.")]
    public bool prioritizePlayback;

    // 기존 코드 호환용 (data.sfx 그대로 사용 가능)
    public SfxType sfx => (SfxType)id;
}

[System.Serializable]
public class BgmData : AudioEntry
{
    // 기존 코드 호환용
    public BgmType bgm => (BgmType)id;
}

[System.Serializable]
public class EnvData : AudioEntry
{
    // 기존 코드 호환용
    public EnvType env => (EnvType)id;
}