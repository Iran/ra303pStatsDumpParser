using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ra303pStatsDumpParser;

namespace ra303pStatsDumpParser
{
    class StatsDumpParser
    {
        // Stream reading stuff
        BigEndianReader Bin;
        int Pos;

        int QuitPlayerNumHelper = -1; // To help parse per player QUIT

        int DumpSize = -1;
        int ReportedSize = -1;
        int[] PlayerMoneyHarvested;
        int[] PlayerCredits;
        int[] PlayerQuitStates;
        int[] PlayerColors;
        string[] PlayerNames;
        string[] PlayerSides;
        CratesCollectedStruct[] PlayerCratesCollected;
        int SDFX = -1;
        int GameNumber = -1;
        int NumberOfPlayers = -1;
        int NumberOfRemainingPlayers = -1;
        int IsTournamentGame = -1;
        int StartingCredits = -1;
        int BasesEnabled = -1;
        int OreRegenerates = -1;
        int CratesEnabled = -1;
        int NumberOfAIPlayers = -1;
        int ShroudRegrows = -1;
        int CTFEnabled = -1;
        int StartingUnits = -1;
        int TechLevel = -1;
        string MapName = "UNPARSED";
        string IPAddress1 = "UNPARSED";
        string IPAddress2 = "UNPARSED";
        string Ping = "UNPARSED";
        int CompletionType = -1;
        int GameDuration = -1;
        int StartTime = -1;
        int ProcessorType = -1;
        int AverageFPS = -1;
        uint SystemMemory = 0;
        uint VideoMemory = 0;
        int GameSpeed = -1;
        string Version = "UNPARSED";
        DateTime? GameDateTimeUTC = null; // null is value if not parsed


        public StatsDumpParser(string FileName)
        {
            PlayerMoneyHarvested = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerCredits = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerQuitStates = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerColors = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerCratesCollected = new CratesCollectedStruct[8];
            PlayerSides = new string[8];
            PlayerNames = new string[8];

            this.Parse_Stats_Dump_File(FileName);
        }

        public void Parse_Stats_Dump_File(string FileName)
        {
            using (BinaryReader b = new BinaryReader(File.Open("stats.dmp", FileMode.Open)))
            {
                Bin = new BigEndianReader(b);
                // 2.
                // Position and length variables.
                int Pos = 0;
                this.DumpSize = (int)Bin.BaseStream.Length;

                // Size needs to be at least 4 bytes for the size header in the dump file
                if (this.DumpSize < 4)
                {
                    throw new StatsDumpLengthException();
                }
                this.ReportedSize = Bin.ReadInt16();
                Bin.ReadInt16(); // advance position by 2 bytes
                Pos += 2;

                int length = (int)Bin.BaseStream.Length;
                while (Pos < this.DumpSize)
                {
                    Byte[] Bytes = Bin.ReadBytes(4); Pos += 4;
                    string ID = System.Text.Encoding.Default.GetString(Bytes);

                    if (ID.Contains("NAM"))
                    {
                        Parse_Name_Info(ID);
                    }

                    if (ID.Contains("COL"))
                    {
                        Parse_Color_Info(ID);
                    }

                    else if (ID.Contains("SID"))
                    {
                        Parse_Side_Info(ID);
                    }

                    else if (ID.Contains("HRV"))
                    {
                        Bin.ReadBytes(4);
                        int Money = Bin.ReadInt32();
                        Pos += 8;
                        this.Parse_Money_Harvested_Info(ID, Money);
                    }

                    else if (ID.Contains("CRA") && ID != "CRAT")
                    {
                        this.Parse_Crates_Collected_Info(ID);
                    }

                    else if (ID.Contains("CRD"))
                    {
                        this.Parse_Credits_Info(ID);
                    }

                    else if (ID == "SDFX")
                    {
                        Read_Garbage();
                        this.SDFX = this.Read_Byte();
                    }

                    else if (ID == "IDNO")
                    {
                        Read_Garbage();
                        this.GameNumber = this.Read_32Bits();
                    }

                    else if (ID == "NUMP")
                    {
                        Read_Garbage();
                        this.NumberOfPlayers = this.Read_32Bits();
                    }
                    else if (ID == "REMN")
                    {
                        Read_Garbage();
                        this.NumberOfRemainingPlayers = this.Read_32Bits();
                    }
                    else if (ID == "TRNY")
                    {
                        Read_Garbage();
                        this.IsTournamentGame = this.Read_32Bits();
                    }
                    else if (ID == "CRED")
                    {
                        Read_Garbage();
                        this.StartingCredits = this.Read_32Bits();
                    }
                    else if (ID == "BASE")
                    {
                        this.BasesEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "TIBR")
                    {
                        this.OreRegenerates = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "CRAT")
                    {
                        this.CratesEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "AIPL")
                    {
                        Read_Garbage();
                        this.NumberOfAIPlayers = this.Read_32Bits();
                    }
                    else if (ID == "SHAD")
                    {
                        this.ShroudRegrows = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "FLAG")
                    {
                        this.CTFEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "UNIT")
                    {
                        Read_Garbage();
                        this.StartingUnits = this.Read_32Bits();
                    }
                    else if (ID == "TECH")
                    {
                        Read_Garbage();
                        this.TechLevel = this.Read_32Bits();
                    }
                    else if (ID == "SCEN")
                    {
                        this.MapName = this.Parse_String();
                    }
                    else if (ID == "ADR1")
                    {
                        this.IPAddress1 = this.Parse_String();
                    }
                    else if (ID == "ADR2")
                    {
                        this.IPAddress2 = this.Parse_String();
                    }
                    else if (ID == "PING")
                    {
                        this.Ping = this.Parse_String();
                    }
                    else if (ID == "CMPL")
                    {
                        Read_Garbage();
                        this.CompletionType = this.Read_Byte();
                    }
                    else if (ID == "TIME")
                    {
                        Read_Garbage();
                        this.StartTime = this.Read_32Bits();
                    }
                    else if (ID == "DURA")
                    {
                        Read_Garbage();
                        this.GameDuration = this.Read_32Bits();
                    }
                    else if (ID == "AFPS")
                    {
                        Read_Garbage();
                        this.AverageFPS = this.Read_32Bits();
                    }
                    else if (ID == "PROC")
                    {
                        Read_Garbage();
                        this.ProcessorType = this.Read_Byte();
                    }
                    else if (ID == "MEMO")
                    {
                        Read_Garbage();
                        this.SystemMemory = (uint)this.Read_32Bits();
                    }
                    else if (ID == "VIDM")
                    {
                        Read_Garbage();
                        this.VideoMemory = (uint)this.Read_32Bits();
                    }
                    else if (ID == "SPED")
                    {
                        Read_Garbage();
                        this.GameSpeed = this.Read_Byte();
                    }
                    else if (ID == "VERS")
                    {
                        this.Version = this.Parse_Short_String();
                    }
                    else if (ID == "QUIT")
                    {
                        this.Parse_Quit_State();
                    }
                    else if (ID == "DATE")
                    {
                        this.Parse_Date_Info();
                    }
                }
            }
        }

