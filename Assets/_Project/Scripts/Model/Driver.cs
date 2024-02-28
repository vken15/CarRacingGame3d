namespace CarRacingGame3d
{
    public class Driver
    {
        private int playerNumber;
        private string name;
        private int carID;
        private bool isAI;
        private AIDifficult difficult;
        //private InputType driverInput = InputType.keyboard;
        private int lastRacePosition = 0;
        private int points = 0;
        private ulong networkId;

        public Driver(int playerNumber, string name, int carID, bool isAI, AIDifficult difficult, ulong networkId)
        {
            this.PlayerNumber = playerNumber;
            this.Name = name;
            this.CarID = carID;
            this.IsAI = isAI;
            this.Difficult = difficult;
            //this.DriverInput = driverInput;
            this.networkId = networkId;
        }
        public int PlayerNumber { get => playerNumber; set => playerNumber = value; }
        public string Name { get => name; set => name = value; }
        public int CarID { get => carID; set => carID = value; }
        public bool IsAI { get => isAI; set => isAI = value; }
        //public InputType DriverInput { get => driverInput; set => driverInput = value; }
        public AIDifficult Difficult { get => difficult; set => difficult = value; }
        public int LastRacePosition { get => lastRacePosition; set => lastRacePosition = value; }
        public int Points { get => points; set => points = value; }
        public ulong NetworkId { get => networkId; set => networkId = value; }
    }
}