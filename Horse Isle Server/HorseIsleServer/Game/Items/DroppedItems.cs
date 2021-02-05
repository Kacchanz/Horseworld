﻿using System;
using System.Collections.Generic;
using HISP.Server;

namespace HISP.Game.Items
{
    public class DroppedItems
    {
        public class DroppedItem
        {
            public DroppedItem(ItemInstance itmInstance)
            {
                if (itmInstance == null)
                    throw new NullReferenceException("How could this happen?");
                Instance = itmInstance;
            }
            public int X;
            public int Y;
            public int DespawnTimer;
            public ItemInstance Instance;
        }
        private static int epoch = 0;
        private static List<DroppedItem> droppedItemsList = new List<DroppedItem>();
        public static int GetCountOfItem(Item.ItemInformation item)
        {

            DroppedItem[] droppedItems = droppedItemsList.ToArray();
            int count = 0;
            foreach(DroppedItem droppedItem in droppedItems)
            {
                if(droppedItem.Instance.ItemId == item.Id)
                {
                    count++;
                }
            }
            return count;
        }

        public static DroppedItem[] GetItemsAt(int x, int y)
        {

            DroppedItem[] droppedItems = droppedItemsList.ToArray();
            List<DroppedItem> items = new List<DroppedItem>();
            foreach(DroppedItem droppedItem in droppedItems)
            {
                if(droppedItem.X == x && droppedItem.Y == y)
                {
                    items.Add(droppedItem);
                }
            }
            return items.ToArray();
        }
        public static void ReadFromDatabase()
        {
            DroppedItem[] items = Database.GetDroppedItems();
            foreach (DroppedItem droppedItem in items)
                droppedItemsList.Add(droppedItem); 
        }
        public static void Update()
        {
            int epoch_new = (Int32)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            DespawnItems(epoch, epoch_new);

            GenerateItems();
        }
        public static void RemoveDroppedItem(DroppedItem item)
        {
            int randomId = item.Instance.RandomId;
            Database.RemoveDroppedItem(randomId);
            droppedItemsList.Remove(item);
            
        }

        public static bool IsDroppedItemExist(int randomId)
        {
            try
            {
                GetDroppedItemById(randomId);
                return true;

            }
            catch(KeyNotFoundException)
            {
                return false;
            }
        }
        public static DroppedItem GetDroppedItemById(int randomId)
        {

            DroppedItem[] droppedItems = droppedItemsList.ToArray();

            foreach (DroppedItem item in droppedItems)
            {
                if(item.Instance.RandomId == randomId)
                {
                    return item;
                }
            }

            throw new KeyNotFoundException("Random id: " + randomId.ToString() + " not found");
            
        }
        public static void DespawnItems(int old_epoch, int new_epoch)
        {
            int removedCount = 0;
            DroppedItem[] items = droppedItemsList.ToArray();
            foreach (DroppedItem item in items)
            {
                if(new_epoch + item.DespawnTimer < old_epoch)
                {
                    if(GameServer.GetUsersAt(item.X, item.Y,true,true).Length == 0)
                    {
                        Logger.DebugPrint("Despawned Item at " + item.X + ", " + item.Y);
                        RemoveDroppedItem(item);
                        removedCount++;
                    }
                }
            }
            if(removedCount > 0)
                epoch = new_epoch;
        }