        public void Print_Parsed_Data()
        {
            Console.WriteLine("DumpSize = {0}", this.DumpSize);
            Console.WriteLine("ReportedSize = {0}", this.ReportedSize);
            Console.WriteLine("SDFX = {0}", this.SDFX);
            Console.WriteLine("GameNumber = {0}", this.GameNumber);
            Console.WriteLine("NumberOfPlayers = {0}", this.NumberOfPlayers);
            Console.WriteLine("NumberOfRemainingPlayers = {0}", this.NumberOfRemainingPlayers);
            Console.WriteLine("IsTournamentGame = {0}", this.IsTournamentGame);
            Console.WriteLine("StartingCredits = {0}", this.StartingCredits);
            Console.WriteLine("BasesEnabled = {0}", this.BasesEnabled);
            Console.WriteLine("OreRegenerates = {0}", this.OreRegenerates);
            Console.WriteLine("CratesEnabled = {0}", this.CratesEnabled);
            Console.WriteLine("NumberOfAIPlayers = {0}", this.NumberOfPlayers);
            Console.WriteLine("ShroudRegrows = {0}", this.ShroudRegrows);
            Console.WriteLine("CTFEnabled = {0}", this.CTFEnabled);
            Console.WriteLine("StartingUnits = {0}", this.StartingUnits);
            Console.WriteLine("TechLevel = {0}", this.TechLevel);
            Console.WriteLine("MapName = {0}", this.MapName);
            Console.WriteLine("IPAddress1 = {0}", this.IPAddress1);
            Console.WriteLine("IPAddress2 = {0}", this.IPAddress2);
            Console.WriteLine("Ping = {0}", this.Ping);
            Console.WriteLine("CompletionType = {0}", this.CompletionType);
            Console.WriteLine("GameDuration = {0}", this.GameDuration);
            Console.WriteLine("StartTime = {0}", this.StartTime);
            Console.WriteLine("AverageFPS = {0}", this.AverageFPS);
            Console.WriteLine("ProcessorType = {0}", this.ProcessorType);
            Console.WriteLine("SystemMemory = {0}", this.SystemMemory);
            Console.WriteLine("VideoMemory = {0}", this.VideoMemory);
            Console.WriteLine("GameSpeed = {0}", this.GameSpeed);
            Console.WriteLine("Version = {0}", this.Version);
            Console.WriteLine("GameDateTimeUTC = {0}", this.GameDateTimeUTC.ToString());

            this.Print_Player_Array(this.PlayerMoneyHarvested, "Money harvested for player {0} = {1}");
            this.Print_Player_Array(this.PlayerCredits, "Credits for player {0} = {1}");
            this.Print_Player_Array(this.PlayerQuitStates, "Quit state for player {0} = {1}");
            this.Print_Player_Array(this.PlayerColors, "Color for player {0} = {1}");
            this.Print_Player_String_Array(this.PlayerSides, "Side for player {0} = {1}");
            this.Print_Player_String_Array(this.PlayerNames, "Name for player {0} = {1}");
        }

