using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;

namespace TurretTest
{
    /// <summary>
    /// A játékvezérlõ
    /// </summary>
    /// 
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        #region Konstansok-változók
        //konstansok, illetve statikus értékek
        const float HUDSizeP = 0.18f, BCCHANCE = .025f;
        const int BaseWidth = 1000, BaseHeight = 492;
        public static Viewport Screen;
        public static GameScore Score;
        public static float GlobalScale;

        //grafikával kapcsolatos mezõk, textúrák
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D TurretTexture, MousePointer, FadeOutScreen, CrossHair, CrossHairBlue, WallHitTexture;
        SpriteFont Font, CountDownFont;
        
        //Létfontosságú listák, melyek tartalmazzák a lövedékeket, vagy a robbanások információit, hanghatásait, valamint a háttérképeket
        public List<Projectile> Projectiles = new List<Projectile>();        
        List<Player> CurrentPlayers = new List<Player>();
        List<ExplosionInfo> CurrentExplosions = new List<ExplosionInfo>();
        List<SoundEffect> ExplosionEffects = new List<SoundEffect>();
        List<Texture2D> Backgrounds = new List<Texture2D>();
        
        //Egyéb hangeffektek, valamint háttérzenék
        SoundEffect DeathLaugh, WallHitSE;
        SoundEffectInstance WallHitSI;
        Song Level1Intro, Level1, Level2, Level3, Level4Intro, Level4, Level5Intro, Level5, MenuMusic;        
                
        Turret turret;
        Random RndG = new Random(17);
        KeyboardState newState, oldState;
        MyTimer ExplosionTimer;

        //Menük, üzenetablakok, az egér, valamint a kijelzõ
        HUD HUDTry;
        MsgBox Quit, DeathMsg, ShowScore, HighScoresWindow;
        Menu MainMenu, LevelSelectMenu;
        MouseCursor TheCursor;

        Level CurrentLevel;
        ShortVisualEffect WallHitEffect;        

        int chkLoadNextLevel = 0,//ebben számoljuk mennyi idõ telt el, a pályateljesítés ellenõrzéséhez szükséges 
            CountDown = 0, //adott pálya kezdete elõtti visszaszámlálást megvalósító változó
            LvlElapsedTime = 0; //adott pályán eltelt játékidõt méri
        string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();//a program verziója        
        bool DirectlyLoaded = false, MenuLoaded = false;//egyéb betöltést jelzõ változók        

        //Aktuális pálya ill. szint jelzéséhez létrehozott enumeráció
        enum Level
        {
            Menu,
            Soldier,
            Veterans,
            Tank,
            Cyborg,
            BlackWidow
        }

        //Egyes robbanások információinak tárolására létrehozott struktúra
        struct ExplosionInfo
        {
            public Vector2 position;
            public int Time, Range, Damage;
        }

        #endregion

        #region Betöltés-Inícializálás
        public Game1(bool FullScreen)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.IsFullScreen = FullScreen;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            ExplosionTimer = new MyTimer(delegate()
            {
                for(int i = 0; i < CurrentExplosions.Count; i++){
                    CurrentPlayers.ForEach(delegate(Player player)
                    {
                        float distance = Vector2.Distance(player.position, CurrentExplosions[i].position);
                        if (distance < CurrentExplosions[i].Range)
                                player.Hurt((int)(CurrentExplosions[i].Damage * 0.2f));                                                
                    }
                    );
                    ExplosionInfo ToDecrement = CurrentExplosions[i];
                    ToDecrement.Time -= 200;
                    CurrentExplosions[i] = ToDecrement;
                }

                CurrentExplosions.RemoveAll(explosion => explosion.Time <= 0);                              
            }, 200);
            
            Quit = new MsgBox(this, "Biztos, hogy ki akarsz lépni?", true);
            MainMenu = new Menu(this, 4);
            ShowScore = new MsgBox(this, "", false);
            TheCursor = new MouseCursor(this, .125f);
            CurrentLevel = Level.Menu;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Drawings.Initialize(GraphicsDevice);
            
            TurretTexture = Content.Load<Texture2D>("Sprites\\Turret\\Turret");
            MousePointer = Content.Load<Texture2D>("Menu\\MenuCursor");
            CrossHair = Content.Load<Texture2D>("Menu\\CrossHair");
            CrossHairBlue = Content.Load<Texture2D>("Menu\\CrossHairBlue");
            
            Font = Content.Load<SpriteFont>("Fonts\\GameFont");
            CountDownFont = Content.Load<SpriteFont>("Fonts\\CountDown");
            FadeOutScreen = new Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Color[] Temp = new Color[FadeOutScreen.Width * FadeOutScreen.Height];
            for (int i = 0; i < Temp.Length; i++)
                Temp[i] = Color.Black;
            FadeOutScreen.SetData(Temp);

            WallHitTexture = Content.Load<Texture2D>("Sprites\\WallHit");
            WallHitEffect = new ShortVisualEffect(this, WallHitTexture, 1f, 3, false);
            WallHitEffect.Center = new Vector2(WallHitTexture.Width / 2, WallHitTexture.Height * .8f);

            Backgrounds.Add(Content.Load<Texture2D>("Menu\\MainBackground"));
            Backgrounds.Add(Content.Load<Texture2D>("Menu\\Level1_Back"));
            Backgrounds.Add(Content.Load<Texture2D>("Menu\\Level2_Back"));
            Backgrounds.Add(Content.Load<Texture2D>("Menu\\Level3_Back"));
            Backgrounds.Add(Content.Load<Texture2D>("Menu\\Level4_Back"));
            Backgrounds.Add(Content.Load<Texture2D>("Menu\\Level5_Back"));

