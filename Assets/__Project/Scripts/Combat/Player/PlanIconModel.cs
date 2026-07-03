namespace Combat.Player
{
    /// <summary>
    /// One icon in a unit's overhead plan row: a queued player ability, a committed enemy
    /// ability, or a committed enemy move (glyph). Pure model — the view resolves visuals.
    /// </summary>
    public readonly struct PlanIconModel
    {
        public int UnitId { get; }
        public int AbilityId { get; }
        public int QueueIndex { get; }
        public bool IsEnemyIntent { get; }
        public bool IsMoveIntent { get; }

        private PlanIconModel(int unitId, int abilityId, int queueIndex, bool isEnemyIntent, bool isMoveIntent)
        {
            UnitId = unitId;
            AbilityId = abilityId;
            QueueIndex = queueIndex;
            IsEnemyIntent = isEnemyIntent;
            IsMoveIntent = isMoveIntent;
        }

        public static PlanIconModel QueuedAbility(int unitId, int abilityId, int queueIndex) =>
            new PlanIconModel(unitId, abilityId, queueIndex, isEnemyIntent: false, isMoveIntent: false);

        public static PlanIconModel EnemyAbilityIntent(int unitId, int abilityId) =>
            new PlanIconModel(unitId, abilityId, queueIndex: 0, isEnemyIntent: true, isMoveIntent: false);

        public static PlanIconModel EnemyMoveIntent(int unitId) =>
            new PlanIconModel(unitId, abilityId: -1, queueIndex: 0, isEnemyIntent: true, isMoveIntent: true);
    }
}
