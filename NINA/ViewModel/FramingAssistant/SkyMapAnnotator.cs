#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model.FramingAssistant;
using NINA.WPF.Base.SkySurvey;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NINA.ViewModel.FramingAssistant {

    public partial class SkyMapAnnotator : BaseINPC, ITelescopeConsumer, ISkyMapAnnotator {
        private readonly DatabaseInteraction dbInstance;
        public ViewportFoV ViewportFoV { get; private set; }
        private List<Constellation> dbConstellations;
        private Dictionary<string, DeepSkyObject> dbDSOs;
        private List<CacheImage> cacheImages;
        private Bitmap img;
        private Bitmap dsoImageBuffer;
        private Graphics g;
        private Graphics dsoImageGraphics;
        private ITelescopeMediator telescopeMediator;
        private CacheSkySurvey cache;

        public SkyMapAnnotator(ITelescopeMediator mediator) {
            this.telescopeMediator = mediator;
            dbInstance = new DatabaseInteraction();
            DSOInViewport = new List<FramingDSO>();
            ConstellationsInViewport = new List<FramingConstellation>();
            FrameLineMatrix = new FrameLineMatrix2();
            ConstellationBoundaries = new Dictionary<string, ConstellationBoundary>();
            cacheImages = new List<CacheImage>();
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CacheSkySurvey cache, CancellationToken ct) {
            telescopeMediator.RemoveConsumer(this);

            AnnotateDSO = true;
            AnnotateGrid = true;

            this.cache = cache;

            ViewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            if (dbConstellations == null) {
                dbConstellations = await dbInstance.GetConstellationsWithStars(ct);
            }

            if (dbDSOs == null) {
                dbDSOs = (await dbInstance.GetDeepSkyObjects(string.Empty, null, new DatabaseInteraction.DeepSkyObjectSearchParams(), ct)).ToDictionary(x => x.Id, y => y);                
            }

            if (ActiveCatalogues == null) {
                ActiveCatalogues = (await dbInstance.GetCatalogues(50, ct))?.Select(x => new ActiveCatalogue(x, true)).ToList() ?? new List<ActiveCatalogue>();
                foreach (var item in ActiveCatalogues) {
                    item.PropertyChanged += (sender, e) => {
                        if (e.PropertyName == nameof(ActiveCatalogue.Active)) {
                            UpdateSkyMap();
                        }
                    };
                }
            }

            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();

            img = new Bitmap((int)ViewportFoV.OriginalWidth, (int)ViewportFoV.OriginalHeight, PixelFormat.Format32bppArgb);
            dsoImageBuffer = new Bitmap((int)ViewportFoV.OriginalWidth, (int)ViewportFoV.OriginalHeight, PixelFormat.Format32bppArgb);

            dsoImageGraphics = Graphics.FromImage(dsoImageBuffer);
            dsoImageGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            dsoImageGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            g = Graphics.FromImage(img);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            FrameLineMatrix.CalculatePoints(ViewportFoV);
            if (ConstellationBoundaries.Count == 0) {
                ConstellationBoundaries = await GetConstellationBoundaries();
            }

            telescopeMediator.RegisterConsumer(this);
            Initialized = true;

            UpdateSkyMap();
        }

        public ViewportFoV ChangeFoV(double vFoVDegrees) {
            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();
            ViewportFoV = new ViewportFoV(ViewportFoV.CenterCoordinates, vFoVDegrees, ViewportFoV.OriginalWidth, ViewportFoV.OriginalHeight, ViewportFoV.Rotation);

            FrameLineMatrix.CalculatePoints(ViewportFoV);

            UpdateSkyMap();

            return ViewportFoV;
        }

        public ICommand DragCommand { get; private set; }

        public FrameLineMatrix2 FrameLineMatrix { get; private set; }

        public List<FramingDSO> DSOInViewport { get; private set; }

        [ObservableProperty]
        private bool initialized;

        public List<FramingConstellation> ConstellationsInViewport { get; private set; }

        [ObservableProperty]
        private IList<ActiveCatalogue> activeCatalogues;

        [ObservableProperty]
        private bool annotateConstellationBoundaries;

        [ObservableProperty]
        private bool dynamicFoV;

        [ObservableProperty]
        private bool annotateConstellations;

        [ObservableProperty]
        private bool annotateGrid;

        [ObservableProperty]
        private bool annotateDSO;

        [ObservableProperty]
        private bool useCachedImages;

        [ObservableProperty]
        private BitmapSource skyMapOverlay;

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport() {
            var dsoList = new Dictionary<string, DeepSkyObject>();

            double minSize = 0;
            if (!(Math.Min(ViewportFoV.OriginalHFoV, ViewportFoV.OriginalVFoV) < 10)) {
                // Stuff has to be at least 3 pixel wide
                minSize = 3 * Math.Min(ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);
            }
            var maxSize = AstroUtil.DegreeToArcsec(2 * Math.Max(ViewportFoV.OriginalHFoV, ViewportFoV.OriginalVFoV));

            var filteredCatalogues = ActiveCatalogues.Where(x => !x.Active).Select(x => x.Name).ToList();

            var filteredDbDSO = dbDSOs
                .Where(d => (d.Value.Size != null && d.Value.Size > minSize && d.Value.Size < maxSize) || ViewportFoV.VFoVDeg <= 10)
                .Where(dso => !filteredCatalogues.Any(dso.Value.Name.StartsWith))
                .ToList();

            // if we're above 90deg centerTop will be different than centerBottom, otherwise it is equal
            if (ViewportFoV.IsAbove90) {
                dsoList = filteredDbDSO.Where(x =>
                    x.Value.Coordinates.Dec > (!ViewportFoV.AboveZero ? -90 : ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom)
                    && x.Value.Coordinates.Dec < (!ViewportFoV.AboveZero ? ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegBottom : 90)
                ).ToDictionary(x => x.Key, y => y.Value);
            } else {
                var raFrom = ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg;
                var raThru = ViewportFoV.TopLeft.RADegrees;
                if (raFrom < 0) {
                    dsoList = filteredDbDSO.Where(x =>
                        (x.Value.Coordinates.RADegrees > 360 + raFrom || x.Value.Coordinates.RADegrees < raThru)
                        && x.Value.Coordinates.Dec > ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom
                        && x.Value.Coordinates.Dec < ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegTop
                    ).ToDictionary(x => x.Key, y => y.Value);
                } else {
                    dsoList = filteredDbDSO.Where(x =>
                        x.Value.Coordinates.RADegrees > (ViewportFoV.TopLeft.RADegrees - ViewportFoV.HFoVDeg)
                        && x.Value.Coordinates.RADegrees < (ViewportFoV.TopLeft.RADegrees)
                        && x.Value.Coordinates.Dec > ViewportFoV.CenterCoordinates.Dec - ViewportFoV.VFoVDegBottom
                        && x.Value.Coordinates.Dec < ViewportFoV.CenterCoordinates.Dec + ViewportFoV.VFoVDegTop
                    ).ToDictionary(x => x.Key, y => y.Value);
                }
            }
            return dsoList;
        }

        public void ClearImagesForViewport() {
            if(cacheImages != null) { 
                foreach(var image in cacheImages) {
                    image.Dispose();
                }
                cacheImages.Clear();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        private List<CacheImage> GetCacheImagesForViewport() {
            using (MyStopWatch.Measure()) {
                double minSize = 6;
                double maxSize = 600;

                var l = new List<CacheImage>();
                foreach (var entry in cache.Cache.Elements("Image")) {
                    double fovW = double.Parse(entry.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
                    double fovH = double.Parse(entry.Attribute("FoVH").Value, CultureInfo.InvariantCulture);

                    if (fovW < minSize || fovW > maxSize) {
                        continue;
                    }

                    double ra = double.Parse(entry.Attribute("RA").Value, CultureInfo.InvariantCulture);
                    double dec = double.Parse(entry.Attribute("Dec").Value, CultureInfo.InvariantCulture);
                    double rotation = double.Parse(entry.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
                    string path = Path.Combine(cache.framingAssistantCachePath, entry.Attribute("FileName").Value);

                    if (AstroUtil.ArcminToArcsec(fovW) > minSize && AstroUtil.ArcminToArcsec(fovH) > minSize) {
                        var existing = cacheImages.FirstOrDefault(x => x.Coordinates.RA == ra && x.Coordinates.Dec == dec);
                        if (existing == null) {
                            existing = new CacheImage(ra, dec, fovW, fovH, rotation, path);
                            cacheImages.Add(existing);
                        }
                        l.Add(existing);
                    }
                }

                l = l.Where(x => {

                    if(ViewportFoV.OriginalHFoV > 50) {
                        // Coarse FoV - just check the center
                        return ViewportFoV.IsInViewPortBounds(x.Coordinates);
                    }

                    var top = x.Coordinates.Shift(0, -AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    var bottom = x.Coordinates.Shift(0, AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    var left = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 2d), 0, x.Rotation);
                    var right = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 2d), 0, x.Rotation);
                    if (ViewportFoV.OriginalHFoV > 10) {
                        // Larger FoV - Check the edge bounds of the image
                        return ViewportFoV.IsInViewPortBounds(x.Coordinates)
                                || ViewportFoV.IsInViewPortBounds(top)
                                || ViewportFoV.IsInViewPortBounds(left)
                                || ViewportFoV.IsInViewPortBounds(right)
                                || ViewportFoV.IsInViewPortBounds(bottom)
                        ;
                    }

                    var topLeft = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 2d), -AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    var topRight = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 2d), -AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    var bottomLeft = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 2d), AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    var bottomRight = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 2d), AstroUtil.ArcminToDegree(x.FoVH / 2d), x.Rotation);
                    if (ViewportFoV.OriginalHFoV > 3) {
                        // Medium FoV - Check the center and edge bounds of the image
                        return ViewportFoV.IsInViewPortBounds(x.Coordinates)
                                || ViewportFoV.IsInViewPortBounds(top)
                                || ViewportFoV.IsInViewPortBounds(left)
                                || ViewportFoV.IsInViewPortBounds(right)
                                || ViewportFoV.IsInViewPortBounds(bottom)
                                || ViewportFoV.IsInViewPortBounds(topLeft)
                                || ViewportFoV.IsInViewPortBounds(topRight)
                                || ViewportFoV.IsInViewPortBounds(bottomLeft)
                                || ViewportFoV.IsInViewPortBounds(bottomRight)
                        ;
                    }

                    var halfTop = x.Coordinates.Shift(0, -AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);
                    var halfBottom = x.Coordinates.Shift(0, AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);
                    var halfLeft = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 4d), 0, x.Rotation);
                    var halfRight = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 4d), 0, x.Rotation);
                    var halfTopLeft = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 4d), -AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);
                    var halfTopRight = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 4d), -AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);
                    var halfBottomLeft = x.Coordinates.Shift(-AstroUtil.ArcminToDegree(x.FoVW / 4d), AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);
                    var halfBottomRight = x.Coordinates.Shift(AstroUtil.ArcminToDegree(x.FoVW / 4d), AstroUtil.ArcminToDegree(x.FoVH / 4d), x.Rotation);

                    // Small FoV - Check most points
                    return ViewportFoV.IsInViewPortBounds(x.Coordinates)
                            || ViewportFoV.IsInViewPortBounds(top)
                            || ViewportFoV.IsInViewPortBounds(left)
                            || ViewportFoV.IsInViewPortBounds(right)
                            || ViewportFoV.IsInViewPortBounds(bottom)
                            || ViewportFoV.IsInViewPortBounds(topLeft)
                            || ViewportFoV.IsInViewPortBounds(topRight)
                            || ViewportFoV.IsInViewPortBounds(bottomLeft)
                            || ViewportFoV.IsInViewPortBounds(bottomRight)


                            || ViewportFoV.IsInViewPortBounds(halfTop)
                            || ViewportFoV.IsInViewPortBounds(halfBottom)
                            || ViewportFoV.IsInViewPortBounds(halfLeft)
                            || ViewportFoV.IsInViewPortBounds(halfRight)
                            || ViewportFoV.IsInViewPortBounds(halfTopLeft)
                            || ViewportFoV.IsInViewPortBounds(halfTopRight)
                            || ViewportFoV.IsInViewPortBounds(halfBottomLeft)
                            || ViewportFoV.IsInViewPortBounds(halfBottomRight)
                    ;


                })
                    //Order in descending order so that smallest field of view is drawn on top, as it most likely contains most details
                    .OrderByDescending(x => x.FoVW).ToList();
                return l;
            }
        }

        public Coordinates ShiftViewport(Vector delta) {
            ViewportFoV.Shift(delta);

            return ViewportFoV.CenterCoordinates;
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            FrameLineMatrix.CalculatePoints(ViewportFoV);
        }

        private Dictionary<string, ConstellationBoundary> ConstellationBoundaries;

        private async Task<Dictionary<string, ConstellationBoundary>> GetConstellationBoundaries() {
            var dic = new Dictionary<string, ConstellationBoundary>();
            var list = await dbInstance.GetConstellationBoundaries(new CancellationToken());
            foreach (var item in list) {
                dic.Add(item.Name, item);
            }
            return dic;
        }

        public List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; private set; } = new List<FramingConstellationBoundary>();

        public void CalculateConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
            foreach (var boundary in ConstellationBoundaries) {
                var frameLine = new FramingConstellationBoundary();
                if (boundary.Value.Boundaries.Any((x) => ViewportFoV.ContainsCoordinates(x))) {
                    foreach (var coordinates in boundary.Value.Boundaries) {
                        var point = coordinates.XYProjection(ViewportFoV);
                        frameLine.Points.Add(new PointF((float)point.X, (float)point.Y));
                    }

                    ConstellationBoundariesInViewPort.Add(frameLine);
                }
            }
        }

        private void UpdateAndAnnotateDSOs() {
            var allGatheredDSO = GetDeepSkyObjectsForViewport();

            var existingDSOs = new List<string>();
            for (int i = DSOInViewport.Count - 1; i >= 0; i--) {
                var dso = DSOInViewport[i];
                if (allGatheredDSO.ContainsKey(dso.Id)) {
                    dso.RecalculateTopLeft(ViewportFoV);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInViewport.RemoveAt(i);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, ViewportFoV));
            }

            foreach (var dso in DSOInViewport) {
                dso.Draw(g);
            }
        }

        private void DrawStars() {
            foreach (var constellation in ConstellationsInViewport) {
                constellation.DrawStars(g);
            }
        }

        private void UpdateAndAnnotateConstellations(bool drawAnnotations) {
            foreach (var constellation in dbConstellations) {
                var viewPortConstellation = ConstellationsInViewport.FirstOrDefault(x => x.Id == constellation.Id);

                var isInViewport = false;
                foreach (var star in constellation.Stars) {
                    if (!ViewportFoV.ContainsCoordinates(star.Coords)) {
                        continue;
                    }

                    isInViewport = true;
                    break;
                }

                if (isInViewport) {
                    if (viewPortConstellation == null) {
                        var framingConstellation = new FramingConstellation(constellation, ViewportFoV);
                        framingConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                        ConstellationsInViewport.Add(framingConstellation);
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            if (drawAnnotations) {
                foreach (var constellation in ConstellationsInViewport) {
                    constellation.DrawAnnotations(g);
                }
            }
        }

        private void UpdateAndDrawConstellationBoundaries() {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                constellationBoundary.Draw(g);
            }
        }

        private void UpdateAndDrawGrid() {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            FrameLineMatrix.Draw(g);
        }

        private Task DrawBufferedDSOImages(CancellationToken ct) {
            return Task.Run(async () => {
                try {
                    var relevantImages = GetCacheImagesForViewport();
                    foreach (var cacheImage in relevantImages) {
                        ct.ThrowIfCancellationRequested();
                        if (File.Exists(cacheImage.ImagePath)) {
                            var image = cacheImage.GetImageForScale(ViewportFoV.OriginalHFoV, ViewportFoV.Width);
                            var sourceR = new RectangleF(0, 0, image.Width, image.Height);

                            var imageResW = AstroUtil.ArcminToArcsec(cacheImage.FoVW) / image.Width;
                            var imageResH = AstroUtil.ArcminToArcsec(cacheImage.FoVH) / image.Height;
                            var conversionW = imageResW / ViewportFoV.ArcSecWidth;
                            var conversionH = imageResH / ViewportFoV.ArcSecHeight;
                            var dest = new RectangleF(-(float)(image.Width * conversionW / 2f), -(float)(image.Height * conversionH / 2f), (float)(image.Width * conversionW), (float)(image.Height * conversionH));

                            var center = cacheImage.Coordinates.XYProjection(ViewportFoV);

                            var panelDeltaX = center.X - ViewportFoV.ViewPortCenterPoint.X;
                            var panelDeltaY = center.Y - ViewportFoV.ViewPortCenterPoint.Y;
                            var referenceCenter = ViewportFoV.CenterCoordinates.Shift(panelDeltaX < 1E-10 ? 1 : 0, panelDeltaY, ViewportFoV.Rotation, ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);

                            var rotation = -(90 - ((float)AstroUtil.CalculatePositionAngle(referenceCenter.RADegrees, cacheImage.Coordinates.RADegrees, referenceCenter.Dec, cacheImage.Coordinates.Dec)));
                            if (panelDeltaX < 0) {
                                rotation += 180;
                            }
                            if (cacheImage.Coordinates.Dec < 0 || (referenceCenter.Dec < 0 && cacheImage.Coordinates.Dec >= 0)) {
                                rotation += 180;
                            }

                            rotation += (float)cacheImage.Rotation;

                            dsoImageGraphics.TranslateTransform((float)center.X, (float)center.Y);
                            dsoImageGraphics.RotateTransform(rotation);
                            dsoImageGraphics.DrawImage(image, dest, sourceR, GraphicsUnit.Pixel);
                            dsoImageGraphics.ResetTransform();
                        }
                    }
                    ct.ThrowIfCancellationRequested();
                    
                    Render();
                } catch (Exception) {
                } finally {
                }
            });
        }

        private void Render() {
            try {
                g.Clear(Color.Transparent);

                if (!DllLoader.IsX86() && UseCachedImages) {
                    g.DrawImage((Bitmap)dsoImageBuffer.Clone(), 0, 0);
                }

                if (!AnnotateConstellations && AnnotateDSO || AnnotateConstellations) {
                    UpdateAndAnnotateConstellations(AnnotateConstellations);
                    if (!AnnotateDSO) {
                        DrawStars();
                    }
                }

                if (AnnotateDSO) {
                    UpdateAndAnnotateDSOs();
                    DrawStars();
                }

                if (AnnotateConstellationBoundaries) {
                    UpdateAndDrawConstellationBoundaries();
                }

                if (AnnotateGrid) {
                    UpdateAndDrawGrid();
                }

                if (telescopeConnected) {
                    DrawTelescope();
                }
                var source = ImageUtility.ConvertBitmap(img, PixelFormats.Bgra32);
                source.Freeze();
                SkyMapOverlay = source;
            } catch (Exception) {
            }
        }

        private CancellationTokenSource renderCts;
        private Task renderTask;
        private Coordinates oldCenter;
        private double oldFoV;
        private bool oldUseCachedImages;
        public void UpdateSkyMap() {
            if (Initialized) {
                var center = ViewportFoV.CenterCoordinates;
                var fov = ViewportFoV.OriginalHFoV;
                var needFullRedraw = center != oldCenter || fov != oldFoV || UseCachedImages != oldUseCachedImages || renderTask == null || renderTask.Status < TaskStatus.RanToCompletion;
                try {
                    try { renderCts?.Cancel(); } catch { }
                    while (renderTask != null && (renderTask.Status < TaskStatus.RanToCompletion)) {
                    }
                } catch (Exception) {
                }                

                oldCenter = ViewportFoV.CenterCoordinates;
                oldFoV = ViewportFoV.OriginalHFoV;
                oldUseCachedImages = UseCachedImages;

                if (needFullRedraw) { 
                    dsoImageGraphics.Clear(Color.Transparent);
                }
                Render();

                if (UseCachedImages && needFullRedraw) {
                    renderCts = new CancellationTokenSource();
                    renderTask = DrawBufferedDSOImages(renderCts.Token);
                }
            }
        }

        private void DrawTelescope() {
            if (ViewportFoV.ContainsCoordinates(telescopeCoordinates)) {
                System.Windows.Point scopePosition = telescopeCoordinates.XYProjection(ViewportFoV);
                g.DrawEllipse(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y - 15), 30, 30);
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y - 15),
                    (float)(scopePosition.X), (float)(scopePosition.Y - 5));
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y + 5),
                    (float)(scopePosition.X), (float)(scopePosition.Y + 15));
                g.DrawLine(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y),
                    (float)(scopePosition.X - 5), (float)(scopePosition.Y));
                g.DrawLine(ScopePen, (float)(scopePosition.X + 5), (float)(scopePosition.Y),
                    (float)(scopePosition.X + 15), (float)(scopePosition.Y));
            }
        }

        private static readonly Pen ScopePen = new Pen(Color.FromArgb(128, Color.Yellow), 2.0f);

        private bool telescopeConnected;
        private Coordinates telescopeCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (deviceInfo.Connected && deviceInfo.Coordinates != null) {
                telescopeConnected = true;
                var coordinates = deviceInfo.Coordinates.Transform(Epoch.J2000);
                if (Math.Abs(telescopeCoordinates.RADegrees - coordinates.RADegrees) > 0.01 || Math.Abs(telescopeCoordinates.Dec - coordinates.Dec) > 0.01) {
                    telescopeCoordinates = coordinates;
                    var p = coordinates.XYProjection(ViewportFoV);                    
                    if (!ViewportFoV.IsOutOfViewportBounds(p)) {                        
                        UpdateSkyMap();
                    }
                    
                }
            } else {
                telescopeConnected = false;
            }
        }

        public void Dispose() {
            telescopeMediator.RemoveConsumer(this);
        }
    }
}