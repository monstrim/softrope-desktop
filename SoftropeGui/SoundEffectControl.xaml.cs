﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Teknohippy.Softrope;
using System.Windows.Threading;
using SoftropeGui.Properties;
using System.Windows.Input;
using System.Collections.Generic;

namespace SoftropeGui
{
    public partial class SoundEffectControl
    {
        public SoundEffectControl(SoundEffect soundEffect)
        {
            this.InitializeComponent();
            SoundEffect = soundEffect;
            SliderHigh.Maximum = Settings.Default.MaximumLoop;
            SliderLow.Maximum = Settings.Default.MaximumLoop;
            SoundEffect.PlayingSample += new EventHandler(PlayingSample);
            SoundEffect.Ended += new EventHandler(Ended);

            LoadSoundEffect();
        }

        public SoundEffectControl()
            : this(new SoundEffect())
        {

        }

        private void LoadSoundEffect()
        {
            SoundEffectNameText.Text = SoundEffect.Name;
            MuteCheckBox.IsChecked = SoundEffect.Mute;
            VolumeSlider.Value = SoundEffect.SoundEffectVolume;
            PreDelayCheckBox.IsChecked = SoundEffect.PreDelay;
            LoopCheckBox.IsChecked = SoundEffect.IsLooping;
            int temp = SoundEffect.LoopGap; //following line invokes ChangeLoopText which sets LoopGap
            SliderHigh.Value = SoundEffect.LoopGap + SoundEffect.LoopGapVariance;
            SliderLow.Value = temp;
            SequentialCheckBox.IsChecked = SoundEffect.IsPlayList;
            ChangeLoopText();
            foreach (Sample sample in SoundEffect.Samples)
            {
                CreateSampleControl(SoundEffect, sample);
            }
            if (SoundEffect.Samples.Count == 0)
            {
                Sample sample = new Sample();
                SoundEffect.Samples.Add(sample);
                CreateSampleControl(SoundEffect, sample);
            }
        }

