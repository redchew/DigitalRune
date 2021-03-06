﻿// DigitalRune Engine - Copyright (C) DigitalRune GmbH
// This file is subject to the terms and conditions defined in
// file 'LICENSE.TXT', which is part of this source code package.

using DigitalRune.Geometry.Partitioning;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;


namespace DigitalRune.Geometry.Content.Pipeline
{
  /// <summary>
  /// Writes a <see cref="DynamicAabbTree{T}"/> to binary format.
  /// </summary>
  /// <typeparam name="T">The type of the items in the spatial partition.</typeparam>
  [ContentTypeWriter]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
  public class DynamicAabbTreeWriter<T> : ContentTypeWriter<DynamicAabbTree<T>>
  {
    /// <summary>
    /// Gets the assembly qualified name of the runtime target type.
    /// </summary>
    /// <param name="targetPlatform">The target platform.</param>
    /// <returns>The qualified name.</returns>
    public override string GetRuntimeType(TargetPlatform targetPlatform)
    {
      return typeof(DynamicAabbTree<T>).AssemblyQualifiedName;
    }


    /// <summary>
    /// Gets the assembly qualified name of the runtime loader for this type.
    /// </summary>
    /// <param name="targetPlatform">Name of the platform.</param>
    /// <returns>Name of the runtime loader.</returns>
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
      return typeof(DynamicAabbTreeReader<T>).AssemblyQualifiedName;
    }


    /// <summary>
    /// Compiles a strongly typed object into binary format.
    /// </summary>
    /// <param name="output">The content writer serializing the value.</param>
    /// <param name="value">The value to write.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    protected override void Write(ContentWriter output, DynamicAabbTree<T> value)
    {
      // BasePartition<T>
      output.Write(value.EnableSelfOverlaps);
      output.WriteSharedResource(value.Filter);

      // DynamicAabbTree<T>
      output.Write(value.BottomUpBuildThreshold);
      output.Write(value.EnableMotionPrediction);
      output.Write(value.MotionPrediction);
      output.Write(value.OptimizationPerFrame);
      output.Write(value.RelativeMargin);
    }
  }
}
