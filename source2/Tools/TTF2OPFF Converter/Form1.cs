﻿//#define GZipCompression
//#define DeflateCompression

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.IO.Compression;

namespace TTF2OPFF_Converter
{
    public partial class Form1 : Form
    {
        private string OutputFileName;

        public Form1()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFileName = saveFileDialog1.FileName;
                textBox1.Text = OutputFileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (FontFamily f in FontFamily.Families)
            {
                FontComboBox.Items.Add(f.Name);
            }
            FontComboBox.SelectedIndex = 0;
        }

        private void ConvertButton_Click(object sender, EventArgs e)
        {
            if (OutputFileName == null || OutputFileName == "")
            {
                MessageBox.Show("Invalid File Name!");
            }
            else
            {
#if DeflateCompression || GZipCompression
                FileStream str;
#else
                FileStream strm;
#endif
                if (!File.Exists(OutputFileName))
                {
#if DeflateCompression || GZipCompression
                    str = File.Create(OutputFileName);
#else
                    strm = File.Create(OutputFileName);
#endif
                }
                else
                {
                    if (MessageBox.Show("A file at '" + OutputFileName + "' already exists! Would you like to overwrite it?", "File Already Exists", MessageBoxButtons.YesNoCancel) == System.Windows.Forms.DialogResult.Yes)
                    {
#if DeflateCompression || GZipCompression
                        str = new FileStream(OutputFileName, FileMode.Truncate);
#else
                        strm = new FileStream(OutputFileName, FileMode.Truncate);
#endif
                    }
                    else
                    {
                        return;
                    }
                }
#if DeflateCompression
                DeflateStream strm = new DeflateStream(str, CompressionMode.Compress);
#elif GZipCompression
                GZipStream strm = new GZipStream(str, CompressionMode.Compress);
#endif

                strm.WriteByte(0);
                strm.WriteByte(0);
                strm.WriteByte(0);
                strm.WriteByte(0);
                strm.WriteByte(0); // Write the 8 empty bytes of the header
                strm.WriteByte(0);
                strm.WriteByte(0);
                strm.WriteByte(0);
                
                
                string FontName = (string)FontComboBox.SelectedItem;

                byte[] buffer = ASCIIEncoding.ASCII.GetBytes(FontName);
                int i = 256 - buffer.Length;
                if (i < 0)
                {
                    throw new Exception("Font Name is to long!");
                }
                else
                {
                    strm.Write(buffer, 0, buffer.Length);
                    for (int i2 = 0; i2 < i; i2++)
                    {
                        strm.WriteByte(0); // Fill the rest of the 256 bytes.
                    }
                }

                System.Windows.Media.Typeface t = new System.Windows.Media.Typeface(FontName);
                System.Windows.Media.GlyphTypeface glyph;
                if (t.TryGetGlyphTypeface(out glyph))
                {
                    t = null;
                    IDictionary<int, ushort> charKeyMap = (IDictionary<int, ushort>)glyph.CharacterToGlyphMap;
                    glyph = null;
                    SortedList<int, int> chars = new SortedList<int, int>();
                    foreach (KeyValuePair<int, ushort> c in charKeyMap)
                    {
                        chars.Add(c.Key, c.Key);
                    }
                    charKeyMap = null;
                    Font f = new Font(FontName, 30, GraphicsUnit.Pixel);

                    UInt64 charsToWrite = (ulong)chars.Keys.Count;
                    buffer = BitConverter.GetBytes(charsToWrite);
                    strm.Write(buffer, 0, buffer.Length); // Write the number of chars to read.
                    
                    int prevChar = 0;
                    foreach (KeyValuePair<int, int> ch in chars)
                    {
                        Bitmap Backend = new Bitmap(32, 32);
                        Graphics g = Graphics.FromImage(Backend);
                        g.Clear(Color.White);
                        g.DrawString(new String(new char[] { (char)ch.Key }), f, new SolidBrush(Color.Black), 0, 0);
                        g.Flush(System.Drawing.Drawing2D.FlushIntention.Flush);
                        if (prevChar + 1 == ch.Key)
                        {
                            strm.WriteByte(255); // write that it's incremented from the previous char.
                        }
                        else
                        {
                            strm.WriteByte(0); // write that it's not incremented from the previous char.
                            buffer = BitConverter.GetBytes(ch.Key);
                            strm.Write(buffer, 0, buffer.Length); // Write the char number.
                        }
                        pictureBox1.Image = Image.FromHbitmap(Backend.GetHbitmap());
                        pictureBox1.Refresh();
                        //for (int isoa = 0; isoa < 10000; isoa++)
                        //{
                        //}
                        strm.WriteByte(1); // write that it's normal style.
                        strm.WriteByte(32); // write the height
                        strm.WriteByte(32); // write the width
                        int len = 128;
                        buffer = ConvertToByteArray(Backend);
                        strm.Write(buffer, 0, len);
                        prevChar = ch.Key;
                    }

                    strm.Flush();
                    strm.Close();
                    strm.Dispose();
					//pictureBox1.Image = null;
					
                    MessageBox.Show("Conversion Completed Successfully!");
                }
                else
                {
                    throw new Exception("Unable to load the glyph typeface!");
                }
            }
        }

        private byte[] ConvertToByteArray(Bitmap b)
        {
            bool[] bits = new bool[1024];
            int bitnum = 0;
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    if (b.GetPixel(x, y) == Color.FromArgb(255, 255, 255))
                    {
                        bits[bitnum] = false;
                    }
                    else
                    {
                        bits[bitnum] = true;
                    }
                    bitnum++;
                }
            }
            int bytes = bits.Length / 8;
            if ((bits.Length % 8) != 0)
            {
                bytes++;
            }
            byte[] arr2 = new byte[bytes];
            int bitIndex = 0, byteIndex = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    arr2[byteIndex] |= (byte)(((byte)1) << bitIndex);
                }
                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }
            return arr2;
        }

        private void FontComboBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
    }
}
