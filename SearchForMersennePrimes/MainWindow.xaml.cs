using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SearchForMersennePrimes
{
    public partial class MainWindow : Window
    {
        DLib.Math.Seeker.MersennePrime.Local mersennePrimeSeeker;
        bool closing;
        const string savePath = @"C:\Users\dan24\OneDrive\Dokumente\Mathematik\Mersenne-Primzahlen\SFMPSaves";

        public MainWindow() => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            closing = false;
            mersennePrimeSeeker = new DLib.Math.Seeker.MersennePrime.Local();
            new Thread(() =>
            {
                ulong totalPhysicalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true), ramCounter = new PerformanceCounter("Memory", "Available Bytes", true);
                while (!closing)
                {
                    string cpuUtilisation = ((int)cpuCounter.NextValue()).ToString() + "%", ramUtilisation = (int)((1 - ramCounter.NextValue() / totalPhysicalMemory) * 100) + "%";
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        textBoxCPUUtilisation.Text = cpuUtilisation;
                        textBoxRamUtilisation.Text = ramUtilisation;
                        if (mersennePrimeSeeker.Running && !mersennePrimeSeeker.Paused)
                        {
                            textBlockTime.Text = new TimeSpan(0, 0, (int)mersennePrimeSeeker.Time.TotalSeconds).ToString();
                            textBoxCurrentlyTested.Text = "2^" + mersennePrimeSeeker.NextExponent + "-1";
                            if (listBox.Items.Count != mersennePrimeSeeker.MersennePrimeCount)
                            {
                                listBox.Items.Clear();
                                foreach (var exponent in mersennePrimeSeeker.MersennePrimeExponents)
                                    listBox.Items.Add("2^" + exponent + "-1 is prime");
                                textBoxPrimeCount.Text = mersennePrimeSeeker.MersennePrimeCount.ToString();
                            }
                            if (listBox.SelectedIndex == -1 && mersennePrimeSeeker.MersennePrimeCount != 0)
                            {
                                var mersennePrime = mersennePrimeSeeker.MersennePrimes.Last();
                                textBoxExplorationDate.Text = mersennePrime.explorationDate.ToString();
                                textBoxTotalTime.Text = mersennePrime.totalTime.ToString();
                                textBoxTestTime.Text = mersennePrime.testTime.ToString();
                                textBoxExponent.Text = mersennePrime.exponent.ToString();
                            }
                        }
                    }));
                    Thread.Sleep(1000);
                }
            }).Start();
            textBoxStartExponent.Text = "5";
            textBoxThreadCount.Text = Environment.ProcessorCount.ToString();
            textBlockVersion.Text = DLib.Math.Seeker.MersennePrime.Local.version;
            SetStoppedWindowMode();
            ReloadSaves();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            mersennePrimeSeeker.Stop();
        }

        private void buttonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (mersennePrimeSeeker.Running)
            {
                mersennePrimeSeeker.Stop();
                SetStoppedWindowMode();
            }
            else
            {
                mersennePrimeSeeker.Start(uint.Parse(textBoxStartExponent.Text), byte.Parse(textBoxThreadCount.Text));
                SetStartedWindowMode();
            }
        }

        private void buttonPauseContinue_Click(object sender, RoutedEventArgs e)
        {
            if (mersennePrimeSeeker.Paused)
            {
                mersennePrimeSeeker.Continue();
                buttonPauseContinue.Content = "Pause";
            }
            else
            {
                mersennePrimeSeeker.Pause();
                buttonPauseContinue.Content = "Continue";
            }
        }

        private void buttonSaveLoad_Click(object sender, RoutedEventArgs e)
        {
            if (mersennePrimeSeeker.Started)
            {
                if (!mersennePrimeSeeker.Save(textBoxSaveName.Text, savePath))
                    MessageBox.Show("Saving failed");
                ReloadSaves();
            }
            else
            {
                mersennePrimeSeeker.Start(savePath + @"\" + comboBoxSaveName.Text, byte.Parse(textBoxThreadCount.Text));
                SetStartedWindowMode();
            }
        }

        private void textBoxStartExponent_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!uint.TryParse(textBoxStartExponent.Text, out uint i))
            {
                textBoxStartExponent.Text = "5";
                MessageBox.Show("The start exponent can not be set. Maybe the input is not a number or it is too large. It will be set on default.", "Input error");
            }
        }

        private void textBoxThreadCount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (byte.TryParse(textBoxThreadCount.Text, out byte b))
                mersennePrimeSeeker.ThreadCount = b;
            else
            {
                textBoxThreadCount.Text = Environment.ProcessorCount.ToString();
                mersennePrimeSeeker.ThreadCount = (byte)Environment.ProcessorCount;
                MessageBox.Show("The thread count can not be set. Maybe the count is not a number or it is too large. It will be set on default.", "Input error");
            }
        }

        void SetStartedWindowMode()
        {
            textBlockSelectedPrime.Visibility = Visibility.Visible;
            textBlockExponent.Visibility = Visibility.Visible;
            textBlockTotalTime.Visibility = Visibility.Visible;
            textBlockTestTime.Visibility = Visibility.Visible;
            textBlockExplorationDate.Visibility = Visibility.Visible;
            textBoxExponent.Visibility = Visibility.Visible;
            textBoxTotalTime.Visibility = Visibility.Visible;
            textBoxTestTime.Visibility = Visibility.Visible;
            textBoxExplorationDate.Visibility = Visibility.Visible;
            comboBoxSaveName.Visibility = Visibility.Hidden;
            textBoxSaveName.Visibility = Visibility.Visible;
            textBlockState.Visibility = Visibility.Visible;
            textBlockCurrentlyTested.Visibility = Visibility.Visible;
            textBlockPrimeCount.Visibility = Visibility.Visible;
            textBoxCurrentlyTested.Visibility = Visibility.Visible;
            textBoxPrimeCount.Visibility = Visibility.Visible;
            buttonPauseContinue.IsEnabled = true;
            buttonDelete.IsEnabled = false;
            textBoxSaveName.Text = DateTime.Now.ToString().Replace(":", "") + "v" + DLib.Math.Seeker.MersennePrime.Local.version;
            buttonStartStop.Content = "Stop";
            buttonPauseContinue.Content = "Pause";
            buttonSaveLoad.Content = "Save";
        }

        void SetStoppedWindowMode()
        {
            textBlockSelectedPrime.Visibility = Visibility.Hidden;
            textBlockExponent.Visibility = Visibility.Hidden;
            textBlockTotalTime.Visibility = Visibility.Hidden;
            textBlockTestTime.Visibility = Visibility.Hidden;
            textBlockExplorationDate.Visibility = Visibility.Hidden;
            textBoxExponent.Visibility = Visibility.Hidden;
            textBoxTotalTime.Visibility = Visibility.Hidden;
            textBoxTestTime.Visibility = Visibility.Hidden;
            textBoxExplorationDate.Visibility = Visibility.Hidden;
            comboBoxSaveName.Visibility = Visibility.Visible;
            textBoxSaveName.Visibility = Visibility.Hidden;
            textBlockState.Visibility = Visibility.Hidden;
            textBlockCurrentlyTested.Visibility = Visibility.Hidden;
            textBlockPrimeCount.Visibility = Visibility.Hidden;
            textBoxCurrentlyTested.Visibility = Visibility.Hidden;
            textBoxPrimeCount.Visibility = Visibility.Hidden;
            buttonPauseContinue.IsEnabled = false;
            buttonDelete.IsEnabled = true;
            textBlockTime.Text = "";
            buttonStartStop.Content = "Start";
            buttonPauseContinue.Content = "Pause";
            buttonSaveLoad.Content = "Load";
            listBox.Items.Clear();
        }

        void ReloadSaves()
        {
            comboBoxSaveName.Items.Clear();
            if (Directory.Exists(savePath))
                foreach (FileInfo save in new DirectoryInfo(savePath).GetFiles())
                    comboBoxSaveName.Items.Add(save.Name);
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(savePath + @"\" + comboBoxSaveName.Text.ToString());
            ReloadSaves();
        }

        private void listBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (listBox.SelectedIndex != -1)
            {
                var mersennePrime = mersennePrimeSeeker.MersennePrimes[listBox.SelectedIndex];
                textBoxExplorationDate.Text = mersennePrime.explorationDate.ToString();
                textBoxTotalTime.Text = mersennePrime.totalTime.ToString();
                textBoxTestTime.Text = mersennePrime.testTime.ToString();
                textBoxExponent.Text = mersennePrime.exponent.ToString();
            }
        }
    }
}
