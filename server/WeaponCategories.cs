using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace ServiceStars.Server;

public static class WeaponCategories
{
    // LAUNCHER includes underbarrel attachments, which are outside WEAPON.
    private static readonly MongoId[] Supported = [BaseClasses.WEAPON, BaseClasses.THROW_WEAP,
        BaseClasses.KNIFE, BaseClasses.LAUNCHER];
    public static bool Accepts(string template, Func<MongoId, MongoId, bool> isOfBaseclass)
    {
        var id = new MongoId(template);
        foreach (var category in Supported)
            if (isOfBaseclass(id, category)) return true;
        return false;
    }
}
