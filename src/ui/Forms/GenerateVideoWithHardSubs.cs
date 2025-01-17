﻿using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class GenerateVideoWithHardSubs : Form
    {
        private bool _abort;
        private readonly Subtitle _assaSubtitle;
        private readonly VideoInfo _videoInfo;
        private readonly string _inputVideoFileName;
        private static readonly Regex FrameFinderRegex = new Regex(@"[Ff]rame=\s*\d+", RegexOptions.Compiled);
        private long _processedFrames;
        private long _startTicks;
        private long _totalFrames;
        public string VideoFileName { get; private set; }

        public GenerateVideoWithHardSubs(Subtitle assaSubtitle, string inputVideoFileName, VideoInfo videoInfo, int? fontSize)
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);

            _videoInfo = videoInfo;
            Text = LanguageSettings.Current.GenerateVideoWithBurnedInSubs.Title;
            labelInfo.Text = LanguageSettings.Current.GenerateVideoWithBurnedInSubs.Info;
            _assaSubtitle = assaSubtitle;
            _inputVideoFileName = inputVideoFileName;
            buttonOK.Text = LanguageSettings.Current.Watermark.Generate;
            labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait;
            labelFontSize.Text = LanguageSettings.Current.ExportPngXml.FontSize;
            buttonCancel.Text = LanguageSettings.Current.General.Cancel;
            progressBar1.Visible = false;
            labelPleaseWait.Visible = false;
            labelProgress.Text = string.Empty;

            numericUpDownWidth.Value = _videoInfo.Width;
            numericUpDownHeight.Value = _videoInfo.Height;
            if (fontSize.HasValue)
            {
                numericUpDownFontSize.Value = fontSize.Value;

                var left = Math.Max(labelResolution.Left + labelResolution.Width, labelFontSize.Left + labelFontSize.Width) + 5;
                numericUpDownFontSize.Left = left;
                numericUpDownWidth.Left = left;
                labelX.Left = numericUpDownWidth.Left + numericUpDownWidth.Width + 3;
                numericUpDownHeight.Left = labelX.Left + labelX.Width + 3;
            }
            else
            {
                groupBoxSettings.Visible = false;
                Height -= groupBoxSettings.Height - 20;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _abort = true;
            if (buttonOK.Enabled)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrWhiteSpace(outLine.Data))
            {
                return;
            }

            var match = FrameFinderRegex.Match(outLine.Data);
            if (match.Success)
            {
                var arr = match.Value.Split('=');
                if (arr.Length > 0 && long.TryParse(arr[1], out var f))
                {
                    _processedFrames = f;
                }
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            buttonOK.Enabled = false;
            numericUpDownFontSize.Enabled = false;
            using (var saveDialog = new SaveFileDialog { FileName = string.Empty, Filter = "MP4|*.mp4|Matroska|*.mkv|WebM|*.webm" })
            {
                if (saveDialog.ShowDialog(this) != DialogResult.OK)
                {
                    buttonOK.Enabled = true;
                    numericUpDownFontSize.Enabled = true;
                    return;
                }

                VideoFileName = saveDialog.FileName;
            }

            if (File.Exists(VideoFileName))
            {
                File.Delete(VideoFileName);
            }

            if (numericUpDownFontSize.Visible)
            {
                var fontSize = (int)numericUpDownFontSize.Value;
                var style = AdvancedSubStationAlpha.GetSsaStyle("Default", _assaSubtitle.Header);
                style.FontSize = fontSize;
                var styleLine = style.ToRawAss();
                _assaSubtitle.Header = AdvancedSubStationAlpha.AddTagToHeader("Style", styleLine, "[V4+ Styles]", _assaSubtitle.Header);
            }

            if (Configuration.Settings.General.RightToLeftMode && LanguageAutoDetect.CouldBeRightToLeftLanguage(_assaSubtitle))
            {
                for (var index = 0; index < _assaSubtitle.Paragraphs.Count; index++)
                {
                    var paragraph = _assaSubtitle.Paragraphs[index];
                    if (LanguageAutoDetect.ContainsRightToLeftLetter(paragraph.Text))
                    {
                        paragraph.Text = Utilities.FixRtlViaUnicodeChars(paragraph.Text);
                    }
                }
            }

            var format = new AdvancedSubStationAlpha();
            var assaTempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ass");
            File.WriteAllText(assaTempFileName, format.ToText(_assaSubtitle, null));

            groupBoxSettings.Enabled = false;
            progressBar1.Maximum = (int)_videoInfo.TotalFrames;
            progressBar1.Visible = true;
            labelPleaseWait.Visible = true;
            var process = VideoPreviewGenerator.GenerateHardcodedVideoFile(
                _inputVideoFileName,
                assaTempFileName,
                VideoFileName,
                (int)numericUpDownWidth.Value,
                (int)numericUpDownHeight.Value,
                OutputHandler);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _totalFrames = (long)_videoInfo.TotalFrames;
            _startTicks = DateTime.UtcNow.Ticks;
            timer1.Start();

            while (!process.HasExited)
            {
                System.Threading.Thread.Sleep(100);
                Application.DoEvents();
                if (_abort)
                {
                    process.Kill();
                }

                progressBar1.Value = (int)_processedFrames;
            }

            progressBar1.Visible = false;
            labelPleaseWait.Visible = false;
            timer1.Stop();
            labelProgress.Text = string.Empty;
            groupBoxSettings.Enabled = true;

            try
            {
                File.Delete(assaTempFileName);
            }
            catch
            {
                // ignore
            }

            DialogResult = _abort ? DialogResult.Cancel : DialogResult.OK;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_processedFrames <= 0 || _videoInfo.TotalFrames <= 0)
            {
                return;
            }

            var durationMs = (DateTime.UtcNow.Ticks - _startTicks) / 10_000;
            var msPerFrame = (float)durationMs / _processedFrames;
            var estimatedTotalMs = msPerFrame * _totalFrames;
            var estimatedLeft = ToProgressTime(estimatedTotalMs - durationMs);
            labelProgress.Text = estimatedLeft;
        }

        private string ToProgressTime(float estimatedTotalMs)
        {
            var timeCode = new TimeCode(estimatedTotalMs);
            if (timeCode.TotalSeconds < 60)
            {
                return string.Format(LanguageSettings.Current.GenerateVideoWithBurnedInSubs.TimeRemainingSeconds, (int)Math.Round(timeCode.TotalSeconds));
            }

            return string.Format(LanguageSettings.Current.GenerateVideoWithBurnedInSubs.TimeRemainingMinutesAndSeconds, timeCode.Minutes + timeCode.Hours * 60, timeCode.Seconds);
        }

        private void numericUpDownWidth_ValueChanged(object sender, EventArgs e)
        {
            var v = (int)numericUpDownWidth.Value;
            if (v % 2 == 1)
            {
                numericUpDownWidth.Value++;
            }
        }

        private void numericUpDownHeight_ValueChanged(object sender, EventArgs e)
        {
            var v = (int)numericUpDownHeight.Value;
            if (v % 2 == 1)
            {
                numericUpDownHeight.Value++;
            }
        }
    }
}
