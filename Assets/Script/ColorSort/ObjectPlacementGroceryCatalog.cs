/// <summary>
/// Object Placement category map (bible):
/// Yellow=Fruits, Red=Vegetables, Blue=Beverages, Green=Fragile, Brown=Gadgets.
/// Box color is the category label — items are not required to match that skin color.
/// Beverages: sealed/to-go only (no glassware). Fragile: breakable non-drink only.
/// </summary>
public enum ObjectPlacementGroceryKind
{
    Fruit = 0,
    Vegetable = 1,
    Beverage = 2,
    Fragile = 3,
    Gadget = 4
}

public static class ObjectPlacementGroceryCatalog
{
    public const int PhaseISetCount = 3;
    public const int PhaseIItemsPerSet = 10;
    public const int PhaseITotalItems = PhaseISetCount * PhaseIItemsPerSet;

    public const string ItemsRoot = "Assets/ObjectPlacement/Items";

    public static readonly string[] CategoryFolders =
    {
        "01_Fruits_Yellow",
        "02_Vegetables_Red",
        "03_Beverages_Blue",
        "04_Fragile_Green",
        "05_Gadgets_Brown"
    };

    public static string FolderFor(ObjectPlacementGroceryKind kind)
    {
        int i = (int)kind;
        if (i < 0 || i >= CategoryFolders.Length)
            return CategoryFolders[0];
        return CategoryFolders[i];
    }

    public static string DisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "Item";

        string cleaned = id;
        if (cleaned.StartsWith("Fruit_")) cleaned = cleaned.Substring(6);
        else if (cleaned.StartsWith("Veg_")) cleaned = cleaned.Substring(4);
        else if (cleaned.StartsWith("Drink_")) cleaned = cleaned.Substring(6);
        else if (cleaned.StartsWith("Fragile_")) cleaned = cleaned.Substring(8);
        else if (cleaned.StartsWith("Gadget_")) cleaned = cleaned.Substring(7);

        cleaned = cleaned.Replace('_', ' ').Replace('-', ' ');
        char[] chars = cleaned.ToCharArray();
        bool cap = true;
        for (int i = 0; i < chars.Length; i++)
        {
            if (cap && char.IsLetter(chars[i]))
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                cap = false;
            }
            else if (chars[i] == ' ')
            {
                cap = true;
            }
        }

        return new string(chars);
    }
}
