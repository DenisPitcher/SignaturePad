//
// SignaturePad.cs: User Control subclass for Windows Phone to allow users to draw their signature on 
//				   the device to be captured as an image or vector.
//
// Author:
//   Timothy Risi (timothy.risi@gmail.com)
//
// Copyright (C) 2012 Timothy Risi
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Xamarin.Controls
{
	public partial class SignaturePad : UserControl
	{
        Color strokeColor;
        public Color StrokeColor
        {
            get { return strokeColor; }
            set
            {
                strokeColor = value;
            }
        }

        
        public SignaturePad ()
		{
			InitializeComponent ();
			Initialize ();
		}

        const int minPenSize = 2;
        const int penSizeIncrement = 2;
        int penSize;

        void Initialize ()
		{
            this.InitializeComponent();

            penSize = minPenSize + penSizeIncrement * 1;

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Colors.Black;
            drawingAttributes.Size = new Size(penSize, penSize);
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;

            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
        }

		//Delete the current signature
		public void Clear ()
		{
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        public bool IsBlank
        {
            get
            {
                return inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0;
            }
        }

        Color backgroundColor;
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                LayoutRoot.Background = new SolidColorBrush(value);
            }
        }

        public TextBlock Caption
        {
            get { return captionLabel; }
        }

        public string CaptionText
        {
            get { return captionLabel.Text; }
            set { captionLabel.Text = value; }
        }

        public TextBlock ClearLabel
        {
            get { return btnClear; }
        }

        public string ClearLabelText
        {
            get { return btnClear.Text; }
            set { btnClear.Text = value; }
        }

        public TextBlock SignaturePrompt
        {
            get { return textBlock1; }
        }

        public string SignaturePromptText
        {
            get { return textBlock1.Text; }
            set { textBlock1.Text = value; }
        }

        public Border SignatureLine
        {
            get { return border1; }
        }

        public Brush SignatureLineBrush
        {
            get { return border1.Background; }
            set { border1.Background = value; }
        }

        float lineWidth;
        public float StrokeWidth
        {
            get { return lineWidth; }
            set
            {
                lineWidth = value;
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.Size = new Windows.Foundation.Size(lineWidth, lineWidth);
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
                if (!IsBlank)
                    image.Source = GetImage(false);
            }
        }

        //Create an array containing all of the points used to draw the signature.  Uses (0, 0)
        //to indicate a new line.
        public Point[] Points
        {
            get
            {
                IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                var inkPoints = strokes.SelectMany(x => x.GetInkPoints()).ToList();
                return inkPoints.Select(x => x.Position).ToList().ToArray();
            }
        }

        public void LoadPoints(Point[] loadedPoints)
        {
            if (loadedPoints == null || loadedPoints.Count() == 0)
                return;

            // TODO: loading points is not supported at the moment
            return;
        }

        private void btnClear_Click (object sender, RoutedEventArgs e)
		{
			Clear ();
		}

        float getScaleFromSize(Size size, Size original)
        {
            double scaleX = size.Width / original.Width;
            double scaleY = size.Height / original.Height;

            return (float)Math.Min(scaleX, scaleY);
        }

        Size getSizeFromScale(float scale, Size original)
        {
            double width = original.Width * scale;
            double height = original.Height * scale;

            return new Size(width, height);
        }

        #region GetImage
        //Create a WriteableBitmap of the currently drawn signature with default colors.
        public WriteableBitmap GetImage (bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, RenderSize, 1, shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (Size size, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, size, getScaleFromSize (size, RenderSize), shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (float scale, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, getSizeFromScale (scale, RenderSize), scale, shouldCrop, keepAspectRatio);
		}

		//Create a WriteableBitmap of the currently drawn signature with the specified Stroke color.
		public WriteableBitmap GetImage (Color strokeColor, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, RenderSize, 1, shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (Color strokeColor, Size size, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, size, getScaleFromSize (size, RenderSize), shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (Color strokeColor, float scale, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, Colors.Transparent, getSizeFromScale (scale, RenderSize), scale, shouldCrop, keepAspectRatio);
		}

		//Create a WriteableBitmap of the currently drawn signature with the specified Stroke and Fill colors.
		public WriteableBitmap GetImage (Color strokeColor, Color fillColor, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, fillColor, RenderSize, 1, shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (Color strokeColor, Color fillColor, Size size, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, fillColor, size, getScaleFromSize (size, RenderSize), shouldCrop, keepAspectRatio);
		}

		public WriteableBitmap GetImage (Color strokeColor, Color fillColor, float scale, bool shouldCrop = true, bool keepAspectRatio = true)
		{
			return GetImage (strokeColor, fillColor, getSizeFromScale (scale, RenderSize), scale, shouldCrop, keepAspectRatio);
		}

		WriteableBitmap GetImage (Color strokeColor, Color fillColor, Size size, float scale, bool shouldCrop = true, bool keepAspectRatio = true)
		{
            if (size.Width == 0 || size.Height == 0 || scale <= 0 || strokeColor == null || fillColor == null)
				return null;

            WriteableBitmap bitmap = OnSaveAsync(size).Result;
			return bitmap;
		}
		#endregion

        async Task<WriteableBitmap> OnSaveAsync(Size size)
        {
            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);

            WriteableBitmap bitmap = new WriteableBitmap(Convert.ToInt32(size.Width), Convert.ToInt32(size.Height));
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
	}
}
