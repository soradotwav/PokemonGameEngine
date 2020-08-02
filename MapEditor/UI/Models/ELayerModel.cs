﻿using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class ELayerModel : IDisposable
    {
        private readonly byte _eLayerNum;
        private Blockset.Block _block;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal ELayerModel(byte eLayerNum)
        {
            _eLayerNum = eLayerNum;
            Text = $"E-Layer {_eLayerNum:X2}";
            Bitmap = new WriteableBitmap(new PixelSize(OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY), new Vector(96, 96), PixelFormat.Bgra8888);
        }

        internal void SetBlock(Blockset.Block block)
        {
            _block = block;
            UpdateBitmap();
        }
        internal unsafe void UpdateBitmap()
        {
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtils.TransparencyGrid(bmpAddress, OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY, OverworldConstants.Tile_NumPixelsX / 2, OverworldConstants.Tile_NumPixelsY / 2);
                _block.Draw(bmpAddress, OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY, 0, 0, _eLayerNum);
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}