using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Colors tree nodes.
    /// </summary>
    class ColorManager
    {
        readonly GlobalManager _globalManager;

        internal ColorManager(GlobalManager globalManager)
        {
            _globalManager = globalManager;
        }

        /// <summary>
        /// Generates colors for each grouping in a visually pleasing manner.
        /// </summary>
        internal void AssignGroupColors()
        {
            // Go through each grouping level.
            foreach (var groupingLevel in _globalManager.Groupings)
            {
                // Get the groupings, using a different collection for themes.
                var groupings = groupingLevel.Key == "Theme" ? _globalManager.MainThemeColors : groupingLevel.Value;

                // Determine which groups haven't been colored yet.
                var uncoloredGroups = groupings.Where(group => group.Value == string.Empty);

                // Generate colors for each uncolored group.
                var colorsNeeded = groupingLevel.Key == "Theme" ? 0 : uncoloredGroups.Count();
                var colors = GenerateGroupColors(colorsNeeded);
                
                // Give each uncolored group a color.
                var colorIndex = 0;
                foreach (var group in uncoloredGroups)
                {
                    // Determine the color to use.
                    var color = groupingLevel.Key == "Theme" ? Color.FromArgb(255, 255, 255) : colors[colorIndex++];

                    // Store its value in HTML format.
                    groupings.TryUpdate(group.Key, ColorTranslator.ToHtml(color), string.Empty);
                }
            }
        }

        /// <summary>
        /// A little better than pure random, but not really.
        /// </summary>
        static List<Color> GenerateGroupColors(int colorCount)
        {
            var colors = new List<Color>();

            // Generate the minimum number of colors needed for this level.
            var random = new Random();
            for (var count = 0; count < colorCount; count++)
            {
                // Generate a color.
                var hue = count * 360 / colorCount; // Hue defines the color.
                var saturation = (random.NextDouble() * 0.6) + 0.2; // The color 'amount'.
                var lightness = (random.NextDouble() * 0.7) + 0.15; // The color brightness.

                // Convert this HSL color to RGB.
                var color = HslToColor(hue, saturation, lightness);

                // Store the color to be returned.
                colors.Add(color);
            }

            // Mix up the order of the colors.
            ShuffleColors(colors);

            return colors;
        }

        /// <summary>
        /// Fisher-Yates shuffle algorithm for mixing up the color order.
        /// </summary>
        static void ShuffleColors(IList<Color> colors)
        {
            // Work backwards, swapping the last color with a randomly selected color.
            var random = new Random();
            for (var lastIndex = colors.Count - 1; lastIndex > 0; lastIndex--) // > 0 avoids unnecessary final iteration.
            {
                // + 1 gives a chance to not move.
                var swapIndex = random.Next(lastIndex + 1);

                // Perform the swap.
                var lastColor = colors[lastIndex];
                colors[lastIndex] = colors[swapIndex];
                colors[swapIndex] = lastColor;
            }
        }

        /// <summary>
        /// .NET does not convert from HSL or HSV to RGB so this has to be done.
        /// See: http://en.wikipedia.org/wiki/File:HSV-RGB-comparison.svg
        /// </summary>
        /// <param name="hue">Represents location on a hex wheel in degrees and should be [0, Double.MaxValue].</param>
        /// <param name="saturation">Should be [0, 1].</param>
        /// <param name="lightness">Should be [0, 1].</param>
        /// <returns>A color object with the corresponding RGB values.</returns>
        static Color HslToColor(double hue, double saturation, double lightness)
        {
            // 0 = 0 degrees to 59 degrees, 1 = 60 degrees to 119 degrees, etc...
            var hueRange = Convert.ToInt32(Math.Floor(hue / 60)) % 6;

            // Get only the fractional part of hue that was cut off in hueRange.
            var hueFraction = hue / 60 - Math.Floor(hue / 60);

            // The three 'values'.
            var topValue = (lightness <= 0.5) ? (lightness * (saturation + 1)) : (lightness + saturation - lightness * saturation);
            var bottomValue = 2 * lightness - topValue;
            var risingValue = (topValue - bottomValue) * hueFraction;

            // These represent the possible values in a given range.
            var top = Convert.ToByte(255 * topValue);
            var bottom = Convert.ToByte(255 * bottomValue);
            var rising = Convert.ToByte(255 * (bottomValue + risingValue));
            var falling = Convert.ToByte(255 * (topValue - risingValue));

            // Pick from one of the 6 ranges defined in the picture.
            switch (hueRange)
            {
                case 0:
                    return Color.FromArgb(top, rising, bottom);
                case 1:
                    return Color.FromArgb(falling, top, bottom);
                case 2:
                    return Color.FromArgb(bottom, top, rising);
                case 3:
                    return Color.FromArgb(bottom, falling, top);
                case 4:
                    return Color.FromArgb(rising, bottom, top);
                case 5:
                    return Color.FromArgb(top, bottom, falling);
                default:
                    return Color.Empty; // Should never happen.
            }
        }
    }
}