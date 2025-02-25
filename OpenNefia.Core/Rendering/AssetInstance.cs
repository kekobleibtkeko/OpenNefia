﻿using OpenNefia.Core.Maths;

namespace OpenNefia.Core.Rendering
{
    /// <summary>
    /// Represents one copy of a loaded asset's graphics data, which
    /// can be reused with multiple <see cref="AssetDrawable"/>s.
    /// </summary>
    public interface IAssetInstance : IDisposable
    {
        int PixelWidth { get; }
        int PixelHeight { get; }
        Vector2i PixelSize { get; }

        Vector2 VirtualSize(float uiScale);
        Vector2 VirtualSize(float uiScale, string regionID);
        float VirtualWidth(float uiScale);
        float VirtualHeight(float uiScale);

        AssetPrototype Asset { get; }
        IReadOnlyDictionary<string, UIBox2i> Regions { get; }
        uint CountX { get; }
        uint CountY { get; }

        Love.SpriteBatch MakeBatch(List<AssetInstance.AssetBatchPart> parts, int maxSprites = 2048);
        Love.SpriteBatch MakeSpriteBatch(int count, Love.SpriteBatchUsage usage);

        void DrawUnscaled(float x, float y, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawUnscaled(UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawRegionUnscaled(Love.Quad quad, float x, float y, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawRegionUnscaled(UIBox2 quad, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawRegionUnscaled(Love.Quad quad, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawRegionUnscaled(string regionId, float x = 0, float y = 0, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void Draw(float uiScale, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void Draw(float uiScale, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void Draw(float uiScale, Love.Quad quad, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
        void DrawRegion(float uiScale, string regionId, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default);
    }
    
    public class AssetInstance : IAssetInstance
    {
        /// <summary>
        /// Data to submit to <see cref="MakeBatch(List{AssetBatchPart}, int, SpriteBatchUsage)"/>
        /// </summary>
        public class AssetBatchPart
        {
            public string RegionId { get; set; } = string.Empty;

            /// <summary>
            /// Position of the part, in physical pixels.
            /// </summary>
            public Vector2i PixelPosition { get; set; }

            /// <summary>
            /// X position of the part, in physical pixels.
            /// </summary>
            public int PixelX => PixelPosition.X;

            /// <summary>
            /// Y position of the part, in physical pixels.
            /// </summary>
            public int PixelY => PixelPosition.Y;

            /// <summary>
            /// Constructs a new <see cref="AssetBatchPart"/>.
            /// </summary>
            /// <param name="id">ID of the batch part.</param>
            /// <param name="pixelX">X position of the part in physical pixels.</param>
            /// <param name="pixelY">Y position of the part in physical pixels.</param>
            public AssetBatchPart(string id, int pixelX, int pixelY)
            {
                RegionId = id;
                PixelPosition = new(pixelX, pixelY);
            }

            public override string ToString()
            {
                return $"{RegionId}: ({PixelX}, {PixelY})";
            }
        }

        public AssetPrototype Asset { get; }
        private Love.Image Image { get; }

        private Dictionary<string, Love.Quad> Quads;
        private AssetRegions _regions;

        public IReadOnlyDictionary<string, UIBox2i> Regions => _regions;

        public uint CountX { get; }
        public uint CountY { get; }

        public int PixelWidth { get => Image.GetWidth(); }
        public int PixelHeight { get => Image.GetHeight(); }
        public Vector2i PixelSize => new(PixelWidth, PixelHeight);

        public Vector2 VirtualSize(float uiScale) => (Vector2)PixelSize / uiScale;
        public Vector2 VirtualSize(float uiScale, string regionID)
        {
            var region = Regions[regionID];
            return (Vector2)region.Size / uiScale;
        }
        public float VirtualWidth(float uiScale) => PixelWidth / uiScale;
        public float VirtualHeight(float uiScale) => PixelWidth / uiScale;

        private Love.Quad _assetRegionQuad;

        public AssetInstance(AssetPrototype asset, Love.Image image, AssetRegions regions)
        {
            Asset = asset;
            Image = image;
            Quads = new Dictionary<string, Love.Quad>();
            CountX = Asset.CountX;
            CountY = Asset.CountY;
            _regions = regions;
            _assetRegionQuad = Love.Graphics.NewQuad(0, 0, 1, 1, Image.GetWidth(), Image.GetHeight());

            AddImplicitRegions();
            SetupQuads();
        }

        private void AddImplicitRegions()
        {
            var countX = (int)CountX;
            var countY = (int)CountY;

            var imageWidth = Image.GetWidth();
            var imageHeight = Image.GetHeight();

            if (countX > 1 || countY > 1)
            {
                var width = imageWidth / countX;
                var height = imageHeight / countY;

                var regionId = 0;
                for (int j = 0; j < countY; j++)
                {
                    for (int i = 0; i < countX; i++)
                    {
                        _regions[regionId.ToString()] = UIBox2i.FromDimensions(width * i, height * j, width, height);
                        regionId++;
                    }
                }
            }
            else
            {
                _regions["0"] = UIBox2i.FromDimensions(0, 0, imageWidth, imageHeight);
            }
        }

        private void SetupQuads()
        {
            var imageWidth = Image.GetWidth();
            var imageHeight = Image.GetHeight();

            foreach (var pair in _regions)
            {
                var regionId = pair.Key;
                var region = pair.Value;
                Quads[regionId] = Love.Graphics.NewQuad(region.Left, region.Top, region.Width, region.Height, imageWidth, imageHeight);
            }
        }

        public Love.SpriteBatch MakeBatch(List<AssetBatchPart> parts, int maxSprites = 2048)
        {
            var batch = Love.Graphics.NewSpriteBatch(Image, maxSprites, Love.SpriteBatchUsage.Static);
            batch.Clear();

            foreach (var part in parts)
            {
                batch.Add(Quads[part.RegionId], part.PixelX, part.PixelY);
            }

            batch.Flush();

            return batch;
        }

        public Love.SpriteBatch MakeSpriteBatch(int count, Love.SpriteBatchUsage usage)
        {
            return Love.Graphics.NewSpriteBatch(Image, count, usage);
        }

        public void DrawUnscaled(UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            DrawUnscaled(box.Left, box.Top, box.Width, box.Height, centered, rotation, originOffset);
        }

        public void DrawUnscaled(float x, float y, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            GraphicsEx.DrawImage(Image, x, y, width, height, centered, rotation, originOffset);
        }

        public void DrawRegionUnscaled(UIBox2 quad, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            _assetRegionQuad.SetViewport(quad);
            DrawRegionUnscaled(_assetRegionQuad, box, centered, rotation, originOffset);
        }

        public void DrawRegionUnscaled(Love.Quad quad, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            GraphicsEx.DrawImage(quad, Image, box.Left, box.Top, box.Width, box.Height, centered, rotation, originOffset);
        }

        public void DrawRegionUnscaled(Love.Quad quad, float x, float y, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            GraphicsEx.DrawImage(quad, Image, x, y, width, height, centered, rotation, originOffset);
        }

        public void DrawRegionUnscaled(string regionId, float x, float y, float width = 0, float height = 0, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            var quad = Quads[regionId];
            if (quad == null)
            {
                throw new ArgumentException($"Invalid region ID {regionId}");
            }

            GraphicsEx.DrawImageRegion(Image, quad, x, y, width, height, centered, rotation, originOffset);
        }

        public void Draw(float uiScale, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            if (vwidth == null)
                vwidth = PixelWidth;
            if (vheight == null)
                vheight = PixelHeight;

            DrawUnscaled(vx * uiScale, vy * uiScale, vwidth.Value * uiScale, vheight.Value * uiScale, centered, rotation, originOffset * uiScale);
        }

        public void Draw(float uiScale, UIBox2 box, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            Draw(uiScale, box.Left, box.Top, box.Width, box.Height, centered, rotation, originOffset);
        }

        public void Draw(float uiScale, Love.Quad quad, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            if (vwidth == null)
                vwidth = quad.GetViewport().Width;
            if (vheight == null)
                vheight = quad.GetViewport().Height;

            DrawRegionUnscaled(quad, vx * uiScale, vy * uiScale, vwidth.Value * uiScale, vheight.Value * uiScale, centered, rotation, originOffset * uiScale);
        }

        public void DrawRegion(float uiScale, string regionId, float vx, float vy, float? vwidth = null, float? vheight = null, bool centered = false, float rotation = 0, Maths.Vector2 originOffset = default)
        {
            Draw(uiScale, Quads[regionId], vx, vy, vwidth, vheight, centered, rotation, originOffset);
        }

        public void Dispose()
        {
            foreach (var quad in Quads.Values)
            {
                quad.Dispose();
            }
            Quads.Clear();
            _assetRegionQuad.Dispose();
        }
    }
}