        public void Parse_Quit_State()
        {
            Read_Garbage();
            int PlayerNum = this.QuitPlayerNumHelper;

            this.PlayerQuitStates[PlayerNum - 1] = this.Read_Byte();
        }

        public void Parse_Date_Info()
        {
            Read_Garbage();
            FileTime FTime = new FileTime();

            FTime.dwLowDateTime = this.Read_Unsigned_32Bits();
            FTime.dwHighDateTime = this.Read_Unsigned_32Bits();

            this.GameDateTimeUTC = DateTime.FromFileTimeUtc(FileTime.FileTime_To_Long(FTime));
        }

        public void Parse_Color_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerColors[PlayerNum - 1] = this.Read_Byte();
        }

        public void Parse_Credits_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerCredits[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Crates_Collected_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            CratesCollectedStruct Crates = Parse_Crates_Collected_For_Player();

            this.PlayerCratesCollected[PlayerNum - 1] = Crates;
        }

        public string Parse_String()
        {
            byte[] Bytes = Bin.ReadBytes(4);
            int Length = (int)Bytes[3];

            byte[] StringBytes = Bin.ReadBytes(Length);
            string RetString = System.Text.Encoding.Default.GetString(StringBytes);

            int AlignRead = 4 - (Length % 4);
            if (AlignRead == 4) { AlignRead = 0; }
            Bin.ReadBytes(AlignRead); // Read for 4 byte alignment

            Console.WriteLine("Length = {0}, AlignRead = {1}", Length, AlignRead);
            Pos += AlignRead + 4 + Length;
            return RetString;
        }

        public string Parse_Short_String()
        {
            Read_Garbage();


            byte[] StringBytes = Bin.ReadBytes(4); Pos += 4;
            string RetString = System.Text.Encoding.Default.GetString(StringBytes);
            return RetString;
        }

        public int Read_32Bits()
        {
            Pos += 4;
            return Bin.ReadInt32();
        }

        public uint Read_Unsigned_32Bits()
        {
            Pos += 4;
            return Bin.ReadUInt32();
        }

        public int Read_Byte()
        {
            Pos += 4;
            int ByteRead = (int)Bin.ReadBigEndianBytes(1)[0];
            Bin.ReadBigEndianBytes(3); // throw away
            return ByteRead;
        }

        public void Read_Garbage()
        {
            this.Read_32Bits();
        }

        // Return 1 if ASCII string "ON" is read or 0 if "OFF" is read,
        // return -2 if something else was read
        public int Read_ON_Or_OFF()
        {
            Read_Garbage();

            int Ret = -2;
            int Bytes = this.Read_32Bits();

            if (Bytes == (int)0x4F4E0000) { Ret = 1; }
            if (Bytes == (int)0x4F464600) { Ret = 0; }

            return Ret;
        }

