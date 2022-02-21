using AnythingGalleryLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackroomsTileset
{
    public class BackroomsTileset : ITileset
    {
        public string Name => "Backrooms";
        public string ResourceName => "backrooms";
        public TileData[] Tiles => new TileData[] {
            new TileData() {
                Name = "BackroomsEntrance",
                Tilesets = new string[]{ "Backrooms", "ModernGallery" }
            },
            new TileData() {
                Name = "BackroomsHallway_01",
                Tilesets = new string[]{ "Backrooms" },
                ScannerCounts = new Dictionary<string, int>(){ { "image", 2 } }
            },
            new TileData() {
                Name = "BackroomsHallway_02",
                Tilesets = new string[]{ "Backrooms" },
                ScannerCounts = new Dictionary<string, int>(){ { "image", 1 }, { "info", 1 } }
            },
        };

        public void AttachMissingComponents(GameObject obj)
        {
            if (obj.name == "BackroomsEntrance")
            {
                Light doorLight = (Light)(obj.transform.Find("Spot Light")?.gameObject?.GetComponent<Light>());
                Jigsaw.Connector connector = (Jigsaw.Connector)(obj.transform.Find("Connectors")?.Find("Connector")?.gameObject?.GetComponent<Jigsaw.Connector>());

                if (connector != null && doorLight != null)
                {
                    BackroomsEntrance behavior = (BackroomsEntrance)obj.AddComponent(typeof(BackroomsEntrance));
                    behavior.DoorLight = doorLight;
                    behavior.DoorConnector = connector;
                }
            }
        }
    }
}
