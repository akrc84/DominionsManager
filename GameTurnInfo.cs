using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominionsManager
{
    public class GameTurnInfo : IComparable<GameTurnInfo>
    {
        //TODO change that this object is comprehenisive game file instad on item in list
        public string GameName;
        public int Turn;
        public GameAttachment Data;

        /// <summary>
        /// Returns last turn saved localy
        /// </summary>
        public int LastTurn
        {

            get
            {
                int i = 0;
                //needs rewrite
                foreach (string item in Directory.GetFiles(Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), GameName), "*.trn.*"))
                {
                    int x = 0;
                    if (int.TryParse(new FileInfo(item).Extension.Replace(".", "").ToString(), out x))
                    {
                        if (i < x)
                        {
                            i = x;
                        }
                    }
                }
                return i;
            }
        }

        public GameTurnInfo(string gameName)
        {
            GameName = gameName;
        }

        public GameTurnInfo(string gameName, int turn, GameAttachment data)
        {
            GameName = gameName;
            Turn = turn;
            Data = data;
        }

        public GameTurnInfo(string gameName, string subject, GameAttachment data)
        {
            string turn = subject.Substring(subject.Length - 2, 2);

            GameName = gameName;

            if (!int.TryParse(turn.Trim(), out Turn))
                Turn = 1;

            Data = data;
        }

        public GameTurnInfo(string gameName, FileInfo saveFileInfo)
        {
            if (!int.TryParse(saveFileInfo.Extension.Replace(".",""), out Turn))
            {
                Turn = -1;
            }

            GameName = gameName;

            Data = new GameAttachment();

            Data.fileName = saveFileInfo.FullName;
        }


        /// <summary>
        /// Returns file game turn commands (.2h)
        /// </summary>
        public FileInfo GameTurnFile
        {
            get
            {
                foreach (string item in Directory.GetFiles(Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), GameName), "*.2h"))
                {
                    return new FileInfo(item);
                }

                return null;
            }
        }

        public FileInfo CurrentTurnFile
        {
            get
            {
                foreach (string item in Directory.GetFiles(Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), GameName), "*." + this.Turn.ToString()))
                {
                    return new FileInfo(item);
                }

                return null;
            }
        }

        public FileInfo TurnFile
        {
            get
            {
                foreach (string item in Directory.GetFiles(Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), GameName), "*.trn"))
                {
                    return new FileInfo(item);
                }

                return null;
            }
        }

        public int CompareTo(GameTurnInfo other)
        {
            if (other.Turn < this.Turn)
                return 1;
            else
                return this.Turn.CompareTo(other.Turn);
        }

        public override string ToString()
        {
            return "Turn " + (Turn.ToString().PadLeft(2,'0'));
        }

        internal void SaveGameFoder(bool isLatestTurn)
        {
            string targetDir = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.SaveGameDir), this.GameName);

            if (isLatestTurn)
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(targetDir, this.Data.fileName), this.Data.data);
            else
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(targetDir, this.Data.fileName + "." + this.Turn.ToString()), this.Data.data);
        }
    }
}
