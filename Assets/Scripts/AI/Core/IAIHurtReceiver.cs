namespace DZ_3C.AI.Core
{
    public interface IAIHurtReceiver
    {
        void ReceiveAIDamage(float damage, string buffId, object attacker);
    }
}
