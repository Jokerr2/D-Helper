using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class IconHelper
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SETICON = 0x80u;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        /// <summary>
        /// Clone a resource Icon for Form/tray use. Never dispose Properties.Resources.* —
        /// ResourceManager returns a shared instance.
        /// </summary>
        public static Icon CloneIcon(Icon source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // Copy via memory stream so the clone owns an independent HICON
            // (Icon.Clone() can still share state that breaks after other Disposes).
            using var ms = new MemoryStream();
            source.Save(ms);
            ms.Position = 0;
            return new Icon(ms);
        }

        /// <summary>
        /// Assign a fresh independent icon to a form. Does NOT dispose the previous icon —
        /// disposing Form.Icon (or a getter copy) causes ObjectDisposedException on CreateHandle.
        /// </summary>
        public static void ApplyFormIcon(Form form, Icon source)
        {
            if (form == null || source == null) return;
            try
            {
                form.Icon = CloneIcon(source);
                form.ShowIcon = true;
                if (form.IsHandleCreated)
                    PushTaskbarIcons(form, form.Icon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyFormIcon: {ex.Message}");
            }
        }

        public static void PushTaskbarIcons(Form form, Icon? icon)
        {
            if (form == null || icon == null || !form.IsHandleCreated) return;
            try
            {
                SendMessage(form.Handle, WM_SETICON, ICON_BIG, icon.Handle);
                SendMessage(form.Handle, WM_SETICON, ICON_SMALL, icon.Handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pushing taskbar icon: {ex.Message}");
            }
        }

        public static void SetIcon(Form form, Bitmap icon)
        {
            try
            {
                var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null)
                    SendMessage(form.Handle, WM_SETICON, ICON_BIG, exeIcon.Handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting icon {ex.Message}");
            }
        }

    }
}
