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
    /// A j�t�kvez�rl�
    /// </summary>
    /// 
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        #region Konstansok-v�ltoz�k
        //konstansok, illetve statikus �rt�kek
        const float HUDSizeP = 0.18f, BCCHANCE = .025f;
        const int BaseWidth = 1000, BaseHeight = 492;
        public static Viewport Screen;
        public static GameScore Score;
        public static float GlobalScale;

        //grafik�val kapcsolatos mez�k, text�r�k
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D TurretTexture, MousePointer, FadeOutScreen, CrossHair, CrossHairBlue, WallHitTexture;
        SpriteFont Font, CountDownFont;
        
        //L�tfontoss�g� list�k, melyek tartalmazz�k a l�ved�keket, vagy a robban�sok inform�ci�it, hanghat�sait, valamint a h�tt�rk�peket
        public List<Projectile> Projectiles = new List<Projectile>();        
        List<Player> CurrentPlayers = new List<Player>();
        List<ExplosionInfo> CurrentExplosions = new List<ExplosionInfo>();
        List<SoundEffect> ExplosionEffects = new List<SoundEffect>();
        List<Texture2D> Backgrounds = new List<Texture2D>();
        
        //Egy�b hangeffektek, valamint h�tt�rzen�k
        SoundEffect DeathLaugh, WallHitSE;
        SoundEffectInstance WallHitSI;
        Song Level1Intro, Level1, Level2, Level3, Level4Intro, Level4, Level5Intro, Level5, MenuMusic;        
                
        Turret turret;
        Random RndG = new Random(17);
        KeyboardState newState, oldState;
        MyTimer ExplosionTimer;

        //Men�k, �zenetablakok, az eg�r, valamint a kijelz�
        HUD HUDTry;
        MsgBox Quit, DeathMsg, ShowScore, HighScoresWindow;
        Menu MainMenu, LevelSelectMenu;
        MouseCursor TheCursor;

        Level CurrentLevel;
        ShortVisualEffect WallHitEffect;        

        int chkLoadNextLevel = 0,//ebben sz�moljuk mennyi id� telt el, a p�lyateljes�t�s ellen�rz�s�hez sz�ks�ges 
            CountDown = 0, //adott p�lya kezdete el�tti visszasz�ml�l�st megval�s�t� v�ltoz�
            LvlElapsedTime = 0; //adott p�ly�n eltelt j�t�kid�t m�ri
        string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();//a program verzi�ja        
        bool DirectlyLoaded = false, MenuLoaded = false;//egy�b bet�lt�st jelz� v�ltoz�k        

        //Aktu�lis p�lya ill. szint jelz�s�hez l�trehozott enumer�ci�
        enum Level
        {
            Menu,
            Soldier,
            Veterans,
            Tank,
            Cyborg,
            BlackWidow
        }

        //Egyes robban�sok inform�ci�inak t�rol�s�ra l�trehozott strukt�ra
        struct ExplosionInfo
        {
            public Vector2 position;
            public int Time, Range, Damage;
        }

        #endregion

        #region Bet�lt�s-In�cializ�l�s
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
            
            Quit = new MsgBox(this, "Biztos, hogy ki akarsz l�pni?", true);
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

        #region F�vez�rl�s
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {         
            if (!MenuLoaded)
            {//Men�k bet�lt�se, �s hozz�ad�sa a komponensekhez
                Components.Add(MainMenu);
                Components.Add(TheCursor);
                Components.Add(WallHitEffect);
                TheCursor.AddCursor("Norm�lKurzor", MousePointer);
                TheCursor.AddCursor("C�lkereszt", CrossHair);
                TheCursor.AddCursor("C�lkeresztK�k", CrossHairBlue);
                TheCursor.CurrentCursor = "C�lkereszt";
                MainMenu.yOffset = 225 + (GraphicsDevice.Viewport.Height - 225 - 4 * 90) / 2;
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\NewGame"), LoadLevelOne, true);
                MainMenu.AddButton(Content.Load<Texture2D>("Menu\\LoadLevel"), delegate() {                   
                    if (Score == null)
                        Score = new GameScore("scores.dat", 5, new int[] { 180, 240, 200, 180, 360 });//t�rolt pontok bet�lt�se
                    else
                        Score.LoadScores();
                    
                    if (LevelSelectMenu == null)
                    {                      
                        LevelSelectMenu = new Menu(this, 5);
                        Components.Add(LevelSelectMenu);                        
                        int Progress = Score.GetProgress();
                        LevelSelectMenu.yOffset = 250 + (GraphicsDevice.Viewport.Height - 250 - 4 * 90) / 2;
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level1"), LoadLevelOne, true);//Gomb hozz�ad�sa a men�h�z
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level2"), () => { DirectlyLoaded = true; LoadLevelTwo(); }, Progress > 0 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level3"), () => { DirectlyLoaded = true; LoadLevelThree(); }, Progress > 1 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level4"), () => { DirectlyLoaded = true; LoadLevelFour(); }, Progress > 2 ? true : false);
                        LevelSelectMenu.AddButton(Content.Load<Texture2D>("Menu\\Level5"), () => { DirectlyLoaded = true; LoadLevelFive(); }, Progress > 3 ? true : false);
                    }
                    else//ha m�r be van t�ltve a men�, akkor csak rendezn�nk kell a gombokat, mert lehet a j�t�kos tov�bb jutott a j�t�kban, �s �gy enged�lyezni kell n�h�ny men�pontot/gombot
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

                //a kil�p�s �zenetablak gombjainak be�ll�t�sa k�vetkezik
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

            if (IsActive)//Ha a j�t�k jelenleg az akt�v alkalmaz�s, vagyis az ablak f�kuszban van
            {
                //lek�rj�k a billenty�zet �llapot�t
                newState = Keyboard.GetState();
                if (newState.IsKeyDown(Keys.Pause) && !oldState.IsKeyDown(Keys.Pause) && !Quit.Show) //Pause eset�n           
                    PauseAll();
                else if (Keyboard.GetState().IsKeyDown(Keys.Escape) && !oldState.IsKeyDown(Keys.Escape))//Escape eset�n
                {
                    if (LevelSelectMenu != null && LevelSelectMenu.Show)
                    {
                        LevelSelectMenu.Show = false;
                        MainMenu.Show = true;
                    }
                    else
                    {
                        if (!MainMenu.Show && (DeathMsg == null || DeathMsg != null && !DeathMsg.Show) &&
                            (ShowScore == null || ShowScore != null && !ShowScore.Show) && CountDown == 0)//J�t�k k�zben, ha nincs m�s ablak el�l, akkor megjelen�tj�k vagy elrejtj�k a kil�p�s ablakot
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
                    Components.ToList().ForEach(delegate(IGameComponent component)//destination index meg hasonl�k
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

            if (CountDown < 0)//Ha a visszasz�ml�l�s v�get �rt, elind�tjuk a j�t�kot
            {
                if(turret.Paused)
                    PauseAll();
                CountDown = 0;
                TheCursor.CurrentCursor = "C�lkereszt";//j�t�k k�zben a c�lkereszt eg�rmutat�ra v�ltunk

            }

            if (HighScoresWindow != null && !HighScoresWindow.Show && !HighScoresWindow.HideStarted)
            {
                HighScoresWindow.Dispose();
                HighScoresWindow = null;
            }

            if (turret != null && CurrentLevel != Level.Menu /*&& !turret.Paused*/)
                LvlElapsedTime += gameTime.ElapsedGameTime.Milliseconds;

            TheCursor.CurrentCursor = CurrentLevel == Level.Menu || Quit.Show || (DeathMsg != null && DeathMsg.Show)
                || (ShowScore != null && ShowScore.Show) ? "Norm�lKurzor" : CurrentLevel == Level.Tank ? "C�lkeresztK�k": "C�lkereszt";

            //adott szintnek/k�perny�nek megfelel� h�tt�rzene kiv�laszt�sa, �s lej�tsz�sa (ha �pp nem megy m�s)
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
        /// Az id�k�z�nk�nti ellen�rz�s ezt a met�dust futtatja le, mely megn�zi meghaltak e m�r az ellens�gek �s/vagy a j�t�kos, 
        /// �s az annak megfelel� ablakot jelen�ti meg -> hal�l �zenet, melyn�l lehet�s�g van az �jrakezd�sre, ha nem fogyott el az �let�nk
        /// VAGY ha nyert�nk, az adott p�ly�n el�rt pontsz�mokat megmutatjuk �s tov�bb l�p�nk a k�vetkez� p�ly�ra
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

                        DeathMsg = new MsgBox(this, EnemyDead ? "D�ntetlen! A p�ly�t �jrakezdheted, �letet nem vesz�tesz." : (turret.Lives > 0 ? "MEGHALT�L �S ELVESZ�TETTED EGY �LETEDET! �JRAKEZDED?" :
                                "MEGHALT�L �S ELFOGYOTT AZ �SSZES �LETED! A J�T�KOT �JRAKEZDHETED A F�MEN�B�L, VAGY FOLYTATHATOD A P�LYAV�LASZT�S MEN�B�L."),
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
                            Message += "\n5000 pont : +1 �let";
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
        /// Komponensek t�rl�se a komponensekb�l, �s az �lland�ak visszarak�sa. Csak egy teljes Clear utas�t�s tudja elt�ntetni az �sszes
        /// fennmarad� komponenst, mely nem akar �nmag�t�l, vagy �pp r�hat�ssal megsemmis�lni.
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
        /// Minden j�t�kelem, effekt feldolgoz�s�nak sz�netetlet�se, �gymond pillanat-�llj funkci�ja.
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
        /// A szintv�laszt�s men� gombjainak �jrabe�ll�t�sa
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

        #region �tk�z�s�rz�kel�s

        /// <summary>
        /// A met�dus a l�ved�kek �tk�z�s�t ellen�rzi l�ved�kekkel �S a j�t�kosokkal. �tk�z�s eset�n megh�vja mindk�t j�t�kelem tal�lat 
        /// met�dus�t (Hit), �gy mindig a megfelel� dolog t�rt�nik.
        /// A met�dus el�sz�r azt ellen�rzi, hogy a k�t j�t�kelem k�z�tti �tk�z�s lehets�ges e, illetve foglalkozunk e vele. 
        /// Ez a j�t�kelem t�pus�b�l megmondhat�. Ha foglalkoznunk kell vele, akkor megn�zz�k a k�r�lhat�rol� t�glalapjaik �ssze�rnek e.
        /// Ha igen, pixel-pontos �tk�z�st is vizsg�lunk, mely ha tal�l �tfed�st, hivatalosan is kimondhatjuk, hogy �sszetal�lkozott a k�t j�t�kelem
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
        /// A j�t�kosok �tk�z�s�nek vizsg�lata j�t�kossal. K�l�n met�dus sz�ks�ges r�, mert ha t�l sok�ig �rnek egym�sba a j�t�kosok,
        /// akkor a t�l sok pixel-pontos vizsg�lat lelass�thatja a j�t�kot. �ppen ez�rt a met�dust m�sodpercenk�nt csak n�h�nyszor h�vjuk meg.
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

        #region K�v�lr�l el�rhet� met�dusok - Jelz� f�ggv�nyek a komponenseknek

        /// <summary>
        /// K�v�lr�l el�rhet� f�ggv�ny a j�t�kelemek sz�m�ra, melyek felrobbannak. Itt �tadj�k az adatokat magukr�l, �s beker�lnek a
        /// az aktu�lis robban�sok list�j�ba. A v�g�n term�szetesen egy v�letlenszer� robban�s-hangeffekt is megsz�laltat�sra ker�l.
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
        /// Ha l�v�s t�rt�nt, arr�l egyes j�t�kosoknak is tudniuk, vagyis hallaniuk kell
        /// </summary>
        /// <param name="from">Kit�l j�tt a l�v�s, ha Ellens�gt�l, akkor hamis</param>
        /// <param name="position">A l�v�s helye</param>
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
        /// Szint�n k�v�lr�l el�rhet� f�ggv�ny egyes ellens�geknek, megmondja l�that� e a j�t�kos abban az ir�nyban, amerre �pp
        /// n�z az ellens�g. A met�dusban 50 fokos l�t�sz�g van megadva, mivel ez a f�ggv�ny nem egy igazi l�t�st eld�nt� f�ggv�ny
        /// hanem azt mondja meg, egyvonalban van e az ellens�ggel a j�t�kos annyira, hogy r� is l�hessen.
        /// </summary>
        /// <param name="EnemyPosition">Ellens�g poz�ci�ja</param>
        /// <param name="rotation">Ellens�g ir�nysz�ge (vagyis, hogy �pp merre n�z)</param>
        /// <param name="enemy">Maga az ellens�g</param>
        /// <returns>Az �gy� poz�ci�ja, vagy Vector2.Zero, ha nincs a l�t�sz�gben</returns>
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
        /// A f�ggv�ny megn�zi, van e az �tban b�rmilyen j�t�kos, mert b�r bar�ts�gos t�z az ellens�gek k�zt nincs,
        /// de ha kereszt�l l�nek egym�son, mintha nem l�tn�k a m�sikat, az nem n�z ki t�l j�l.
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
        /// A j�t�kos konkr�t �rz�kel�se a kifinomultabb, fejlettebb ellens�gek sz�m�ra
        /// </summary>
        /// <returns>Maga a j�t�kos/�gy�</returns>
        public Turret SensePlayer()
        {
            return (Turret)CurrentPlayers.Find(player => player is Turret);
        }

        /// <summary>
        /// �j l�ved�k hozz�ad�sa a komponensek, ill. a k�l�n nyilv�ntartott l�ved�kek k�z�.
        /// </summary>
        /// <param name="projectile">A l�ved�k</param>
        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
            Components.Add(projectile);
        }

        /// <summary>
        /// Ha egy l�ved�k a falnak csap�dott, azt vizu�lis �s hangeffektel is �rz�keltetj�k
        /// </summary>
        /// <param name="position">Becsap�d�s helye</param>
        /// <param name="rotation">A l�ved�k forgat�si/ir�ny sz�ge</param>
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

        #region P�lyabet�lt�s
        /// <summary>
        /// P�lya bet�lt�se, illetve az el�tte l�v� sz�ks�ges inicaliz�l�s itt t�rt�nik.
        /// </summary>
        /// <param name="Players">Az ellens�gek az adott p�ly�n</param>
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
        /// Az els� p�lya bet�lt�se
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
        /// A m�sodik p�lya bet�lt�se
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
        /// A harmadik p�lya bet�lt�se
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
        /// A negyedik p�lya bet�lt�se
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
        /// Az �t�dik, �s egyben utols� p�lya bet�lt�se
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
        /// A k�vetkez� p�lya bet�lt�se. Att�l f�gg�en, hogy �pp hol vagyunk, automatikusan bet�lti az azut�nit. Ha a men�ben vagyunk
        /// �rtelemszer�en az els� p�ly�t fogja bet�lteni.
        /// Az utols� p�ly�n viszont m�r csak egy nevet fog bek�rni, hogy az El�rt pontsz�mok list�ra felker�lhess�nk.
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
                    HScores.Add(new KeyValuePair<string, int>(Microsoft.VisualBasic.Interaction.InputBox("Gratul�lok, v�gigvitted a j�t�kot!\nAdd meg a neved, hogy felker�lhess a pontsz�m-list�ra.", "N�v megad�sa", "Default", 0, 0), Score.GetFullScore()));
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

        #region Pontsz�mok kezel�se
        /// <summary>
        /// Az El�rt pontsz�mok list�j�nak megmutat�sa a j�t�kosnak egy ablakban
        /// </summary>
        /// <param name="HSMessage">A list�t tartalmaz� sz�veges �zenet</param>
        private void ShowHighScores(string HSMessage)
        {
            HighScoresWindow = new MsgBox(this, HSMessage.Length > 0 ? "Legnagyobb el�rt pontsz�mok:\n" + HSMessage: "A j�t�kot m�g senki se vitte v�gig, nincs eredm�ny a list�n!", false);
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
        /// El�rt pontsz�mok bet�lt�se
        /// </summary>
        /// <param name="HScores">Kulcs-�rt�k p�r lista, ahov� be lehet t�lteni a pontsz�mokat</param>
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

        #region Kirajzol�s
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            //megfelel� h�tt�r kirajzol�sa
            if (Backgrounds.Count > (int)CurrentLevel)
            {
                float bg_scale1 = (float)Screen.Width / (float)Backgrounds[(int)CurrentLevel].Width, bg_scale2 = (float)Screen.Height / (float)Backgrounds[(int)CurrentLevel].Height;
                spriteBatch.Draw(Backgrounds[(int)CurrentLevel], new Vector2(Screen.Width / 2, 0), null, Color.White, MathHelper.ToRadians(0), new Vector2(Backgrounds[(int)CurrentLevel].Width / 2, 0), CurrentLevel == Level.Menu ? 1f : bg_scale1 > bg_scale2 ? bg_scale1 : bg_scale2, SpriteEffects.None, 1f);
            }

            if (CountDown > 0)//visszasz�ml�l�s eset�n
            {
                spriteBatch.Draw(FadeOutScreen, new Vector2(0, 0), null, new Color(255, 255, 255, (int)((float)CountDown / 3500.0 * 255)), MathHelper.ToRadians(0), Vector2.Zero, 1f, SpriteEffects.None, 1f);
                string LevelMessage;
                switch (CurrentLevel)
                {
                    case Level.Soldier:
                        LevelMessage = "1. p�lya - A katona";
                        break;
                    case Level.Veterans:
                        LevelMessage = "2. p�lya - A veter�nok";
                        break;
                    case Level.Tank:
                        LevelMessage = "3. p�lya - A tank";
                        break;
                    case Level.Cyborg:
                        LevelMessage = "4. p�lya - A kiborg";
                        break;
                    case Level.BlackWidow:
                        LevelMessage = "5. p�lya - A fekete �zvegy";
                        break;
                    default:
                        LevelMessage = "";
                        break;
                }

                spriteBatch.DrawString(CountDownFont, LevelMessage, new Vector2(Screen.Width / 2, Screen.Height / 2) - CountDownFont.MeasureString(LevelMessage).Y * Vector2.UnitY, Color.Gray, 0f, CountDownFont.MeasureString(LevelMessage) / 2, 0.5f, SpriteEffects.None, 1);
                spriteBatch.DrawString(CountDownFont, (CountDown / 1000).ToString(), new Vector2(Screen.Width / 2, Screen.Height / 2) - CountDownFont.MeasureString("3") / 2, CountDown > 2000 ? Color.Blue : Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                CountDown -= gameTime.ElapsedGameTime.Milliseconds;
            }
            //A men�ben a verzi�sz�m ki�r�sa
            if (CurrentLevel == Level.Menu)
                spriteBatch.DrawString(Font, "�gy�harc (2012) Nagy S�ndor - " + strVersion, new Vector2(0, GraphicsDevice.Viewport.Height - Font.MeasureString(strVersion).Y), Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1);

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
