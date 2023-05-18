using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace pract3
{
    public partial class MainWindow : Window
    {
        private List<string> audioFiles; // Список путей к аудиофайлам
        private int currentFileIndex; // Индекс текущего аудиофайла
        private CancellationTokenSource cancellationTokenSource; // Источник для отмены потока
        private double maxDuration = 0;
        private bool IsSliderValueBeingUpdated = false;
        private DispatcherTimer sliderUpdateTimer;
        private bool isSliderDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            audioFiles = new List<string>();
            currentFileIndex = 0;
        }

        private void OpenBtn_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFolder();
        }

        private async void OpenFolder()
        {
            var folderPicker = new CommonOpenFileDialog();
            folderPicker.IsFolderPicker = true;
            CommonFileDialogResult result = folderPicker.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                string folderPath = folderPicker.FileName;
                await LoadAudioFiles(folderPath);
                PlayAudio();
            }
        }

        private async Task LoadAudioFiles(string folderPath)
        {
            // Очистка списка аудиофайлов
            audioFiles.Clear();

            // Получение всех файлов в выбранной папке с расширениями mp3, m4a, wav и прочими
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".mp3") || file.EndsWith(".m4a") || file.EndsWith(".wav"))
                .ToArray();

            audioFiles.AddRange(files);

            // Обновление ListBox с файлами
            FileTxt.ItemsSource = audioFiles.Select(System.IO.Path.GetFileName);
        }

        private void PlayAudio()
        {
            if (audioFiles.Count == 0)
                return;

            string audioPath = audioFiles[currentFileIndex];
            media.Source = new Uri(audioPath);
            media.Play();

            UpdateAudioSlider();
            UpdateTimer();

            // Отмена предыдущего потока, если есть
            cancellationTokenSource?.Cancel();

            // Запуск нового потока для обновления слайдера и таймера
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Task.Run(() => UpdateAudioPosition());
        }

        private void UpdateAudioSlider()
        {
            audioSlider.Minimum = 0;

            if (media.NaturalDuration.HasTimeSpan)
            {
                double maxDuration = media.NaturalDuration.TimeSpan.TotalSeconds;

                // Проверяем, что текущая позиция не выходит за пределы длительности трека
                double currentPosition = Math.Min(media.Position.TotalSeconds, maxDuration);

                // Устанавливаем значение слайдера пропорционально длительности трека
                audioSlider.Value = currentPosition;

                timer_end.Text = media.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void UpdateTimer()
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                timer_start.Text = media.Position.ToString(@"mm\:ss");
                timer_end.Text = media.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
            else
            {
                timer_start.Text = "00:00";
                timer_end.Text = "00:00";
            }
        }

        private void UpdateAudioPosition()
        {
            if (!audioSlider.IsMouseCaptureWithin && media.NaturalDuration.HasTimeSpan)
            {
                double maxDuration = media.NaturalDuration.TimeSpan.TotalSeconds;
                double sliderValue = audioSlider.Value;

                if (sliderValue > maxDuration)
                    sliderValue = maxDuration;

                media.Position = TimeSpan.FromSeconds(sliderValue);
                UpdateTimer();
            }
        }

        private void FileTxt_SelectionChanged(object sender, RoutedEventArgs e)
        {
            currentFileIndex = FileTxt.SelectedIndex;
            PlayAudio();
        }

        private void media_MediaEnded_1(object sender, RoutedEventArgs e)
        {
            if (currentFileIndex < audioFiles.Count - 1)
                currentFileIndex++;
            else
                currentFileIndex = 0;

            PlayAudio();
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void LeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileIndex > 0)
                currentFileIndex--;
            else
                currentFileIndex = audioFiles.Count - 1;

            PlayAudio();
        }

        private void RightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileIndex < audioFiles.Count - 1)
                currentFileIndex++;
            else
                currentFileIndex = 0;

            PlayAudio();
        }

        private void RepeatBtn_Click(object sender, RoutedEventArgs e)
        {
            media.Position = TimeSpan.Zero;
            media.Play();
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (audioFiles.Count > 1)
            {
                Random random = new Random();
                audioFiles = audioFiles.OrderBy(x => random.Next()).ToList();
                currentFileIndex = 0;
                PlayAudio();
            }
        }

        private void audioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(audioSlider.Value);
                media.Position = newPosition;
                UpdateTimer();
            }
        }
        private void SliderUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (media.NaturalDuration.HasTimeSpan && !isSliderDragging)
            {
                audioSlider.Value = media.Position.TotalSeconds;
                UpdateTimer();
            }
        }
        private void audioSlider_DragStarted(object sender, RoutedEventArgs e)
        {
            isSliderDragging = true;
        }

        private void audioSlider_DragCompleted(object sender, RoutedEventArgs e)
        {
            isSliderDragging = false;
            media.Position = TimeSpan.FromSeconds(audioSlider.Value);
            UpdateTimer();
        }
    }
}