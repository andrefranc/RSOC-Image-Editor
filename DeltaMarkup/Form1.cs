using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DeltaMarkup
{
    

    public partial class Form1 : Form
    {
        public string timestamp;
        public string description;

        public int originalWidth;
        public int originalHeight;
        private float aspectRatio;
        public Bitmap originalImage;

        private bool isResizing = false;
        private Point lastMousePos;

        private bool isDrawing = false;
        private Point startPoint;
        private Point endPoint;
        //private Rectangle currentRect;
        private Shape.ShapeType shapeType = Shape.ShapeType.Rectangle; // Rectangle by default
        private Color fillColor = Color.Transparent; // Default fill color
        private Color selectedColor = Color.Black; // Default color

        //private List<(Rectangle rect, Color color)> rectangles = new List<(Rectangle, Color)>();

        // List to store shapes
        private List<Shape> shapes = new List<Shape>();
        //private Shape currentShape;

        public Form1()
        {
            InitializeComponent();

            // Set pictureBox2 and label1 as child controls of pictureBox1
            pictureBox2.Parent = pictureBox1;
            label1.Parent = pictureBox1;

            // Adjust their locations relative to pictureBox1
            pictureBox2.Location = new Point(0, 0);
            label1.Location = new Point(pictureBox2.Location.X, pictureBox2.Location.Y + pictureBox2.Height);

            // Set pictureBox1 dimensions to perfectly fit pictureBox2 and label1
            pictureBox1.Size = new Size(pictureBox2.Width, pictureBox2.Height + label1.Height);

            // Subscribe to mouse events for pictureBox2
            pictureBox2.MouseDown += pictureBox2_MouseDown;
            pictureBox2.MouseMove += pictureBox2_MouseMove;
            pictureBox2.MouseUp += pictureBox2_MouseUp;
            pictureBox2.Paint += pictureBox2_Paint;

            originalWidth = pictureBox2.Width;
            originalHeight = pictureBox2.Height;

        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PasteImageFromClipboard();
        }

        private void PasteImageFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    SetImageInPictureBox(image);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif",
                Title = "Select an Image File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var image = Image.FromFile(openFileDialog.FileName);
                if (image != null)
                {
                    SetImageInPictureBox(image);
                }
            }
        }

        private void SetImageInPictureBox(Image image)
        {
            originalImage = new Bitmap(image);

            // Scale the image to fit within pictureBox2 while keeping its original width
            float scaleFactor = (float)originalWidth / image.Width;
            int newHeight = (int)(image.Height * scaleFactor);

            // Create a new bitmap with the scaled dimensions
            Bitmap scaledImage = new Bitmap(originalWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                g.DrawImage(image, 0, 0, originalWidth, newHeight);
            }

            // Set the scaled image to pictureBox2 and adjust its height
            pictureBox2.Image = scaledImage;
            pictureBox2.Height = newHeight;
            pictureBox2.Width = originalWidth;

            // Update the aspect ratio based on the new image
            aspectRatio = (float)pictureBox2.Width / pictureBox2.Height;

            this.Invalidate(); // Trigger a repaint to update the border
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            description = textBox1.Text;

            if (checkBox1.Checked == true) // only include timestamp if checkbox is checked
            {
                label1.Text = timestamp + " – " + description;
            }
            else
            {
                label1.Text = description;
            }
            //this.Invalidate(); // Trigger a repaint to update the border 
        }

        private void maskedTextBox1_TextChanged(object sender, EventArgs e)
        {
            timestamp = maskedTextBox1.Text;

            if (checkBox1.Checked == true) // only include timestamp if checkbox is checked
            {
                label1.Text = timestamp + " – " + description;
                //this.Invalidate(); // Trigger a repaint to update the border
            }
            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = CaptureControl(pictureBox1);
            Clipboard.SetImage(bitmap);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = CaptureControl(pictureBox1);

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveFileDialog.Title = "Save Image As...";

                // Clean the timestamp and description to remove special characters
                
                string cleanedDescription = CleanFileName(description);

                if (timestamp == null)
                {
                    saveFileDialog.FileName = $"{cleanedDescription}";
                }
                else
                {
                    string cleanedTimestamp = CleanFileName(timestamp);
                    saveFileDialog.FileName = $"{cleanedTimestamp} - {cleanedDescription}";
                }
                

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Save the image to the selected path
                    string filePath = saveFileDialog.FileName;
                    ImageFormat format = ImageFormat.Png;

                    switch (Path.GetExtension(filePath).ToLower())
                    {
                        case ".jpg":
                            format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;
                    }

                    bitmap.Save(filePath, format);
                }
            }
        }

        private string CleanFileName(string input)
        {
            // Remove invalid characters from the file name, keeping spaces
            return Regex.Replace(input, "[^a-zA-Z0-9 ]+", "");
        }

        private Bitmap CaptureControl(Control control)
        {
            // Define the border thickness in pixels
            int borderThickness = 2;

            // Create a bitmap with additional space for the border
            Bitmap bitmap = new Bitmap(control.Width + borderThickness * 2, control.Height + borderThickness * 2);

            // Draw the control onto the bitmap
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                control.DrawToBitmap(bitmap, new Rectangle(borderThickness, borderThickness, control.Width, control.Height));

                // Draw the border on the bitmap
                using (Pen pen = new Pen(Color.Black, borderThickness))
                {
                    g.DrawRectangle(pen, new Rectangle(borderThickness - 1, borderThickness - 1, control.Width + 1, control.Height + 1));
                }

                
            }

            return bitmap;
        }

        private void pictureBox2_SizeChanged(object sender, EventArgs e)
        {
            label1.MaximumSize = new Size(pictureBox2.Width, 0);
            label1.MinimumSize = new Size(pictureBox2.Width, 0);
            label1.Location = new Point(pictureBox2.Location.X, pictureBox2.Location.Y + pictureBox2.Height);
            pictureBox1.Size = new Size(pictureBox2.Width, pictureBox2.Height + label1.Height);
            this.Invalidate(); // Trigger a repaint to update the border
        }

        private void label1_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Size = new Size(pictureBox2.Width, pictureBox2.Height + label1.Height);
            this.Invalidate(); // Trigger a repaint to update the border
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            // Check if an image is set in pictureBox2
            if (pictureBox2.Image == null)
            {
                return;
            }

            // Check if the mouse is in the bottom-right corner
            if (e.Button == MouseButtons.Left &&
                e.X >= pictureBox2.Width - 10 &&
                e.Y >= pictureBox2.Height - 10)
            {
                isResizing = true;
                lastMousePos = e.Location;
                pictureBox2.Cursor = Cursors.SizeNWSE;
            }
            else if (e.Button == MouseButtons.Left && (listBox1.SelectedItem != null || listBox2.SelectedItem != null))
            {
                // Start drawing a rectangle if a color is selected
                
                isDrawing = true;
                startPoint = e.Location;
                endPoint = startPoint;
                //currentRect = new Rectangle();
            }
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {

            if (isResizing)
            {
                pictureBox2.Cursor = Cursors.SizeNWSE;

                int dx = e.X - lastMousePos.X;
                int dy = e.Y - lastMousePos.Y;

                // Calculate the new dimensions while maintaining the aspect ratio
                int newWidth, newHeight;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    newWidth = pictureBox2.Width + dx;
                    newHeight = (int)(newWidth / aspectRatio);
                }
                else
                {
                    newHeight = pictureBox2.Height + dy;
                    newWidth = (int)(newHeight * aspectRatio);
                }

                pictureBox2.Size = new Size(newWidth, newHeight);
                pictureBox1.Size = new Size(pictureBox2.Width, pictureBox2.Height + label1.Height);

                lastMousePos = e.Location;
            }
            else if (isDrawing)
            {
                pictureBox2.Cursor = Cursors.Cross;

                // Update the current rectangle dimensions
                //int x = Math.Min(startPoint.X, e.X);
                //int y = Math.Min(startPoint.Y, e.Y);
                //int width = Math.Abs(startPoint.X - e.X);
                //int height = Math.Abs(startPoint.Y - e.Y);

                endPoint = e.Location;
                //currentRect = new Rectangle(x, y, width, height);
                //currentShape = new Shape(Shape.ShapeType.Rectangle, startPoint, endPoint, selectedColor, fillColor);
                pictureBox2.Invalidate(); // Trigger a repaint to update the drawing
            }
            else
            {
                // Check if the mouse is in the bottom-right corner
                if (e.X >= pictureBox2.Width - 10 && e.Y >= pictureBox2.Height - 10)
                {
                    pictureBox2.Cursor = Cursors.SizeNWSE;
                }
                else if (listBox1.SelectedItem != null || listBox2.SelectedItem != null)
                {
                    pictureBox2.Cursor = Cursors.Cross;
                }
                else
                {
                    pictureBox2.Cursor= Cursors.Default;
                }
            }
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                pictureBox2.Cursor = Cursors.Default;

                // Resize the image to fit the new size of pictureBox2
                if (pictureBox2.Image != null)
                {
                    Bitmap resizedImage = new Bitmap(originalImage, pictureBox2.Size);

                    // Dispose of the old image to release resources
                    pictureBox2.Image.Dispose();
                    pictureBox2.Image = resizedImage;
                }

                // Update the shape points to reflect the new size
                UpdateShapePoints();

                // Redraw the border after resizing
                this.Invalidate();
            }
            else if (isDrawing)
            {
                isDrawing = false;

                // Add the current rectangle to the list
                shapes.Add(new Shape(shapeType, startPoint, endPoint, selectedColor, fillColor));

                // Trigger a repaint to update the drawing
                pictureBox2.Invalidate();
            }
        }

        private void UpdateShapePoints()
        {
            // Calculate scaling factors
            float scaleX = (float)pictureBox2.Width / originalWidth;
            float scaleY = (float)pictureBox2.Height / originalHeight;

            // Update the start and end points of each shape
            for (int i = 0; i < shapes.Count; i++)
            {
                Shape shape = shapes[i];

                // Update the start point
                Point newStartPoint = new Point(
                    (int)(shape.StartPoint.X * scaleX),
                    (int)(shape.StartPoint.Y * scaleY)
                );

                // Update the end point
                Point newEndPoint = new Point(
                    (int)(shape.EndPoint.X * scaleX),
                    (int)(shape.EndPoint.Y * scaleY)
                );

                // Replace the shape with the updated points
                shapes[i] = new Shape(shape.Type, newStartPoint, newEndPoint, shape.BorderColor, shape.FillColor);
            }

            // Update the originalWidth and originalHeight to the new size
            originalWidth = pictureBox2.Width;
            originalHeight = pictureBox2.Height;

            // Invalidate the PictureBox to redraw all shapes with updated positions
            pictureBox2.Invalidate();
        }


        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            // Draw the stored shapes
            foreach (var shape in shapes)
            {
                using (Pen borderPen = new Pen(shape.BorderColor, 2))
                using (SolidBrush fillBrush = new SolidBrush(shape.FillColor))
                {
                    Rectangle rect = new Rectangle(
                        Math.Min(shape.StartPoint.X, shape.EndPoint.X),
                        Math.Min(shape.StartPoint.Y, shape.EndPoint.Y),
                        Math.Abs(shape.StartPoint.X - shape.EndPoint.X),
                        Math.Abs(shape.StartPoint.Y - shape.EndPoint.Y));

                    if (shape.Type == Shape.ShapeType.Rectangle)
                    {
                        e.Graphics.FillRectangle(fillBrush, rect);
                        e.Graphics.DrawRectangle(borderPen, rect);
                    }
                    else if (shape.Type == Shape.ShapeType.Ellipse)
                    {
                        e.Graphics.FillEllipse(fillBrush, rect);
                        e.Graphics.DrawEllipse(borderPen, rect);
                    }
                    
                }
            }

            // Draw the current rectangle
            if (isDrawing)
            {
                using (Pen borderPen = new Pen(selectedColor, 2))
                using (SolidBrush fillBrush = new SolidBrush(fillColor))
                {
                    Rectangle rect = new Rectangle(
                        Math.Min(startPoint.X, endPoint.X),
                        Math.Min(startPoint.Y, endPoint.Y),
                        Math.Abs(startPoint.X - endPoint.X),
                        Math.Abs(startPoint.Y - endPoint.Y));

                    if (shapeType == Shape.ShapeType.Rectangle)
                    {
                        e.Graphics.FillRectangle(fillBrush, rect);
                        e.Graphics.DrawRectangle(borderPen, rect);
                    }
                    else if (shapeType == Shape.ShapeType.Ellipse)
                    {
                        e.Graphics.FillEllipse(fillBrush, rect);
                        e.Graphics.DrawEllipse(borderPen, rect);
                    }
                }

                
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Define the border thickness in pixels
            int borderThickness = 2;

            // Draw the border around pictureBox1
            using (Pen pen = new Pen(Color.Black, borderThickness))
            {
                Rectangle borderRect = new Rectangle(pictureBox1.Location.X - 1, pictureBox1.Location.Y - 1, pictureBox1.Width + 2, pictureBox1.Height + 2);
                e.Graphics.DrawRectangle(pen, borderRect);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            // Set the selected color based on the selected item in the listBox1
            if (listBox1.SelectedItem != null)
            {
                listBox2.SelectedIndex = -1; // deselect from censorship options

                fillColor = Color.Transparent;
                shapeType = Shape.ShapeType.Rectangle;

                switch (listBox1.SelectedIndex)
                {
                    case 0: // POI
                        selectedColor = Color.Red;
                        break;
                    case 1: // Tier II SRT
                        selectedColor = Color.Green;
                        break;
                    case 2: // Tier I SRT
                        selectedColor = Color.Yellow;
                        break;
                    case 3: // NES
                        selectedColor = Color.Purple;
                        break;
                    case 4: // PD
                        selectedColor = Color.DeepSkyBlue;
                        break;
                    case 5: // EMS
                        selectedColor = Color.Orange;
                        break;
                    case 6: // tenant
                        selectedColor = Color.DeepPink;
                        break;
                    case 7: // black
                        selectedColor = Color.Black;
                        break;
                    case 8: // white
                        selectedColor = Color.White;
                        break;
                    default:
                        selectedColor = Color.Black; // Default color
                        break;
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                label1.Text = description;
            }
            else
            {
                label1.Text = timestamp + " – " + description;
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                listBox1.SelectedIndex = -1; // deselect from frame options

                fillColor = Color.Black;
                selectedColor = Color.Transparent;

                if (listBox2.SelectedIndex == 0)
                {
                    shapeType = Shape.ShapeType.Ellipse;
                }
                else
                {
                    shapeType = Shape.ShapeType.Rectangle;
                }
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (shapes.Count > 0)
            {
                shapes.RemoveAt(shapes.Count - 1);
                pictureBox2.Invalidate(); // Trigger a repaint to update the drawing
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (shapes.Count > 0)
            {
                shapes.Clear();
                pictureBox2.Invalidate(); // Trigger a repaint to update the drawing
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            maskedTextBox1.Clear(); // clear timestamp
            textBox1.Clear(); // clear description box
            label1.Text = "Enter an event description."; // reset caption
            
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image.Dispose();
                pictureBox2.Image = null; // remove image
            }
            
            pictureBox2.Size = new Size(originalWidth, originalHeight);
            pictureBox1.Size = new Size(pictureBox2.Width, pictureBox2.Height + label1.Height); // reset dimensions

            if (shapes.Count > 0)
            {
                shapes.Clear(); // clear drawings
            }

            listBox1.SelectedIndex = -1;
            listBox2.SelectedIndex = -1; // clear drawing selections



            this.Invalidate();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
     "Tips:" + Environment.NewLine + Environment.NewLine +
     "• Click the reset button to clear all contents from a previous image before starting a new one." + Environment.NewLine + Environment.NewLine +
     "• Click and drag the bottom-right corner of the image to resize it. Any items drawn on the image will also automatically resize." + Environment.NewLine + Environment.NewLine +
     "• Entering a timestamp will automatically add it to the file name when the image is saved, even if it is not included in the caption." + Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine +
     "Developed by Andres Garcia" + Environment.NewLine +
     "Please reach out to andres.garcia@aus.com with any questions and/or suggestions.",
     "RSOC Image Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class Shape
    {
        public enum ShapeType
        {
            Rectangle,
            Ellipse
            // Add more shape types if needed
        }

        public ShapeType Type { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Color BorderColor { get; set; }
        public Color FillColor { get; set; }

        public Shape(ShapeType type, Point startPoint, Point endPoint, Color borderColor, Color fillColor)
        {
            Type = type;
            StartPoint = startPoint;
            EndPoint = endPoint;
            BorderColor = borderColor;
            FillColor = fillColor;
        }
    }
}