            for (int i = 1; i < 10; i++)
                ExplosionEffects.Add(Content.Load<SoundEffect>("Sounds\\Misc\\rocket" + i));

            for (int i = 1; i < 4; i++)
                ExplosionEffects.Add(Content.Load<SoundEffect>("Sounds\\Misc\\grenade" + i));

            Level1Intro = Content.Load<Song>("Sounds\\Soldier\\Level 1 Intro V1");
            Level1 = Content.Load<Song>("Sounds\\Soldier\\Level 1 Background");
            Level2 = Content.Load<Song>("Sounds\\Soldier\\Level 2 Intro-Background");
            Level3 = Content.Load<Song>("Sounds\\Tank\\Level 3 - Background");
            Level4Intro = Content.Load<Song>("Sounds\\Cyborg\\Level 4 Intro V2");
            Level4 = Content.Load<Song>("Sounds\\Cyborg\\Level 4 - Iron-Man");
            Level5Intro = Content.Load<Song>("Sounds\\BlackWidow\\Level 5 Intro V2");
            Level5 = Content.Load<Song>("Sounds\\BlackWidow\\Level 5 - Cautious-Path");
            MenuMusic = Content.Load<Song>("Sounds\\Misc\\Menu");
            DeathLaugh = Content.Load<SoundEffect>("Sounds\\Misc\\laugh2");
            WallHitSE = Content.Load<SoundEffect>("Sounds\\Misc\\WallHit");
            WallHitSI = WallHitSE.CreateInstance();
            WallHitSI.Volume = 0.2f;

            Screen = new Viewport(0, 0, GraphicsDevice.Viewport.Width, (int)(GraphicsDevice.Viewport.Height - HUDSizeP * GraphicsDevice.Viewport.Height));
            GlobalScale = (float)(Math.Sqrt(Screen.Width * Screen.Width + Screen.Height * Screen.Height) / Math.Sqrt(BaseWidth * BaseWidth + BaseHeight * BaseHeight));

            if ((float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height > 1.7)
                GlobalScale *= 0.8f;

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        #endregion

        #region Fõvezérlés
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {         
            if (!MenuLoaded)
            {//Menük betöltése, és hozzáadása a komponensekhez
                Components.Add(MainMenu);
                Components.Add(TheCursor);
                Components.Add(WallHitEffect);
                TheCursor.AddCursor("NormálKurzor", MousePointer);
                TheCursor.AddCursor("Célkereszt", CrossHair);
                TheCursor.AddCursor("CélkeresztKék", CrossHairBlue);
                TheCursor.CurrentCursor = "Célkereszt";
                MainMenu.yOffset = 225 + (GraphicsDevice.Viewport.Height - 225 - 4 * 90) / 2;
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\NewGame"), LoadLevelOne, true);
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\LoadLevel"), delegate() {                   
                    if (Score == null)
                        Score = new GameScore("scores.dat", 5, new int[] { 180, 240, 200, 180, 360 });//tárolt pontok betöltése
                    else
                        Score.LoadScores();
                    
                    if (LevelSelectMenu == null)
                    {                      
                        LevelSelectMenu = new Menu(this, 5);
                        Components.Add(LevelSelectMenu);                        
                        int Progress = Score.GetProgress();
                        LevelSelectMenu.yOffset = 250 + (GraphicsDevice.Viewport.Height - 250 - 4 * 90) / 2;
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level1"), LoadLevelOne, true);//Gomb hozzáadása a menühöz
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level2"), () => { DirectlyLoaded = true; LoadLevelTwo(); }, Progress > 0 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level3"), () => { DirectlyLoaded = true; LoadLevelThree(); }, Progress > 1 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level4"), () => { DirectlyLoaded = true; LoadLevelFour(); }, Progress > 2 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level5"), () => { DirectlyLoaded = true; LoadLevelFive(); }, Progress > 3 ? true : false);
                    }
                    else//ha már be van töltve a menü, akkor csak rendeznünk kell a gombokat, mert lehet a játékos tovább jutott a játékban, és így engedélyezni kell néhány menüpontot/gombot
                        ManageLevelButtons();
                    
