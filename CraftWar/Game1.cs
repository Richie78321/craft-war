using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;

namespace CraftWar
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public static Map currentMap;
        public static Player mainPlayer;
        public static NetworkManager networkManager;
        public static GameTime gameTime;
        public int currentSeed;

        public Game1(NetworkManager networkManager, int seed)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //Initialize Screen Settings
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            //graphics.IsFullScreen = true;

            Game1.networkManager = networkManager;
            currentSeed = seed;
        }

        protected override void Initialize()
        {
            //Initialize Game Settings
            IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            //Create loading window
            LoadingScreen loadingScreen = new LoadingScreen();
            Thread loadingScreenThread = new Thread(new ThreadStart(() => { loadingScreen.ShowDialog(); }));
            loadingScreenThread.Start();

            spriteBatch = new SpriteBatch(GraphicsDevice);

            //waitForLoadingScreenLoad(loadingScreen);
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Loading Textures..."); }));
            //Load Inventory Textures and Fonts
            Inventory.inventoryCellTexture = Content.Load<Texture2D>("GUI/Inventory/inventorySquare");
            Inventory.inventoryFont = Content.Load<SpriteFont>("GUI/Inventory/inventoryFont");
            Inventory.inventoryDurabilityTexture = Content.Load<Texture2D>("GUI/Inventory/durabilityTexture");

            //Load GUI Textures and Fonts
            GUI.GUIFont = Content.Load<SpriteFont>("GUI/GUIFont");
            GUIButton.buttonFont = Content.Load<SpriteFont>("GUI/GUIFont");
            GUIButton.buttonTextures = new Texture2D[2];
            GUIButton.buttonTextures[0] = Content.Load<Texture2D>("GUI/Button/buttonTexture0");
            GUIButton.buttonTextures[1] = Content.Load<Texture2D>("GUI/Button/buttonTexture1");
            GUIButton.buttonSoundEffects = new SoundEffect[2];
            GUIButton.buttonSoundEffects[0] = Content.Load<SoundEffect>("GUI/Button/buttonHoverSound");
            GUIButton.buttonSoundEffects[1] = Content.Load<SoundEffect>("GUI/Button/buttonClickSound");
            GUI.craftSoundEffect = Content.Load<SoundEffect>("GUI/Craft/craftSound");

            //Load TileTypes and break overlays
            Texture2D[] standardBreakOverlay = new Texture2D[7];
            for (int i = 0; i < standardBreakOverlay.Length; i++) standardBreakOverlay[i] = Content.Load<Texture2D>("Tile/Break Overlay/breakOverlay" + i);
            TileMaterial dirtTileMaterial = new TileMaterial(.1F);
            TerrainTileType.dirtTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/dirtTile") }, "Dirt", 500, standardBreakOverlay, dirtTileMaterial);
            TerrainTileType.grassTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/grassTile") }, "Grass", 500, standardBreakOverlay, dirtTileMaterial, droppedItemType: TerrainTileType.dirtTile.droppedItemType);
            TileMaterial stoneMaterial = new TileMaterial(.05F);
            TerrainTileType.stoneTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/stoneTile") }, "Stone", 1500, standardBreakOverlay, stoneMaterial);
            TerrainTileType.borderTile = new TileType(new[] { Content.Load<Texture2D>("Tile/borderTile") }, "Border", 0, standardBreakOverlay, new TileMaterial(0), breakable: false);
            TerrainTileType.logTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Wood/logTile0"), Content.Load<Texture2D>("Tile/Wood/logTile1") }, "Log", 1000, standardBreakOverlay, new TileMaterial(.06F));
            TerrainTileType.branchTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Wood/Tree Extras/branchLeft"), Content.Load<Texture2D>("Tile/Wood/Tree Extras/branchRight") }, "Branch", TerrainTileType.logTile.breakTime, TerrainTileType.logTile.breakOverlay, TerrainTileType.logTile.tileMaterial, droppedItemType: TerrainTileType.logTile.droppedItemType);
            TerrainTileType.woodTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Wood/woodTile") }, "Wood", 750, standardBreakOverlay, new TileMaterial(.06F));
            TerrainTileType.leavesTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Wood/leavesTile") }, "Leaves", 100, standardBreakOverlay, new TileMaterial(.06F));
            TerrainTileType.grassSurfaceTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/grassTileSurface") }, "Grass Surface", 0, standardBreakOverlay, dirtTileMaterial);
            TerrainTileType.grassSurfaceTile.droppedItemType = null;
            TerrainTileType.surfaceDirtTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/topDirtTile") }, "Top Dirt", 500, standardBreakOverlay, dirtTileMaterial, droppedItemType: TerrainTileType.dirtTile.droppedItemType);
            TerrainTileType.surfaceDirtSurfaceTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Terrain/topDirtTileSurface") }, "Dirt Surface", 0, standardBreakOverlay, dirtTileMaterial);
            TerrainTileType.surfaceDirtSurfaceTile.droppedItemType = null;
            OreTileType.coalOre = new TileType(new[] { Content.Load<Texture2D>("Tile/Ore/coalTexture") }, "Coal Ore", 1500, standardBreakOverlay, stoneMaterial);
            OreTileType.copperOre = new TileType(new[] { Content.Load<Texture2D>("Tile/Ore/copperTexture") }, "Copper Ore", 1500, standardBreakOverlay, stoneMaterial);
            OreTileType.ironOre = new TileType(new[] { Content.Load<Texture2D>("Tile/Ore/ironTexture") }, "Iron Ore", 1500, standardBreakOverlay, stoneMaterial);
            OreTileType.goldOre = new TileType(new[] { Content.Load<Texture2D>("Tile/Ore/goldTexture") }, "Gold Ore", 1500, standardBreakOverlay, stoneMaterial);
            OreTileType.diamondOre = new TileType(new[] { Content.Load<Texture2D>("Tile/Ore/diamondTexture") }, "Diamond Ore", 1500, standardBreakOverlay, stoneMaterial);
            LightingTileType.playerGlowTile = new TileType(new[] { Content.Load<Texture2D>("Tile/borderTile") }, "Player Glow Tile", 0, standardBreakOverlay, new TileMaterial(0), collidable: false, lightSource: true, radianceLevel: Player.playerRadianceLevel, breakable: false);
            Texture2D[] torchTextureArray = new Texture2D[4];
            for (int i = 0; i < torchTextureArray.Length; i++) torchTextureArray[i] = Content.Load<Texture2D>("Tile/Lighting/torchTileTexture" + i);
            LightingTileType.torchTile = new TileType(torchTextureArray, "Torch", 0, standardBreakOverlay, new TileMaterial(0F), lightSource: true, radianceLevel: 8, collidable: false, requiresSupport: true, millisecondsBetweenFrames: 150);
            FunctioningTileType.doorTile = new TileType(new[] { Content.Load<Texture2D>("Tile/Object/woodDoorClosed"), Content.Load<Texture2D>("Tile/Object/woodDoorOpen") }, "Door", 1250, standardBreakOverlay, new TileMaterial(.06F), OnInteraction: new EventHandler((sender, e) =>
            {
                //Door function
                Tile senderTile = (Tile)sender;
                if (!currentMap.isContainingEntities(senderTile.rectangle) && !(new Rectangle((int)(mainPlayer.rigidBody.collisionRectangle.X - mainPlayer.relativeOffset.X), (int)(mainPlayer.rigidBody.collisionRectangle.Y - mainPlayer.relativeOffset.Y), mainPlayer.rigidBody.collisionRectangle.Width, mainPlayer.rigidBody.collisionRectangle.Height).Intersects(senderTile.rectangle)))
                {
                    //Add texture change and sound
                    senderTile.collidable = !senderTile.collidable;

                    //Change texture array location
                    if (senderTile.textureArrayLocation == 1)
                    {
                        senderTile.textureArrayLocation = 0;
                    }
                    else
                    {
                        senderTile.textureArrayLocation = 1;
                    }

                    //Check for other door tiles to interact with
                    if (senderTile.mapPosition[1] - 1 >= 0)
                    {
                        if (currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] - 1] != null && !currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] - 1].interactedThisTick && currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] - 1].tileType == FunctioningTileType.doorTile) currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] - 1].tileInteracted();
                    }
                    if (senderTile.mapPosition[1] + 1 < currentMap.tileMap.GetLength(1))
                    {
                        if (currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] + 1] != null && !currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] + 1].interactedThisTick && currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] + 1].tileType == FunctioningTileType.doorTile) currentMap.tileMap[senderTile.mapPosition[0], senderTile.mapPosition[1] + 1].tileInteracted();
                    }
                }
            }));
            SurfaceTiles.dirtRight = Content.Load<Texture2D>("Tile/Surface Tile/dirtSurfaceRight");
            SurfaceTiles.dirtLeft = Content.Load<Texture2D>("Tile/Surface Tile/dirtSurfaceLeft");
            SurfaceTiles.dirtBottom = Content.Load<Texture2D>("Tile/Surface Tile/dirtSurfaceBottom");
            SurfaceTiles.stoneBottom = Content.Load<Texture2D>("Tile/Surface Tile/stoneSurfaceBottom");
            SurfaceTiles.stoneTop = Content.Load<Texture2D>("Tile/Surface Tile/stoneSurfaceTop");
            SurfaceTiles.stoneLeft = Content.Load<Texture2D>("Tile/Surface Tile/stoneSurfaceLeft");
            SurfaceTiles.stoneRight = Content.Load<Texture2D>("Tile/Surface Tile/stoneSurfaceRight");
            SurfaceTiles.leavesLeft = Content.Load<Texture2D>("Tile/Wood/Tree Extras/leavesLeft");
            SurfaceTiles.leavesRight = Content.Load<Texture2D>("Tile/Wood/Tree Extras/leavesRight");
            SurfaceTiles.leavesBottom = Content.Load<Texture2D>("Tile/Wood/Tree Extras/leavesBottom");
            SurfaceTiles.leavesTop = Content.Load<Texture2D>("Tile/Wood/Tree Extras/leavesTop");
            SurfaceTiles.logLeft = Content.Load<Texture2D>("Tile/Wood/Tree Extras/logLeft");
            SurfaceTiles.logRight = Content.Load<Texture2D>("Tile/Wood/Tree Extras/logRight");
            SurfaceTiles.rootLeft = Content.Load<Texture2D>("Tile/Wood/Tree Extras/treeRootLeft");
            SurfaceTiles.rootRight = Content.Load<Texture2D>("Tile/Wood/Tree Extras/treeRootRight");

            //Load bullet textures
            Projectile.flyingBulletTexture = Content.Load<Texture2D>("Texture/Bullet/flyingBulletTexture");

            //Load drop sounds
            Drop.dropPickupSoundEffect = Content.Load<SoundEffect>("Drop/pickupSound");

            //TileType edits
            TerrainTileType.leavesTile.droppedItemType = null;

            //Load backgrounds
            Background defaultBackground = new Background("Grasslands", new SkyTile[]
            {
                new SkyTile(Content.Load<Texture2D>("Tile/Background/bkgr_mf_sky"), 25),
                new SkyTile(Content.Load<Texture2D>("Tile/Background/bkgr_mf_clouds"), 15, .0005F),
                new SkyTile(Content.Load<Texture2D>("Tile/Background/bkgr_mf_mountains"), 20),
                new SkyTile(Content.Load<Texture2D>("Tile/Background/bkgr_mf_hills"), 10)
            });

            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Generating Map..."); }));
            //Load map
            currentMap = new Map(1280, 640, GraphicsDevice.Viewport.Height, GraphicsDevice.Viewport.Width, defaultBackground, seed: currentSeed);
            const int terrainHeight = 320;
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Generating Terrain..."); }));
            currentMap.generateTerrain(terrainHeight: terrainHeight, levelVariation: 1, dirtDepth: 5);
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Generating Ore..."); }));
            currentMap.generateOre(terrainHeight: terrainHeight, coalMinDepth: terrainHeight, copperMinDepth: terrainHeight + 20, ironMinDepth: terrainHeight + 25, goldMinDepth: terrainHeight + 100, diamondMinDepth: terrainHeight + 225, coalRarity: 50, copperRarity: 35, ironRarity: 35, goldRarity: 20, diamondRarity: 5);
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Generating Caves..."); }));
            currentMap.generateCaves(numberOfCaves: 250, caveRadius: 1, maxCaveSteps: 2000);
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Generating Trees..."); }));
            currentMap.generateTrees(treeDensity: 1, treeHeight: 4, treeHeightVariance: 1, leafDensity: 9, branchDensity: 5);
            currentMap.addSurfaceTiles();
            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Loading Lighting Engine..."); }));
            currentMap.addInitialLighting();

            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Loading Player..."); }));
            //Load player
            Texture2D[] p1WalkTexture = new Texture2D[2];
            for (int i = 0; i < p1WalkTexture.Length; i++) p1WalkTexture[i] = Content.Load<Texture2D>("Player/P1/alienGreen_walk" + i);
            mainPlayer = new Player(Content.Load<Texture2D>("Player/P1/alienGreen_stand"), p1WalkTexture, Content.Load<Texture2D>("Player/P1/alienGreen_jump"), .75F, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, new Inventory("Inventory", new[] { 8, 3 }, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height / 2, yOffset: GraphicsDevice.Viewport.Height / 2), new StatManager(100));
            mainPlayer.playerGUI.craftInventory = new Inventory("Crafting", new[] { 3, 3 }, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height / 2, selectable: false);
            mainPlayer.playerGUI.craftButton = new GUIButton(new Rectangle(mainPlayer.playerGUI.craftInventory.inventoryCells[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0) - 1, 0].rectangle.Right + GUI.GUIItemPadding, mainPlayer.playerGUI.craftInventory.inventoryCells[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0) - 1, 0].rectangle.Top, mainPlayer.playerGUI.craftInventory.inventoryCells[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0) - 1, 0].rectangle.Width * 2, mainPlayer.playerGUI.craftInventory.inventoryCells[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0) - 1, 0].rectangle.Height), "Craft", new GUIButton.action(() =>
            {
                //Craft commands
                mainPlayer.playerGUI.attemptCraft();
            }
            ));
            mainPlayer.playerGUI.craftInventory.OnInventoryChanged += (sender1, e1) =>
            {
                mainPlayer.playerGUI.checkForCraftable();
            };
            currentMap.placePlayer(mainPlayer);

            //Load otherplayer content
            OtherPlayer.otherPlayerSpritesheet = mainPlayer.spritesheetManager.spritesheet;

            loadingScreen.Invoke(new Action(() => { loadingScreen.updateLoadingLabel("Loading Crafting Recipes..."); }));
            //Load crafting recipes
            loadCraftingRecipes();

            //Adds initial player items
            mainPlayer.playerGUI.playerInventory.addItem(new Item(LightingTileType.torchTile.tileItemType), 4);

            //TEST MOVEMENT AI
            testAI = new Enemy(Content.Load<Texture2D>("Player/P1/alienGreen_stand"), p1WalkTexture, Content.Load<Texture2D>("Player/P1/alienGreen_jump"), new[] { mainPlayer.drawRectangle.Width, mainPlayer.drawRectangle.Height }, .75F, new RigidBody(1, mainPlayer.rigidBody.collisionRectangle), new MovementAI(10, .1F, .23F, playerAggression: true, minDistanceFromTarget: 2 * currentMap.tileSize), new StatManager(100), 1000, new Enemy.Attack((target) =>
            {
                //Actions at attack
                if (target is Player)
                {
                    ((Player)target).statManager.health -= 5;
                }
            }));
            testAI.movementAI.rigidBody.collisionRectangle.X += 10 * Game1.currentMap.tileSize;
            currentMap.mapEntities.Add(testAI);

            //END OF LOADING. Request map log
            if (!networkManager.host) networkManager.localGameClient.sendString(((int)GameServer.NetworkKeyword.hostRequest).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.mapLogRequest).ToString() + GameServer.messageSeparator);

            //CLOSE LOADING SCREEN
            loadingScreenThread.Abort();
            loadingScreenThread = new Thread(new ThreadStart(() => { loadingScreen.Close(); }));
        }

        //TEST
        public Enemy testAI;

        private void loadCraftingRecipes()
        {
            //COAL
            OreTileType.coalOre.droppedItemType = new ItemType("Coal", Content.Load<Texture2D>("Item/ingotTexture"), Color.Black);

            //DIAMOND
            OreTileType.diamondOre.droppedItemType = new ItemType("Diamond", Content.Load<Texture2D>("Item/ingotTexture"), Color.Aqua);

            //IRON INGOT
            ItemType ironIngot = new ItemType("Iron Ingot", Content.Load<Texture2D>("Item/ingotTexture"), Color.DarkGray);
            ironIngot.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            ironIngot.craftingRecipe[0, 0, 0] = 1; ironIngot.craftingRecipe[0, 0, 1] = OreTileType.coalOre.droppedItemType;
            ironIngot.craftingRecipe[1, 0, 0] = 2; ironIngot.craftingRecipe[1, 0, 1] = OreTileType.ironOre.droppedItemType;
            ironIngot.amountMadeFromCraft = 2;
            ironIngot.craftable = true;

            //COPPER INGOT
            ItemType copperIngot = new ItemType("Copper Ingot", Content.Load<Texture2D>("Item/ingotTexture"), Color.SaddleBrown);
            copperIngot.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            copperIngot.craftingRecipe[0, 0, 0] = 1; copperIngot.craftingRecipe[0, 0, 1] = OreTileType.coalOre.droppedItemType;
            copperIngot.craftingRecipe[1, 0, 0] = 2; copperIngot.craftingRecipe[1, 0, 1] = OreTileType.copperOre.droppedItemType;
            copperIngot.amountMadeFromCraft = 2;
            copperIngot.craftable = true;

            //GOLD INGOT
            ItemType goldIngot = new ItemType("Gold Ingot", Content.Load<Texture2D>("Item/ingotTexture"), Color.Goldenrod);
            goldIngot.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            goldIngot.craftingRecipe[0, 0, 0] = 1; goldIngot.craftingRecipe[0, 0, 1] = OreTileType.coalOre.droppedItemType;
            goldIngot.craftingRecipe[1, 0, 0] = 2; goldIngot.craftingRecipe[1, 0, 1] = OreTileType.goldOre.droppedItemType;
            goldIngot.amountMadeFromCraft = 2;
            goldIngot.craftable = true;

            //WOOD
            TerrainTileType.woodTile.tileItemType.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            TerrainTileType.woodTile.tileItemType.craftingRecipe[0, 0, 0] = 1; TerrainTileType.woodTile.tileItemType.craftingRecipe[0, 0, 1] = TerrainTileType.logTile.droppedItemType;
            TerrainTileType.woodTile.tileItemType.amountMadeFromCraft = 4;
            TerrainTileType.woodTile.tileItemType.craftable = true;

            //PICKAXE 1
            loadPickaxe(TerrainTileType.woodTile.tileItemType, TerrainTileType.stoneTile.tileItemType, 100, 1, "Stone Pickaxe");

            //PICKAXE 2
            loadPickaxe(TerrainTileType.woodTile.tileItemType, copperIngot, 200, 1.5F, "Copper Pickaxe");

            //PICKAXE 3
            loadPickaxe(TerrainTileType.woodTile.tileItemType, ironIngot, 200, 2, "Iron Pickaxe");

            //PICKAXE 4
            loadPickaxe(TerrainTileType.woodTile.tileItemType, goldIngot, 200, 3, "Gold Pickaxe");

            //PICKAXE 5
            loadPickaxe(TerrainTileType.woodTile.tileItemType, OreTileType.diamondOre.droppedItemType, 300, 4, "Diamond Pickaxe");

            //Torch
            LightingTileType.torchTile.tileItemType.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            LightingTileType.torchTile.tileItemType.craftingRecipe[0, 0, 0] = 1; LightingTileType.torchTile.tileItemType.craftingRecipe[0, 0, 1] = OreTileType.coalOre.droppedItemType;
            LightingTileType.torchTile.tileItemType.craftingRecipe[0, 1, 0] = 1; LightingTileType.torchTile.tileItemType.craftingRecipe[0, 1, 1] = TerrainTileType.woodTile.tileItemType;
            LightingTileType.torchTile.tileItemType.amountMadeFromCraft = 4;
            LightingTileType.torchTile.tileItemType.craftable = true;

            //Door
            FunctioningTileType.doorTile.tileItemType.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 0, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 0, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 1, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 1, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 2, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[0, 2, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 0, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 0, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 1, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 1, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 2, 0] = 1; FunctioningTileType.doorTile.tileItemType.craftingRecipe[1, 2, 1] = TerrainTileType.woodTile.tileItemType;
            FunctioningTileType.doorTile.tileItemType.amountMadeFromCraft = 1;
            FunctioningTileType.doorTile.tileItemType.craftable = true;
        }

        private void loadPickaxe(ItemType handleItem, ItemType axeItem, int durability, float tileDestructionMultiplierAdditive, string name)
        {
            EventHandler pickaxeOnTileBreakAction = new EventHandler((sender, e) =>
            {
                mainPlayer.playerGUI.playerInventory.selectedCell.updateItemDurability();
            });

            ItemType pickaxe = new ItemType(name, Content.Load<Texture2D>("Item/pickaxeTexture"), Color.White, durability, equipAction: new ItemType.ItemAction(() =>
            {
                //Equip Action
                mainPlayer.tileDestructionMultiplier += tileDestructionMultiplierAdditive;
                mainPlayer.OnTileBreak += pickaxeOnTileBreakAction;
                return true;
            }
            ), unequipAction: new ItemType.ItemAction(() =>
            {
                //Unequip Action
                mainPlayer.tileDestructionMultiplier -= tileDestructionMultiplierAdditive;
                mainPlayer.OnTileBreak -= pickaxeOnTileBreakAction;
                return true;
            }));
            pickaxe.craftingRecipe = new object[mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(0), mainPlayer.playerGUI.craftInventory.inventoryCells.GetLength(1), 2];
            pickaxe.craftingRecipe[0, 0, 0] = 1; pickaxe.craftingRecipe[0, 0, 1] = axeItem;
            pickaxe.craftingRecipe[1, 0, 0] = 1; pickaxe.craftingRecipe[1, 0, 1] = axeItem;
            pickaxe.craftingRecipe[2, 0, 0] = 1; pickaxe.craftingRecipe[2, 0, 1] = axeItem;
            pickaxe.craftingRecipe[1, 1, 0] = 1; pickaxe.craftingRecipe[1, 1, 1] = handleItem;
            pickaxe.craftingRecipe[1, 2, 0] = 1; pickaxe.craftingRecipe[1, 2, 1] = handleItem;
            pickaxe.craftable = true;
        }

        protected override void UnloadContent()
        {
            
        }

        private KeyboardState pastKeyboardState = Keyboard.GetState();
        protected override void Update(GameTime gameTime)
        {
            //Update gameTime
            Game1.gameTime = gameTime;

            //Update server
            networkManager.updateGameServer();

            //Receive data from server
            networkManager.receiveInformationFromGameServer();

            //Update day-night cycle
            LightingManager.updateDayNightCycle(gameTime);

            //Player controller
            mainPlayer.keyboardController(Keyboard.GetState(), Mouse.GetState());
            mainPlayer.mouseController(Mouse.GetState(), gameTime, Keyboard.GetState());

            //Update physics
            mainPlayer.updatePhysics();

            //TEST PROJECTILE
            if (pastKeyboardState.IsKeyDown(Keys.R) && Keyboard.GetState().IsKeyUp(Keys.R))
            {
                currentMap.entityAddQueue.Add(new Projectile(Projectile.flyingBulletTexture, new Vector2(1F, -1F), new RigidBody(1, new Rectangle(mainPlayer.rigidBody.collisionRectangle.X - (int)(mainPlayer.relativeOffset.X), mainPlayer.rigidBody.collisionRectangle.Y - (int)(mainPlayer.relativeOffset.Y), 10, 10)), 100, affectedByGravity: false, entityBlacklist: new Entity[] { Game1.mainPlayer }));
            }

            //Update map
            currentMap.update(gameTime, GraphicsDevice.Viewport.Width);

            //TEST
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                LightingManager.skyLightIntensity -= .005F;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                LightingManager.skyLightIntensity += .005F;
            }

            //Send information to server
            networkManager.sendInformationToGameServer();

            //TEST
            pastKeyboardState = Keyboard.GetState();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(LightingManager.skyColor);

            spriteBatch.Begin();

            //Draw background
            currentMap.currentBackground.drawSkyTiles(spriteBatch, GraphicsDevice.Viewport.Height, GraphicsDevice.Viewport.Width);

            spriteBatch.End();

            //Transforms
            Matrix transform = Matrix.CreateTranslation(mainPlayer.relativeOffset.X, mainPlayer.relativeOffset.Y, 0);
            spriteBatch.Begin(new SpriteSortMode(), null, null, null, null, null, transform);

            //Draw map
            currentMap.drawMap(spriteBatch, gameTime);

            //Draw other players
            foreach (OtherPlayer b in OtherPlayer.otherPlayers) b.draw(spriteBatch);

            //Removes transform
            spriteBatch.End();

            spriteBatch.Begin();

            //Draw player
            mainPlayer.draw(spriteBatch);

            //TEST DRAW PLAYER HEALTH
            spriteBatch.DrawString(GUI.GUIFont, LightingManager.skyLightIntensity.ToString(), Vector2.Zero, Color.White);

            //Draw GUI
            mainPlayer.playerGUI.drawGUI(spriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, Mouse.GetState());

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}