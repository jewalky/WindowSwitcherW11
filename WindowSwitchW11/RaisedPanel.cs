namespace WindowSwitchW11
{
    internal class RaisedPanel : Panel
    {
        public RaisedPanel()
        {
            this.BorderStyle = BorderStyle.None;  // Disable default border
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }
    }
}