using UnityEngine;

/// <summary>
/// 게임의 배경음악, 효과음, 환경음 재생과 볼륨을 관리하는 오디오 매니저입니다.
/// </summary>
public partial class AudioManager : SingletonBase<AudioManager>
{
    [Header("# 오디오 데이터베이스 ====")]
    [SerializeField] private AudioDatabase audioDatabase;

    [Header("# 배경음악 설정 ====")]
    [SerializeField, Range(0f, 1f)]
    private float bgmVolume = 1f;

    [Header("# 효과음 설정 ====")]
    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 1f;

    [SerializeField, Min(1)]
    private int channels = 10;

    [Header("# 환경음 설정 ====")]
    [SerializeField, Range(0f, 1f)]
    private float envVolume = 1f;

    private AudioSource bgmPlayer;
    private AudioSource envPlayer;
    private AudioSource[] sfxPlayers = System.Array.Empty<AudioSource>();
    private int channelIndex;
    private double[] sfxStartedAt = System.Array.Empty<double>();
    private bool[] sfxPrioritized = System.Array.Empty<bool>();
    public bool IsReady => bgmPlayer != null;
    /// <summary>
    /// 배경음악, 효과음, 환경음 플레이어와 오디오 필터를 생성 및 초기화합니다.
    /// </summary>
    /// 

    public override void Initialize()
    {

    }

    // 임시
    void Awake()
    {
        Init();
        Register();
    }