                    LevelSelectMenu.Show = true;
                    CurrentLevel = Level.Menu;
                }, true);                
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\HighScores"), delegate() 
                {
                    List<KeyValuePair<string, int>> HScores = new List<KeyValuePair<string, int>>();
                    LoadHighScores(HScores);

                    string ScoresMessage = "";
                    foreach (var score in HScores)                    
                        ScoresMessage += score.Key + "  ->  " + score.Value + " pont\n";

                    ShowHighScores(ScoresMessage);
                }, true);
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\Quit"), delegate() { this.Exit(); }, true);
              
                MainMenu.Show = true;
                MenuLoaded = true;
                ExplosionTimer.Start();

                //a kilépés üzenetablak gombjainak beállítása következik
                Components.Add(Quit);
                Quit.YesAction = delegate()
                {
                    Quit.HideWindow();
                    ClearComponents();                    
                    MainMenu.Show = true;
                    CurrentLevel = Level.Menu;
                    turret.Destroy();
                    turret.Dispose();
                    turret = null;
                    Score = null;
                    MediaPlayer.Stop();
                };
                Quit.NoAction = delegate()
                {
                    Quit.HideWindow();
                    PauseAll();                    
                };
            }

            if (IsActive)//Ha a játék jelenleg az aktív alkalmazás, vagyis az ablak fókuszban van
            {
                //lekérjük a billentyûzet állapotát
                newState = Keyboard.GetState();
                if (newState.IsKeyDown(Keys.Pause) && !oldState.IsKeyDown(Keys.Pause) && !Quit.Show) //Pause esetén           
                    PauseAll();
                else if (Keyboard.GetState().IsKeyDown(Keys.Escape) && !oldState.IsKeyDown(Keys.Escape))//Escape esetén
                {
                    if (LevelSelectMenu != null && LevelSelectMenu.Show)
                    {
                        LevelSelectMenu.Show = false;
                        MainMenu.Show = true;
                    }
                    else
                    {
                        if (!MainMenu.Show && (DeathMsg == null || DeathMsg != null && !DeathMsg.Show) &&
                            (ShowScore == null || ShowScore != null && !ShowScore.Show) && CountDown == 0)//Játék közben, ha nincs más ablak elõl, akkor megjelenítjük vagy elrejtjük a kilépés ablakot
                        {
                            if (!Quit.Show && !Quit.HideStarted)
                            {
                                Quit.ShowWindow();
                                PauseAll();
                            }
                            else
                            {
                                Quit.HideWindow();
                                PauseAll();
                            }
                        }
                    }
                }
                oldState = newState;
            }

            lock (Components)
            {
            retry:
                try
                {
                    Components.ToList().ForEach(delegate(IGameComponent component)//destination index meg hasonlók
                    {
                        if (component is GameObject)
                            if (((GameObject)component).Disposable)
                                ((GameObject)component).Dispose();
                    });
                }
                catch
                {
                    goto retry;
                }
            }

            if (CurrentLevel != Level.Menu && !Quit.Show)
            {
                chkLoadNextLevel += gameTime.ElapsedGameTime.Milliseconds;

                if (chkLoadNextLevel > 1500)
                {
                    chkLoadNextLevel = 0;
                    CheckLevelStatus();
                }
                Projectiles.RemoveAll(item => item == null || !item.Alive);
                
                if(chkLoadNextLevel % 10 == 0)
                    CollisionPlayers();

                BulletsHitCollision();
            }

            if (CountDown < 0)//Ha a visszaszámlálás véget ért, elindítjuk a játékot
            {
                if(turret.Paused)
                    PauseAll();
                CountDown = 0;
                TheCursor.CurrentCursor = "Célkereszt";//játék közben a célkereszt egérmutatóra váltunk

            }

            if (HighScoresWindow != null && !HighScoresWindow.Show && !HighScoresWindow.HideStarted)
            {
                HighScoresWindow.Dispose();
                HighScoresWindow = null;
            }

            if (turret != null && CurrentLevel != Level.Menu /*&& !turret.Paused*/)
                LvlElapsedTime += gameTime.ElapsedGameTime.Milliseconds;

            TheCursor.CurrentCursor = CurrentLevel == Level.Menu || Quit.Show || (DeathMsg != null && DeathMsg.Show)
                || (ShowScore != null && ShowScore.Show) ? "NormálKurzor" : CurrentLevel == Level.Tank ? "CélkeresztKék": "Célkereszt";

            //adott szintnek/képernyõnek megfelelõ háttérzene kiválasztása, és lejátszása (ha épp nem megy más)
            switch (CurrentLevel)
            {
                case Level.Soldier:
                    if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(Level1);
                    break;
                case Level.Veterans:
                     if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(Level2);
                    break;
                case Level.Tank:
                    if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(Level3);
                    break;
                case Level.Cyborg:
                    if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(Level4);
                    break;
                case Level.BlackWidow:
                    if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(Level5);
                    break;
                case Level.Menu:
                    if (MediaPlayer.State == MediaState.Stopped)
                        MediaPlayer.Play(MenuMusic);
                    break;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Az idõközönkénti ellenõrzés ezt a metódust futtatja le, mely megnézi meghaltak e már az ellenségek és/vagy a játékos, 
        /// és az annak megfelelõ ablakot jeleníti meg -> halál üzenet, melynél lehetõség van az újrakezdésre, ha nem fogyott el az életünk
        /// VAGY ha nyertünk, az adott pályán elért pontszámokat megmutatjuk és tovább lépünk a következõ pályára
        /// </summary>
        private void CheckLevelStatus()
        {
            lock (this)
            {
                if (Components.Where(component => component is Projectile).ToList().Count == 0 && CurrentExplosions.Count == 0 && CurrentLevel != Level.Menu
                   && (DeathMsg == null || DeathMsg != null && !DeathMsg.Show) && !ShowScore.Show)
                {
                    bool EnemyDead = Components.Where(component => component is Enemy && ((Enemy)component).Alive).ToList().Count == 0 || Components.Count(player=> player is Enemy) == 0
                    ? true : false;

                    if (!turret.Alive)
                    {
                        if (DeathMsg != null)
                            DeathMsg.Dispose();

                        DeathMsg = new MsgBox(this, EnemyDead ? "Döntetlen! A pályát újrakezdheted, életet nem veszítesz." : (turret.Lives > 0 ? "MEGHALTÁL ÉS ELVESZÍTETTED EGY ÉLETEDET! ÚJRAKEZDED?" :
                                "MEGHALTÁL ÉS ELFOGYOTT AZ ÖSSZES ÉLETED! A JÁTÉKOT ÚJRAKEZDHETED A FÕMENÜBÕL, VAGY FOLYTATHATOD A PÁLYAVÁLASZTÁS MENÜBÕL."),
                                EnemyDead ? false : turret.Lives > 0 ? true : false);

                        if (turret.Lives > 0 && !EnemyDead)
                            DeathMsg.NoAction = delegate()
                            {
                                DeathMsg.HideWindow();
                                ClearComponents();
                                MainMenu.Show = true;
                                CurrentLevel = Level.Menu;
                                turret.Destroy();
                                turret.Dispose();
                                turret = null;
                                Score = null;
                                MediaPlayer.Stop();
                            };

                        DeathMsg.YesAction = EnemyDead ? (Action)delegate()
                        {
                            DeathMsg.HideWindow();
                            CurrentLevel--;
                            LoadNextLevel();
                            Score.ResetLevel();
                            turret.Lives++;
                        }
                        : turret.Lives > 0 ? (Action)delegate()
                        {
                            DeathMsg.HideWindow();
                            CurrentLevel--;
                            LoadNextLevel();
                            Score.ResetLevel();
                        }
                        : (Action)delegate()
                        {
                            DeathMsg.HideWindow();
                            CurrentLevel = Level.Menu;
                            MainMenu.Show = true;
                            Score = null;
                            turret.Destroy();
                            turret.Dispose();
                            turret = null;
                            ClearComponents();
                            MediaPlayer.Stop();
                        };

                        if (!DeathMsg.Show)
                        {
                            Components.Add(DeathMsg);
                            DeathMsg.ShowWindow();
                        }
                                                
                        DeathLaugh.Play();
                    }
                    else if (EnemyDead)
                    {
                        turret.Enabled = false;
                        Score.LevelFinished(LvlElapsedTime / 1000, turret.Lives);
                        
                        int tempScore = Score.GetScoreToLevel();
                        string Message = Score.GetScore();
                        if (tempScore > 5000 && Score.GetBonusGained() == 0)
                        {
                            turret.Lives++;
                            Score.BonusGained();
                            Message += "\n5000 pont : +1 élet";
                        }

                        ShowScore = new MsgBox(this, Message, false);
                        ShowScore.NoMessageProcessing = true;
                        Components.Add(ShowScore);
                        ShowScore.YesAction = delegate() { ShowScore.HideWindow(); turret.Enabled = true; LoadNextLevel(); };
                        ShowScore.ShowWindow();
                    }
                }
            }
        }

        /// <summary>
        /// Komponensek törlése a komponensekbõl, és az állandóak visszarakása. Csak egy teljes Clear utasítás tudja eltûntetni az összes
        /// fennmaradó komponenst, mely nem akar önmagától, vagy épp ráhatással megsemmisülni.
        /// </summary>
        private void ClearComponents()
        {
            CurrentPlayers.ForEach(delegate(Player player)
            {
                if (player is Enemy)
                {
                    player.Destroy();
                    player.Dispose();
                }
            });

            Projectiles.Clear();
            CurrentPlayers.Clear();
            CurrentExplosions.Clear();
            Components.Clear();
            Components.Add(MainMenu);
            Components.Add(LevelSelectMenu);
            if (ShowScore != null)
                Components.Add(ShowScore);
            if (DeathMsg != null)
                Components.Add(DeathMsg);
            if (HighScoresWindow != null)
                Components.Add(HighScoresWindow);
            Components.Add(Quit);
            Components.Add(TheCursor);
        }

        /// <summary>
        /// Minden játékelem, effekt feldolgozásának szünetetletése, úgymond pillanat-állj funkciója.
        /// </summary>
        private void PauseAll()
        {
            Components.ToList().ForEach(component =>
            {
                if (component is MovingGameObject)
                    ((MovingGameObject)component).Pause();
                else if (component is ShortVisualEffect)
                    ((ShortVisualEffect)component).Pause();
            });
            ExplosionTimer.Pause();
        }

        /// <summary>
        /// A szintválasztás menü gombjainak újrabeállítása
        /// </summary>
        private void ManageLevelButtons()
        {
            int Progress = Score.GetProgress();
            LevelSelectMenu.EnableButton(1, Progress > 0 ? true : false);
            LevelSelectMenu.EnableButton(2, Progress > 1 ? true : false);
            LevelSelectMenu.EnableButton(3, Progress > 2 ? true : false);
            LevelSelectMenu.EnableButton(4, Progress > 3 ? true : false);
        }
        #endregion

        #region Ütközésérzékelés

        /// <summary>
        /// A metódus a lövedékek ütközését ellenõrzi lövedékekkel ÉS a játékosokkal. Ütközés esetén meghívja mindkét játékelem találat 
        /// metódusát (Hit), így mindig a megfelelõ dolog történik.
        /// A metódus elõször azt ellenõrzi, hogy a két játékelem közötti ütközés lehetséges e, illetve foglalkozunk e vele. 
        /// Ez a játékelem típusából megmondható. Ha foglalkoznunk kell vele, akkor megnézzük a körülhatároló téglalapjaik összeérnek e.
        /// Ha igen, pixel-pontos ütközést is vizsgálunk, mely ha talál átfedést, hivatalosan is kimondhatjuk, hogy összetalálkozott a két játékelem
        /// </summary>
        void BulletsHitCollision()
        {
            lock (this)
            {
                for (int i = 0; i < Projectiles.Count; i++)
                {
                    for (int j = 0; j < Components.Count; j++)
                    {
                        if (!(Components[j] is GameObject) || Components[j] == null || Projectiles[i] == null ||
                            (Components[j] is Enemy && Projectiles[i].FromEnemy && !Projectiles[i].RicochetStarted && !(Projectiles[i] is VampiricBlast)) ||
                            (Components[j] is Turret && (!Projectiles[i].FromEnemy && !Projectiles[i].RicochetStarted || Projectiles[i].FromEnemy && Projectiles[i] is VampiricBlast && !((VampiricBlast)Projectiles[i]).BulletState) ||                            
                            (Components[j] is Projectile && (!((Projectile)Components[j]).Shootable && !Projectiles[i].Shootable)) ||
                            (Components[j] is SuicideDrone && Projectiles[i] is SuicideDrone) ||
                            !Projectiles[i].Alive || !((GameObject)Components[j]).Alive))
                            continue;

                        if (Projectiles[i].BoundingRectangle.Intersects(((GameObject)Components[j]).BoundingRectangle))
                        {
                            if (SomeMath.IntersectPixels(Projectiles[i].ShapeTransform, Projectiles[i].sprite.Width,
                                            Projectiles[i].sprite.Height, Projectiles[i].TextureData,
                                            ((GameObject)Components[j]).ShapeTransform, ((GameObject)Components[j]).sprite.Width,
                                            ((GameObject)Components[j]).sprite.Height, ((GameObject)Components[j]).TextureData))
                            {
                                ((GameObject)Components[j]).Hit(Projectiles[i]);
                                Projectiles[i].Hit((GameObject)Components[j]);
                            }
                        }
                    }

                    if (i > Projectiles.Count - 1)
                        continue;

                    for (int j = i + 1; j < Projectiles.Count; j++)
                    {
                        if (Projectiles.Count < i + 1 || Projectiles.Count < j + 1|| Projectiles[i] == null || Projectiles[j] == null || Projectiles[i].RicochetStarted || Projectiles[j].RicochetStarted ||
                            RndG.NextDouble() > BCCHANCE || Projectiles[i] is Rocket || Projectiles[j] is Rocket ||
                            (Projectiles[i].FromEnemy && Projectiles[j].FromEnemy))
                            continue;                        

                        if (Projectiles[i].BoundingRectangle.Intersects(Projectiles[j].BoundingRectangle))
                        {
                            if (SomeMath.IntersectPixels(Projectiles[i].ShapeTransform, Projectiles[i].sprite.Width,
                                            Projectiles[i].sprite.Height, Projectiles[i].TextureData,
                                            Projectiles[j].ShapeTransform, Projectiles[j].sprite.Width,
                                            Projectiles[j].sprite.Height, Projectiles[j].TextureData))
                            {
                                Projectiles[i].Hit(Projectiles[j]);
                                Projectiles[j].Hit(Projectiles[i]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A játékosok ütközésének vizsgálata játékossal. Külön metódus szükséges rá, mert ha túl sokáig érnek egymásba a játékosok,
        /// akkor a túl sok pixel-pontos vizsgálat lelassíthatja a játékot. Éppen ezért a metódust másodpercenként csak néhányszor hívjuk meg.
        /// </summary>
        void CollisionPlayers()
        {
            for (int i = 0; i < CurrentPlayers.Count - 1; i++ )
            {
                if (!CurrentPlayers[i].Alive)
                    continue;

                for (int j = i + 1; j < CurrentPlayers.Count; j++)
                {
                    if (!CurrentPlayers[j].Alive)
                        continue;

                    if (CurrentPlayers[i].BoundingRectangle.Intersects(CurrentPlayers[j].BoundingRectangle))
                    {
                        if (SomeMath.IntersectPixels(CurrentPlayers[i].ShapeTransform, CurrentPlayers[i].sprite.Width,
                                            CurrentPlayers[i].sprite.Height, CurrentPlayers[i].TextureData,
                                            CurrentPlayers[j].ShapeTransform, CurrentPlayers[j].sprite.Width,
                                            CurrentPlayers[j].sprite.Height, CurrentPlayers[j].TextureData))
                        {
                            CurrentPlayers[i].Hit(CurrentPlayers[j]);
                            CurrentPlayers[j].Hit(CurrentPlayers[i]);
                        }
                    }
                }
            }
        }
        #endregion     

        #region Kívülrõl elérhetõ metódusok - Jelzõ függvények a komponenseknek

        /// <summary>
        /// Kívülrõl elérhetõ függvény a játékelemek számára, melyek felrobbannak. Itt átadják az adatokat magukról, és bekerülnek a
        /// az aktuális robbanások listájába. A végén természetesen egy véletlenszerû robbanás-hangeffekt is megszólaltatásra kerül.
        /// </summary>
        /// <param name="Entity"></param>
        /// <param name="gm"></param>
        public void Explosion(GameObject Entity, GameObject gm)
        {
            CurrentExplosions.Add(new ExplosionInfo()
            {
                position = gm.position,
                Range = gm is Rocket ? ((Rocket)gm).AOERange : ((Grenade)gm).AOERange,
                Time = 800,
                Damage = gm is Rocket ? ((Rocket)gm).Damage : ((Grenade)gm).Damage
            }
            );

            if (gm is Grenade)
                ExplosionEffects[ExplosionEffects.Count - RndG.Next(3) - 1].Play();
            else
                ExplosionEffects[RndG.Next(ExplosionEffects.Count - 3)].Play();
        }
        
        /// <summary>
        /// Ha lövés történt, arról egyes játékosoknak is tudniuk, vagyis hallaniuk kell
        /// </summary>
        /// <param name="from">Kitõl jött a lövés, ha Ellenségtõl, akkor hamis</param>
        /// <param name="position">A lövés helye</param>
        public void Shot(bool from, Vector2 position)
        {
            if (!from)
            {
                CurrentPlayers.ForEach(delegate(Player player)
                {
                    if (player is Turret)
                        return;
                    if (!((Enemy)player).InPlainSight)
                        ((Enemy)player).Heard(position);
                });
            }
        }

        /// <summary>
        /// Szintén kívülrõl elérhetõ függvény egyes ellenségeknek, megmondja látható e a játékos abban az irányban, amerre épp
        /// néz az ellenség. A metódusban 50 fokos látószög van megadva, mivel ez a függvény nem egy igazi látást eldöntõ függvény
        /// hanem azt mondja meg, egyvonalban van e az ellenséggel a játékos annyira, hogy rá is lõhessen.
        /// </summary>
        /// <param name="EnemyPosition">Ellenség pozíciója</param>
        /// <param name="rotation">Ellenség irányszöge (vagyis, hogy épp merre néz)</param>
        /// <param name="enemy">Maga az ellenség</param>
        /// <returns>Az ágyú pozíciója, vagy Vector2.Zero, ha nincs a látószögben</returns>
        public Vector2 CanSeePlayer(Vector2 EnemyPosition, float rotation, Enemy enemy)
        {
            float startRotation = rotation - MathHelper.ToRadians(25);           

            for (int i = 0; i <= 50; i++)
            {
                Vector2 Direction = new Vector2((float)Math.Sin(startRotation + MathHelper.ToRadians(i)), -(float)Math.Cos(startRotation + MathHelper.ToRadians(i)));               
                Ray ray = new Ray(new Vector3(EnemyPosition, 0), new Vector3(EnemyPosition + (2500 * Direction), 0));                
                BoundingBox bb = new BoundingBox(new Vector3(turret.BoundingRectangle.Left, turret.BoundingRectangle.Top
                       , -1),
                       new Vector3(turret.BoundingRectangle.Right, turret.BoundingRectangle.Bottom
                           , 1));

                if (ray.Intersects(bb).HasValue)                
                    if (Vector2.Distance(EnemyPosition + Direction, turret.position) < Vector2.Distance(EnemyPosition, turret.position))
                        if (enemy == null || !InTheWay(enemy, ray))                        
                                return turret.position;                                        
            }
            
            return Vector2.Zero;
        }
        
        /// <summary>
        /// A függvény megnézi, van e az útban bármilyen játékos, mert bár barátságos tûz az ellenségek közt nincs,
        /// de ha keresztül lõnek egymáson, mintha nem látnák a másikat, az nem néz ki túl jól.
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="ray"></param>
        /// <returns></returns>
        private bool InTheWay(Enemy enemy, Ray ray)
        {
            lock(this)
                foreach (Player player in CurrentPlayers)
                {
                    if (player is Enemy && !player.Equals(enemy))
                    {
                        BoundingBox bb = new BoundingBox(new Vector3(player.BoundingRectangle.Left, player.BoundingRectangle.Top
                            , -1), new Vector3(player.BoundingRectangle.Right, player.BoundingRectangle.Bottom, 1));

                        if (ray.Intersects(bb).HasValue)
                            return true;
                    }
                }
            return false;
        }

        /// <summary>
        /// A játékos konkrét érzékelése a kifinomultabb, fejlettebb ellenségek számára
        /// </summary>
        /// <returns>Maga a játékos/ágyú</returns>
        public Turret SensePlayer()
        {
            return (Turret)CurrentPlayers.Find(player => player is Turret);
        }

        /// <summary>
        /// Új lövedék hozzáadása a komponensek, ill. a külön nyilvántartott lövedékek közé.
        /// </summary>
        /// <param name="projectile">A lövedék</param>
        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
            Components.Add(projectile);
        }

        /// <summary>
        /// Ha egy lövedék a falnak csapódott, azt vizuális és hangeffektel is érzékeltetjük
        /// </summary>
        /// <param name="position">Becsapódás helye</param>
        /// <param name="rotation">A lövedék forgatási/irány szöge</param>
        public void WallHit(Vector2 position, float rotation)
        {
            if (!Components.Contains(WallHitEffect))
            {
                WallHitEffect = new ShortVisualEffect(this, WallHitTexture, 1f, 6, false);
                WallHitEffect.Center = new Vector2(WallHitTexture.Width / 2, WallHitTexture.Height * .8f);
                Components.Add(WallHitEffect);
            }

            WallHitEffect.ShowEffect(position, rotation - MathHelper.Pi);
            WallHitSI.Play();
        }
        #endregion

        #region Pályabetöltés
        /// <summary>
        /// Pálya betöltése, illetve az elõtte lévõ szükséges inicalizálás itt történik.
        /// </summary>
        /// <param name="Players">Az ellenségek az adott pályán</param>
        private void LoadLevel(params object[] Players)
        {
            if (Score == null)            
                Score = new GameScore("scores.dat", 5, new int[] { 180, 240, 200, 180, 360 });

            Score.Level = (int)CurrentLevel;
            Score.ResetLevel();

            for(int i = 0; i < Components.Count; i++)
            {
                if(Components[i] is Enemy || Components[i] is HUD)
                    ((DrawableGameComponent)Components[i]).Dispose();
            }
            if (turret == null)
            {
                turret = new Turret(this, TurretTexture, new Vector2(Screen.Width / 2, Screen.Height), -MathHelper.PiOver2);
                turret.AmmoSetup(25, 4, 1.5f);
                if (DirectlyLoaded)
                {
                    turret.Lives = Score.GetLives();
                    DirectlyLoaded = false;
                }
            }
            else
                turret.Reset();

            CurrentPlayers.Add(turret);
            Components.Add(turret);            

            HUDTry = new HUD(this, Color.DarkGray, Color.White, true, false, HUDSizeP);
            HUDTry.AddLeft(turret);
            Components.Add(HUDTry);   

            foreach (Player player in Players)
            {                
                CurrentPlayers.Add(player);
                Components.Add(player);
                HUDTry.AddRight(player);
            }

            HUDTry.Started = true;
            PauseAll();           
            CountDown = 3500;
            LvlElapsedTime = 0;
        }

        /// <summary>
        /// Az elsõ pálya betöltése
        /// </summary>
        private void LoadLevelOne()
        {
            Soldier soldier = new Soldier(this, new Texture2D[] { 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierV0"), 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SpriteGrenade"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeath"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeath02")}
                        , new Vector2(250, 200), 2);

            soldier.AmmoSetup(15, 4, 1);
            soldier.ToLook = false;
            CurrentLevel = Level.Soldier;
            LoadLevel(soldier);
            MediaPlayer.Play(Level1Intro);            
        }

        /// <summary>
        /// A második pálya betöltése
        /// </summary>
        private void LoadLevelTwo()
        {
            Soldier soldier1 = new Soldier(this, new Texture2D[] { 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierV1"), 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SpriteGrenadeV1"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeathV1"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeathV1_02")}
                    , new Vector2(250, 300), 2),
                    soldier2 = new Soldier(this, new Texture2D[] { 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierV2"), 
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SpriteGrenadeV2"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeathV2"),
                    Content.Load<Texture2D>("Sprites\\Soldiers\\SoldierDeathV2_02")}, 
                    new Vector2(RndG.Next(200, 800), RndG.Next(200, 400)), 7);

            soldier1.AmmoSetup(15, 6, 1);
            soldier2.AmmoSetup(25, 4, 1);
            CurrentLevel = Level.Veterans;
            LoadLevel(soldier1, soldier2);
            MediaPlayer.Play(Level2);
        }

        /// <summary>
        /// A harmadik pálya betöltése
        /// </summary>
        private void LoadLevelThree() 
        {
            Tank tank = new Tank(this, Content.Load<Texture2D>("Sprites\\Tank\\tankbodyV2"),
                Content.Load<Texture2D>("Sprites\\Tank\\tankhood"), new Vector2(Screen.Width / 1.2f, Screen.Height / 3f), 2);
            
            tank.AmmoSetup(40, 5, 1.5f);
            CurrentLevel = Level.Tank;
            LoadLevel(tank);
            MediaPlayer.Play(Level3);
        }

        /// <summary>
        /// A negyedik pálya betöltése
        /// </summary>
        private void LoadLevelFour()
        {
            Cyborg cyborg = new Cyborg(this, Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgMovement"), new Vector2(250, 200), 2);
            cyborg.AmmoSetup(30, 6, 1.5f);
            CurrentLevel = Level.Cyborg;
            LoadLevel(cyborg);
            MediaPlayer.Play(Level4Intro);
        }

        /// <summary>
        /// Az ötödik, és egyben utolsó pálya betöltése
        /// </summary>
        private void LoadLevelFive()
        {
            BlackWidow blackwidow = new BlackWidow(this, Content.Load<Texture2D>("Sprites\\BlackWidow\\BlackWidowMoveV3"), new Vector2(250, 200), 2);
            blackwidow.AmmoSetup(45, 6, 1.5f);
            CurrentLevel = Level.BlackWidow;
            LoadLevel(blackwidow);
            MediaPlayer.Play(Level5Intro);
        }

        /// <summary>
        /// A következõ pálya betöltése. Attól függõen, hogy épp hol vagyunk, automatikusan betölti az azutánit. Ha a menüben vagyunk
        /// értelemszerûen az elsõ pályát fogja betölteni.
        /// Az utolsó pályán viszont már csak egy nevet fog bekérni, hogy az Elért pontszámok listára felkerülhessünk.
        /// </summary>
        private void LoadNextLevel()
        {
            ClearComponents();
            switch (CurrentLevel)
            {
                case Level.Menu:
                    LoadLevelOne();
                    break;
                case Level.Soldier:                    
                    LoadLevelTwo();
                    break;
                case Level.Veterans:
                    LoadLevelThree();
                    break;
                case Level.Tank:
                    LoadLevelFour();
                    break;
                case Level.Cyborg:
                    LoadLevelFive();
                    break;
                case Level.BlackWidow:                    
                    CurrentLevel = Level.Menu;

                    List<KeyValuePair<string, int>> HScores = new List<KeyValuePair<string, int>>();
                    LoadHighScores(HScores);
                    bool wasFS = graphics.IsFullScreen;
                    graphics.IsFullScreen = false;
                    graphics.ApplyChanges();
                    HScores.Add(new KeyValuePair<string, int>(Microsoft.VisualBasic.Interaction.InputBox("Gratulálok, végigvitted a játékot!\nAdd meg a neved, hogy felkerülhess a pontszám-listára.", "Név megadása", "Default", 0, 0), Score.GetFullScore()));
                    graphics.IsFullScreen = wasFS;
                    graphics.ApplyChanges();
                    HScores.Sort(delegate(KeyValuePair<string, int> p1, KeyValuePair<string, int> p2)
                    {
                        if (p1.Value > p2.Value)
                            return -1;
                        else if (p1.Value == p2.Value)
                            return 0;
                        else
                            return 1;
                    });                    
                    
                    BinaryWriter bw = new BinaryWriter(File.Create("highscores.dat"));
                    int i = 0;
                    string HSMessage = "";
                    foreach (KeyValuePair<string, int> score in HScores)
                    {
                        bw.Write(score.Key);
                        bw.Write(score.Value);
                        HSMessage += score.Key + "  ->  " + score.Value + " pont\n";

                        i++;
                        if (i == 5)
                            break;
                    }
                    bw.Close();
                    
                    ShowHighScores(HSMessage);
                    break;
            }
        }
        #endregion

        #region Pontszámok kezelése
        /// <summary>
        /// Az Elért pontszámok listájának megmutatása a játékosnak egy ablakban
        /// </summary>
        /// <param name="HSMessage">A listát tartalmazó szöveges üzenet</param>
        private void ShowHighScores(string HSMessage)
        {
            HighScoresWindow = new MsgBox(this, HSMessage.Length > 0 ? "Legnagyobb elért pontszámok:\n" + HSMessage: "A játékot még senki se vitte végig, nincs eredmény a listán!", false);
            HighScoresWindow.YesAction = delegate()
            {
                ClearComponents();
                MainMenu.Show = true;
                if (turret != null)
                    turret.Dispose();
                HighScoresWindow.HideWindow();
            };
            Components.Add(HighScoresWindow);
            HighScoresWindow.ShowWindow();            
        }

        /// <summary>
        /// Elért pontszámok betöltése
        /// </summary>
        /// <param name="HScores">Kulcs-érték pár lista, ahová be lehet tölteni a pontszámokat</param>
        private void LoadHighScores(List<KeyValuePair<string, int>> HScores)
        {
            BinaryReader br = new BinaryReader(File.Open("highscores.dat", FileMode.OpenOrCreate));

            while (br.PeekChar() != -1)
            {
                HScores.Add(new KeyValuePair<string, int>(br.ReadString(), br.ReadInt32()));
            }

            br.Close();
        }
        #endregion

        #region Kirajzolás
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            //megfelelõ háttér kirajzolása
            if (Backgrounds.Count > (int)CurrentLevel)
            {
                float bg_scale1 = (float)Screen.Width / (float)Backgrounds[(int)CurrentLevel].Width, bg_scale2 = (float)Screen.Height / (float)Backgrounds[(int)CurrentLevel].Height;
                spriteBatch.Draw(Backgrounds[(int)CurrentLevel], new Vector2(Screen.Width / 2, 0), null, Color.White, MathHelper.ToRadians(0), new Vector2(Backgrounds[(int)CurrentLevel].Width / 2, 0), CurrentLevel == Level.Menu ? 1f : bg_scale1 > bg_scale2 ? bg_scale1 : bg_scale2, SpriteEffects.None, 1f);
            }

            if (CountDown > 0)//visszaszámlálás esetén
            {
                spriteBatch.Draw(FadeOutScreen, new Vector2(0, 0), null, new Color(255, 255, 255, (int)((float)CountDown / 3500.0 * 255)), MathHelper.ToRadians(0), Vector2.Zero, 1f, SpriteEffects.None, 1f);
                string LevelMessage;
                switch (CurrentLevel)
                {
                    case Level.Soldier:
                        LevelMessage = "1. pálya - A katona";
                        break;
                    case Level.Veterans:
                        LevelMessage = "2. pálya - A veteránok";
                        break;
                    case Level.Tank:
                        LevelMessage = "3. pálya - A tank";
                        break;
                    case Level.Cyborg:
                        LevelMessage = "4. pálya - A kiborg";
                        break;
                    case Level.BlackWidow:
                        LevelMessage = "5. pálya - A fekete özvegy";
                        break;
                    default:
                        LevelMessage = "";
                        break;
                }

                spriteBatch.DrawString(CountDownFont, LevelMessage, new Vector2(Screen.Width / 2, Screen.Height / 2) - CountDownFont.MeasureString(LevelMessage).Y * Vector2.UnitY, Color.Gray, 0f, CountDownFont.MeasureString(LevelMessage) / 2, 0.5f, SpriteEffects.None, 1);
                spriteBatch.DrawString(CountDownFont, (CountDown / 1000).ToString(), new Vector2(Screen.Width / 2, Screen.Height / 2) - CountDownFont.MeasureString("3") / 2, CountDown > 2000 ? Color.Blue : Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                CountDown -= gameTime.ElapsedGameTime.Milliseconds;
            }
            //A menüben a verziószám kiírása
            if (CurrentLevel == Level.Menu)
                spriteBatch.DrawString(Font, "Ágyúharc (2012) Nagy Sándor - " + strVersion, new Vector2(0, GraphicsDevice.Viewport.Height - Font.MeasureString(strVersion).Y), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1);

            try
            {
                spriteBatch.End();
            }
            catch
            {
                spriteBatch.Dispose();
                spriteBatch = new SpriteBatch(GraphicsDevice);
            }

            base.Draw(gameTime);
        }
        #endregion        
    }
}
