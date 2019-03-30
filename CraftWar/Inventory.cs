using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace CraftWar
{
    public class Inventory
    {
        public static Texture2D inventoryCellTexture;
        public static Texture2D inventoryDurabilityTexture;
        public static SpriteFont inventoryFont;
        public const int inventoryScreenPortion = 2;
        public const float itemInCellPadding = .1F;

        //Object
        public string inventoryName;
        public InventoryCell[,] inventoryCells;
        public InventoryCell selectedCell = null;
        public bool selectable;
        public int cellSize;
        public event EventHandler OnInventoryChanged;

        public Inventory(string inventoryName, int[] inventorySize, int drawAreaWidth, int drawAreaHeight, int xOffset = 0, int yOffset = 0, bool selectable = true)
        {
            this.inventoryName = inventoryName;
            inventoryCells = new InventoryCell[inventorySize[0], inventorySize[1]];

            for (int i = 0; i < inventoryCells.GetLength(0); i++)
            {
                for (int j = 0; j < inventoryCells.GetLength(1); j++)
                {
                    inventoryCells[i, j] = new InventoryCell(new Rectangle());
                }
            }

            OnInventoryChanged = new EventHandler((object sender, EventArgs e) => { });
            this.selectable = selectable;
            initializeInventoryForArea(drawAreaWidth, drawAreaHeight, xOffset, yOffset);
        }

        public void initializeInventoryForArea(int drawAreaWidth, int drawAreaHeight, int xOffset = 0, int yOffset = 0)
        {
            if ((float)drawAreaWidth / inventoryCells.GetLength(0) < (float)drawAreaHeight / inventoryCells.GetLength(1))
            {
                //Prioritize drawAreaWidth
                cellSize = (drawAreaWidth / inventoryScreenPortion) / inventoryCells.GetLength(0);
            }
            else
            {
                //Prioritize drawAreaHeight
                cellSize = (drawAreaHeight / inventoryScreenPortion) / inventoryCells.GetLength(1);
            }

            for (int i = 0; i < inventoryCells.GetLength(0); i++)
            {
                for (int j = 0; j < inventoryCells.GetLength(1); j++)
                {
                    //Create rectangles
                    inventoryCells[i, j].rectangle = new Rectangle((((drawAreaWidth - (cellSize * inventoryCells.GetLength(0))) / 2) + (i * cellSize)) + xOffset, (((drawAreaHeight - (cellSize * inventoryCells.GetLength(1))) / 2) + (j * cellSize)) + yOffset, cellSize, cellSize);
                }
            }
        }

        public void drawInventory(SpriteBatch spriteBatch)
        {
            foreach (InventoryCell b in inventoryCells)
            {
                //Draw cell
                b.drawInventoryCell(b.rectangle, selectedCell == b, spriteBatch, inventoryFont);
            }
        }

        public bool mouseInteraction(MouseState mouseState, MouseState pastMouseState, KeyboardState keyboardState, ref List<Item> itemsHeld)
        {
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.rectangle.Contains(mouseState.X, mouseState.Y))
                {
                    //Interaction is with this inventory cell
                    if (keyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        if (selectable)
                        {
                            //Cell selection
                            if (pastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released)
                            {
                                //Cell is selected
                                if (selectedCell == b)
                                {
                                    selectedCell.useItem(InventoryCell.useEventType.unequipItem);
                                    selectedCell = null;
                                }
                                else
                                {
                                    if (selectedCell != null)
                                    {
                                        selectedCell.useItem(InventoryCell.useEventType.unequipItem);
                                    }
                                    selectedCell = b;
                                    selectedCell.useItem(InventoryCell.useEventType.equipItem);
                                }
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //Item movement
                        if (pastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released)
                        {
                            //Either swap, deposit, or grab items
                            if (b.items.Count > 0 && itemsHeld.Count > 0 && b.items[0].itemType == itemsHeld[0].itemType)
                            {
                                //Item types are the same. Add to slot
                                b._items.AddRange(itemsHeld);
                                itemsHeld.Clear();
                            }
                            else
                            {
                                List<Item> itemHeldBuffer = itemsHeld;
                                itemsHeld = b.items;
                                b._items = itemHeldBuffer;
                            }
                            //Call inventory changed event
                            OnInventoryChanged.Invoke(this, null);
                            return true;
                        }
                        else if (pastMouseState.RightButton == ButtonState.Pressed && mouseState.RightButton == ButtonState.Released)
                        {
                            //Add one of items held to the inventory cell if it contains nothing or the same type
                            if (itemsHeld.Count > 0 && (b.items.Count == 0 || b.items[0].itemType == itemsHeld[0].itemType))
                            {
                                //Can add one
                                b.items.Add(itemsHeld[0]);
                                itemsHeld.Remove(itemsHeld[0]);
                                OnInventoryChanged.Invoke(this, null);
                                return true;
                            }
                            else if (itemsHeld.Count == 0 && b.items.Count > 0)
                            {
                                //Split items in cell into held
                                int amountToTake = b.items.Count / 2;
                                for (int i = 0; i < amountToTake; i++)
                                {
                                    itemsHeld.Add(b.items[0]);
                                    b._items.Remove(b._items[0]);
                                }
                                
                                if (amountToTake > 0)
                                {
                                    OnInventoryChanged.Invoke(this, null);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool addItem(Item item, int amount = 1)
        {
            //Check for pre existing inventory cells with the item type
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.items.Count > 0 && b.items[0].itemType == item.itemType)
                {
                    if (b.addItem(item, amount))
                    {
                        OnInventoryChanged.Invoke(this, null);
                        return true;
                    }
                }
            }

            //Add to empty inventory cell
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.addItem(item, amount))
                {
                    OnInventoryChanged.Invoke(this, null);
                    return true;
                }
            }

            return false;
        }

        public bool addItems(Item[] items)
        {
            //Check for pre existing inventory cells with the item type
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.items.Count > 0 && b.items[0].itemType == items[0].itemType)
                {
                    foreach (Item c in items)
                    {
                        b.addItem(c);
                    }
                    OnInventoryChanged.Invoke(this, null);
                    return true;
                }
            }

            //Add to empty inventory cell
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.items.Count == 0)
                {
                    foreach (Item c in items)
                    {
                        b.addItem(c);
                    }
                    OnInventoryChanged.Invoke(this, null);
                    return true;
                }
            }

            return false;
        }

        public bool areItemsAvailable(ItemType itemType, int quantity)
        {
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.items.Count > 0 && b.items.Count >= quantity && b.items[0].itemType == itemType)
                {
                    return true;
                }
            }

            return false;
        }

        public void addAllItemsTo(Inventory inventoryToAddTo, Vector2 dropPoint)
        {
            foreach (InventoryCell b in inventoryCells)
            {
                if (b.items.Count > 0)
                {
                    Game1.currentMap.entityAddQueue.Add(new Drop(b.items.ToArray(), new RigidBody(Drop.massesOfPlayer), dropPoint));
                    b.items.Clear();
                    OnInventoryChanged.Invoke(this, null);
                }
            }
        }

        public void moveItemCellTo(Inventory inventoryToAddTo, Vector2 dropPoint, InventoryCell inventoryCell)
        {
            if (inventoryCell.items.Count > 0)
            {
                //Drop to ground
                Game1.currentMap.entityAddQueue.Add(new Drop(inventoryCell.items.ToArray(), new RigidBody(Drop.massesOfPlayer), dropPoint));
                inventoryCell.items.Clear();
                OnInventoryChanged.Invoke(this, null);
            }
        }

        public void clearAll()
        {
            foreach (InventoryCell b in inventoryCells)
            {
                b.items.Clear();
            }
            OnInventoryChanged.Invoke(this, null);
        }

        public void removeItemsFromCraft(ItemType itemCrafted)
        {
            for (int i = 0; i < itemCrafted.craftingRecipe.GetLength(0); i++)
            {
                for (int j = 0; j < itemCrafted.craftingRecipe.GetLength(1); j++)
                {
                    if (itemCrafted.craftingRecipe[i, j, 1] != null)
                    {
                        for (int p = 0; p < (int)itemCrafted.craftingRecipe[i, j, 0]; p++)
                        {
                            if (inventoryCells[i, j].items.Count > 0)
                            {
                                inventoryCells[i, j]._items.Remove(inventoryCells[i, j]._items[0]);
                            }
                        }
                    }
                }
            } 

            OnInventoryChanged.Invoke(this, null);
        }
    }

    public class InventoryCell
    {
        public enum useEventType
        {
            leftClick,
            rightClick,
            equipItem,
            unequipItem
        }

        public Rectangle rectangle;
        public List<Item> items
        {
            get
            {
                return _items;
            }
        }
        public List<Item> _items;

        public InventoryCell(Rectangle rectangle)
        {
            this.rectangle = rectangle;
            _items = new List<Item>();
        }

        public bool isContainingTiles()
        {
            if (items.Count > 0)
            {
                if (items[0].itemType is TileItemType)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void useItem(useEventType useEventType)
        {
            if (items.Count > 0)
            {
                if (useEventType == useEventType.leftClick && items[0].itemType.leftClickAction != null)
                {
                    if (items[0].itemType.leftClickAction.Invoke()) updateItemDurability();
                }
                else if (useEventType == useEventType.rightClick && items[0].itemType.rightClickAction != null)
                {
                    if (items[0].itemType.rightClickAction.Invoke()) updateItemDurability();
                }
                else if (useEventType == useEventType.equipItem && items[0].itemType.equipAction != null)
                {
                    items[0].itemType.equipAction.Invoke();
                }
                else if (useEventType == useEventType.unequipItem && items[0].itemType.unequipAction != null)
                {
                    items[0].itemType.unequipAction.Invoke();
                }
            }
        }

        public void updateItemDurability()
        {
            if (items.Count > 0)
            {
                items[0].currentDurability--;
                if (items[0].currentDurability <= 0)
                {
                    //Remove item
                    if (items[0].itemType.unequipAction != null)
                    {
                        //Act as if item was unequiped
                        items[0].itemType.unequipAction.Invoke();
                    }

                    items.Remove(items[0]);

                    if (items.Count > 0)
                    {
                        //Act as if new item was equipped
                        if (items[0].itemType.equipAction != null)
                        {
                            items[0].itemType.equipAction.Invoke();
                        }
                    }
                }
            }
        }

        public bool addItem(Item item, int amount = 1)
        {
            if (items.Count == 0)
            {
                //Can add items because it is empty
                for (int i = 0; i < amount; i++)
                {
                    items.Add(item);
                }
                return true;
            }
            else
            {
                if (items[0].itemType == item.itemType)
                {
                    //Item has the same itemType. Can add
                    for (int i = 0; i < amount; i++)
                    {
                        items.Add(item);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void drawInventoryCell(Rectangle rectangle, bool selected, SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!selected)
            {
                spriteBatch.Draw(Inventory.inventoryCellTexture, rectangle, Color.White);
            }
            else
            {
                spriteBatch.Draw(Inventory.inventoryCellTexture, rectangle, Color.Blue);
            }

            if (items.Count > 0)
            {
                //Draw item
                spriteBatch.Draw(items[0].itemType.itemTexture, new Rectangle(rectangle.X + (int)(Inventory.itemInCellPadding * rectangle.Width), rectangle.Y + (int)(Inventory.itemInCellPadding * rectangle.Width), rectangle.Width - ((int)(Inventory.itemInCellPadding * rectangle.Width) * 2), rectangle.Height - ((int)(Inventory.itemInCellPadding * rectangle.Width) * 2)), items[0].itemType.drawTint);

                if (items.Count > 1)
                {
                    //Draw item amount
                    spriteBatch.DrawString(font, items.Count.ToString(), new Vector2(rectangle.X - font.MeasureString(items.Count.ToString()).X + rectangle.Width - (int)((Inventory.itemInCellPadding * rectangle.Width)), rectangle.Y - font.MeasureString(items.Count.ToString()).Y + rectangle.Height - (int)(Inventory.itemInCellPadding * rectangle.Width)), Color.White);
                }

                //Draw item durability
                if (items[0].currentDurability < items[0].itemType.durability)
                {
                    //Item is damaged
                    Rectangle itemDurabilityRectangle = new Rectangle(rectangle.X + ((int)(Inventory.itemInCellPadding * rectangle.Width) * 2), rectangle.Bottom - ((int)(Inventory.itemInCellPadding * rectangle.Width) * 2), rectangle.Width - ((int)(Inventory.itemInCellPadding * rectangle.Width) * 4), (int)((Inventory.itemInCellPadding * rectangle.Width) / 2));
                    float durabilityRatio = (float)items[0].currentDurability / items[0].itemType.durability;
                    itemDurabilityRectangle.Width = (int)(itemDurabilityRectangle.Width * durabilityRatio);
                    spriteBatch.Draw(Inventory.inventoryDurabilityTexture, itemDurabilityRectangle, Color.White);
                }
            }
        }

        public void drawItemLabel(Vector2 drawLocation, SpriteBatch spriteBatch)
        {
            if (items.Count > 0)
            {
                spriteBatch.Draw(Inventory.inventoryCellTexture, new Rectangle((int)drawLocation.X, (int)(drawLocation.Y - (((GUI.GUIItemPadding * 2) + Inventory.inventoryFont.MeasureString(items[0].itemType.name).Y) / 2)), (int)((GUI.GUIItemPadding * 2) + Inventory.inventoryFont.MeasureString(items[0].itemType.name).X), (int)((GUI.GUIItemPadding * 2) + Inventory.inventoryFont.MeasureString(items[0].itemType.name).Y)), Color.White);
                spriteBatch.DrawString(Inventory.inventoryFont, items[0].itemType.name, new Vector2(drawLocation.X + GUI.GUIItemPadding, (drawLocation.Y - (((GUI.GUIItemPadding * 2) + Inventory.inventoryFont.MeasureString(items[0].itemType.name).Y) / 2)) + GUI.GUIItemPadding), Color.Black);
            }
        }
    }

    public class ItemType
    {
        public static List<ItemType> itemTypes = new List<ItemType>();
        public static List<ItemType> craftableItemTypes = new List<ItemType>();
        public delegate bool ItemAction();

        //Object
        public string name;
        public ItemAction leftClickAction;
        public ItemAction rightClickAction;
        public ItemAction equipAction;
        public ItemAction unequipAction;
        public Texture2D itemTexture;
        public int durability;
        public Color drawTint;

        //Crafting
        public bool craftable
        {
            get
            {
                return craftingRecipe != null;
            }
            set
            {
                if (value)
                {
                    ItemType.craftableItemTypes.Add(this);
                }
            }
        }
        public object[,,] craftingRecipe;
        public int amountMadeFromCraft;

        public bool canBeCraftedFromInventory(Inventory inventory)
        {
            if (!craftable || craftingRecipe == null)
            {
                //Uncraftable item
                return false;
            }
            else
            {
                for (int i = 0; i < inventory.inventoryCells.GetLength(0); i++)
                {
                    for (int j = 0; j < inventory.inventoryCells.GetLength(1); j++)
                    {
                        if (craftingRecipe[i, j, 1] != null)
                        {
                            if (inventory.inventoryCells[i, j].items.Count < (int)craftingRecipe[i, j, 0] || inventory.inventoryCells[i, j].items[0].itemType != (ItemType)craftingRecipe[i, j, 1])
                            {
                                //Cell does not contained required item
                                return false;
                            }
                        }
                        else
                        {
                            //Check if cell in inventory is empty
                            if (inventory.inventoryCells[i, j].items.Count > 0)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        public ItemType(string name, Texture2D itemTexture, Color drawTint, int durability = 1, ItemAction leftClickAction = null, ItemAction rightClickAction = null, ItemAction equipAction = null, ItemAction unequipAction = null, object[,,] craftingRecipe = null, int amountMadeFromCraft = 1)
        {
            this.name = name;
            this.leftClickAction = leftClickAction;
            this.rightClickAction = rightClickAction;
            this.itemTexture = itemTexture;
            this.durability = durability;
            this.amountMadeFromCraft = amountMadeFromCraft;
            this.equipAction = equipAction;
            this.unequipAction = unequipAction;
            this.drawTint = drawTint;

            //Not craftable if craftingRecipe is null
            if (craftingRecipe != null)
            {
                this.craftingRecipe = craftingRecipe;
            }

            //If craftable adds to list referencing all craftable items
            if (craftable)
            {
                craftableItemTypes.Add(this);
            }

            itemTypes.Add(this);
        }
    }

    public class TileItemType : ItemType
    {
        public TileType tileTypeToPlace;

        public TileItemType(string name, TileType tileType, Color drawTint, int durability = 1, ItemAction leftClickAction = null, ItemAction rightClickAction = null, bool craftable = false, object[,,] craftingRecipe = null, int amountMadeFromCraft = 1) : base(name, tileType.textures[0], drawTint, durability, leftClickAction, rightClickAction, amountMadeFromCraft: amountMadeFromCraft, craftingRecipe: craftingRecipe)
        {
            tileTypeToPlace = tileType;
        }
    }

    public class Item
    {
        [JsonIgnore]
        public ItemType itemType;
        public int currentDurability;

        public Item(ItemType itemType)
        {
            this.itemType = itemType;
            if (itemType != null)
            {
                currentDurability = itemType.durability;
            }
        }
    }
}

