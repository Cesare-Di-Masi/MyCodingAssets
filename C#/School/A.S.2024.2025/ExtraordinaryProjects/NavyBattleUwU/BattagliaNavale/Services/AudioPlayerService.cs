using Plugin.Maui.Audio;

namespace BattagliaNavale.Services
{
    public class AudioPlayerService
    {
        private static AudioPlayerService _instance;
        public static AudioPlayerService Instance => _instance ??= new AudioPlayerService();

        private IAudioPlayer _player;

        private bool _isMuted = false;
        public bool IsMuted => _isMuted;

        private bool _isPaused = false;

        public void Play(string file = "jack_sparrow.mp3")
        {
            if (_isPaused)
            {
                _isPaused = false;
                _player.Play();
                return;
            }

            if (_player == null || !_player.IsPlaying)
            {
                var audioFile = FileSystem.OpenAppPackageFileAsync(file).Result;
                _player = AudioManager.Current.CreatePlayer(audioFile);
            }

            _player.Play();
            _player.Volume = _isMuted ? 0 : 1;
        }

        public void Stop()
        {
            _player?.Stop();
        }

        public void Pause()
        {
            _isPaused = true;
            _player?.Pause();
        }

        public void Mute()
        {
            if (_player != null)
            {
                _isMuted = !_isMuted;
                _player.Volume = _isMuted ? 0 : 1;
            }
        }
    }
}