        public void Parse_Side_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);

            this.PlayerSides[PlayerNum - 1] = Parse_Short_String();

            this.QuitPlayerNumHelper = PlayerNum; // To help parsing QUIT per player
        }

        public void Parse_Name_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);

            this.PlayerNames[PlayerNum - 1] = Parse_String();

        }

        public CratesCollectedStruct Parse_Crates_Collected_For_Player()
        {
            CratesCollectedStruct Crates = new CratesCollectedStruct();

            Crates.MoneyCrates = this.Read_32Bits();
            Crates.UnitCrates = this.Read_32Bits();
            Crates.ParabombCrates = this.Read_32Bits();
            Crates.HealCrates = this.Read_32Bits();
            Crates.StealthCrates = this.Read_32Bits();
            Crates.ExplosionCrates = this.Read_32Bits();
            Crates.NapalmDeathCrates = this.Read_32Bits();
            Crates.SquadCrates = this.Read_32Bits();
            Crates.MapReshroud = this.Read_32Bits();
            Crates.MapRevealCrates = this.Read_32Bits();
            Crates.SonarPulseCrates = this.Read_32Bits();
            Crates.ArmorUpgradeCrates = this.Read_32Bits();
            Crates.SpeedUpgradeCrates = this.Read_32Bits();
            Crates.FirepowerUpgradeCrates = this.Read_32Bits();
            Crates.OneShotNukeCrates = this.Read_32Bits();
            Crates.TimeQuakeCrates = this.Read_32Bits();
            Crates.IronCurtainCrates = this.Read_32Bits();
            Crates.ChronoVortexCrates = this.Read_32Bits();

            return Crates;
        }

        public void Parse_Money_Harvested_Info(string ID, int Money)
        {
            int PlayerNumber = Get_Player_Number_From_ID(ID);

            this.PlayerMoneyHarvested[PlayerNumber - 1] = Money;
        }

        public int Get_Player_Number_From_ID(string ID)
        {
            int PlayerNumber = -1;

            string tmp = new string(ID[3], 1);
            Int32.TryParse(tmp, out PlayerNumber);


            if (PlayerNumber < 1 || PlayerNumber > 8)
            {
                throw new StatsDumpException("Player number was incorrectly parsed from ID string");
            }

            return PlayerNumber;
        }

        public void Print_Player_Array(int[] Array, string Format)
        {
            int PlayerNumber = 1;
            foreach (int Element in Array)
            {
                if (Element != -1)
                {
                    Console.WriteLine(Format, PlayerNumber, Element);
                }
                PlayerNumber++;
            }
        }
        public void Print_Player_String_Array(string[] Array, string Format)
        {
            int PlayerNumber = 1;
            foreach (string Element in Array)
            {
                if (Element != null)
                {
                    Console.WriteLine(Format, PlayerNumber, Element);
                }
                PlayerNumber++;
            }
        }
    }

    public struct FileTime
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public static long FileTime_To_Long(FileTime fileTime)
        {
            long returnedLong;
            // Convert 4 high-order bytes to a byte array
            byte[] highBytes = BitConverter.GetBytes(fileTime.dwHighDateTime);
            // Resize the array to 8 bytes (for a Long)
            Array.Resize(ref highBytes, 8);

            // Assign high-order bytes to first 4 bytes of Long
            returnedLong = BitConverter.ToInt64(highBytes, 0);
            // Shift high-order bytes into position
            returnedLong = returnedLong << 32;
            // Or with low-order bytes
            returnedLong = returnedLong | fileTime.dwLowDateTime;
            // Return long 
            return returnedLong;
        }
    }

    [Serializable()]
    public class StatsDumpLengthException : StatsDumpException
    {
        public StatsDumpLengthException() : base() { }
        public StatsDumpLengthException(string message) : base(message) { }
        public StatsDumpLengthException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected StatsDumpLengthException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    [Serializable()]
    public class StatsDumpException : System.Exception
    {
        public StatsDumpException() : base() { }
        public StatsDumpException(string message) : base(message) { }
        public StatsDumpException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected StatsDumpException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    public class BigEndianReader
    {
        public BigEndianReader(BinaryReader baseReader)
        {
            mBaseReader = baseReader;
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(ReadBigEndianBytes(2), 0);
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBigEndianBytes(2), 0);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBigEndianBytes(4), 0);
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBigEndianBytes(4), 0);
        }

        public byte[] ReadBigEndianBytes(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = count - 1; i >= 0; i--)
                bytes[i] = mBaseReader.ReadByte();

            return bytes;
        }

        public byte[] ReadBytes(int count)
        {
            return mBaseReader.ReadBytes(count);
        }

        public void Close()
        {
            mBaseReader.Close();
        }

        public Stream BaseStream
        {
            get { return mBaseReader.BaseStream; }
        }

        private BinaryReader mBaseReader;
    }
}

public struct CratesCollectedStruct
{
    public int MoneyCrates;
    public int UnitCrates;
    public int ParabombCrates;
    public int HealCrates;
    public int StealthCrates;
    public int ExplosionCrates;
    public int NapalmDeathCrates;
    public int SquadCrates;
    public int MapReshroud;
    public int MapRevealCrates;
    public int SonarPulseCrates;
    public int ArmorUpgradeCrates;
    public int SpeedUpgradeCrates;
    public int FirepowerUpgradeCrates;
    public int OneShotNukeCrates;
    public int TimeQuakeCrates;
    public int IronCurtainCrates;
    public int ChronoVortexCrates;
};