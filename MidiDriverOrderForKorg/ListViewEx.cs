using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MidiDriverOrderForKorg
{
    // https://stackoverflow.com/questions/2691726/how-can-i-remove-the-selection-border-on-a-listviewitem
    
    internal class ListViewEx : ListView
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_CHANGEUISTATE = 0x127;
        private const int UIS_SET = 1;
        private const int UISF_HIDEFOCUS = 0x1;
        
        public ListViewEx()
        {
            this.View = View.Details;
            this.FullRowSelect = true;
            this.OverrideDoubleBuffered = true;

            // removes the ugly dotted line around the focused item
            SendMessage(this.Handle, WM_CHANGEUISTATE, MakeLong(UIS_SET, UISF_HIDEFOCUS), 0);
        }

        public bool OverrideDoubleBuffered
        {
            get => base.DoubleBuffered;
            set => base.DoubleBuffered = value;
        }

        private int MakeLong(int wLow, int wHigh)
        {
            int low = (int)IntLoWord(wLow);
            short high = IntLoWord(wHigh);
            int product = 0x10000 * (int)high;
            int mkLong = (int)(low | product);
            return mkLong;
        }

        private short IntLoWord(int word)
        {
            return (short)(word & short.MaxValue);
        }
    }
}
