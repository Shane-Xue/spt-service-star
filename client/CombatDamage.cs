namespace ServiceStars
{
    public static class CombatDamage
    {
        // Explicit combat causes only: a stale weapon reference must not turn
        // starvation, falling or other environmental deaths into weapon credit.
        public static bool IsSupported(string cause)
        {
            switch (cause)
            {
                case "Bullet": case "Blunt": case "Melee":
                case "Explosion": case "GrenadeFragment": case "Landmine":
                case "LightBleeding": case "HeavyBleeding":
                case "Flame": case "HotGases": case "ThermobaricExplosion":
                case "Poison": case "LethalToxin":
                    return true;
                default: return false;
            }
        }
    }
}
