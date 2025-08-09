namespace WindowSwitchW11
{
    internal class WindowLabel : Label
    {
        private bool _selected = false;
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int xy = 0;
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;
            if (_selected)
            {
                Pen pen = new Pen(Color.Black);
                for (int i = 0; i < 2; i++)
                    e.Graphics.DrawRectangle(pen, xy + i, xy + i, width - (i << 1) - 1, height - (i << 1) - 1);
            }
        }
    }
}
