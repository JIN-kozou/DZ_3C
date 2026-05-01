namespace DZ_3C.AI.Core
{
    public interface IMonsterAttack
    {
        void PerformAttack(AITargetable target, float baseDamage, float aoeRadius, string buffId);
    }
}
