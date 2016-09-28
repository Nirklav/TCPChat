using System;
using System.Drawing;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Model.Common.Dto
{
  [Serializable]
  [BinType("ColorDto")]
  public class ColorDto
  {
    [BinField("r")]
    public byte Red;

    [BinField("g")]
    public byte Green;

    [BinField("b")]
    public byte Blue;

    public ColorDto()
    {
    }

    public ColorDto(Color color)
    {
      Red = color.R;
      Green = color.G;
      Blue = color.B;
    }

    public Color ToColor()
    {
      return Color.FromArgb(Red, Green, Blue);
    }
  }
}
