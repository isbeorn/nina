// Accord Imaging Library
// The Accord.NET Framework
// http://accord-framework.net
//
// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Imaging.Textures
{
    using Accord.Math;
    using System;

    /// <summary>
    /// Marble texture.
    /// </summary>
    /// 
    /// <remarks><para>The texture generator creates textures with effect of marble.
    /// The <see cref="XPeriod"/> and <see cref="YPeriod"/> properties allow to control the look
    /// of marble texture in X/Y directions.</para>
    /// 
    /// <para>The generator is based on the <see cref="Accord.Math.PerlinNoise">Perlin noise function</see>.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create texture generator
    /// MarbleTexture textureGenerator = new MarbleTexture( );
    /// // generate new texture
    /// float[,] texture = textureGenerator.Generate( 320, 240 );
    /// // convert it to image to visualize
    /// Bitmap textureImage = TextureTools.ToBitmap( texture );
    /// </code>
    ///
    /// <para><b>Result image:</b></para>
    /// <img src="..\images\imaging\marble_texture.jpg" width="320" height="240" />
    /// </remarks>
    /// 
    public class MarbleTexture : BaseTextureGenerator, ITextureGenerator
    {
        // Perlin noise function used for texture generation
        private PerlinNoise noise = new PerlinNoise(2, 0.65, 1.0 / 32, 1.0);

        private double xPeriod = 5.0;
        private double yPeriod = 10.0;

        /// <summary>
        /// X period value, ≥ 2. Default is 5.
        /// </summary>
        /// 
        /// <remarks>Default value is set to <b>5</b>.</remarks>
        /// 
        public double XPeriod
        {
            get { return xPeriod; }
            set
            {
                if (value < 2)
                    throw new ArgumentOutOfRangeException("The X period must be equal to or higher than 2.");
                xPeriod = value;
            }
        }

        /// <summary>
        /// Y period value, ≥ 2. Default is 10.
        /// </summary>
        /// 
        /// <remarks>Default value is set to <b>10</b>.</remarks>
        /// 
        public double YPeriod
        {
            get { return yPeriod; }
            set
            {
                if (value < 2)
                    throw new ArgumentOutOfRangeException("The Y period must be equal to or higher than 2.");
                yPeriod = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarbleTexture"/> class.
        /// </summary>
        /// 
        public MarbleTexture()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarbleTexture"/> class.
        /// </summary>
        /// 
        /// <param name="xPeriod">X period value.</param>
        /// <param name="yPeriod">Y period value.</param>
        /// 
        public MarbleTexture(double xPeriod, double yPeriod)
        {
            this.xPeriod = xPeriod;
            this.yPeriod = yPeriod;
        }

        /// <summary>
        /// Generate texture.
        /// </summary>
        /// 
        /// <param name="width">Texture's width.</param>
        /// <param name="height">Texture's height.</param>
        /// 
        /// <returns>Two dimensional array of intensities.</returns>
        /// 
        /// <remarks>Generates new texture of the specified size.</remarks>
        /// 
        public override float[,] Generate(int width, int height)
        {
            var texture = new float[height, width];
            double xFact = xPeriod / width;
            double yFact = yPeriod / height;

            int r = Accord.Math.Random.Generator.Random.Next(5000);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double a = (x * xFact + y * yFact + noise.Function2D(x + r, y + r));
                    texture[y, x] = Math.Min(1.0f, (float)Math.Abs(Math.Sin(a * Math.PI)));

                }
            }

            return texture;
        }

    }
}
