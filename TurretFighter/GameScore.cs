using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace TurretTest
{
    /// <summary>
    /// A játékban elért jó eredményekért kapott, pontszámokat kezelő osztály
    /// </summary>
    public class GameScore
    {
        #region Tagok
        const int ACCURACY = 150, FINISH = 100, KILL = 50, LIVES = 25, LIVESATSTART = 3;//Szorzók az egyes pontok elszámolásához

        List<LevelScore> Scores, OldScores;//Aktuális pontok és a régiek
        List<int> TargetTimes;//pályateljesítési célidők tárolása
        FileStream SaveFile;
        private int MaxLevel;
        string FileName;
        public bool RocketHit = false, NormalHit = false;
        
        public int Level
        {
            get;
            set;
        }
        
        [Serializable]
        private class LevelScore//Pontszámadatok tárolása
        {
            public int Shot = 0, Hit = 0, ShotScore = 0, HitScore = 0, EnemyKilled, Level = 0, 
                Sum = 0, BonusNoNormalHit, BonusNoRocketHit, LivesBonus, ScoreBonus = 0;
            public float Accuracy, FinishTime;
        }
        #endregion

        #region Konstruktor
        public GameScore(string file, int max_level, int[] target_times)
        {           
            TargetTimes = new List<int>(target_times);
            FileName = file;
            LoadScores();
            MaxLevel = max_level;

            if (Scores.Count > 5 || Scores.Count == 0)
                throw new Exception("Ezt elkúrtuk, de rendesen! Scores.Count = " + Scores.Count);            
        }
        #endregion

        #region Vezérlés
        /// <summary>
        /// Lövés esetén pontot számolunk el, megadott szorzóval
        /// </summary>
        /// <param name="score"></param>
        /// <param name="multiplier"></param>
        public void ScoreShot(int score, int multiplier)
        {
            Scores[Level - 1].ShotScore += multiplier * score;
            Scores[Level - 1].Shot++; 
        }

        /// <summary>
        /// Találat esetén is pontot számolunk el, a szint szorzójával (Ha azt vesszük minden pálya nehezebb mint az előző, ez logikus is, hogy megjutalmazzuk érte a játékost)
        /// </summary>
        /// <param name="score"></param>
        public void ScoreHit(int score)
        {
            Scores[Level - 1].HitScore += score * Level;
            Scores[Level - 1].Hit++;
        }

        /// <summary>
        /// Befejeztük a pályát (tehát győztünk), elszámoljuk a pontokat
        /// </summary>
        /// <param name="TimeInSecs">Az eltelt játékidő</param>
        /// <param name="Lives">Az életek száma</param>
        public void LevelFinished(int TimeInSecs, int Lives)
        {
            Scores[Level - 1].Level = Level;
            Scores[Level - 1].FinishTime = (float)TimeInSecs / (float)TargetTimes[Level - 1];
            Scores[Level - 1].FinishTime = Scores[Level - 1].FinishTime >= 1 ? 0 : (1 - Scores[Level - 1].FinishTime) * FINISH;
            Scores[Level - 1].Accuracy = (float)Scores[Level - 1].Hit / (float)Scores[Level - 1].Shot * ACCURACY;
            Scores[Level - 1].EnemyKilled = Scores[Level - 1].Level * KILL;
            Scores[Level - 1].BonusNoNormalHit = NormalHit ? 0 : FINISH;
            Scores[Level - 1].BonusNoRocketHit = RocketHit ? 0 : FINISH / 2;
            Scores[Level - 1].Sum = (int)Scores[Level - 1].FinishTime + (int)Scores[Level - 1].Accuracy + Scores[Level - 1].ShotScore + Scores[Level - 1].HitScore
                + Scores[Level - 1].EnemyKilled + Scores[Level - 1].BonusNoNormalHit + Scores[Level - 1].BonusNoRocketHit + (Scores[Level - 1].LivesBonus = LIVES * Lives);

            SaveScore();
            Level++;
        }

        /// <summary>
        /// A pontok összesítésének összerakása egy szöveges változóba
        /// </summary>
        /// <returns>az összesítés</returns>
        public string GetScore()
        {
            string result = "Lövések: " + Scores[Level - 2].ShotScore + " pont\nTalálatok: " + Scores[Level - 2].HitScore + " pont\nPontosság: " +
                Scores[Level - 2].Accuracy + " pont\nIdő bónusz: " + Scores[Level - 2].FinishTime + " pont\nEllenség bónusz: " + Scores[Level - 2].EnemyKilled
                + " pont\nKitérés bónusz: " + (Scores[Level - 2].BonusNoNormalHit + Scores[Level - 2].BonusNoRocketHit) + " pont\nÉletek bónusz: " + Scores[Level - 2].LivesBonus +
                " pont\nTeljes (aktuális pálya): " + Scores[Level - 2].Sum + " pont\nÖsszesen: " + GetScoreToLevel() + " pont";

            if (Level > MaxLevel)
                Level--;

            return result;
        }

        /// <summary>
        /// A teljes végeredmény kikérése
        /// </summary>
        /// <returns></returns>
        public int GetFullScore()
        {
            return Scores.Sum(score => score.Sum);
        }

        /// <summary>
        /// Aktuális pálya visszaállítása
        /// </summary>
        public void ResetLevel()
        {
            try
            {
                Scores[Level - 1] = new LevelScore();
            }
            catch
            {
                Scores.Add(new LevelScore());
            }
            //FinishedCurrent = false;
        }

        //Pontok mentése fájlba
        private void SaveScore()
        {
            SaveFile = new FileStream("scores.dat", FileMode.Create);                
            
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(SaveFile, Scores);            
            SaveFile.Close();                        
        }

        /// <summary>
        /// Az eddig elért szintek megszámlálása
        /// </summary>
        /// <returns></returns>
        public int GetProgress()
        {
            return Scores.Count(score => score != null && score.Sum != 0);
        }

        /// <summary>
        /// Adott szintig elért összpontszám
        /// </summary>
        /// <returns></returns>
        public int GetScoreToLevel()
        {
            int ScoreToLevel = 0;

            for (int i = 0; i < Level - 1; i++)
                ScoreToLevel += Scores[i].Sum;

            return ScoreToLevel;
        }

        /// <summary>
        /// Pontszámok betöltése fájlból
        /// </summary>
        public void LoadScores()
        {
            SaveFile = new FileStream(FileName, FileMode.OpenOrCreate);
            BinaryFormatter bformatter = new BinaryFormatter();

            if(Scores != null)
            {
                Scores.Clear();
                OldScores.Clear();
            }

            try
            {
                Scores = (List<LevelScore>)bformatter.Deserialize(SaveFile);
            }
            catch
            {
                Scores = new List<LevelScore>();
                Scores.Add(new LevelScore());
            }

            if (Scores.Count == 0)
                Scores.Add(new LevelScore());

            OldScores = Scores;
            SaveFile.Close();
        }

        /// <summary>
        /// Előző pályáról megmaradt életek lekérése
        /// </summary>
        /// <returns></returns>
        public int GetLives()
        {
            if (Level == 1)
                return LIVESATSTART;

            return Scores[Level - 2].LivesBonus / LIVES;
        }

        /// <summary>
        /// Bónusz pontok megadása
        /// </summary>
        public void BonusGained()
        {
            Scores[Level - 2].ScoreBonus++;
        }

        /// <summary>
        /// Kapott e már bónuszt a játékos, és mennyit
        /// </summary>
        public int GetBonusGained()
        {
            int j = 0;
            for (int i = 0; i < Level - 1; i++)
                j += Scores[i].ScoreBonus;

            return j;
        }

        /// <summary>
        /// Teljes visszaállítás
        /// </summary>
        /// <param name="Level"></param>
        public void Reset(int Level)
        {
            this.Level = Level;
        }
        #endregion
    }
}
