using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AnythingGalleryLoader
{
    public class TileData
    {
        public string Name { get; set; }
        public string[] Tilesets { get; set; }
        public Dictionary<string, int> ScannerCounts { get; set; }
    }
    public interface ITileset
    {
        string Name { get; }
        string ResourceName { get; }
        TileData[] Tiles { get; }
        void AttachMissingComponents(GameObject obj);
    }
}