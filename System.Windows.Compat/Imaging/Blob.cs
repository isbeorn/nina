#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CVPoint = OpenCvSharp.Point;
using AccordPoint = Accord.IntPoint;

namespace Accord.Imaging {
    /// <summary>
    /// Blob object representing a connected region in an image
    /// Uses OpenCV for blob detection
    /// </summary>
    public class Blob {
        /// <summary>
        /// Rectangle that bounds the blob
        /// </summary>
        public Rectangle Rectangle { get; set; }

        /// <summary>
        /// Area of the blob in pixels
        /// </summary>
        public int Area { get; set; }

        /// <summary>
        /// Center of gravity (centroid) of the blob
        /// </summary>
        public Point2d CenterOfGravity { get; set; }

        /// <summary>
        /// Fullness of the blob (ratio of area to bounding rectangle area)
        /// </summary>
        public double Fullness { get; set; }

        /// <summary>
        /// The contour points that define this blob
        /// </summary>
        internal CVPoint[] EdgePoints { get; set; }

        /// <summary>
        /// Unique identifier for this blob
        /// </summary>
        public int ID { get; set; }

        internal Blob(int id, CVPoint[] contour) {
            ID = id;
            EdgePoints = contour;

            if (contour != null && contour.Length > 0) {
                var rect = Cv2.BoundingRect(contour);
                Rectangle = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
                Area = contour.Length;

                // Calculate center of gravity
                var moments = Cv2.Moments(contour);
                if (moments.M00 != 0) {
                    CenterOfGravity = new Point2d(moments.M10 / moments.M00, moments.M01 / moments.M00);
                } else {
                    CenterOfGravity = new Point2d(Rectangle.X + Rectangle.Width / 2.0, Rectangle.Y + Rectangle.Height / 2.0);
                }

                // Calculate fullness
                double rectArea = Rectangle.Width * Rectangle.Height;
                Fullness = rectArea > 0 ? Area / rectArea : 0;
            }
        }
    }

    /// <summary>
    /// Blob counter - counts and extracts stand alone objects in images
    /// Uses OpenCV's findContours for blob detection
    /// </summary>
    public class BlobCounter {
        private List<Blob> blobs = new List<Blob>();
        private Mat processedImage;

        /// <summary>
        /// Minimum width of a blob to be included
        /// </summary>
        public int MinWidth { get; set; } = 1;

        /// <summary>
        /// Minimum height of a blob to be included
        /// </summary>
        public int MinHeight { get; set; } = 1;

        /// <summary>
        /// Maximum width of a blob to be included
        /// </summary>
        public int MaxWidth { get; set; } = int.MaxValue;

        /// <summary>
        /// Maximum height of a blob to be included
        /// </summary>
        public int MaxHeight { get; set; } = int.MaxValue;

        /// <summary>
        /// Filter blobs by size
        /// </summary>
        public bool FilterBlobs { get; set; } = false;

        /// <summary>
        /// Process image searching for blobs
        /// </summary>
        /// <param name="image">Binary image to process</param>
        public void ProcessImage(Bitmap image) {
            blobs.Clear();

            // Convert Bitmap to Mat (Bitmap has implicit conversion to Mat in our implementation)
            Mat mat = image;
            processedImage = mat.Clone();

            // Find contours using OpenCV
            CVPoint[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(processedImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Convert contours to blobs
            int id = 0;
            foreach (var contour in contours) {
                if (contour.Length < 3) continue; // Skip contours with too few points

                var blob = new Blob(id++, contour);

                // Apply size filters if enabled
                if (FilterBlobs) {
                    if (blob.Rectangle.Width < MinWidth || blob.Rectangle.Width > MaxWidth ||
                        blob.Rectangle.Height < MinHeight || blob.Rectangle.Height > MaxHeight) {
                        continue;
                    }
                }

                blobs.Add(blob);
            }
        }

        /// <summary>
        /// Get information about detected blobs
        /// </summary>
        /// <returns>Array of detected blobs</returns>
        public Blob[] GetObjectsInformation() {
            return blobs.ToArray();
        }

        /// <summary>
        /// Get edge points of a specific blob
        /// </summary>
        /// <param name="blob">Blob to get edge points for</param>
        /// <returns>List of edge points as Accord.IntPoint</returns>
        public List<AccordPoint> GetBlobsEdgePoints(Blob blob) {
            if (blob.EdgePoints == null || blob.EdgePoints.Length == 0) {
                return new List<AccordPoint>();
            }

            // Convert OpenCV Points to Accord IntPoints
            return blob.EdgePoints.Select(p => new AccordPoint(p.X, p.Y)).ToList();
        }

        /// <summary>
        /// Get number of detected objects
        /// </summary>
        public int ObjectsCount => blobs.Count;
    }
}
