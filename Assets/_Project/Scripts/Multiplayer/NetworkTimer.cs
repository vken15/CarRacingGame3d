namespace CarRacingGame3d
{
    public class NetworkTimer
    {
        float timer;
        public float MinTimeBetweenTicks { get; }
        public int CurrentTick { get; private set; }

        public NetworkTimer(float serverTickRate)
        {
            MinTimeBetweenTicks = 1.0f / serverTickRate;    
        }

        public void Update(float deltaTime) 
        {
            timer += deltaTime;
        }

        public bool ShouldTick()
        {
            if (timer >= MinTimeBetweenTicks)
            {
                timer -= MinTimeBetweenTicks;
                CurrentTick++;
                return true;
            }
            return false;
        }
    }
}
