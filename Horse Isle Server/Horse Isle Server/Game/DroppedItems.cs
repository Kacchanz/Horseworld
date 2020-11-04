﻿using System;
using System.Collections.Generic;
using HISP.Server;

namespace HISP.Game
{
    class DroppedItems
    {
        public struct DroppedItem
        {
            public int X;
            public int Y;
            public int DespawnTimer;
            public ItemInstance instance;
        }
        private static int epoch = 0;
        private static List<DroppedItem> droppedItemsList = new List<DroppedItem>();
        public static int GetCountOfItem(Item.ItemInformation item)
        {

            DroppedItem[] dropedItems = droppedItemsList.ToArray();
            int count = 0;
            foreach(DroppedItem droppedItem in dropedItems)
            {
                if(droppedItem.instance.ItemId == item.Id)
                {
                    count++;
                }
            }
            return count;
        }

        public static DroppedItem[] GetItemsAt(int x, int y)
        {

            DroppedItem[] dropedItems = droppedItemsList.ToArray();
            List<DroppedItem> items = new List<DroppedItem>();
            foreach(DroppedItem droppedItem in dropedItems)
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
            int randomId = item.instance.RandomId;
            Database.RemoveDroppedItem(randomId);
            for (int i = 0; i < droppedItemsList.Count; i++) 
            {
                if(droppedItemsList[i].instance.RandomId == randomId)
                {
                    droppedItemsList.RemoveAt(i);
                    return;
                }
            }
            
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

            DroppedItem[] dropedItems = droppedItemsList.ToArray();

            foreach (DroppedItem item in dropedItems)
            {
                if(item.instance.RandomId == randomId)
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
                        RemoveDroppedItem(item);
                        removedCount++;
                    }
                }
            }
            if(removedCount > 0)
                epoch = new_epoch;
        }

        public static void AddItem(ItemInstance item, int x, int y)
        {
            DroppedItem droppedItem = new DroppedItem();
            droppedItem.X = x;
            droppedItem.Y = y;
            droppedItem.DespawnTimer = 1500;
            droppedItem.instance = item;
            droppedItemsList.Add(droppedItem);
        }
        public static void GenerateItems()
        {
            int newItems = 0;
            foreach (Item.ItemInformation item in Item.Items)
            {
                int count = GetCountOfItem(item);
                while (count < item.SpawnParamaters.SpawnCap)
                {

                    count++;

                    int despawnTimer = GameServer.RandomNumberGenerator.Next(900, 1500);

                    if (item.SpawnParamaters.SpawnInArea != null)
                    {

                    }
                    else if (item.SpawnParamaters.SpawnOnSpecialTile != null)
                    {

                    }
                    else if (item.SpawnParamaters.SpawnNearSpecialTile != null)
                    {

                    }
                    else if (item.SpawnParamaters.SpawnOnTileType != null)
                    {
                        
                        while (true)
                        {
                            // Pick a random isle:
                            int isleId = GameServer.RandomNumberGenerator.Next(0, World.Isles.Count);
                            World.Isle isle = World.Isles[isleId];

                            // Pick a random location inside the isle
                            int tryX = GameServer.RandomNumberGenerator.Next(isle.StartX, isle.EndX);
                            int tryY = GameServer.RandomNumberGenerator.Next(isle.StartY, isle.EndY);


                            if (World.InTown(tryX, tryY) || World.InSpecialTile(tryX, tryY))
                                continue;

                            if (Map.CheckPassable(tryX, tryY)) // Can the player walk here?
                            {
                                int TileID = Map.GetTileId(tryX, tryY, false);
                                string TileType = Map.TerrainTiles[TileID - 1].Type; // Is it the right type?

                                if (item.SpawnParamaters.SpawnOnTileType == TileType)
                                {
                                    ItemInstance instance = new ItemInstance(item.Id);
                                    DroppedItem droppedItem = new DroppedItem();
                                    droppedItem.X = tryX;
                                    droppedItem.Y = tryY;
                                    droppedItem.DespawnTimer = despawnTimer;
                                    droppedItem.instance = instance;
                                    droppedItemsList.Add(droppedItem);
                                    Database.AddDroppedItem(droppedItem);
                                    Logger.DebugPrint("Created Item ID: " + instance.ItemId + " in " + isle.Name + " at: X: " + droppedItem.X + " Y: " + droppedItem.Y);
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
            Logger.InfoPrint("Generating items, (this may take awhile on a fresh database!)");
            GenerateItems();
        }

    }
}
