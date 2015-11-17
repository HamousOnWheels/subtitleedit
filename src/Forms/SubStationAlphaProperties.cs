﻿using Nikse.SubtitleEdit.Core;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class SubStationAlphaProperties : PositionAndSizeForm
    {
        private readonly Subtitle _subtitle;
        private readonly bool _isSubStationAlpha;
        private readonly string _videoFileName;

        public SubStationAlphaProperties(Subtitle subtitle, SubtitleFormat format, string videoFileName, string subtitleFileName)
        {
            InitializeComponent();
            _subtitle = subtitle;
            _isSubStationAlpha = format.Name == SubStationAlpha.NameOfFormat;
            _videoFileName = videoFileName;

            var l = Configuration.Settings.Language.SubStationAlphaProperties;
            if (_isSubStationAlpha)
            {
                Text = l.TitleSubstationAlpha;
                labelWrapStyle.Visible = false;
                comboBoxWrapStyle.Visible = false;
                checkBoxScaleBorderAndShadow.Visible = false;
                Height = Height - (comboBoxWrapStyle.Height + checkBoxScaleBorderAndShadow.Height + 8);
            }
            else
            {
                Text = l.Title;
            }

            comboBoxWrapStyle.SelectedIndex = 2;
            comboBoxCollision.SelectedIndex = 0;

            string header = subtitle.Header;
            if (subtitle.Header == null)
            {
                var ssa = new SubStationAlpha();
                var sub = new Subtitle();
                var lines = new List<string>();
                foreach (string line in subtitle.ToText(ssa).SplitToLines())
                    lines.Add(line);
                string title = "Untitled";
                if (!string.IsNullOrEmpty(subtitleFileName))
                    title = Path.GetFileNameWithoutExtension(subtitleFileName);
                else if (!string.IsNullOrEmpty(videoFileName))
                    title = Path.GetFileNameWithoutExtension(videoFileName);
                ssa.LoadSubtitle(sub, lines, title);
                header = sub.Header;
            }

            if (header != null)
            {
                foreach (string line in header.SplitToLines())
                {
                    string s = line.ToLowerInvariant().Trim();
                    if (s.StartsWith("title:"))
                    {
                        textBoxTitle.Text = s.Remove(0, 6).Trim();
                    }
                    else if (s.StartsWith("original script:"))
                    {
                        textBoxOriginalScript.Text = s.Remove(0, 16).Trim();
                    }
                    else if (s.StartsWith("original translation:"))
                    {
                        textBoxTranslation.Text = s.Remove(0, 21).Trim();
                    }
                    else if (s.StartsWith("original editing:"))
                    {
                        textBoxEditing.Text = s.Remove(0, 17).Trim();
                    }
                    else if (s.StartsWith("original timing:"))
                    {
                        textBoxTiming.Text = s.Remove(0, 16).Trim();
                    }
                    else if (s.StartsWith("synch point:"))
                    {
                        textBoxSyncPoint.Text = s.Remove(0, 12).Trim();
                    }
                    else if (s.StartsWith("script updated by:"))
                    {
                        textBoxUpdatedBy.Text = s.Remove(0, 18).Trim();
                    }
                    else if (s.StartsWith("update details:"))
                    {
                        textBoxUpdateDetails.Text = s.Remove(0, 15).Trim();
                    }
                    else if (s.StartsWith("collisions:"))
                    {
                        if (s.Remove(0, 11).Trim() == "reverse")
                            comboBoxCollision.SelectedIndex = 1;
                    }
                    else if (s.StartsWith("playresx:"))
                    {
                        int number;
                        if (int.TryParse(s.Remove(0, 9).Trim(), out number))
                            numericUpDownVideoWidth.Value = number;
                    }
                    else if (s.StartsWith("playresy:"))
                    {
                        int number;
                        if (int.TryParse(s.Remove(0, 9).Trim(), out number))
                            numericUpDownVideoHeight.Value = number;
                    }
                    else if (s.StartsWith("scaledborderandshadow:"))
                    {
                        checkBoxScaleBorderAndShadow.Checked = s.Remove(0, 22).Trim().Equals("yes");
                    }
                }
            }

            groupBoxScript.Text = l.Script;
            labelTitle.Text = l.ScriptTitle;
            labelOriginalScript.Text = l.OriginalScript;
            labelTranslation.Text = l.Translation;
            labelEditing.Text = l.Editing;
            labelTiming.Text = l.Timing;
            labelSyncPoint.Text = l.SyncPoint;
            labelUpdatedBy.Text = l.UpdatedBy;
            labelUpdateDetails.Text = l.UpdateDetails;
            groupBoxResolution.Text = l.Resolution;
            labelVideoResolution.Text = l.VideoResolution;
            groupBoxOptions.Text = l.Options;
            labelCollision.Text = l.Collision;
            labelWrapStyle.Text = l.WrapStyle;
            checkBoxScaleBorderAndShadow.Text = l.ScaleBorderAndShadow;

            buttonOK.Text = Configuration.Settings.Language.General.Ok;
            buttonCancel.Text = Configuration.Settings.Language.General.Cancel;

            UiUtil.FixLargeFonts(this, buttonCancel);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private string GetDefaultHeader()
        {
            SubtitleFormat format;
            if (_isSubStationAlpha)
                format = new SubStationAlpha();
            else
                format = new AdvancedSubStationAlpha();
            var sub = new Subtitle();
            string text = format.ToText(sub, string.Empty);
            var lines = new List<string>();
            foreach (string line in text.SplitToLines())
                lines.Add(line);
            format.LoadSubtitle(sub, lines, string.Empty);
            return sub.Header.Trim();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (_subtitle.Header == null || !_subtitle.Header.Contains("[script info]", StringComparison.OrdinalIgnoreCase))
                _subtitle.Header = GetDefaultHeader();

            string title = textBoxTitle.Text;
            if (string.IsNullOrWhiteSpace(title))
                title = "untitled";
            UpdateTag("Title", title, false);
            UpdateTag("Original Script", textBoxOriginalScript.Text, string.IsNullOrWhiteSpace(textBoxOriginalScript.Text));
            UpdateTag("Original Translation", textBoxTranslation.Text, string.IsNullOrWhiteSpace(textBoxTranslation.Text));
            UpdateTag("Original Editing", textBoxEditing.Text, string.IsNullOrWhiteSpace(textBoxEditing.Text));
            UpdateTag("Original Timing", textBoxTiming.Text, string.IsNullOrWhiteSpace(textBoxTiming.Text));
            UpdateTag("Synch Point", textBoxSyncPoint.Text, string.IsNullOrWhiteSpace(textBoxSyncPoint.Text));
            UpdateTag("Script Updated By", textBoxUpdatedBy.Text, string.IsNullOrWhiteSpace(textBoxUpdatedBy.Text));
            UpdateTag("Update Details", textBoxUpdateDetails.Text, string.IsNullOrWhiteSpace(textBoxUpdateDetails.Text));
            UpdateTag("PlayResX", numericUpDownVideoWidth.Value.ToString(), numericUpDownVideoWidth.Value == 0);
            UpdateTag("PlayResY", numericUpDownVideoHeight.Value.ToString(), numericUpDownVideoHeight.Value == 0);
            if (comboBoxCollision.SelectedIndex == 0)
                UpdateTag("collisions", "Normal", false); // normal
            else
                UpdateTag("collisions", "Reverse", false); // reverse

            if (!_isSubStationAlpha)
            {
                UpdateTag("wrapstyle", comboBoxWrapStyle.SelectedIndex.ToString(), false);
                if (checkBoxScaleBorderAndShadow.Checked)
                    UpdateTag("ScaledBorderAndShadow", "yes", false);
                else
                    UpdateTag("ScaledBorderAndShadow", "no", false);
            }

            DialogResult = DialogResult.OK;
        }

        private void UpdateTag(string tag, string text, bool remove)
        {
            bool scriptInfoOn = false;
            var sb = new StringBuilder();
            bool found = false;
            foreach (string line in _subtitle.Header.SplitToLines())
            {
                if (line.StartsWith("[script info]", StringComparison.OrdinalIgnoreCase))
                {
                    scriptInfoOn = true;
                }
                else if (line.StartsWith('['))
                {
                    if (!found && scriptInfoOn && !remove)
                        sb.AppendLine(tag + ": " + text);
                    sb = new StringBuilder(sb.ToString().TrimEnd());
                    sb.AppendLine();
                    sb.AppendLine();
                    scriptInfoOn = false;
                }

                string s = line.ToLower();
                if (s.StartsWith(tag.ToLower() + ":"))
                {
                    if (!remove)
                        sb.AppendLine(line.Substring(0, tag.Length) + ": " + text);
                    found = true;
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            _subtitle.Header = sb.ToString().Trim();
        }

        private void SubStationAlphaProperties_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
        }

        private void buttonGetResolutionFromVideo_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = Configuration.Settings.Language.General.OpenVideoFileTitle;
            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = Utilities.GetVideoFileFilter(false);
            openFileDialog1.FileName = string.Empty;
            if (string.IsNullOrEmpty(openFileDialog1.InitialDirectory) && !string.IsNullOrEmpty(_videoFileName))
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(_videoFileName);
                openFileDialog1.FileName = Path.GetFileName(_videoFileName);
            }

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                VideoInfo info = UiUtil.GetVideoInfo(openFileDialog1.FileName);
                if (info != null && info.Success)
                {
                    numericUpDownVideoWidth.Value = info.Width;
                    numericUpDownVideoHeight.Value = info.Height;
                }
            }
        }

    }
}