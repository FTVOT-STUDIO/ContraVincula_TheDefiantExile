using UnityEngine;

/// <summary>
/// MonoBehaviour 기반 싱글톤의 등록과 생명주기를 관리합니다.
/// 실제 초기화 순서는 GameBootstrap이 관리합니다.
/// </summary>
public abstract class SingletonBase<T> : Manager
    where T : SingletonBase<T>
{
    private static T instance;
    private static bool isQuitting;
    private static uint sessionVersion;

    public static T Instance
    {
        get
        {
            EnsureCurrentSession();
            if (isQuitting)
                return null;

            if (instance == null)
                Debug.LogError($"{typeof(T).Name}이 등록되지 않았습니다. GameBootstrap 초기화 순서를 확인하세요.");

            return instance;
        }
    }

    public static bool HasInstance
    {
        get
        {
            EnsureCurrentSession();
            return instance != null && !isQuitting;
        }
    }

    public static bool TryGetInstance(out T value)
    {
        EnsureCurrentSession();
        value = isQuitting ? null : instance;

        return value != null;
    }

    public override bool Register()
    {
        EnsureCurrentSession();
        T current = (T)this;

        if (instance != null)
        {
            if (instance == current)
                return false;

            //            Debug.LogWarning($"{typeof(T).Name}이 중복으로 등록되었습니다.");

            Destroy(gameObject);
            return false;
        }

        instance = current;
        return true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    protected virtual void OnApplicationQuit()
    {
        EnsureCurrentSession();
        isQuitting = true;
    }

    private static void EnsureCurrentSession()
    {
        if (sessionVersion == SingletonRuntimeSession.Version) return;
        sessionVersion = SingletonRuntimeSession.Version;
        instance = null;
        isQuitting = false;
    }
}

// 자동 실행은 일반 타입에서 처리하고 각 싱글톤은 직접 호출 시 새 세션 상태를 반영합니다.
internal static class SingletonRuntimeSession
{
    internal static uint Version { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        unchecked { Version++; }
    }
}
