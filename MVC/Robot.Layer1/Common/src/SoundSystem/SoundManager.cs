using System;
using System.Collections.Generic;
using Lime;

namespace Robot.Layer1.Common.SoundSystem
{
	public class SoundManager
	{
		public static SoundManager Instance { get; private set; }

		private readonly float musicTransitionDuration;

		private readonly Node controller;
		private readonly ISoundManagerExtension extension;

		private Sound activeJingleSound;
		private Sound activeMusicSound;
		private Sound activeAmbientSound;

		private string activeMusicPath;
		private string activeAmbientPath;

		private readonly Dictionary<string, DateTime> effectTimestamps = new Dictionary<string, DateTime>();

		private readonly List<MusicTransitionHelper> musicTransitions = new List<MusicTransitionHelper>();

		private readonly Stack<string> musicStack = new Stack<string>();

		public string ActiveAmbientPath => activeAmbientPath;

		private SoundManager(Node controller, ISoundManagerExtension extension, float musicTransitionDuration)
		{
			this.controller = controller;
			this.extension = extension;
			this.musicTransitionDuration = musicTransitionDuration;
		}

		private void Initialize()
		{
			extension.Subscribe(OnSettingsChanged);
			controller.Updated += Update;
		}

		private void OnSettingsChanged()
		{
			AudioSystem.SetGroupVolume(AudioChannelGroup.Music, extension.MusicEnabled ? extension.MusicVolume : 0);
			AudioSystem.SetGroupVolume(AudioChannelGroup.Voice, extension.VoiceEnabled ? extension.VoiceVolume : 0);
			AudioSystem.SetGroupVolume(AudioChannelGroup.Effects,
				extension.EffectsEnabled ? extension.EffectsVolume : 0);
		}

		public Sound PlayEffect(
			string soundFile, string polyphonicGroup = null,
			float pan = 0, int polyphonicMinInterval = 50, float pitch = 1, float priority = 0.5f, float volume = 1.0f,
			bool looped = false
		)
		{
			if (polyphonicGroup != null) {
				var now = DateTime.Now;

				if (
					effectTimestamps.TryGetValue(polyphonicGroup, out var value) &&
					(now - value).Milliseconds < polyphonicMinInterval
				) {
					return new Sound();
				}

				effectTimestamps[polyphonicGroup] = now;
			}

			var sound = AudioSystem.Play(
				soundFile, AudioChannelGroup.Effects, looped, priority, 0, false, volume, pan
			);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (pitch != 1) {
				sound.Pitch = pitch;
			}

			return sound;
		}

		public Sound PlayJingle(string soundFile)
		{
			activeMusicSound?.Stop(0.5f);
			activeJingleSound?.Stop(0.5f);
			activeJingleSound = AudioSystem.PlayMusic(soundFile, false);
			return activeJingleSound;
		}

		public void StopJingle()
		{
			if (activeJingleSound == null) {
				return;
			}
			activeJingleSound.Stop(0.5f);
			activeJingleSound = null;
			OnJingleStopped();
		}

		private void OnJingleStopped()
		{
			activeMusicSound?.Resume();
		}

		public Sound PlayAmbient(string soundFile)
		{
			if (activeAmbientPath == soundFile) {
				return activeAmbientSound;
			}

			activeAmbientSound?.Stop(0.5f);

			activeAmbientPath = soundFile;
			activeAmbientSound = soundFile == null
				? null
				: AudioSystem.PlayEffect(soundFile, true, 100, 0.5f);

			return activeAmbientSound;
		}

		public Sound PlayMusic(string soundFile, bool looped = true, float volume = 1.0f)
		{
			return PlayMusic(soundFile, null, looped, volume);
		}

		public void ResumeMusic((string soundFile, Sound sound) savedMusic)
		{
			PlayMusic(savedMusic.soundFile, savedMusic.sound);
		}

		private Sound PlayMusic(string soundFile, Sound preparedSound, bool looped = true, float volume = 1.0f)
		{
			if (activeMusicPath == soundFile) {
				return activeMusicSound;
			}

			if (activeMusicSound != null) {
				musicTransitions.Add(new MusicTransitionHelper(activeMusicSound, musicTransitionDuration));
			}

			bool paused = activeJingleSound != null;

			activeMusicPath = soundFile;
			if (soundFile == null) {
				activeMusicSound = null;
			} else {
				if (preparedSound == null) {
					activeMusicSound = AudioSystem.PlayMusic(
						soundFile, looped, 100, musicTransitionDuration, paused, volume
					);
				} else {
					activeMusicSound = preparedSound;
					preparedSound.Resume(musicTransitionDuration);
				}
			}

			return activeMusicSound;
		}

		public (string soundFile, Sound sound) StopMusic(float fadeoutTime = 0)
		{
			if (activeMusicPath == null) {
				return (null, null);
			}

			string previousMusicPath = activeMusicPath;
			var previousMusicSound = activeMusicSound;

			activeMusicSound?.Stop(fadeoutTime);
			activeMusicPath = null;
			activeMusicSound = null;

			return (previousMusicPath, previousMusicSound);
		}

		public void PushMusic()
		{
			musicStack.Push(activeMusicPath);
		}

		public void PopMusic()
		{
			PlayMusic(musicStack.Pop());
		}

		private void Update(float delta)
		{
			if (activeJingleSound != null) {
				if (activeJingleSound.IsStopped) {
					activeJingleSound = null;
					OnJingleStopped();
				}
			}

			for (int i = musicTransitions.Count - 1; i >= 0; i--) {
				var musicTransition = musicTransitions[i];

				if (musicTransition.Advance(delta)) {
					musicTransitions.RemoveAt(i);
				}
			}
		}

		private class MusicTransitionHelper
		{
			private readonly Sound oldMusic;
			private readonly float duration;
			private float timePassed;

			public MusicTransitionHelper(Sound oldMusic, float duration)
			{
				this.oldMusic = oldMusic;
				this.duration = duration;
			}

			public bool Advance(float deltaTime)
			{
				timePassed += deltaTime;

				if (timePassed < duration) {
					return false;
				}

				oldMusic.Stop(duration);
				return true;
			}
		}

		public static void Activate(Node controller, ISoundManagerExtension extension, float musicTransitionDuration)
		{
			if (Instance != null) {
				throw new InvalidOperationException($"{nameof(SoundManager)} already activated");
			}
			Instance = new SoundManager(controller, extension, musicTransitionDuration);
			Instance.Initialize();
		}
	}
}
