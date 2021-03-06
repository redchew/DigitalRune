﻿// DigitalRune Engine - Copyright (C) DigitalRune GmbH
// This file is subject to the terms and conditions defined in
// file 'LICENSE.TXT', which is part of this source code package.

using DigitalRune.Geometry.Shapes;
using Microsoft.Xna.Framework.Content;


namespace DigitalRune.Geometry.Content
{
  /// <summary>
  /// Reads an <see cref="OrthographicViewVolume"/> from binary format.
  /// </summary>
  public class OrthographicViewVolumeReader : ContentTypeReader<OrthographicViewVolume>
  {
#if !MONOGAME
    /// <summary>
    /// Determines if deserialization into an existing object is possible.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the type can be deserialized into an existing instance; 
    /// <see langword="false"/> otherwise.
    /// </value>
    public override bool CanDeserializeIntoExistingObject
    {
      get { return true; }
    }
#endif


    /// <summary>
    /// Reads a strongly typed object from the current stream.
    /// </summary>
    /// <param name="input">The <see cref="ContentReader"/> used to read the object.</param>
    /// <param name="existingInstance">An existing object to read into.</param>
    /// <returns>The type of object to read.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    protected override OrthographicViewVolume Read(ContentReader input, OrthographicViewVolume existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new OrthographicViewVolume();

      float left = input.ReadSingle();
      float right = input.ReadSingle();
      float bottom = input.ReadSingle();
      float top = input.ReadSingle();
      float near = input.ReadSingle();
      float far = input.ReadSingle();
      existingInstance.Set(left, right, bottom, top, near, far);

      return existingInstance;
    }
  }
}
