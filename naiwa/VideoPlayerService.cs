using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace naiwa
{
    public class VideoPlayerService
    {
        private readonly MediaElement _mediaElement;
        private readonly TextBlock _errorText;
        private DispatcherTimer? _playTimer;
        private bool _isPlaying = false;
        private int _playRetryCount;
        private List<string> _videoFiles = new List<string>();
        private int _currentVideoIndex = 0;
        private readonly string[] _videoExtensions = { ".mp4", ".wmv", ".avi", ".mov", ".m4v" };

        public VideoPlayerService(MediaElement mediaElement, TextBlock errorText)
        {
            _mediaElement = mediaElement ?? throw new ArgumentNullException(nameof(mediaElement));
            _errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        }

        public void LoadVideo()
        {
            ScanVideoFiles();

            if (_videoFiles.Count == 0)
            {
                _errorText.Text = "找不到视频文件";
                _errorText.Visibility = Visibility.Visible;
                return;
            }

            _errorText.Visibility = Visibility.Collapsed;
            PlayNextVideo();
        }

        private void ScanVideoFiles()
        {
            _videoFiles.Clear();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                var files = Directory.GetFiles(baseDir);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (_videoExtensions.Contains(ext))
                    {
                        _videoFiles.Add(file);
                    }
                }

                _videoFiles.Sort();
            }
            catch
            {
                _videoFiles.Clear();
            }
        }

        private void PlayNextVideo()
        {
            if (_videoFiles.Count == 0) return;

            _isPlaying = false;

            if (_currentVideoIndex >= _videoFiles.Count)
            {
                _currentVideoIndex = 0;
            }

            string videoPath = _videoFiles[_currentVideoIndex];
            _mediaElement.Source = new Uri(videoPath);
            _playRetryCount = 0;
            if (_playTimer != null)
            {
                _playTimer.Stop();
                _playTimer.Tick -= PlayTimer_Tick;
                _playTimer = null;
            }
            _playTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _playTimer.Tick += PlayTimer_Tick;
            _playTimer.Start();
        }

        private void PlayTimer_Tick(object? sender, EventArgs e)
        {
            if (_isPlaying)
            {
                _playTimer?.Stop();
                return;
            }

            _playRetryCount++;
            _mediaElement.Volume = 1;
            _mediaElement.Play();

            if (_mediaElement.Position > TimeSpan.Zero || _playRetryCount >= 10)
            {
                _playTimer?.Stop();
            }
        }

        public void OnMediaOpened()
        {
            _playTimer?.Stop();
            _mediaElement.Volume = 1;
            _mediaElement.Play();
            _isPlaying = true;
        }

        public void OnMediaEnded()
        {
            _isPlaying = false;
            _currentVideoIndex++;
            PlayNextVideo();
        }

        public void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            _isPlaying = false;
            _playTimer?.Stop();
            _currentVideoIndex++;
            PlayNextVideo();
        }
    }
}
