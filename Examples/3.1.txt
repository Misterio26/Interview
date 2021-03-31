public class Player
{
    public int Health {
        get; private set;
    }

    public Player(int health) {
        Health = health;
    }

    public void SetHealth(int value) {
        Health = value;
    }
}

class Program
{
    private const int NewPayerHealth = 100;
    private const int Damage = 10;

    private Player player;
    
    public static void Main(string[] args)
    {
        // Создаем нового игрока.
        player = new Player(NewPayerHealth);
        // Ударяем игрока.
        player.SetHealth(player.Health - Damage);
    }
}
