using System;
using System.Collections.Generic;

[Serializable]
public class CraftingRecipeConfig {
    public string RecipeID;
    public string TargetProstheticID;
    
    public CraftingCost Cost;
}

[Serializable]
public class CraftingCost {
    public int Money;
    public List<CraftingRequirement> RequiredItems = new List<CraftingRequirement>();
}

[Serializable]
public class CraftingRequirement {
    public string ConfigID;
    public int Count;
}
