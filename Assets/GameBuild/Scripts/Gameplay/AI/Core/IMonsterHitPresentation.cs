namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 由关卡/预制体挂载的实现类配置受击与死亡表现（动画、特效、音效等）。
    /// 挂在与 <see cref="MonsterHurtReceiver"/> 同一物体上，并在 Inspector 中赋给「受击表现」字段。
    /// </summary>
    public interface IMonsterHitPresentation
    {
        /// <summary>每次成功扣血时调用（含致死当帧会先调本方法再调 <see cref="OnDeath"/>）。</summary>
        void OnDamaged(in MonsterDamageContext context);

        /// <summary>血量归零时调用一次。</summary>
        void OnDeath(in MonsterDamageContext context);
    }
}
