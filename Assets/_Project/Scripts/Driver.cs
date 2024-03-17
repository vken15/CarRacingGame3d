namespace CarRacingGame3d
{
    public class Driver
    {
        private ushort playerNumber;
        private string name;
        private ushort carID;
        private bool isAI;
        private AIDifficult difficult;
        //private InputType driverInput = InputType.keyboard;
        private int lastRacePosition = 0;
        private int points = 0;
        private ulong networkId;

        public Driver(ushort playerNumber, string name, ushort carID, bool isAI, ulong networkId, AIDifficult difficult = AIDifficult.Easy)
        {
            this.PlayerNumber = playerNumber;
            this.Name = name;
            this.CarID = carID;
            this.IsAI = isAI;
            this.Difficult = difficult;
            //this.DriverInput = driverInput;
            this.networkId = networkId;
        }
        public ushort PlayerNumber { get => playerNumber; set => playerNumber = value; }
        public string Name { get => name; set => name = value; }
        public ushort CarID { get => carID; set => carID = value; }
        public bool IsAI { get => isAI; set => isAI = value; }
        //public InputType DriverInput { get => driverInput; set => driverInput = value; }
        public AIDifficult Difficult { get => difficult; set => difficult = value; }
        public int LastRacePosition { get => lastRacePosition; set => lastRacePosition = value; }
        public int Score { get => points; set => points = value; }
        public ulong ClientId { get => networkId; set => networkId = value; }
    }
}