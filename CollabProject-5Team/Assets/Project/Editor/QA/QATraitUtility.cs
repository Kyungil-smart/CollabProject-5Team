namespace GameDevTycoon.EditorQA
{
    public static class QATraitUtility
    {
        public static bool TryGetExpectedTraitRole(Role role, out TraitRole traitRole)
        {
            switch (role)
            {
                case Role.PLANNER:
                    traitRole = TraitRole.Planning;
                    return true;
                case Role.PROGRAMMER:
                    traitRole = TraitRole.Develop;
                    return true;
                case Role.ARTIST:
                    traitRole = TraitRole.Art;
                    return true;
                default:
                    traitRole = default;
                    return false;
            }
        }

        public static bool TryGetTraitData(Trait trait, out TraitData data)
        {
            data = null;
            if (!TraitTable.All.ContainsKey(trait))
                return false;

            data = TraitTable.Get(trait);
            return data != null;
        }
    }
}
