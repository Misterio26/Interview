public class ExtPlayer : Player
{
	private event Action innerChanged;
	public event Action Changed {
		add {
			innerChanged += value;
			value();
		}
		remove {
			innerChanged -= value;
		}
	}
}

class ExtProgram : Program
{
	// Виджет, отображающий игроку здоровье.
	private TextView healthView;

	private int? previousHealth;

	public static void ExtMain(string[] args)
	{
		// Вызов кода по созданию игрока.
		Main(args);

		player.Changed += OnPlayerChanged;

		// Ударяем игрока.
		HitPlayer();
	}

	private void OnPlayerChanged()
	{
		healthView.Text = player.Health.ToString();
		if (previousHealth != null && player.Health - previousHealth < -10) {
			healthView.Color = Color.Red;
		} else {
			healthView.Color = Color.White;
		}
		previousHealth = player.Health;
	}
}
