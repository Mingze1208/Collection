namespace Collection.Runtime.Enemy
{
    /// <summary>敌人状态：Idle → Seek(声源) → Camp(声源)，驻留直到下一次听到声音。</summary>
    public enum EnemyState
    {
        Idle = 0,
        Seek = 1,
        Camp = 2,
    }
}
