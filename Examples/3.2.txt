
public class Player
{
	[Serializable]
	public int Health {
		get;
		private set;
	}

	public Player() {
	}

	public void Hit(int damage) {
		Health -= damage;
	}
}

public class Settings
{
	[Serializable]
	public int Damage { get; }
}

class Program
{
	private const string NewPlayerPath = "NewPlayer.json";
	private const string SettingsPath = "Settings.json";

	private Player player;

	public static void Main(string[] args)
	{
		// Создаем нового игрока.
		var player = Serializer.LoadFromFile<Player>(NewPlayerPath);
		// Ударяем игрока.
		var settings = Serializer.LoadFromFile<Settings>(SettingsPath);
		player.Hit(settings.Damage);
	}
}
