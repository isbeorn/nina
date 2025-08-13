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
    /// Wood texture.
    /// </summary>
    /// 
    /// <remarks><para>The texture generator creates textures with effect of
    /// rings on trunk's shear. The <see cref="Rings"/> property allows to specify the
    /// desired amount of wood rings.</para>
    /// 
    /// <para>The generator is based on the <see cref="Accord.Math.PerlinNoise">Perlin noise function</see>.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create texture generator
    /// WoodTexture textureGenerator = new WoodTexture();
    /// 
    /// // generate new texture
    /// float[,] texture = textureGenerator.Generate(320, 240);
    /// 
    /// // convert it to image to visualize
    /// Bitmap textureImage = texture.ToBitmap();
    /// </code>
    ///
    /// <para><b>Result image:</b></para>
    /// <img src="..\images\imaging\wood_texture.jpg" width="320" height="240" />
    /// </remarks>
    /// 
    public class WoodTexture : BaseTextureGenerator, ITextureGenerator
    {
        // Perlin noise function used for texture generation
        private PerlinNoise noise = new PerlinNoise(8, 0.5, 1.0 / 32, 0.05);

        // rings amount
        private double rings = 12;

        /// <summary>
        /// Wood rings amount, ≥ 3. Default is 12.
        /// </summary>
        /// 
        /// <remarks><para>The property sets the amount of wood rings, which make effect of
        /// rings on trunk's shear.</para>
        /// 
        /// <para>Default value is set to <b>12</b>.</para></remarks>
        /// 
        public double Rings
        {
            get { return rings; }
            set
            {
                if (value < 3)
                    throw new ArgumentOutOfRangeException("value", "Number of rings must be equal to or higher than 3.");
                rings = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WoodTexture"/> class.
        /// </summary>
        /// 
        public WoodTexture()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WoodTexture"/> class.
        /// </summary>
        /// 
        /// <param name="rings">Wood rings amount.</param>
        /// 
        public WoodTexture(double rings)
        {
            this.rings = rings;
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
            int w2 = width / 2;
            int h2 = height / 2;

            int r = Accord.Math.Random.Generator.Random.Next(5000);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double xv = (double)(x - w2) / width;
                    double yv = (double)(y - h2) / height;

                    double a = (Math.Sqrt(xv * xv + yv * yv) + noise.Function2D(x + r, y + r));
                    texture[y, x] = Math.Min(1.0f, (float)Math.Abs(Math.Sin(a * Math.PI * 2 * rings)));
                }
            }

            return texture;
        }

    }
}
