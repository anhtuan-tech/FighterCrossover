using UnityEngine;

public static class SelectionData
{
    // Player 1 Selections
    public static string characterImageUrl1 { get; set; }
    public static string characterPrefabUrl1 { get; set; }
    public static string supportImageUrl1 { get; set; }
    public static string supportPrefabUrl1 { get; set; }


    // Player 2 Selections
    public static string characterImageUrl2 { get; set; }
    public static string characterPrefabUrl2 { get; set; }
    public static string supportImageUrl2 { get; set; }
    public static string supportPrefabUrl2 { get; set; }

    // Map Details (If you decide to extract background/platforms dynamically later)
    public static string background { get; set; }
    public static string mapLocation { get; set; }
}