    void Init()
    {
        if (bgmPlayer != null) return;
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        if (bgmPlayer != null) bgmPlayer.volume = bgmVolume;

        GameObject envObject = new GameObject("EnvPlayer");
        envObject.transform.parent = transform;
        envPlayer = envObject.AddComponent<AudioSource>();
        envPlayer.playOnAwake = false;
        envPlayer.loop = true;
        if (envPlayer != null) envPlayer.volume = envVolume;

        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[Mathf.Max(1, channels)];
        sfxStartedAt = new double[sfxPlayers.Length];
        sfxPrioritized = new bool[sfxPlayers.Length];

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
        }
        ApplySettings();
        PlayBgm(BgmType.Main_Theme);
        PlayEnv(EnvType.Wind);
    }

    /// <summary>
    /// 지정된 환경음을 재생합니다.
    /// </summary>
    /// <param name="env">재생할 환경음 타입입니다.</param>
    public void PlayEnv(EnvType env)
    {
        if (audioDatabase == null || envPlayer == null) return;
        EnvData data = audioDatabase.GetEnvData(env);
        if (data == null || data.audioClip == null) return;

        envPlayer.clip = data.audioClip;
        envPlayer.Play();
    }

    public void PlayBgm(BgmType bgm)
    {
        if (audioDatabase == null || bgmPlayer == null) return;
        BgmData data = audioDatabase.GetBgmData(bgm);
        if (data == null || data.audioClip == null) return;

        bgmPlayer.clip = data.audioClip;
        bgmPlayer.Play();
    }

    /// <summary>
    /// 지정된 효과음을 사용 가능한 채널에서 재생합니다.
    /// 동일 효과음의 최대 동시 재생 개수를 제한할 수 있습니다.
    /// </summary>
    /// <param name="sfx">재생할 효과음 타입입니다.</param>
    /// <param name="maxPlayCount">
    /// 동일 효과음의 최대 동시 재생 개수입니다.
    /// 0 이하이면 효과음 데이터에 설정된 값을 사용합니다.
    /// </param>
    public void PlaySfx(SfxType sfx, int maxPlayCount = 0)
    {
        if (audioDatabase == null || sfxPlayers.Length == 0) return;
        SfxData data = audioDatabase.GetSfxData(sfx);
        if (data == null || data.audioClip == null) return;
        AudioClip targetClip = data.audioClip;
        int limit = Mathf.Max(1, maxPlayCount <= 0 ? data.maxPlayCount : maxPlayCount);
        int playingCount = 0;
        int oldestMatch = -1;
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            if (!sfxPlayers[i].isPlaying || sfxPlayers[i].clip != targetClip) continue;
            playingCount++;
            if (oldestMatch < 0 || sfxStartedAt[i] < sfxStartedAt[oldestMatch]) oldestMatch = i;
        }
        if (playingCount >= limit)
        {
            if (data.prioritizePlayback) PlayOnSfxChannel(oldestMatch, data);
            return;
        }

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int index = (channelIndex + i) % sfxPlayers.Length;
            if (sfxPlayers[index].isPlaying) continue;
            PlayOnSfxChannel(index, data);
            return;
        }
        if (!data.prioritizePlayback) return;

        // UI 피드백은 반복 클릭음을 먼저 교체하고, 모두 중요하면 가장 오래된 소리를 교체합니다.
        int replacement = -1;
        for (int i = 0; i < sfxPlayers.Length; i++)
            if (replacement < 0 || (sfxPrioritized[replacement] && !sfxPrioritized[i]) ||
(sfxPrioritized[replacement] == sfxPrioritized[i] && sfxStartedAt[i] < sfxStartedAt[replacement]))
                replacement = i;
        PlayOnSfxChannel(replacement, data);
    }

    private void PlayOnSfxChannel(int index, SfxData data)
    {
        if (index < 0 || index >= sfxPlayers.Length) return;
        AudioSource source = sfxPlayers[index];
        source.Stop();
        source.clip = data.audioClip;
        source.Play();
        sfxStartedAt[index] = Time.unscaledTimeAsDouble;
        sfxPrioritized[index] = data.prioritizePlayback;
        channelIndex = (index + 1) % sfxPlayers.Length;
    }

    /// <summary>
    /// 현재 재생 중인 환경음을 정지합니다.
    /// </summary>
    public void StopEnv()
    {
        if (envPlayer != null && envPlayer.isPlaying)
        {
            envPlayer.Stop();
        }
    }

    /// <summary>
    /// 지정된 타입의 효과음을 모두 정지합니다.
    /// </summary>
    /// <param name="sfx">정지할 효과음 타입입니다.</param>
    public void StopSfx(SfxType sfx)
    {
        if (audioDatabase == null || sfxPlayers.Length == 0) return;
        SfxData data = audioDatabase.GetSfxData(sfx);
        if (data == null || data.audioClip == null) return;
        foreach (AudioSource sfxSource in sfxPlayers)
        {
            if (sfxSource.isPlaying &&
                sfxSource.clip == data.audioClip)
            {
                sfxSource.Stop();
            }
        }
    }

    /// <summary>
    /// 현재 재생 중인 모든 효과음을 정지합니다.
    /// </summary>
    public void StopAllSfx()
    {
        foreach (AudioSource sfxSource in sfxPlayers)
        {
            if (sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
        }
    }

    /// <summary>
    /// 배경음악 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">설정할 볼륨 값입니다. 0에서 1 사이로 제한됩니다.</param>
    public void SetBgmVolume(float volume)
    {

    }

    /// <summary>
    /// 현재 배경음악 볼륨을 반환합니다.
    /// </summary>
    /// <returns>현재 배경음악 볼륨입니다.</returns>
    public float GetBgmVolume()
    {
        return 0;
    }

    /// <summary>
    /// 환경음 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">설정할 볼륨 값입니다. 0에서 1 사이로 제한됩니다.</param>
    public void SetEnvVolume(float volume)
    {

    }

    /// <summary>
    /// 현재 환경음 볼륨을 반환합니다.
    /// </summary>
    /// <returns>현재 환경음 볼륨입니다.</returns>
    public float GetEnvVolume()
    {
        return 0;
    }

    /// <summary>
    /// 효과음 볼륨을 설정하고 모든 효과음 채널에 적용합니다.
    /// </summary>
    /// <param name="volume">설정할 볼륨 값입니다. 0에서 1 사이로 제한됩니다.</param>
    public void SetSfxVolume(float volume)
    {

    }

    /// <summary>
    /// 현재 효과음 볼륨을 반환합니다.
    /// </summary>
    /// <returns>현재 효과음 볼륨입니다.</returns>
    public float GetSfxVolume()
    {
        return 0;
    }
    private void ApplySettings()
    {

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

}

