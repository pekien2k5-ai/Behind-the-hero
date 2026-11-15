using BTH.Core.Ai;

namespace BTH.Core.Run
{
    public readonly struct IntentDeclaredEvent
    {
        public readonly EnemyIntent Intent;

        public IntentDeclaredEvent(EnemyIntent intent)
        {
            Intent = intent;
        }
    }
}
