using UnityEngine;

public class InGameManager : MonoBehaviour, IManager
{
    protected GameManager GameManager
    {
        get;
        private set;
    }

    public virtual void SetGameManager(GameManager gameManager)
    {
        if (gameManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: 연결하려는 GameManager가 Null입니다!");
            return;
        }
        GameManager = gameManager;
    }

    public virtual void Initialize()
    {

    }

    public virtual void Clear()
    {

    }

}
