namespace Client.Main.Core.Utilities;

public static class ItemDropPolicy
{
    // MuMain ItemCategories::IsHighValueItem and TradeRestrictions::IsDropBan.
    // Rental state is explicit because the current variable-length item protocol
    // does not encode MuMain's rental/expiration bits.
    public static string GetBlockReason(InventoryItem item, bool rented = false, bool expired = false)
    {
        if (item?.Definition == null)
            return "This item cannot be dropped.";

        var definition = item.Definition;
        int key = definition.Group * 512 + definition.Id;
        var details = item.Details;
        if (ItemDataParser.TryParseExtendedItemData(item.RawData, out var data) &&
            data.HasHarmony && data.HarmonyOption != 0)
            return "Reinforced items cannot be dropped.";

        bool valuable = MuMainDropRules.Valuable.Contains(key) || details.IsAncient ||
            details.IsExcellent || (definition.Group < 12 && System.Math.Max(item.Level, details.Level) > 6) ||
            (definition.Group == 13 && definition.Id == 20 && System.Math.Max(item.Level, details.Level) == 0);
        if (rented && !expired)
            valuable = false;
        else if (MuMainDropRules.RentalPets.Contains(key))
            valuable = rented && expired;

        if (valuable)
            return "You are not allowed to drop this expensive item.";
        if (MuMainDropRules.DropBanned.Contains(key) || (rented && MuMainDropRules.RentalDropBanned.Contains(key)))
            return "This item cannot be dropped.";
        return null;
    }
}
