using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MidiDriverOrderForKorg
{
    public partial class Form1 : Form
    {
        private const string WdmaudDriver = "wdmaud.drv";
        private const string Wdmaud2Driver = "wdmaud2.drv";
        private const string Midi2Msg = "MIDI 2.0 services are installed.\nWe DO NOT recommend using this tool.\nWindows MIDI 2.0 should fix the Korg driver issues.";
       
        private bool _hasWdmaud2;

        public Form1()
        {
            InitializeComponent();
            Setup();
        }

        private void Setup()
        {
            this.Text = $@"{Application.ProductName} {Application.ProductVersion}";
            _listView.Columns.AddRange(new[]
            {
                new ColumnHeader() { Text = @"No", Width = 30 },
                new ColumnHeader() { Text = @"Name", Width = 300 },
                new ColumnHeader() { Text = @"Alias", Width = 45 },
                new ColumnHeader() { Text = @"Korg", Width = 35, TextAlign = HorizontalAlignment.Center},
                new ColumnHeader() { Text = @"Registry Path", Width = 750 },
            });
        }

        private void AddListViewEntry(RegistryEntry entry)
        {
            var lvItem = _listView.Items.Add("");
            lvItem.SubItems.Add(entry.DeviceName);
            lvItem.SubItems.Add(entry.Alias);
            lvItem.SubItems.Add(entry.IsKorg ? "Y" : "");
            lvItem.SubItems.Add(entry.FullKey);
            lvItem.Tag = entry;
            UpdateListItemDetails(lvItem);
        }

        private void DoRefresh()
        {
            var entries = Utils.GetRegistryEntries();
            if (entries == null)
            {
                MessageBox.Show(@"Failed to get midi driver entries or no drivers are installed");
                return;
            }

            _hasWdmaud2 = Utils.AreMidiServicesPresent();
            if (_hasWdmaud2)
            {
                foreach (var entry in entries)
                {
                    if (entry.Alias == "midi" || entry.Alias == "midi1")
                        entry.Alias = "";
                }
            }

            string errorMsg = null;
            try
            {
                entries = Utils.SortEntries(entries);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            _listView.Items.Clear();
            var hasKorg = false;
            _listView.BeginUpdate();
            try
            {
                if (_hasWdmaud2)
                {
                    AddListViewEntry(new RegistryEntry("midi", "wdmaud - Midi 2.0 driver (locked)", WdmaudDriver, "", false, true));
                    AddListViewEntry(new RegistryEntry("midi1", "wdmaud2 - Midi 2.0 driver (locked)", Wdmaud2Driver, "", false, true));
                }
                foreach (var entry in entries)
                {
                    AddListViewEntry(entry);
                    hasKorg |= entry.IsKorg;
                }
            }
            finally
            {
                _listView.EndUpdate();
            }
            InvalidateControlsForSelection();
            _tsMoveKorg.Enabled = _miMoveKorgToTop.Enabled = hasKorg;

            if (errorMsg!=null)
            {
                MessageBox.Show($"Current ordering of midi drivers is invalid.\nError: {errorMsg}\n\nPlease fix the ordering", @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateListItemDetails(ListViewItem lv)
        {
            var idx = lv.Index;
            if (_hasWdmaud2 && (idx == 0 || idx == 1))
                lv.BackColor = Color.Salmon;
            else
                lv.BackColor = (idx <= Utils.MidiAliasMaxIdx) ? Color.AntiqueWhite : SystemColors.Window;
            lv.Text = (idx + 1).ToString();
        }

        private ListViewItem GetSelectedListItem()
        {
            var selItems = _listView.SelectedItems;
            if (selItems.Count != 1)
                return null;
            var sel = selItems[0];
            return sel;
        }

        private void MoveItem(bool moveUp)
        {
            var sel = GetSelectedListItem();
            if (sel == null)
                return;

            var idx = sel.Index;

            if (_hasWdmaud2)
            {
                if (idx == 0 || idx == 1)
                    return;
                if (moveUp && idx == 2)
                    return;
            }

            var offset = moveUp ? -1 : 1;
            if ((moveUp && idx == 0) || (!moveUp && idx == _listView.Items.Count - 1))
                return;
            _listView.BeginUpdate();
            var oldLv = _listView.Items[idx+offset];
            try
            {
                _listView.Items.RemoveAt(idx);
                _listView.Items.Insert(idx + offset, sel);
                UpdateListItemDetails(oldLv);
                UpdateListItemDetails(sel);
            }
            finally
            {
                _listView.EndUpdate();
            }
            sel.Selected = true;
            sel.EnsureVisible();
        }

        private void _listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            InvalidateControlsForSelection();
        }

        private void InvalidateControlsForSelection()
        {
            var isSelected = _listView.SelectedItems.Count != 0;
            var selIdx = isSelected ? _listView.SelectedItems[0].Index : -1;

            bool canMoveUp = isSelected && selIdx > 0;
            bool canMoveDown = isSelected && selIdx < _listView.Items.Count - 1;

            if (_hasWdmaud2)
            {
                if (selIdx == 0 || selIdx == 1)
                {
                    canMoveUp = false;
                    canMoveDown = false;
                }
                if (selIdx == 2)
                {
                    canMoveUp = false;
                }
            }

            _tsUp.Enabled = _miMoveUp.Enabled = canMoveUp;
            _tsDown.Enabled = _miMoveDown.Enabled = canMoveDown;
            _lvmiCopyRegKeyToClipboard.Enabled = _miCopyRegKeyToClipboard.Enabled = isSelected;
        }

        private void DoSave()
        {
            if (_hasWdmaud2)
            {
                var res = MessageBox.Show($"{Midi2Msg}\n\nDo you want to proceed?", 
                    "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes)
                    return;
            }

            
            var idx = Utils.MidiAliasLowIdx;
            try
            {
                var entriesToWrite = new List<RegistryEntry>(_listView.Items.Count);
                foreach (ListViewItem item in _listView.Items)
                {
                    var entry = (RegistryEntry)item.Tag;
                    entriesToWrite.Add(entry);
                    try
                    {
                        Utils.WriteEntry(entry, idx++);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error: {e.Message}\n\nAborting...", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                try
                {
                    Utils.UpdateDrivers32Aliases(entriesToWrite);
                }
                catch (Exception e)
                {
                    MessageBox.Show($@"Failed to update Drivers32 aliases. Error={e.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                DoRefresh();
            }
            MessageBox.Show(@"Device order saved", @"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DoMoveKorgToTop()
        {
            var list = new LinkedList<ListViewItem>();
            foreach (ListViewItem item in _listView.Items)
            {
                var entry = (RegistryEntry)item.Tag;
                if (entry.IsKorg)
                    list.AddLast(item);
            }
            // Should never happen
            if (list.Count == 0)
                return;

            var nextIdx = 0;
            if (_hasWdmaud2)
                nextIdx = 2;

            _listView.BeginUpdate();
            try
            {
                foreach (var item in list)
                {
                    if (item.Index < nextIdx && _hasWdmaud2)
                        continue; // Special drivers at 0, 1 should not be moved if they were Korg (unlikely but safe)

                    if (item.Index != nextIdx)
                    {
                        _listView.Items.RemoveAt(item.Index);
                        _listView.Items.Insert(nextIdx, item);
                    }
                    nextIdx++;
                }
                foreach (ListViewItem lv in _listView.Items)
                {
                    UpdateListItemDetails(lv);
                }
            }
            finally
            {
                _listView.EndUpdate();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            DoRefresh();
            if (_hasWdmaud2)
            {
                MessageBox.Show($"{Midi2Msg}\n\nmidi & midi1 aliases cannot be moved", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnRefresh_Click(object sender, EventArgs e)
        {
            DoRefresh();
        }

        private void OnSave_Click(object sender, EventArgs e)
        {
            DoSave();
        }

        private void OnMoveUp_Click(object sender, EventArgs e)
        {
            MoveItem(true);
        }

        private void OnMoveDown_Click(object sender, EventArgs e)
        {
            MoveItem(false);
        }

        private void OnMoveKorg_Click(object sender, EventArgs e)
        {
            DoMoveKorgToTop();
        }

        private void _miQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void _miRefresh_Click(object sender, EventArgs e)
        {
            DoRefresh();
        }

        private void _miSave_Click(object sender, EventArgs e)
        {
            DoSave();
        }

        private void _miMoveUp_Click(object sender, EventArgs e)
        {
            MoveItem(true);
        }

        private void _miMoveDown_Click(object sender, EventArgs e)
        {
            MoveItem(false);
        }

        private void _miMoveKorgToTop_Click(object sender, EventArgs e)
        {
            DoMoveKorgToTop();
        }

        private void DoCopyRegKeyToClipboard()
        {
            var sel = GetSelectedListItem();
            if (sel == null)
                return;
            var entry = (RegistryEntry)sel.Tag;
            Clipboard.SetText(entry.FullKey);
        }


        private void CopyRegKeyToClipboard_Click(object sender, EventArgs e)
        {
            DoCopyRegKeyToClipboard();
        }

        private void _miCopyRegKeyToClipboard_Click(object sender, EventArgs e)
        {
            DoCopyRegKeyToClipboard();
        }

        private void _miAbout_Click(object sender, EventArgs e)
        {
            new AboutForm().ShowDialog(this);
        }
    }
    
}