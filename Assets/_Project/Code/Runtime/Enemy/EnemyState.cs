namespace Collection.Runtime.Enemy
{
    /// <summary>敌人状态：Idle → Seek(声源) → Camp(声源)。</summary>
    public enum EnemyState
    {
        Idle = 0,
        Seek = 1,
        Camp = 2,
    }
}
