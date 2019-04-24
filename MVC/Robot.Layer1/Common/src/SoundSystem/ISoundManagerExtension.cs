using System;

namespace Robot.Layer1.Common.SoundSystem
{
	public interface ISoundManagerExtension
	{
		bool MusicEnabled { get; }
		bool EffectsEnabled { get; }
		bool VoiceEnabled { get; }
		float MusicVolume { get; }
		float VoiceVolume { get; }
		float EffectsVolume { get; }
		void Subscribe(Action onChanged);
	}
}
