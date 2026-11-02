using UnityEngine;

public abstract class Manager : MonoBehaviour
{
    public abstract void Initialize();

    public virtual bool Register()
    {
        return true;
    }
}