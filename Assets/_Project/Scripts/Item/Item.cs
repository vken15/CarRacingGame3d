namespace CarRacingGame3d
{
    public interface IItem
    {
        int ItemID { get; }

        abstract public void UseItem();
    }

    public class Fuel : IItem
    {
        public int ItemID => 1;

        public void UseItem()
        {
            throw new System.NotImplementedException();
        }
    }
}
