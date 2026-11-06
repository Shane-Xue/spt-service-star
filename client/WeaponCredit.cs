using EFT.InventoryLogic;

namespace ServiceStars
{
    internal static class WeaponCredit
    {
        // Launcher is a GearMod, ThrowWeap and knives are Items, not Weapons.
        // This predicate is shared by kill capture and the inspect window.
        internal static bool IsSupported(Item item)
        {
            return item != null && (item is Weapon || item is ThrowWeap || item is Launcher ||
                item.GetItemComponent<KnifeComponent>() != null);
        }
    }
}
