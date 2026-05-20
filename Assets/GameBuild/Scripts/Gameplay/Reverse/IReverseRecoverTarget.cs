namespace DZ_3C.Reverse
{
    /// <summary>
    /// 给 Buff 系统的 RecoverResource 路由用的解耦接口。
    /// Player.cs 只看见这个接口，不直接引用 ReverseCoreStack。
    /// </summary>
    public interface IReverseRecoverTarget
    {
        /// <summary>
        /// 从最内层（锚）开始链式补血：先填锚，锚满了再填 cores[0]、cores[1] ... cores[N-1]。
        /// 用 ReverseBatteryZone 的回血 buff 走这里。
        /// </summary>
        /// <param name="amount">本次要灌进来的总血量（正数）</param>
        void RecoverFromInnermost(float amount);
    }
}
