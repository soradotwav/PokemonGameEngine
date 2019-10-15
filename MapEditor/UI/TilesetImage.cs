﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class TilesetImage : Control, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private const Stretch _bitmapStretch = Stretch.None;
        private const int numTilesX = 8;

        private readonly Selection _selection;
        public event EventHandler<Tileset.Tile> SelectionCompleted;

        private Tileset _tileset;
        public Tileset Tileset
        {
            get => _tileset;
            set
            {
                if (_tileset != value)
                {
                    _tileset = value;
                    OnTilesetChanged();
                    OnPropertyChanged(nameof(Tileset));
                }
            }
        }

        private bool _isSelecting;

        private WriteableBitmap _bitmap;
        private Size _bitmapSize;

        public TilesetImage()
        {
            _selection = new Selection();
            _selection.Changed += OnSelectionChanged;

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
        }

        public override void Render(DrawingContext context)
        {
            if (_tileset != null)
            {
                var viewPort = new Rect(Bounds.Size);
                Vector scale = _bitmapStretch.CalculateScaling(Bounds.Size, _bitmapSize);
                Size scaledSize = _bitmapSize * scale;
                Rect destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
                Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / scale));

                context.DrawImage(_bitmap, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 8, _selection.Y * 8, _selection.Width * 8, _selection.Height * 8);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_tileset != null)
            {
                if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                {
                    return _bitmapSize;
                }
                else
                {
                    return _bitmapStretch.CalculateSize(availableSize, _bitmapSize);
                }
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_tileset != null)
            {
                return _bitmapStretch.CalculateSize(finalSize, _bitmapSize);
            }
            return new Size();
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_tileset != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)pos.X / 8, (int)pos.Y / 8, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_tileset != null && _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isSelecting = false;
                    FireSelectionCompleted();
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
        private void FireSelectionCompleted()
        {
            if (SelectionCompleted != null)
            {
                int index = _selection.X + (_selection.Y * numTilesX);
                SelectionCompleted.Invoke(this, index >= _tileset.Tiles.Length ? null : _tileset.Tiles[index]);
            }
        }
        private void ResetSelectionAndInvalidateVisual()
        {
            _isSelecting = false;
            _selection.Start(0, 0, 1, 1);
            InvalidateVisual();
        }
        private unsafe void OnTilesetChanged()
        {
            if (_tileset != null)
            {
                Tileset.Tile[] tiles = _tileset.Tiles;
                int numTilesY = (tiles.Length / numTilesX) + (tiles.Length % numTilesX != 0 ? 1 : 0);
                int bmpWidth = numTilesX * 8;
                int bmpHeight = numTilesY * 8;
                if (_bitmap == null || _bitmap.PixelSize.Height != bmpHeight)
                {
                    _bitmap = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                    _bitmapSize = new Size(bmpWidth, bmpHeight);
                }
                using (ILockedFramebuffer l = _bitmap.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    int x = 0;
                    int y = 0;
                    for (int i = 0; i < tiles.Length; i++, x++)
                    {
                        if (x >= numTilesX)
                        {
                            x = 0;
                            y++;
                        }
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, 4, 4, 0xFFBFBFBF);
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, (x * 8) + 4, y * 8, 4, 4, 0xFFFFFFFF);
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x * 8, (y * 8) + 4, 4, 4, 0xFFFFFFFF);
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, (x * 8) + 4, (y * 8) + 4, 4, 4, 0xFFBFBFBF);
                        RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, tiles[i].Colors, false, false);
                    }
                    // Draw an X for the unavailable ones
                    for (; x < numTilesX; x++)
                    {
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, 8, 8, 0xFF000000);
                        for (int py = 0; py < 8; py++)
                        {
                            for (int px = 0; px < 8; px++)
                            {
                                if (px == py)
                                {
                                    RenderUtil.DrawUnchecked(bmpAddress + (x * 8) + px + (((y * 8) + py) * bmpWidth), 0xFFFF0000);
                                    RenderUtil.DrawUnchecked(bmpAddress + (x * 8) + (7 - px) + (((y * 8) + py) * bmpWidth), 0xFFFF0000);
                                }
                            }
                        }
                    }
                }
                ResetSelectionAndInvalidateVisual();
                FireSelectionCompleted();
            }
        }
    }
}
