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


        int DumpSize = -1;
        int ReportedSize = -1;
        int[] PlayerMoneyHarvested;
        int[] PlayerCredits;
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

        public StatsDumpParser(string FileName)
        {
            PlayerMoneyHarvested = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerCredits = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerCratesCollected = new CratesCollectedStruct[8];

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
                if (this.DumpSize< 4 )
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


                    if ( ID.Contains("HRV") )
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
                        this.CratesEnabled = this.Read_32Bits();
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

            this.Print_Player_Array(this.PlayerMoneyHarvested, "Money harvested for player {0} = {1}");
            this.Print_Player_Array(this.PlayerCredits, "Credits for player {0} = {1}");
        }

        public void Parse_Credits_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            this.PlayerCredits[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Crates_Collected_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            CratesCollectedStruct Crates = Parse_Crates_Collected_For_Player();

            this.PlayerCratesCollected[PlayerNum - 1] = Crates;
        }

        public int Read_32Bits()
        {
            Pos += 4;
            return Bin.ReadInt32();
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