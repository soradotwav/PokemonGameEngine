﻿using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class SubLayerModel : IDisposable
    {
        private Blockset.Block _block;
        private byte _eLayerNum;
        private readonly byte _subLayerNum;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal SubLayerModel(Blockset.Block block, byte eLayerNum, byte subLayerNum)
        {
            _block = block;
            _eLayerNum = eLayerNum;
            _subLayerNum = subLayerNum;
            Text = $"Sub-Layer {_subLayerNum:X2}";
            Bitmap = new WriteableBitmap(new PixelSize(OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY), new Vector(96, 96), PixelFormat.Bgra8888);
            UpdateBitmap();
        }

        internal void SetBlock(Blockset.Block block)
        {
            _block = block;
            UpdateBitmap();
        }
        internal void SetELayer(byte e)
        {
            if (_eLayerNum != e)
            {
                _eLayerNum = e;
                UpdateBitmap();
            }
        }
        internal void UpdateBitmap()
        {
            UpdateBitmap(Bitmap, _block, _eLayerNum, _subLayerNum);
        }

        internal static Blockset.Block.Tile GetTile(Blockset.Block block, byte eLayerNum, byte subLayerNum, int x, int y)
        {
            List<Blockset.Block.Tile> layers = block.Tiles[y][x][eLayerNum];
            return layers.Count <= subLayerNum ? null : layers[subLayerNum];
        }
        internal static unsafe void UpdateBitmap(WriteableBitmap bitmap, Blockset.Block block, byte eLayerNum, byte subLayerNum)
        {
            using (ILockedFramebuffer l = bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                for (int y = 0; y < OverworldConstants.Block_NumTilesY; y++)
                {
                    int py = y * OverworldConstants.Tile_NumPixelsY;
                    for (int x = 0; x < OverworldConstants.Block_NumTilesX; x++)
                    {
                        int px = x * OverworldConstants.Tile_NumPixelsX;
                        Blockset.Block.Tile t = GetTile(block, eLayerNum, subLayerNum, x, y);
                        if (t != null)
                        {
                            RenderUtils.TransparencyGrid(bmpAddress, OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY, px, py, OverworldConstants.Tile_NumPixelsX / 2, OverworldConstants.Tile_NumPixelsY / 2, OverworldConstants.Block_NumTilesX, OverworldConstants.Block_NumTilesY);
                            t.Draw(bmpAddress, OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY, px, py);
                        }
                        else
                        {
                            RenderUtils.ClearUnchecked(bmpAddress, OverworldConstants.Block_NumPixelsX, px, py, OverworldConstants.Tile_NumPixelsX, OverworldConstants.Tile_NumPixelsY);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}