        public static void AddItem(ItemInstance item, int x, int y, int despawnTimer=1500)
        {
            DroppedItem droppedItem = new DroppedItem(item);
            droppedItem.X = x;
            droppedItem.Y = y;
            droppedItem.DespawnTimer = despawnTimer;
            droppedItemsList.Add(droppedItem);
            Database.AddDroppedItem(droppedItem);
        }
        public static void GenerateItems()
        {

            Logger.InfoPrint("Generating items, (this may take awhile on a fresh database!)");

            int newItems = 0;
            foreach (Item.ItemInformation item in Item.Items)
            {
                int count = GetCountOfItem(item);
                while (count < item.SpawnParamaters.SpawnCap)
                {

                    count++;

                    int despawnTimer = GameServer.RandomNumberGenerator.Next(900, 1500);

                    if (item.SpawnParamaters.SpawnInZone != null)
                    {
                        World.Zone spawnArea = World.GetZoneByName(item.SpawnParamaters.SpawnInZone);

                        while(true)
                        {
                            // Pick a random location inside the zone
                            int tryX = GameServer.RandomNumberGenerator.Next(spawnArea.StartX, spawnArea.EndX);
                            int tryY = GameServer.RandomNumberGenerator.Next(spawnArea.StartY, spawnArea.EndY);


                            if (World.InSpecialTile(tryX, tryY))
                                continue;

                            
                            if (Map.CheckPassable(tryX, tryY)) // Can the player walk here?
                            {
                                int TileID = Map.GetTileId(tryX, tryY, false);
                                string TileType = Map.TerrainTiles[TileID - 1].Type; // Is it the right type?

                                if (item.SpawnParamaters.SpawnOnTileType == TileType)
                                {
                                    if (GetItemsAt(tryX, tryY).Length > 25) // Max items in one tile.
                                        continue;

                                    ItemInstance instance = new ItemInstance(item.Id);
                                    AddItem(instance, tryX, tryY, despawnTimer);
                                    Logger.DebugPrint("Created Item ID: " + instance.ItemId + " in ZONE: " + spawnArea.Name + " at: X: " + tryX + " Y: " + tryY);
                                    newItems++;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                    else if (item.SpawnParamaters.SpawnOnSpecialTile != null)
                    {
                        while(true)
                        {
                            // Pick a random special tile
                            World.SpecialTile[] possileTiles = World.GetSpecialTilesByName(item.SpawnParamaters.SpawnOnSpecialTile);
                            World.SpecialTile spawnOn = possileTiles[GameServer.RandomNumberGenerator.Next(0, possileTiles.Length)];

                            if (Map.CheckPassable(spawnOn.X, spawnOn.Y))
                            {
                                if (GetItemsAt(spawnOn.X, spawnOn.Y).Length > 25) // Max items in one tile.
                                    continue;

                                ItemInstance instance = new ItemInstance(item.Id);
                                AddItem(instance, spawnOn.X, spawnOn.Y, despawnTimer);
                                Logger.DebugPrint("Created Item ID: " + instance.ItemId + " at: X: " + spawnOn.X + " Y: " + spawnOn.Y);
                                newItems++;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                    else if (item.SpawnParamaters.SpawnNearSpecialTile != null)
                    {
                        while (true)
                        {
                            // Pick a random special tile
                            World.SpecialTile[] possileTiles = World.GetSpecialTilesByName(item.SpawnParamaters.SpawnNearSpecialTile);
                            World.SpecialTile spawnNearTile = possileTiles[GameServer.RandomNumberGenerator.Next(0, possileTiles.Length)];

                            // Pick a direction to try spawn in

                            int direction = GameServer.RandomNumberGenerator.Next(0, 4);
                            int tryX = 0;
                            int tryY = 0;
                            if (direction == 0)
                            {
                                tryX = spawnNearTile.X + 1;
                                tryY = spawnNearTile.Y;
                            }
                            else if(direction == 1)
                            {
                                tryX = spawnNearTile.X - 1;
                                tryY = spawnNearTile.Y;
                            }
                            else if(direction == 3)
                            {
                                tryX = spawnNearTile.X;
                                tryY = spawnNearTile.Y + 1; 
                            }
                            else if (direction == 4)
                            {
                                tryX = spawnNearTile.X;
                                tryY = spawnNearTile.Y - 1;
                            }
                            if (World.InSpecialTile(tryX, tryY))
                            {
                                World.SpecialTile tile = World.GetSpecialTile(tryX, tryY);
                                if (tile.Code != null)
                                    continue;
                            }

                            if (Map.CheckPassable(tryX, tryY))
                            {
                                if (GetItemsAt(tryX, tryY).Length > 25) // Max here
                                    continue;

                                ItemInstance instance = new ItemInstance(item.Id); 
                                AddItem(instance, tryX, tryY, despawnTimer);
                                Logger.DebugPrint("Created Item ID: " + instance.ItemId + " at: X: " + tryX + " Y: " + tryY);
                                newItems++;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                    else if (item.SpawnParamaters.SpawnOnTileType != null)
                    {
                        
                        while (true)
                        {
                            // Pick a random location:
                            int tryX = GameServer.RandomNumberGenerator.Next(0, Map.Width);
                            int tryY = GameServer.RandomNumberGenerator.Next(0, Map.Height);

                            if (World.InSpecialTile(tryX, tryY))
                                continue;

                            if (Map.CheckPassable(tryX, tryY)) // Can the player walk here?
                            {
                                int TileID = Map.GetTileId(tryX, tryY, false);
                                string TileType = Map.TerrainTiles[TileID - 1].Type; // Is it the right type?

                                if (item.SpawnParamaters.SpawnOnTileType == TileType)
                                {
                                    if (GetItemsAt(tryX, tryY).Length > 25) // Max here
                                        continue;

                                    ItemInstance instance = new ItemInstance(item.Id);
                                    AddItem(instance, tryX, tryY, despawnTimer);
                                    Logger.DebugPrint("Created Item ID: " + instance.ItemId + " at: X: " + tryX + " Y: " + tryY);
                                    newItems++;
                                    break;

                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }



                }

            }
        }

        public static void Init()
        {
            ReadFromDatabase();
            GenerateItems();
        }

    }
}