        void PlayingSample(object sender, EventArgs e)
        {
            SoundEffect.PlayingSample -= new EventHandler(PlayingSample);
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                foreach (SampleControl sampleControl in SamplesStackPanel.Children)
                {
                    // using IsDefault style to signalize playing sample
                    // TODO: implement custom property
                    sampleControl.OpenSampleButton.IsDefault = (sampleControl.Sample == SoundEffect.CurrentSample);
                    sampleControl.OpenSampleButton.IsCancel = sampleControl.Sample.isMissing;
                }
            }));
            SoundEffect.PlayingSample += new EventHandler(PlayingSample);
        }

        void Ended(object sender, EventArgs e)
        {
            SoundEffect.Ended -= new EventHandler(Ended);
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                foreach (SampleControl sampleControl in SamplesStackPanel.Children)
                {
                    // using IsDefault style to signalize playing sample
                    // TODO: implement custom property
                    sampleControl.OpenSampleButton.IsDefault = false;
                }
            }));
            SoundEffect.Ended += new EventHandler(Ended);
        }

        private SampleControl CreateSampleControl(SoundEffect soundEffect, Sample sample)
        {
            SampleControl sampleControl = new SampleControl(sample, soundEffect);
            SamplesStackPanel.Children.Add(sampleControl);
            sampleControl.Removed += new EventHandler(sampleControl_Removed);
            sampleControl.LoadingSample += new EventHandler(sampleControl_LoadingSample);

            return sampleControl;
        }

        void sampleControl_LoadingSample(object sender, EventArgs e)
        {
            SoundEffect.Stop();
        }

        public SoundEffect SoundEffect { get; set; }

        private void AddSampleButton_Click(object sender, RoutedEventArgs e)
        {
            Sample sample = new Sample();
            SoundEffect.Samples.Add(sample);
            CreateSampleControl(SoundEffect, sample);
        }

        void sampleControl_Removed(object sender, EventArgs e)
        {
            if (SoundEffect.Samples.Count > 1)
            {
                SoundEffect.Stop();
                SampleControl sampleControl = sender as SampleControl;
                SoundEffect.Samples.Remove(sampleControl.Sample);
                SamplesStackPanel.Children.Remove(sampleControl);
            }
        }

        private void SoundEffectNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SoundEffect != null)
            {
                SoundEffect.Name = SoundEffectNameText.Text;
            }
        }

        private void LoopButton_Checked(object sender, RoutedEventArgs e)
        {
            SoundEffect.IsLooping = true;
        }

        private void LoopButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SoundEffect.IsLooping = false;
        }

        private void PreDelayButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SoundEffect.PreDelay = false;
        }

        private void PreDelayButton_Checked(object sender, RoutedEventArgs e)
        {
            SoundEffect.PreDelay = true;
        }

        private void SliderHigh_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SliderHigh.Value < SliderLow.Value)
            {
                SliderHigh.Value = SliderLow.Value;
            }
            SliderLow.SelectionEnd = SliderHigh.Value;
            ChangeLoopText();
        }

        private void ChangeLoopText()
        {

            if (LoopStatus != null)
            {
                string message = "";
                if (SliderHigh.Value == 0 && SliderLow.Value == 0 && LoopCheckBox.IsChecked == true)
                {
                    message = "Loop with no gap.";
                }
                else if (LoopCheckBox.IsChecked == false)
                {
                    message = "No looping.";
                }
                else if (SliderHigh.Value == SliderLow.Value)
                {
                    message = String.Format("Insert {0:F1} second gap before looping.", SliderLow.Value / 1000);
                }
                else
                {
                    message = String.Format("Insert {0:F1} - {1:F1} second gap before looping.", SliderLow.Value / 1000, SliderHigh.Value / 1000);
                }
                LoopStatus.Text = message;
                SliderHigh.ToolTip = message;
                SliderLow.ToolTip = message;
            }

            SoundEffect.LoopGap = (int)(SliderLow.Value);
            SoundEffect.LoopGapVariance = (int)(SliderHigh.Value - SliderLow.Value);


        }

        private void SliderLow_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SliderLow.Value > SliderHigh.Value)
            {
                SliderLow.Value = SliderHigh.Value;
            }
            SliderLow.SelectionStart = SliderLow.Value;

            if (SliderHigh.Value == SliderLow.Value && SliderHigh.Value == SliderHigh.Maximum)
            {
                SliderHigh.Visibility = Visibility.Hidden;
            }
            else
            {
                SliderHigh.Visibility = Visibility.Visible;
            }

            ChangeLoopText();
        }

        private void MuteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.Mute == false)
            {
                SoundEffect.Mute = true;
            }
        }

        private void MuteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.Mute == true)
            {
                SoundEffect.Mute = false;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SoundEffect != null)
            {
                SoundEffect.SoundEffectVolume = (float)VolumeSlider.Value;
            }
        }

        private void PreDelayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.PreDelay == false)
            {
                SoundEffect.PreDelay = true;
            }
        }

        private void PreDelayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.PreDelay == true)
            {
                SoundEffect.PreDelay = false;
            }
        }

        private void LoopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.IsLooping == false)
            {
                SoundEffect.IsLooping = true;
                ChangeLoopText();
            }
        }

        private void LoopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SoundEffect.IsLooping == true)
            {
                SoundEffect.IsLooping = false;
                ChangeLoopText();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SoundEffect.Stop();
            OnRemoved(EventArgs.Empty);
        }

        private void Remove()
        {
            OnRemoved(EventArgs.Empty);
        }

        public event EventHandler Removed;

        protected virtual void OnRemoved(EventArgs e)
        {
            if (Removed != null)
            {
                Removed(this, e);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SoundEffect.IsPlayList = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SoundEffect.IsPlayList = false;
        }

        private void SamplesStackPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SampleControl sampleControl = e.Source as SampleControl;
            if (sampleControl != null)
            {
                startPosition = e.GetPosition(null);
            }
        }

        private Point startPosition;

        private void SamplesStackPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is SampleControl && e.OriginalSource != ((SampleControl)e.Source).ChannelBG_Copy)
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(null);
                Vector dragVector = startPosition - currentPosition;

                if (Math.Abs(dragVector.Length) > 5)
                {
                        SampleControl sampleControl = e.Source as SampleControl;
                    if (sampleControl != null)
                    {
                        DragData dragData = new DragData();
                        dragData.sampleControl = sampleControl;
                        dragData.soundEffectControl = this;

                        DataObject dataObject = new DataObject("DragData", dragData);

                        try
                        {
                            DragDrop.DoDragDrop(sampleControl, dataObject, DragDropEffects.Move);
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        private void SamplesStackPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
        }

        private void SamplesStackPanel_Drop(object sender, DragEventArgs e)
        {
            SampleControl dropsc = e.Source as SampleControl;
            DragData scdrag = e.Data.GetData("DragData") as DragData;
            if (scdrag != null)
            {
                SampleControl sc = scdrag.sampleControl;
                if (dropsc == null)
                {
                    SamplesStackPanel.Children.Add(sc);
                    SoundEffect.Samples.Add(sc.Sample);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    var ext = System.IO.Path.GetExtension(file);
                    if((new List<string> {".wav",".mp3",".wma",".ogg",".mp1",".mp2",".aiff"}).Contains(ext))
                    {
                        Sample sample = new Sample();
                        SampleControl sc = CreateSampleControl(SoundEffect, sample);
                        SoundEffect.Samples.Add(sample);
                        sample.FileName = file;
                        sc.SampleNameText.Text = System.IO.Path.GetFileName(file);
                    }
                }
            }
        }

        private void SamplesStackPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData("DragData") is DragData scdrag)
            {
                SampleControl sc = scdrag.sampleControl;
                Int32 dropIndex = -1;

                if (e.Source is SampleControl dropsc && dropsc != sc)
                {
                    dropIndex = SamplesStackPanel.Children.IndexOf(dropsc);
                }
                else if (e.Source is StackPanel dropstack && dropstack.Name == "DummyDropPanel" && !SamplesStackPanel.Children.Contains(sc))
                {
                    dropIndex = SamplesStackPanel.Children.Count;
                }

                if (dropIndex >= 0) { 

                    try
                    {
                        scdrag.soundEffectControl.SamplesStackPanel.Children.Remove(sc);
                        scdrag.soundEffectControl.SoundEffect.Samples.Remove(sc.Sample);
                    }
                    catch (Exception ex)
                    {
                        LoopStatus.Text = ex.Message;
                    }

                    SamplesStackPanel.Children.Insert(dropIndex, sc);
                    SoundEffect.Samples.Insert(dropIndex, sc.Sample);
                }
            }
        }







    }

    public class DragData
    {
        public SampleControl sampleControl;
        public SoundEffectControl soundEffectControl;
    }
}