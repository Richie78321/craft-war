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
    public class GUI
    {
        public const int GUIItemPadding = 10;
        public static SpriteFont GUIFont;
        public static SoundEffect craftSoundEffect;

        //Object
        public Inventory playerInventory;
        public Inventory otherInventory = null;
        public bool inventoryMenuOpen = false;
        public Player player;

        //Craft menu
        public Inventory craftInventory;
        public GUIButton craftButton;
        public ItemType craftableItem = null;
        public List<Item> itemsHeld = new List<Item>();

        public bool craftWindowOpen = false;

        public GUI(Player player)
        {
            this.player = player;
            playerInventory = player.inventory;
        }

        private void enableCraftingInventory()
        {
            //Enable
            inventoryMenuOpen = true;
            craftWindowOpen = true;
            otherInventory = craftInventory;
            craftButton.visible = true;
        }

        private void disableCraftingInventory()
        {
            //Disable
            //Adds items from craft back to player
            craftInventory.addAllItemsTo(playerInventory, new Vector2(player.drawRectangle.Center.X, player.drawRectangle.Center.Y));
            otherInventory = null;
            craftButton.visible = false;
            craftWindowOpen = false;
        }

        private KeyboardState pastKeyboardState = Keyboard.GetState();
        public void keyboardInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyUp(Keys.I) && pastKeyboardState.IsKeyDown(Keys.I))
            {
                inventoryMenuOpen = !inventoryMenuOpen;
                if (!inventoryMenuOpen)
                {
                    disableCraftingInventory();
                }
            }
            if (keyboardState.IsKeyUp(Keys.C) && pastKeyboardState.IsKeyDown(Keys.C))
            {
                if (otherInventory == craftInventory)
                {
                    disableCraftingInventory();
                    inventoryMenuOpen = false;
                }
                else
                {
                    enableCraftingInventory();
                }
            }
            
            //Checks for interaction with tile
            if (!inventoryMenuOpen && keyboardState.IsKeyUp(Keys.E) && pastKeyboardState.IsKeyDown(Keys.E))
            {
                //Check for selection of tile
                if (Game1.currentMap.tileMap[(int)Math.Floor((mouseState.X - player.relativeOffset.X) / Game1.currentMap.tileSize), (int)Math.Floor((mouseState.Y - player.relativeOffset.Y) / Game1.currentMap.tileSize)] != null) Game1.currentMap.tileMap[(int)Math.Floor((mouseState.X - player.relativeOffset.X) / Game1.currentMap.tileSize), (int)Math.Floor((mouseState.Y - player.relativeOffset.Y) / Game1.currentMap.tileSize)].tileInteracted();
            }

            pastKeyboardState = keyboardState;
        }

        private MouseState pastMouseState = Mouse.GetState();
        public bool mouseInput(MouseState mouseState, KeyboardState keyboardState)
        {
            //Check inventory for interaction
            if (inventoryMenuOpen)
            {
                //Check for craft button interaction
                craftButton.interact(mouseState, pastMouseState);

                //Check for interaction in playerInventory
                if (!playerInventory.mouseInteraction(mouseState, pastMouseState, keyboardState, ref itemsHeld))
                {
                    //Check for interaction in otherInventory (if one exists)
                    if (otherInventory != null && otherInventory.mouseInteraction(mouseState, pastMouseState, keyboardState, ref itemsHeld))
                    {
                        //Successful interaction with inventory. No other GUI
                        pastMouseState = mouseState;
                        return true;
                    }
                }
                else
                {
                    //Successful interaction with inventory. No other GUI
                    pastMouseState = mouseState;
                    return true;
                }

                //Checks for final click to drop items in hand
                if (itemsHeld.Count > 0)
                {
                    if (pastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released && GameMath.distance(player.drawRectangle.Center, new Point(mouseState.X, mouseState.Y)) <= player.tileInteractionRadius)
                    {
                        //Drop items in hand if any are held
                        Drop newDrop = new Drop(itemsHeld.ToArray(), new RigidBody(Drop.massesOfPlayer), new Vector2(mouseState.X - player.relativeOffset.X, mouseState.Y - player.relativeOffset.Y));
                        Game1.currentMap.entityAddQueue.Add(newDrop);
                        itemsHeld.Clear();
                        Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.entityAdd).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.dropAdd).ToString() + GameServer.dataSeparator + JsonConvert.SerializeObject(newDrop.itemsInDrop) + GameServer.dataSeparator + newDrop.itemsInDrop[0].itemType.name + GameServer.dataSeparator + (mouseState.X - player.relativeOffset.X) + GameServer.dataSeparator + (mouseState.Y - player.relativeOffset.Y) + GameServer.messageSeparator;
                    }
                    else if (pastMouseState.RightButton == ButtonState.Pressed && mouseState.RightButton == ButtonState.Released && GameMath.distance(player.drawRectangle.Center, new Point(mouseState.X, mouseState.Y)) <= player.tileInteractionRadius)
                    {
                        Drop newDrop = new Drop(new Item[] { itemsHeld[0] }, new RigidBody(Drop.massesOfPlayer), new Vector2(mouseState.X - player.relativeOffset.X, mouseState.Y - player.relativeOffset.Y));
                        Game1.currentMap.entityAddQueue.Add(newDrop);
                        itemsHeld.Remove(itemsHeld[0]);
                        Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.entityAdd).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.dropAdd).ToString() + GameServer.dataSeparator + JsonConvert.SerializeObject(newDrop.itemsInDrop) + GameServer.dataSeparator + newDrop.itemsInDrop[0].itemType.name + GameServer.dataSeparator + (mouseState.X - player.relativeOffset.X) + GameServer.dataSeparator + (mouseState.Y - player.relativeOffset.Y) + GameServer.messageSeparator;
                    }
                }

                //No interaction outside of GUI when inventory is open
                pastMouseState = mouseState;
                return true;
            }

            pastMouseState = mouseState;
            return false;
        }

        public void drawGUI(SpriteBatch spriteBatch, int screenWidth, int screenHeight, MouseState mouseState)
        {
            if (inventoryMenuOpen)
            {
                //Draw inventories
                playerInventory.drawInventory(spriteBatch);
                if (otherInventory != null) otherInventory.drawInventory(spriteBatch);

                //Draw crafting button
                craftButton.draw(spriteBatch);

                //Draw craftable item if not null
                if (craftWindowOpen && craftableItem != null)
                {
                    InventoryCell craftOutcomeCell = new InventoryCell(new Rectangle(craftButton.rectangle.X + ((craftButton.rectangle.Width - craftInventory.cellSize) / 2), craftButton.rectangle.Bottom + GUIItemPadding, craftInventory.cellSize - (GUIItemPadding * 2), craftInventory.cellSize - (GUIItemPadding * 2)));
                    craftOutcomeCell.addItem(new Item(craftableItem), craftableItem.amountMadeFromCraft);
                    craftOutcomeCell.drawInventoryCell(craftOutcomeCell.rectangle, false, spriteBatch, Inventory.inventoryFont);

                    if (craftOutcomeCell.rectangle.Contains(mouseState.X, mouseState.Y))
                    {
                        craftOutcomeCell.drawItemLabel(new Vector2(mouseState.X, mouseState.Y), spriteBatch);
                    }
                }

                //Draw items held if not null
                if (itemsHeld.Count > 0)
                {
                    Rectangle drawRec = new Rectangle(mouseState.X - (playerInventory.cellSize / 2), mouseState.Y - (playerInventory.cellSize / 2), playerInventory.cellSize, playerInventory.cellSize);
                    spriteBatch.Draw(itemsHeld[0].itemType.itemTexture, new Rectangle(drawRec.X + (int)(Inventory.itemInCellPadding * drawRec.Width), drawRec.Y + (int)(Inventory.itemInCellPadding * drawRec.Width), drawRec.Width - ((int)(Inventory.itemInCellPadding * drawRec.Width) * 2), drawRec.Height - ((int)(Inventory.itemInCellPadding * drawRec.Width) * 2)), itemsHeld[0].itemType.drawTint);
                    if (itemsHeld.Count > 1)
                    {
                        spriteBatch.DrawString(Inventory.inventoryFont, itemsHeld.Count.ToString(), new Vector2(drawRec.X - Inventory.inventoryFont.MeasureString(itemsHeld.Count.ToString()).X + drawRec.Width - (int)((Inventory.itemInCellPadding * drawRec.Width) * 1.5F), drawRec.Y - Inventory.inventoryFont.MeasureString(itemsHeld.Count.ToString()).Y + drawRec.Height - (int)(Inventory.itemInCellPadding * drawRec.Width)), Color.White);
                    }
                }

                foreach (InventoryCell b in playerInventory.inventoryCells)
                {
                    if (b.rectangle.Contains(mouseState.X, mouseState.Y))
                    {
                        b.drawItemLabel(new Vector2(mouseState.X, mouseState.Y), spriteBatch);
                    }
                }
                if (otherInventory != null)
                {
                    foreach (InventoryCell b in otherInventory.inventoryCells)
                    {
                        if (b.rectangle.Contains(mouseState.X, mouseState.Y))
                        {
                            b.drawItemLabel(new Vector2(mouseState.X, mouseState.Y), spriteBatch);
                        }
                    }
                }
            }
            else
            {
                //Draw selected item if an item is selected
                if (playerInventory.selectedCell != null)
                {
                    Rectangle drawRectangle = new Rectangle(GUIItemPadding, screenHeight - GUIItemPadding - (playerInventory.selectedCell.rectangle.Height * 2), playerInventory.selectedCell.rectangle.Width * 2, playerInventory.selectedCell.rectangle.Height * 2);
                    playerInventory.selectedCell.drawInventoryCell(drawRectangle, false, spriteBatch, GUI.GUIFont);
                    if (drawRectangle.Contains(mouseState.X, mouseState.Y))
                    {
                        playerInventory.selectedCell.drawItemLabel(new Vector2(mouseState.X, mouseState.Y), spriteBatch);
                    }
                }
            }
        }

        public void checkForCraftable()
        {
            foreach (ItemType b in ItemType.craftableItemTypes)
            {
                if (b.canBeCraftedFromInventory(craftInventory))
                {
                    craftableItem = b;
                    return;
                }
            }

            craftableItem = null;
        }

        public const int particlesFromCraft = 5;
        public const int particleSize = 6;
        public const float particleSpeed = .05F;
        public void attemptCraft()
        {
            if (craftableItem != null)
            {
                //Craft Item
                playerInventory.addItem(new Item(craftableItem), craftableItem.amountMadeFromCraft);

                //Craft effects
                Random random = Game1.currentMap.syncedRandom;
                for (int i = 0; i < particlesFromCraft; i++)
                {
                    Game1.currentMap.particleAddQueue.Add(new TileParticle(particleSize, new Vector2(Game1.mainPlayer.drawRectangle.Center.X, Game1.mainPlayer.drawRectangle.Center.Y), craftableItem.itemTexture, 1500, random, initialVelocity: new Vector2(random.Next(-1, 2) * particleSpeed, random.Next(-1, 2) * particleSpeed)));
                }
                craftSoundEffect.Play();

                //Remove items required for craft
                craftInventory.removeItemsFromCraft(craftableItem);
            }
        }
    }

    public abstract class GUIElement
    {
        public bool visible;
        public Rectangle rectangle;

        public GUIElement(Rectangle rectangle, bool visible = false)
        {
            this.rectangle = rectangle;
            this.visible = visible;
        }

        public abstract void draw(SpriteBatch spriteBatch);
    }

    public class GUIButton : GUIElement
    {
        public delegate void action();
        public static Texture2D[] buttonTextures;
        public static SoundEffect[] buttonSoundEffects;
        public static SpriteFont buttonFont;

        //Object
        public action clickAction;
        public string text;
        public bool hovered = false;

        public GUIButton(Rectangle rectangle, string text, action clickAction, bool visible = false) : base(rectangle, visible)
        {
            this.text = text;
            this.clickAction = clickAction;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            if (visible)
            {
                if (!hovered)
                {
                    spriteBatch.Draw(buttonTextures[0], rectangle, Color.White);
                    spriteBatch.DrawString(buttonFont, text, new Vector2(rectangle.X + ((rectangle.Width - buttonFont.MeasureString(text).X) / 2), rectangle.Y + ((rectangle.Height - buttonFont.MeasureString(text).Y) / 2)), Color.White);
                }
                else
                {
                    spriteBatch.Draw(buttonTextures[1], rectangle, Color.White);
                    spriteBatch.DrawString(buttonFont, text, new Vector2(rectangle.X + ((rectangle.Width - buttonFont.MeasureString(text).X) / 2), rectangle.Y + ((rectangle.Height - buttonFont.MeasureString(text).Y) / 2)), Color.Black);
                }
            }
        }

        public virtual void interact(MouseState mouseState, MouseState pastMouseState)
        {
            if (visible)
            {
                if (rectangle.Contains(mouseState.X, mouseState.Y))
                {
                    if (hovered == false)
                    {
                        buttonSoundEffects[0].Play();
                        hovered = true;
                    }

                    if (mouseState.LeftButton == ButtonState.Released && pastMouseState.LeftButton == ButtonState.Pressed)
                    {
                        //Button has been pressed. Invoke clickAction
                        buttonSoundEffects[1].Play();
                        clickAction.Invoke();
                    }
                }
                else
                {
                    hovered = false;
                }
            }
            else
            {
                hovered = false;
            }
        }
    }
}
