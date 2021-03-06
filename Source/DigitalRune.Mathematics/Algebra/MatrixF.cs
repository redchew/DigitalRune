// DigitalRune Engine - Copyright (C) DigitalRune GmbH
// This file is subject to the terms and conditions defined in
// file 'LICENSE.TXT', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
#if !NETFX_CORE && !SILVERLIGHT && !WP7 && !WP8 && !XBOX && !UNITY && !PORTABLE
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
#endif


namespace DigitalRune.Mathematics.Algebra
{
  /// <summary>
  /// Defines an m x n matrix (single-precision).
  /// </summary>
  /// <remarks>
  /// <para>
  /// All indices are zero-based. The first index is the row, the second is the column:
  /// <code>
  /// [0,0] [0,1] [0,2] ...
  /// [1,0] [1,1] [1,2] ...
  /// [2,0] [2,1] [2,2] ...
  /// ...   ...   ...   ...
  /// </code>
  /// </para>
  /// </remarks>
#if !NETFX_CORE && !SILVERLIGHT && !WP7 && !WP8 && !XBOX && !UNITY && !PORTABLE
  [Serializable]
  [TypeConverter(typeof(ExpandableObjectConverter))]
#endif
  public class MatrixF 
    : IEquatable<MatrixF>, 
#if !NETFX_CORE && !SILVERLIGHT && !WP7 && !WP8 && !XBOX && !UNITY && !PORTABLE
      ISerializable,
#endif
      IXmlSerializable
  {
    // TODO: Remove ArgumentNullException and let runtime throw NullReferenceException. (Minor optimization)

    //--------------------------------------------------------------
    #region Fields
    //--------------------------------------------------------------

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    private float[,] _m;
    #endregion


    //--------------------------------------------------------------
    #region Properties
    //--------------------------------------------------------------

    ///// <summary>
    ///// Gets or sets the internal array that is used to store the vector values.
    ///// </summary>
    ///// <value>The internal array that is used to store the vector values; must not be <see langword="null"/>.</value>
    //public float[,] InternalArray
    //{
    //  get { return _m; }
    //  set
    //  {
    //    if (value == null)
    //      throw new ArgumentNullException();
    //    _m = value;
    //  }
    //}


    /// <overloads>
    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <value>The element at <paramref name="index"/>.</value>
    /// <remarks>
    /// The matrix elements are in row-major order.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// The <paramref name="index"/> is out of range.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
    public float this[int index]
    {
      get
      {
        if (index < 0 || index >= NumberOfRows * NumberOfColumns)
          throw new IndexOutOfRangeException("1-dimensional index for matrix is out of range.");

        return _m[index / NumberOfColumns, index % NumberOfColumns];
      }
      set
      {
        if (index < 0 || index >= NumberOfRows * NumberOfColumns)
          throw new IndexOutOfRangeException("1-dimensional index for matrix is out of range.");

        _m[index / NumberOfColumns, index % NumberOfColumns] = value;
      }
    }


    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <value>The element at the specified row and column.</value>
    /// <remarks>
    /// The indices are zero-based: [0,0] is the first element, [2,2] is the last element.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// The index [<paramref name="row"/>, <paramref name="column"/>] is out of range.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
    public float this[int row, int column]
    {
      get { return _m[row, column]; }
      set { _m[row, column] = value; }
    }


    /// <summary>
    /// Returns the determinant of this matrix.
    /// </summary>
    /// <value>The determinant of this matrix.</value>
    /// <exception cref="MathematicsException">
    /// Matrix is not a square matrix.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
    public float Determinant
    {
      get
      {
        if (!IsSquare)
          throw new MathematicsException("The matrix is not a square matrix.");
        
        return new LUDecompositionF(this).Determinant;
      }
    }


    /// <summary>
    /// Returns the inverse or pseudo-inverse of this matrix.
    /// </summary>
    /// <value>The inverse or pseudo-inverse of this matrix.</value>
    /// <remarks>
    /// <para>
    /// The property does not change this instance. To invert this instance you need to call 
    /// <see cref="Invert"/>.
    /// </para>
    /// <para>
    /// If this matrix is square, the inverse is returned; otherwise the pseudo-inverse.
    /// </para>
    /// </remarks>
    /// <exception cref="MathematicsException">
    /// The matrix is singular (i.e. it is not invertible).
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
    public MatrixF Inverse
    {
      get
      {
        try
        {
          return SolveLinearEquations(this, CreateIdentity(NumberOfRows, NumberOfRows));
        }
        catch (Exception exception)
        {
          throw new MathematicsException("Matrix is singular (i.e. it is not invertible).", exception);
        }
      }
    }


    /// <summary>
    /// Gets a value indicating whether a component of the vector is <see cref="float.NaN"/>.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if a component of the vector is <see cref="float.NaN"/>; otherwise, 
    /// <see langword="false"/>.
    /// </value>
    public bool IsNaN
    {
      get
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            if (Numeric.IsNaN(_m[r, c]))
              return true;

        return false;
      }
    }


    /// <summary>
    /// Gets a value indicating whether this matrix is a square matrix (number of rows is equal to 
    /// number of columns).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this matrix is a square matrix; otherwise, 
    /// <see langword="false"/>.
    /// </value>
    public bool IsSquare
    {
      get { return NumberOfRows == NumberOfColumns; }
    }


    /// <summary>
    /// Gets a value indicating whether this matrix is symmetric.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this matrix is symmetric; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// The matrix elements are compared for equality - no tolerance value to handle numerical
    /// errors is used.
    /// </remarks>
    public bool IsSymmetric
    {
      get
      {
        if (NumberOfColumns != NumberOfRows)
          return false;

        for (int i = 0; i < NumberOfRows; i++)
          for (int j = 0; j < i; j++)
            if (_m[i, j] != _m[j, i])
              return false;

        return true;
      }
    }


    /// <summary>
    /// Gets the one norm of this matrix.
    /// </summary>
    /// <value>The one norm of this matrix.</value>
    /// <remarks>
    /// The one norm is the maximum column sum.
    /// </remarks>
    public float Norm1
    {
      get
      {
        float f = 0;
        for (int c = 0; c < NumberOfColumns; c++)
        {
          float s = 0;
          for (int r = 0; r < NumberOfRows; r++)
            s += Math.Abs(_m[r, c]);

          f = Math.Max(f, s);
        }

        return f;
      }
    }


    /// <summary>
    /// Gets the Frobenius norm of this matrix.
    /// </summary>
    /// <value>The Frobenius norm of this matrix.</value>
    /// <remarks>
    /// The Frobenius norm is the square root of the sum of squares of all elements.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public float NormFrobenius
    {
      get
      {
        float f = 0;
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            f = MathHelper.Hypotenuse(f, _m[r, c]);

        return f;
      }
    }


    /// <summary>
    /// Gets the infinity norm of this matrix.
    /// </summary>
    /// <value>The infinity norm of this matrix.</value>
    /// <remarks>
    /// The infinity norm is the maximum row sum.
    /// </remarks>
    public float NormInfinity
    {
      get
      {
        float f = 0;
        for (int r = 0; r < NumberOfRows; r++)
        {
          float s = 0;
          for (int c = 0; c < NumberOfColumns; c++)
            s += Math.Abs(_m[r, c]);

          f = Math.Max(f, s);
        }

        return f;
      }
    }


    /// <summary>
    /// Gets the number of columns <i>n</i>.
    /// </summary>
    /// <value>The number of columns <i>n</i>.</value>
    public int NumberOfColumns
    {
      get { return _m.GetLength(1); }
    }


    /// <summary>
    /// Gets the number of rows <i>m</i>.
    /// </summary>
    /// <value>The number of rows <i>m</i>.</value>
    public int NumberOfRows
    {
      get { return _m.GetLength(0); }
    }


    /// <summary>
    /// Gets the matrix trace (the sum of the diagonal elements).
    /// </summary>
    /// <value>The matrix trace.</value>
    public float Trace
    {
      get
      {
        int diagonalLength = Math.Min(NumberOfRows, NumberOfColumns);
        float result = 0;
        for (int i = 0; i < diagonalLength; i++)
          result += _m[i, i];

        return result;
      }
    }


    /// <summary>
    /// Returns the transposed of this matrix.
    /// </summary>
    /// <returns>The transposed of this matrix.</returns>
    /// <remarks>
    /// The property does not change this instance. To transpose this instance you need to call 
    /// <see cref="Transpose"/>.
    /// </remarks>
    public MatrixF Transposed
    {
      get
      {
        MatrixF result = Clone();
        result.Transpose();
        return result;
      }
    }
    #endregion


    //--------------------------------------------------------------
    #region Creation & Cleanup
    //--------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class with 4 x 4 matrix elements.
    /// </summary>
    /// <remarks>
    /// <strong>Note:</strong> This constructor is used for serialization. Normally, the other 
    /// constructors should be used.
    /// </remarks>
    public MatrixF()
      : this(4, 4)
    {
    }


    /// <overloads>
    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// <param name="numberOfRows">The number of rows.</param>
    /// <param name="numberOfColumns">The number of columns.</param>
    /// <remarks>
    /// All matrix elements are set to 0.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="numberOfRows"/> or <paramref name="numberOfColumns"/> is negative or 0.
    /// </exception>
    public MatrixF(int numberOfRows, int numberOfColumns)
    {
      if (numberOfRows <= 0)
        throw new ArgumentException("The number of rows must be greater than 0.", "numberOfRows");
      if (numberOfColumns <= 0)
        throw new ArgumentException("The number of columns must be greater than 0.", "numberOfColumns");

      _m = new float[numberOfRows, numberOfColumns];
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// Each element is set to <paramref name="value"/>.
    /// </summary>
    /// <param name="numberOfRows">The number of rows.</param>
    /// <param name="numberOfColumns">The number of columns.</param>
    /// <param name="value">The initial value for the matrix elements.</param>
    /// <remarks>
    /// All matrix elements are set to <paramref name="value"/>.
    /// </remarks>
    public MatrixF(int numberOfRows, int numberOfColumns, float value)
      : this(numberOfRows, numberOfColumns)
    {
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = value;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// <param name="numberOfRows">The number of rows <i>m</i>.</param>
    /// <param name="numberOfColumns">The number of columns <i>n</i>.</param>
    /// <param name="elements">The array with the initial values for the matrix elements.</param>
    /// <param name="order">The order of the matrix elements in <paramref name="elements"/>.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="elements"/> has less than <i>m</i> x <i>n</i> elements.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// <paramref name="elements"/> must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public MatrixF(int numberOfRows, int numberOfColumns, float[] elements, MatrixOrder order)
      : this(numberOfRows, numberOfColumns)
    {
      if (order == MatrixOrder.RowMajor)
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            _m[r, c] = elements[r * NumberOfColumns + c];
      }
      else
      {
        for (int c = 0; c < NumberOfColumns; c++)
          for (int r = 0; r < NumberOfRows; r++)
            _m[r, c] = elements[c * NumberOfRows + r];
      }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// <param name="numberOfRows">The number of rows <i>m</i>.</param>
    /// <param name="numberOfColumns">The number of columns <i>n</i>.</param>
    /// <param name="elements">The list with the initial values for the matrix elements.</param>
    /// <param name="order">The order of the matrix elements in <paramref name="elements"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="elements"/> has less than <i>m</i> x <i>n</i> elements.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// <paramref name="elements"/> must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public MatrixF(int numberOfRows, int numberOfColumns, IList<float> elements, MatrixOrder order)
      : this(numberOfRows, numberOfColumns)
    {
      if (order == MatrixOrder.RowMajor)
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            _m[r, c] = elements[r * NumberOfColumns + c];
      }
      else
      {
        for (int c = 0; c < NumberOfColumns; c++)
          for (int r = 0; r < NumberOfRows; r++)
            _m[r, c] = elements[c * NumberOfRows + r];
      }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// <param name="elements">The array with the initial values for the matrix elements.</param>
    /// <remarks>
    /// The matrix will have the same dimensions <i>m</i> x <i>n</i> as <paramref name="elements"/>.
    /// </remarks>
    /// <exception cref="NullReferenceException">
    /// <paramref name="elements"/> must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    [CLSCompliant(false)]
    public MatrixF(float[,] elements)
    {
      _m = new float[elements.GetLength(0), elements.GetLength(1)];
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = elements[r, c];
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class.
    /// </summary>
    /// <param name="elements">The array with the initial values for the matrix elements.</param>
    /// <remarks>
    /// <paramref name="elements"/>.Length determines the number of rows. 
    /// <paramref name="elements"/>[0].Length determines the number of columns
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// An array in <paramref name="elements"/> has less elements than the first array in 
    /// <paramref name="elements"/>.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// <paramref name="elements"/> or the arrays in elements[0] must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public MatrixF(float[][] elements)
    {
      _m = new float[elements.Length, elements[0].Length];
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = elements[r][c];
    }


#if !NETFX_CORE && !SILVERLIGHT && !WP7 && !WP8 && !XBOX && !UNITY && !PORTABLE
    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixF"/> class with serialized data.
    /// </summary>
    /// <param name="info">The object that holds the serialized object data.</param>
    /// <param name="context">The contextual information about the source or destination.</param>
    /// <exception cref="SerializationException">
    /// Couldn't deserialize <see cref="MatrixF"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly")]
    protected MatrixF(SerializationInfo info, StreamingContext context)
    {
      try
      {
        int m = info.GetInt32("Rows");
        int n = info.GetInt32("Columns");
        float[] elements = (float[]) info.GetValue("Elements", typeof(float[]));

        _m = new float[m, n];
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            _m[r, c] = elements[r * NumberOfColumns + c];
      }
      catch (Exception exception)
      {
        throw new SerializationException("Couldn't deserialize MatrixF.", exception);
      }
    }
#endif
    #endregion


    //--------------------------------------------------------------
    #region Interfaces and Overrides
    //--------------------------------------------------------------

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
      unchecked
      {
        int hashCode = 0;
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            hashCode = (hashCode * 397) ^ _m[r, c].GetHashCode();

        return hashCode;
      }
    }


    /// <overloads>
    /// <summary>
    /// Indicates whether the current object is equal to another object.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">Another object to compare to.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> and this instance are the same type and
    /// represent the same value; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
    {
      MatrixF m = obj as MatrixF;
      if (m == null)
        return false;

      return this == m;
    }


    #region IEquatable<MatrixF> Members
    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true"/> if the current object is equal to the other parameter; otherwise, 
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(MatrixF other)
    {
      return this == other;
    }
    #endregion


    /// <overloads>
    /// <summary>
    /// Returns the string representation of this matrix.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Returns the string representation of this matrix.
    /// </summary>
    /// <returns>The string representation of this matrix.</returns>
    public override string ToString()
    {
      return ToString(CultureInfo.CurrentCulture);
    }


    /// <summary>
    /// Returns the string representation of this matrix using the specified culture-specific format
    /// information.
    /// </summary>
    /// <param name="provider">
    /// An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.
    /// </param>
    /// <returns>The string representation of this matrix.</returns>
    public string ToString(IFormatProvider provider)
    {
      StringBuilder sb = new StringBuilder();
      for (int r = 0; r < NumberOfRows; r++)
      {
        sb.Append("(");
        for (int c = 0; c < NumberOfColumns; c++)
        {
          sb.Append(_m[r, c]);
          if (c + 1 < NumberOfColumns)
            sb.Append("; ");
        }
        sb.Append(")\n");
      }

      return string.Format(provider, sb.ToString());
    }


    #region ISerializable Members

#if !NETFX_CORE && !SILVERLIGHT && !WP7 && !WP8 && !XBOX && !UNITY && !PORTABLE
    /// <summary>
    /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target 
    /// object.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
    /// <param name="context">
    /// The destination (see <see cref="StreamingContext"/>) for this serialization.
    /// </param>
    /// <exception cref="SecurityException">
    /// The caller does not have the required permission.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Rows", NumberOfRows);
      info.AddValue("Columns", NumberOfColumns);
      info.AddValue("Elements", ToArray1D(MatrixOrder.RowMajor));
    }


    /// <summary>
    /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target 
    /// object.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
    /// <param name="context">
    /// The destination (see <see cref="StreamingContext"/>) for this serialization.
    /// </param>
    /// <exception cref="SecurityException">
    /// The caller does not have the required permission.
    /// </exception>
    [SecurityCritical]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info");

      GetObjectData(info, context);
    }
#endif

    #endregion


    #region IXmlSerializable Members

    /// <summary>
    /// This property is reserved, apply the <see cref="XmlSchemaProviderAttribute"/> to the class 
    /// instead.
    /// </summary>
    /// <returns>
    /// An <see cref="XmlSchema"/> that describes the XML representation of the object that is 
    /// produced by the <see cref="IXmlSerializable.WriteXml(XmlWriter)"/> method and consumed by
    /// the <see cref="IXmlSerializable.ReadXml(XmlReader)"/> method.
    /// </returns>
    public XmlSchema GetSchema()
    {
      return null;
    }


    /// <summary>
    /// Generates an object from its XML representation.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="XmlReader"/> stream from which the object is deserialized.
    /// </param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    public void ReadXml(XmlReader reader)
    {
      reader.ReadStartElement();
      reader.ReadStartElement("Rows");
      int m = reader.ReadContentAsInt();
      reader.ReadEndElement();

      reader.ReadStartElement("Columns");
      int n = reader.ReadContentAsInt();
      reader.ReadEndElement();

      reader.ReadStartElement("Elements");
      _m = new float[m, n];
      for (int r = 0; r < m; r++)
        for (int c = 0; c < n; c++)
          _m[r, c] = reader.ReadElementContentAsFloat();

      reader.ReadEndElement();
      reader.ReadEndElement();
    }


    /// <summary>
    /// Converts an object into its XML representation.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="XmlWriter"/> stream to which the object is serialized.
    /// </param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public void WriteXml(XmlWriter writer)
    {
      writer.WriteStartElement("Rows");
      writer.WriteValue(NumberOfRows);
      writer.WriteEndElement();

      writer.WriteStartElement("Columns");
      writer.WriteValue(NumberOfColumns);
      writer.WriteEndElement();

      writer.WriteStartElement("Elements");
      for (int r = 0; r < NumberOfRows; r++)
      {
        for (int c = 0; c < NumberOfColumns; c++)
        {
          writer.WriteStartElement("E");
          writer.WriteValue(_m[r, c]);
          writer.WriteEndElement();
        }
      }
      writer.WriteEndElement();
    }
    #endregion
    #endregion


    //--------------------------------------------------------------
    #region Overloaded Operators
    //--------------------------------------------------------------

    /// <summary>
    /// Negates a matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The negated matrix.</returns>
    /// <remarks>
    /// Each element of the matrix is negated.
    /// </remarks>
    public static MatrixF operator -(MatrixF matrix)
    {
      if (matrix == null)
        return null;

      MatrixF result = matrix.Clone();
      for (int r = 0; r < result.NumberOfRows; r++)
        for (int c = 0; c < result.NumberOfColumns; c++)
          result[r, c] = -result[r, c];

      return result;
    }


    /// <summary>
    /// Negates a matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The negated matrix.</returns>
    /// <remarks>
    /// Each element of the matrix is negated.
    /// </remarks>
    public static MatrixF Negate(MatrixF matrix)
    {
      return -matrix;
    }


    /// <summary>
    /// Adds two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second Matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix1"/> or <paramref name="matrix2"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The numbers of rows or columns of the matrices do not match.
    /// </exception>
    public static MatrixF operator +(MatrixF matrix1, MatrixF matrix2)
    {
      if (matrix1 == null)
        throw new ArgumentNullException("matrix1");
      if (matrix2 == null)
        throw new ArgumentNullException("matrix2");

      CheckMatrixDimensions(matrix1, matrix2);

      MatrixF result = new MatrixF(matrix1.NumberOfRows, matrix1.NumberOfColumns);
      for (int r = 0; r < result.NumberOfRows; r++)
        for (int c = 0; c < result.NumberOfColumns; c++)
          result[r, c] = matrix1[r, c] + matrix2[r, c];

      return result;
    }


    /// <summary>
    /// Adds two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second Matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix1"/> or <paramref name="matrix2"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The numbers of rows or columns of the matrices do not match.
    /// </exception>
    public static MatrixF Add(MatrixF matrix1, MatrixF matrix2)
    {
      return matrix1 + matrix2;
    }


    /// <summary>
    /// Subtracts two matrices.
    /// </summary>
    /// <param name="minuend">The first matrix (minuend).</param>
    /// <param name="subtrahend">The second matrix (subtrahend).</param>
    /// <returns>The difference of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minuend"/> or <paramref name="subtrahend"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The numbers of rows or columns of the matrices do not match.
    /// </exception>
    public static MatrixF operator -(MatrixF minuend, MatrixF subtrahend)
    {
      if (minuend == null)
        throw new ArgumentNullException("minuend");
      if (subtrahend == null)
        throw new ArgumentNullException("subtrahend");

      CheckMatrixDimensions(minuend, subtrahend);

      MatrixF result = new MatrixF(minuend.NumberOfRows, subtrahend.NumberOfColumns);
      for (int r = 0; r < result.NumberOfRows; r++)
        for (int c = 0; c < result.NumberOfColumns; c++)
          result[r, c] = minuend[r, c] - subtrahend[r, c];

      return result;
    }


    /// <summary>
    /// Subtracts two matrices.
    /// </summary>
    /// <param name="minuend">The first matrix (minuend).</param>
    /// <param name="subtrahend">The second matrix (subtrahend).</param>
    /// <returns>The difference of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="minuend"/> or <paramref name="subtrahend"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The numbers of rows or columns of the matrices do not match.
    /// </exception>
    public static MatrixF Subtract(MatrixF minuend, MatrixF subtrahend)
    {
      return minuend - subtrahend;
    }


    /// <overloads>
    /// <summary>
    /// Multiplies a matrix by a scalar, matrix or vector.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Multiplies a matrix and a scalar.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="scalar">The scalar.</param>
    /// <returns>The matrix with each element multiplied by <paramref name="scalar"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    public static MatrixF operator *(MatrixF matrix, float scalar)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");

      MatrixF result = new MatrixF(matrix.NumberOfRows, matrix.NumberOfColumns);
      for (int r = 0; r < result.NumberOfRows; r++)
        for (int c = 0; c < result.NumberOfColumns; c++)
          result[r, c] = matrix[r, c] * scalar;

      return result;
    }


    /// <summary>
    /// Multiplies a matrix by a scalar.
    /// </summary>
    /// <param name="scalar">The scalar.</param>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The matrix with each element multiplied by <paramref name="scalar"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    public static MatrixF operator *(float scalar, MatrixF matrix)
    {
      return matrix * scalar;
    }


    /// <overloads>
    /// <summary>
    /// Multiplies a matrix by a scalar, matrix or vector.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Multiplies a matrix by a scalar.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="scalar">The scalar.</param>
    /// <returns>The matrix with each element multiplied by <paramref name="scalar"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    public static MatrixF Multiply(float scalar, MatrixF matrix)
    {
      return matrix * scalar;
    }


    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>The matrix with the product the two matrices.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix1"/> or <paramref name="matrix2"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The matrix dimensions are not suitable for a matrix multiplication.
    /// </exception>
    public static MatrixF operator *(MatrixF matrix1, MatrixF matrix2)
    {
      if (matrix1 == null)
        throw new ArgumentNullException("matrix1");
      if (matrix2 == null)
        throw new ArgumentNullException("matrix2");

      if (matrix1.NumberOfColumns != matrix2.NumberOfRows)
        throw new ArgumentException("The matrix dimensions are not suitable for a matrix multiplication.");

      MatrixF product = new MatrixF(matrix1.NumberOfRows, matrix2.NumberOfColumns);
      for (int i = 0; i < product.NumberOfRows; i++)
      {
        for (int j = 0; j < product.NumberOfColumns; j++)
        {
          // Compute product[i, j].
          for (int k = 0; k < matrix1.NumberOfColumns; k++)
            product[i, j] += matrix1[i, k] * matrix2[k, j];
        }
      }

      return product;
    }


    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>The matrix with the product the two matrices.</returns>
    public static MatrixF Multiply(MatrixF matrix1, MatrixF matrix2)
    {
      return matrix1 * matrix2;
    }


    /// <summary>
    /// Multiplies a matrix with a column vector.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="vector">The column vector.</param>
    /// <returns>The resulting column vector.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> or <paramref name="vector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The matrix and vector dimensions are not suitable for multiplication.
    /// </exception>
    public static VectorF operator *(MatrixF matrix, VectorF vector)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");
      if (vector == null)
        throw new ArgumentNullException("vector");

      if (matrix.NumberOfColumns != vector.NumberOfElements)
        throw new ArgumentException("The matrix and vector dimensions are not suitable for multiplication.");

      VectorF product = new VectorF(matrix.NumberOfRows);
      for (int i = 0; i < product.NumberOfElements; i++)
        for (int k = 0; k < matrix.NumberOfColumns; k++)
          product[i] += matrix[i, k] * vector[k];

      return product;
    }


    /// <summary>
    /// Multiplies a matrix with a column vector.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="vector">The column vector.</param>
    /// <returns>The resulting column vector.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> or <paramref name="vector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The matrix and vector dimensions are not suitable for multiplication.
    /// </exception>
    public static VectorF Multiply(MatrixF matrix, VectorF vector)
    {
      return matrix * vector;
    }


    /// <summary>
    /// Divides a matrix by a scalar.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="scalar">The scalar.</param>
    /// <returns>The matrix with each element divided by scalar.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    public static MatrixF operator /(MatrixF matrix, float scalar)
    {
      float f = 1 / scalar;
      return matrix * f;
    }


    /// <summary>
    /// Divides a matrix by a scalar.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="scalar">The scalar.</param>
    /// <returns>The matrix with each element divided by scalar.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    public static MatrixF Divide(MatrixF matrix, float scalar)
    {
      float f = 1 / scalar;
      return matrix * f;
    }


    /// <summary>
    /// Tests if two matrices are equal.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>
    /// <see langword="true"/> if the matrices are equal; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// For the test the matrix dimensions and the corresponding elements of the matrices are 
    /// compared.
    /// </remarks>
    public static bool operator ==(MatrixF matrix1, MatrixF matrix2)
    {
      if (ReferenceEquals(matrix1, matrix2))
        return true;
      if (ReferenceEquals(matrix1, null) || ReferenceEquals(matrix2, null))
        return false;

      if (matrix1.NumberOfColumns != matrix2.NumberOfColumns)
        return false;
      if (matrix1.NumberOfRows != matrix2.NumberOfRows)
        return false;

      for (int r = 0; r < matrix1.NumberOfRows; r++)
        for (int c = 0; c < matrix1.NumberOfColumns; c++)
          if (matrix1[r, c] != matrix2[r, c])
            return false;

      return true;
    }


    /// <summary>
    /// Tests if two matrices are not equal.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>
    /// <see langword="true"/> if the matrices are different; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// For the test the corresponding elements of the matrices are compared.
    /// </remarks>
    public static bool operator !=(MatrixF matrix1, MatrixF matrix2)
    {
      return !(matrix1 == matrix2);
    }


    /// <overloads>
    /// <summary>
    /// Converts a matrix to another data type.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Performs an explicit conversion from <see cref="MatrixF"/> to a 2-dimensional 
    /// <see langword="float"/> array.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    public static explicit operator float[,](MatrixF matrix)
    {
      if (matrix == null)
        return null;

      float[,] result = new float[matrix.NumberOfRows, matrix.NumberOfColumns];
      for (int r = 0; r < matrix.NumberOfRows; r++)
        for (int c = 0; c < matrix.NumberOfColumns; c++)
          result[r, c] = matrix[r, c];

      return result;
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to a 2-dimensional <see langword="float"/> array.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    public float[,] ToArray2D()
    {
      return (float[,]) this;
    }


    /// <summary>
    /// Performs an explicit conversion from <see cref="MatrixF"/> to jagged <see langword="float"/>
    /// array.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator float[][](MatrixF matrix)
    {
      if (matrix == null)
        return null;

      float[][] result = new float[matrix.NumberOfRows][];
      for (int r = 0; r < matrix.NumberOfRows; r++)
      {
        result[r] = new float[matrix.NumberOfColumns];
        for (int c = 0; c < matrix.NumberOfColumns; c++)
          result[r][c] = matrix[r, c];
      }

      return result;
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to a jagged <see langword="float"/> array.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    public float[][] ToArrayJagged()
    {
      return (float[][]) this;
    }


    /// <summary>
    /// Performs an explicit conversion from <see cref="MatrixF"/> to <see cref="Matrix22F"/>.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// <paramref name="matrix"/> is not 2x2 matrix.
    /// </exception>
    public static explicit operator Matrix22F(MatrixF matrix)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");
      if (matrix.NumberOfRows != 2)
        throw new InvalidCastException("The number of rows does not match.");
      if (matrix.NumberOfColumns != 2)
        throw new InvalidCastException("The number of columns does not match.");

      return new Matrix22F(matrix[0, 0], matrix[0, 1], 
                           matrix[1, 0], matrix[1, 1]);
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to <see cref="Matrix22F"/>.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="InvalidCastException">
    /// This matrix is not 2x2 matrix.
    /// </exception>
    public Matrix22F ToMatrix22F()
    {
      return (Matrix22F) this;
    }


    /// <summary>
    /// Performs an explicit conversion from <see cref="MatrixF"/> to <see cref="Matrix33F"/>.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// <paramref name="matrix"/> is not 3x3 matrix.
    /// </exception>
    public static explicit operator Matrix33F(MatrixF matrix)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");
      if (matrix.NumberOfRows != 3)
        throw new InvalidCastException("The number of rows does not match.");
      if (matrix.NumberOfColumns != 3)
        throw new InvalidCastException("The number of columns does not match.");

      return new Matrix33F(matrix[0, 0], matrix[0, 1], matrix[0, 2],
                           matrix[1, 0], matrix[1, 1], matrix[1, 2],
                           matrix[2, 0], matrix[2, 1], matrix[2, 2]);
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to <see cref="Matrix33F"/>.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="InvalidCastException">
    /// The matrix is not 3x3 matrix.
    /// </exception>
    public Matrix33F ToMatrix33F()
    {
      return (Matrix33F) this;
    }


    /// <summary>
    /// Performs an explicit conversion from <see cref="MatrixF"/> to <see cref="Matrix44F"/>.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// <paramref name="matrix"/> is not 4x4 matrix.
    /// </exception>
    public static explicit operator Matrix44F(MatrixF matrix)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");
      if (matrix.NumberOfRows != 4)
        throw new InvalidCastException("The number of rows does not match.");
      if (matrix.NumberOfColumns != 4)
        throw new InvalidCastException("The number of columns does not match.");

      return new Matrix44F(matrix[0, 0], matrix[0, 1], matrix[0, 2], matrix[0, 3],
                           matrix[1, 0], matrix[1, 1], matrix[1, 2], matrix[1, 3],
                           matrix[2, 0], matrix[2, 1], matrix[2, 2], matrix[2, 3],
                           matrix[3, 0], matrix[3, 1], matrix[3, 2], matrix[3, 3]);
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to <see cref="Matrix44F"/>.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="InvalidCastException">
    /// This matrix is not 4x4 matrix.
    /// </exception>
    public Matrix44F ToMatrix44F()
    {
      return (Matrix44F) this;
    }


    /// <summary>
    /// Performs an implicit conversion from <see cref="MatrixF"/> to <see cref="MatrixD"/>.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator MatrixD(MatrixF matrix)
    {
      if (matrix == null)
        return null;

      MatrixD matrixD = new MatrixD(matrix.NumberOfRows, matrix.NumberOfColumns);
      for (int r = 0; r < matrix.NumberOfRows; r++)
        for (int c = 0; c < matrix.NumberOfColumns; c++)
          matrixD[r, c] = matrix[r, c];

      return matrixD;
    }


    /// <summary>
    /// Converts this <see cref="MatrixF"/> to <see cref="MatrixD"/>.
    /// </summary>
    /// <returns>The result of the conversion.</returns>
    public MatrixD ToMatrixD()
    {
      return this;
    }
    #endregion


    //--------------------------------------------------------------
    #region Methods
    //--------------------------------------------------------------

    /// <overloads>
    /// <summary>
    /// Sets each matrix element to its absolute value.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Sets each matrix element to its absolute value.
    /// </summary>
    public void Absolute()
    {
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = Math.Abs(_m[r, c]);
    }


    /// <overloads>
    /// <summary>
    /// Clamps near-zero matrix elements to zero.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Clamps near-zero matrix elements to zero.
    /// </summary>
    /// <remarks>
    /// Each matrix element is compared to zero. If the element is in the interval 
    /// [-<see cref="Numeric.EpsilonF"/>, +<see cref="Numeric.EpsilonF"/>] it is set to zero, 
    /// otherwise it remains unchanged.
    /// </remarks>
    public void ClampToZero()
    {
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = Numeric.ClampToZero(_m[r, c]);
    }


    /// <summary>
    /// Clamps near-zero matrix elements to zero.
    /// </summary>
    /// <param name="epsilon">The tolerance value.</param>
    /// <remarks>
    /// Each matrix element is compared to zero. If the element is in the interval 
    /// [-<paramref name="epsilon"/>, +<paramref name="epsilon"/>] it is set to zero, otherwise it 
    /// remains unchanged.
    /// </remarks>
    public void ClampToZero(float epsilon)
    {
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = Numeric.ClampToZero(_m[r, c], epsilon);
    }


    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>A copy of this instance.</returns>
    public MatrixF Clone()
    {
      return new MatrixF(_m);
    }


    /// <summary>
    /// Gets the column with the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The column with the given index.</returns>
    public VectorF GetColumn(int index)
    {
      VectorF column = new VectorF(NumberOfRows);
      for (int r = 0; r < NumberOfRows; r++)
        column[r] = _m[r, index];

      return column;
    }


    /// <summary>
    /// Sets a column.
    /// </summary>
    /// <param name="index">The index of the column.</param>
    /// <param name="columnVector">The column vector.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// The <paramref name="index"/> is out of range or <paramref name="columnVector"/> has to few
    /// elements.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// <paramref name="columnVector"/> must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public void SetColumn(int index, VectorF columnVector)
    {
      for (int r = 0; r < NumberOfRows; r++)
        _m[r, index] = columnVector[r];
    }


    /// <summary>
    /// Gets the row with the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The row with the given index.</returns>
    public VectorF GetRow(int index)
    {
      VectorF row = new VectorF(NumberOfColumns);
      for (int c = 0; c < NumberOfColumns; c++)
        row[c] = _m[index, c];

      return row;
    }


    /// <summary>
    /// Sets a row.
    /// </summary>
    /// <param name="index">The index of the row.</param>
    /// <param name="rowVector">The row vector.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// The <paramref name="index"/> is out of range or <paramref name="rowVector"/> has to few
    /// elements.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// <paramref name="rowVector"/> must not be <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
    public void SetRow(int index, VectorF rowVector)
    {
      for (int c = 0; c < NumberOfColumns; c++)
        _m[index, c] = rowVector[c];
    }


    /// <summary>
    /// Gets the minor matrix.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <returns>The minor matrix.</returns>
    /// <remarks>
    /// The minor matrix is built by removing the given <paramref name="row"/> and the given
    /// <paramref name="column"/>.
    /// </remarks>
    /// <exception cref="MathematicsException">
    /// Cannot get the minor matrix of a 1x1 matrix.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="row"/> or <paramref name="column"/> is out of range.
    /// </exception>
    public MatrixF GetMinor(int row, int column)
    {
      if (NumberOfRows <= 1 || NumberOfColumns <= 1)
        throw new MathematicsException("Cannot get the minor matrix of 1x1 matrix.");
      if (row >= NumberOfRows)
        throw new ArgumentOutOfRangeException("row");
      if (column >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("column");

      MatrixF minor = new MatrixF(NumberOfRows - 1, NumberOfColumns - 1);

      // r and c are indices for the minor matrix. i and j are indices for this matrix.
      int i = 0;
      for (int r = 0; r < NumberOfRows-1; r++)
      {
        // Skip row.
        if (r == row)
          i++;

        int j = 0;
        for (int c = 0; c < NumberOfColumns - 1; c++)
        {
          // Skip column.
          if (c == column)
            j++;

          minor[r, c] = _m[i, j];

          j++;
        }
        i++;
      }

      return minor;
    }


    /// <overloads>
    /// <summary>
    /// Gets a submatrix of this matrix.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Gets a submatrix of this matrix.
    /// </summary>
    /// <param name="startRow">The index of the start row.</param>
    /// <param name="endRow">The index of the end row (included in the submatrix).</param>
    /// <param name="startColumn">The index of the start column.</param>
    /// <param name="endColumn">The index of the end column (included in the submatrix).</param>
    /// <returns>The extracted submatrix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startRow"/> is greater than <paramref name="endRow"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startColumn"/> is greater than <paramref name="endColumn"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startRow"/>, <paramref name="endRow"/>, <paramref name="startColumn"/>, or 
    /// <paramref name="endColumn"/> is out of range.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public MatrixF GetSubmatrix(int startRow, int endRow, int startColumn, int endColumn)
    {
      if (startRow > endRow)
        throw new ArgumentOutOfRangeException("startRow", "startRow must be less than or equal to endRow.");
      if (startColumn > endColumn)
        throw new ArgumentOutOfRangeException("startColumn", "startColumn must be less than or equal to endColumn.");
      if (startRow < 0 || startRow >= NumberOfRows)
        throw new ArgumentOutOfRangeException("startRow");
      if (endRow < 0 || endRow >= NumberOfRows)
        throw new ArgumentOutOfRangeException("endRow");
      if (startColumn < 0 || startColumn >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("startColumn");
      if (endColumn < 0 || endColumn >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("endColumn");

      MatrixF result = new MatrixF(endRow - startRow + 1, endColumn - startColumn + 1);
      for (int r = startRow; r <= endRow; r++)
        for (int c = startColumn; c <= endColumn; c++)
          result[r - startRow, c - startColumn] = _m[r, c];

      return result;
    }


    /// <summary>
    /// Gets a submatrix of this matrix.
    /// </summary>
    /// <param name="startRow">The index of the start row.</param>
    /// <param name="endRow">The index of the end row (included in the submatrix).</param>
    /// <param name="columns">The indices of the columns.</param>
    /// <returns>The extracted submatrix.</returns>
    /// <remarks>
    /// The index array has to be interpreted like this: For example, <c>columns[0] = 5</c> means
    /// that column 5 of this matrix will be copied into column 0 of the submatrix.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startRow"/> is greater than <paramref name="endRow"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startRow"/> or <paramref name="endRow"/> is out of range.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// An index in <paramref name="columns"/> is invalid.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public MatrixF GetSubmatrix(int startRow, int endRow, int[] columns)
    {
      if (startRow > endRow)
        throw new ArgumentOutOfRangeException("startRow", "startRow must be less than or equal to endRow.");
      if (startRow < 0 || startRow >= NumberOfRows)
        throw new ArgumentOutOfRangeException("startRow");
      if (endRow < 0 || endRow >= NumberOfRows)
        throw new ArgumentOutOfRangeException("endRow");


      if (columns == null)
        return null;

      MatrixF result = new MatrixF(endRow - startRow + 1, columns.Length);
      for (int r = startRow; r <= endRow; r++)
        for (int c = 0; c < columns.Length; c++)
          result[r - startRow, c] = _m[r, columns[c]];

      return result;
    }


    /// <summary>
    /// Gets a submatrix of this matrix.
    /// </summary>
    /// <param name="rows">The indices of the rows.</param>
    /// <param name="startColumn">The index of the start column.</param>
    /// <param name="endColumn">The index of the end column (included in the submatrix).</param>
    /// <returns>The extracted submatrix.</returns>
    /// <remarks>
    /// The index array has to be interpreted like this: For example, <c>row[0] = 5</c> means that
    /// row 5 of this matrix will be copied into row 0 of the submatrix.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startColumn"/> is greater than <paramref name="endColumn"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startColumn"/> or <paramref name="endColumn"/> is out of range.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// An index in <paramref name="rows"/> is invalid.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public MatrixF GetSubmatrix(int[] rows, int startColumn, int endColumn)
    {
      if (startColumn > endColumn)
        throw new ArgumentOutOfRangeException("startColumn", "startColumn must be less than or equal to endColumn.");
      if (startColumn < 0 || startColumn >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("startColumn");
      if (endColumn < 0 || endColumn >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("endColumn");

      if (rows == null)
        return null;

      MatrixF result = new MatrixF(rows.Length, endColumn - startColumn + 1);
      for (int r = 0; r < rows.Length; r++)
        for (int c = startColumn; c <= endColumn; c++)
          result[r, c - startColumn] = _m[rows[r], c];

      return result;
    }


    /// <summary>
    /// Gets a submatrix of this matrix.
    /// </summary>
    /// <param name="rows">The indices of the rows.</param>
    /// <param name="columns">The indices of the columns.</param>
    /// <returns>The extracted submatrix.</returns>
    /// <remarks>
    /// The index array has to be interpreted like this: For example, <c>columns[0] = 5</c> means
    /// that column 5 of this matrix will be copied into column 0 of the submatrix.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// An index in <paramref name="rows"/> or <paramref name="columns"/> is invalid.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public MatrixF GetSubmatrix(int[] rows, int[] columns)
    {
      if (rows == null || columns == null)
        return null;

      MatrixF result = new MatrixF(rows.Length, columns.Length);
      for (int r = 0; r < rows.Length; r++)
        for (int c = 0; c < columns.Length; c++)
          result[r, c] = _m[rows[r], columns[c]];

      return result;
    }


    /// <summary>
    /// Inverts the matrix.
    /// </summary>
    /// <exception cref="MathematicsException">
    /// The matrix is singular (i.e. it is not invertible).
    /// </exception>
    public void Invert()
    {
      _m = Inverse._m;
    }


    /// <summary>
    /// Inverts the matrix if it is invertible.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the matrix is invertible; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method is the equivalent to <see cref="Invert"/>, except that no exceptions are thrown.
    /// The return value indicates whether the operation was successful.
    /// </remarks>
    public bool TryInvert()
    {
      try
      {
        Invert();
      }
      catch (MathematicsException)
      {
        return false;
      }

      return true;
    }



    /// <overloads>
    /// <summary>
    /// Sets the elements of this matrix.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Sets the elements of this matrix.
    /// </summary>
    /// <param name="matrix">The matrix from which the elements are copied.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The number of rows or columns of the matrices do not match.
    /// </exception>
    public void Set(MatrixF matrix)
    {
      if (matrix == null)
        throw new ArgumentNullException("matrix");

      CheckMatrixDimensions(this, matrix);

      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = matrix[r, c];
    }


    /// <summary>
    /// Sets the matrix elements to the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Set(float value)
    {
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = value;
    }


    /// <summary>
    /// Sets the matrix elements to the values of the array.
    /// </summary>
    /// <param name="elements">The elements array.</param>
    /// <param name="order">The order of the matrix elements in the array.</param>
    /// <remarks>
    /// <paramref name="elements"/> can have more elements than this instance. The exceeding
    /// elements are ignored.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="elements"/> must have at least
    /// <see cref="NumberOfRows"/> * <see cref="NumberOfColumns"/> elements.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="elements"/> must not be <see langword="null"/>.
    /// </exception>
    public void Set(float[] elements, MatrixOrder order)
    {
      if (elements == null)
        throw new ArgumentNullException("elements");

      if (order == MatrixOrder.RowMajor)
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            _m[r, c] = elements[r * NumberOfColumns + c];
      }
      else
      {
        for (int c = 0; c < NumberOfColumns; c++)
          for (int r = 0; r < NumberOfRows; r++)
            _m[r, c] = elements[c * NumberOfRows + r];
      }
    }



    /// <summary>
    /// Sets the matrix elements to the values of the list.
    /// </summary>
    /// <param name="elements">The elements list.</param>
    /// <param name="order">The order of the matrix elements in the list.</param>
    /// <remarks>
    /// <paramref name="elements"/> can have more elements than this instance. The exceeding 
    /// elements are ignored.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="elements"/> must have at least 
    /// <see cref="NumberOfRows"/>*<see cref="NumberOfColumns"/> elements.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="elements"/> is <see langword="null"/>.
    /// </exception>
    public void Set(IList<float> elements, MatrixOrder order)
    {
      if (elements == null)
        throw new ArgumentNullException("elements");

      if (order == MatrixOrder.RowMajor)
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            _m[r, c] = elements[r * NumberOfColumns + c];
      }
      else
      {
        for (int c = 0; c < NumberOfColumns; c++)
          for (int r = 0; r < NumberOfRows; r++)
            _m[r, c] = elements[c * NumberOfRows + r];
      }
    }


    /// <summary>
    /// Sets the matrix elements to the values of the array.
    /// </summary>
    /// <param name="elements">The elements array.</param>
    /// <remarks>
    /// <paramref name="elements"/> can have more elements than this instance. The exceeding 
    /// elements are ignored.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="elements"/> must have at least 
    /// <see cref="NumberOfRows"/> * <see cref="NumberOfColumns"/> elements.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="elements"/> is <see langword="null"/>.
    /// </exception>
    public void Set(float[][] elements)
    {
      if (elements == null)
        throw new ArgumentNullException("elements");

      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = elements[r][c];
    }


    /// <summary>
    /// Sets the matrix elements to the values of the array.
    /// </summary>
    /// <param name="elements">The elements array.</param>
    /// <remarks>
    /// <paramref name="elements"/> can have more elements than this instance. The exceeding 
    /// elements are ignored.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="elements"/> must have at least 
    /// <see cref="NumberOfRows"/> x <see cref="NumberOfColumns"/> elements.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="elements"/> is <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    [CLSCompliant(false)]
    public void Set(float[,] elements)
    {
      if (elements == null)
        throw new ArgumentNullException("elements");

      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          _m[r, c] = elements[r, c];
    }


    /// <summary>
    /// Sets this matrix to an identity matrix.
    /// </summary>
    /// <remarks>
    /// The elements in the main diagonal are set to <c>1</c>. All other elements are set to 
    /// <c>0</c>.
    /// </remarks>
    public void SetIdentity()
    {
      for (int r = 0; r < NumberOfRows; r++)
      {
        for (int c = 0; c < NumberOfColumns; c++)
        {
          if (r == c)
            _m[r, c] = 1;
          else
            _m[r, c] = 0;
        }
      }
    }


    /// <overloads>
    /// <summary>
    /// Sets a submatrix of this matrix.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Sets a submatrix of this matrix.
    /// </summary>
    /// <param name="submatrix">The submatrix.</param>
    /// <param name="startRow">The index of the start row in this matrix.</param>
    /// <param name="startColumn">The index of the start column in this matrix.</param>
    /// <remarks>
    /// The elements of the submatrix are copied into this matrix, beginning at the position 
    /// [startRow, startColumn].
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">
    /// The <paramref name="startRow"/>, <paramref name="startColumn"/> or the dimensions of the 
    /// submatrix are to high, so that the submatrix does not fit into this matrix.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startRow"/> or <paramref name="startColumn"/> out of range.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="submatrix"/> is <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public void SetSubmatrix(int startRow, int startColumn, MatrixF submatrix)
    {
      if (startRow < 0 || startRow >= NumberOfRows)
        throw new ArgumentOutOfRangeException("startRow");
      if (startColumn < 0 || startColumn >= NumberOfColumns)
        throw new ArgumentOutOfRangeException("startColumn");
      if (submatrix == null)
        throw new ArgumentNullException("submatrix");

      for (int r = 0; r < submatrix.NumberOfRows; r++)
        for (int c = 0; c < submatrix.NumberOfColumns; c++)
          _m[r + startRow, c + startColumn] = submatrix[r, c];
    }


    /// <summary>
    /// Converts this matrix to an array of <see langword="float"/> values.
    /// </summary>
    /// <param name="order">The order of the matrix elements in the array.</param>
    /// <returns>The result of the conversion.</returns>
    public float[] ToArray1D(MatrixOrder order)
    {
      float[] array = new float[NumberOfRows * NumberOfColumns];

      if (order == MatrixOrder.ColumnMajor)
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            array[c * NumberOfRows + r] = _m[r, c];
      }
      else
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            array[r * NumberOfColumns + c] = _m[r, c];
      }

      return array;
    }


    /// <summary>
    /// Converts this matrix to a list of <see langword="float"/> values.
    /// </summary>
    /// <param name="order">The order of the matrix elements in the list.</param>
    /// <returns>The result of the conversion.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
    public List<float> ToList(MatrixOrder order)
    {
      List<float> result = new List<float>(NumberOfRows * NumberOfColumns);

      if (order == MatrixOrder.ColumnMajor)
      {
        for (int c = 0; c < NumberOfColumns; c++)
          for (int r = 0; r < NumberOfRows; r++)
            result.Add(_m[r, c]);
      }
      else
      {
        for (int r = 0; r < NumberOfRows; r++)
          for (int c = 0; c < NumberOfColumns; c++)
            result.Add(_m[r, c]);
      }

      return result;
    }


    /// <summary>
    /// Transposes this matrix.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")]
    public void Transpose()
    {
      float[,] transposed = new float[NumberOfColumns, NumberOfRows];
      for (int r = 0; r < NumberOfRows; r++)
        for (int c = 0; c < NumberOfColumns; c++)
          transposed[c, r] = _m[r, c];

      _m = transposed;
    }
    #endregion


    //--------------------------------------------------------------
    #region Static Methods
    //--------------------------------------------------------------

    /// <summary>
    /// Returns a matrix with the absolute values of the elements of the given matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A matrix with the absolute values of the elements of the given matrix.</returns>
    public static MatrixF Absolute(MatrixF matrix)
    {
      if (matrix == null)
        return null;

      MatrixF result = new MatrixF(matrix.NumberOfRows, matrix.NumberOfColumns);
      for (int r = 0; r < result.NumberOfRows; r++)
        for (int c = 0; c < result.NumberOfColumns; c++)
          result[r, c] = Math.Abs(matrix[r, c]);

      return result;
    }


    /// <overloads>
    /// <summary>
    /// Determines whether two matrices are equal (regarding a given tolerance).
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Determines whether two matrices are equal (regarding the tolerance 
    /// <see cref="Numeric.EpsilonF"/>).
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>
    /// <see langword="true"/> if the matrices are equal (within the tolerance 
    /// <see cref="Numeric.EpsilonF"/>); otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix1"/> or <paramref name="matrix2"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The dimensions of the two matrices are compared and the matrices are compared 
    /// component-wise. If the differences of the components are less than 
    /// <see cref="Numeric.EpsilonF"/> the matrices are considered as being equal.
    /// </remarks>
    public static bool AreNumericallyEqual(MatrixF matrix1, MatrixF matrix2)
    {
      if (matrix1 == null && matrix2 == null)
        return true;

      if (matrix1 == null)
        throw new ArgumentNullException("matrix1");
      if (matrix2 == null)
        throw new ArgumentNullException("matrix2");

      if (matrix1.NumberOfColumns != matrix2.NumberOfColumns)
        return false;
      if (matrix1.NumberOfRows != matrix2.NumberOfRows)
        return false;

      for (int r = 0; r < matrix1.NumberOfRows; r++)
        for (int c = 0; c < matrix2.NumberOfColumns; c++)
          if (Numeric.AreEqual(matrix1[r, c], matrix2[r, c]) == false)
            return false;

      return true;
    }


    /// <summary>
    /// Determines whether two matrices are equal (regarding a specific tolerance).
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <param name="epsilon">The tolerance value.</param>
    /// <returns>
    /// <see langword="true"/> if the matrices are equal (within the tolerance 
    /// <paramref name="epsilon"/>); otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrix1"/> or <paramref name="matrix2"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The dimensions of the two matrices are compared and the matrices are compared 
    /// component-wise. If the differences of the components are less than 
    /// <paramref name="epsilon"/> the matrices are considered as being equal.
    /// </remarks>
    public static bool AreNumericallyEqual(MatrixF matrix1, MatrixF matrix2, float epsilon)
    {
      if (matrix1 == null && matrix2 == null)
        return true;

      if (matrix1 == null)
        throw new ArgumentNullException("matrix1");
      if (matrix2 == null)
        throw new ArgumentNullException("matrix2");

      if (matrix1.NumberOfColumns != matrix2.NumberOfColumns)
        return false;
      if (matrix1.NumberOfRows != matrix2.NumberOfRows)
        return false;

      for (int r = 0; r < matrix1.NumberOfRows; r++)
        for (int c = 0; c < matrix2.NumberOfColumns; c++)
          if (Numeric.AreEqual(matrix1[r, c], matrix2[r, c], epsilon) == false)
            return false;

      return true;
    }


    /// <summary>
    /// Throws exceptions if the matrix dimensions do not match.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <exception cref="ArgumentException">
    /// The numbers of rows or columns of the matrices do not match.
    /// </exception>
    private static void CheckMatrixDimensions(MatrixF matrix1, MatrixF matrix2)
    {
      if (matrix1.NumberOfRows != matrix2.NumberOfRows)
        throw new ArgumentException("The number of rows of the matrices does not match.");

      if (matrix1.NumberOfColumns != matrix2.NumberOfColumns)
        throw new ArgumentException("The number of columns of the matrices does not match.");
    }


    /// <summary>
    /// Returns a matrix with the matrix elements clamped to the range [min, max].
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The matrix with small elements clamped to zero.</returns>
    /// <remarks>
    /// Each matrix element is compared to zero. If it is in the interval 
    /// [-<see cref="Numeric.EpsilonF"/>, +<see cref="Numeric.EpsilonF"/>] it is set to zero, 
    /// otherwise it remains unchanged.
    /// </remarks>
    public static MatrixF ClampToZero(MatrixF matrix)
    {
      if (matrix == null)
        return null;

      MatrixF clampedMatrix = new MatrixF(matrix.NumberOfRows, matrix.NumberOfColumns);
      for (int r = 0; r < matrix.NumberOfRows; r++)
        for (int c = 0; c < matrix.NumberOfColumns; c++)
          clampedMatrix[r, c] = Numeric.ClampToZero(matrix[r, c]);

      return clampedMatrix;
    }


    /// <overloads>
    /// <summary>
    /// Clamps near-zero matrix elements to zero.
    /// </summary>
    /// </overloads>
    /// 
    /// <summary>
    /// Returns a matrix with the matrix elements clamped to the range [min, max].
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="epsilon">The tolerance value.</param>
    /// <returns>The matrix with small elements clamped to zero.</returns>
    /// <remarks>
    /// Each matrix element is compared to zero. If it is in the interval 
    /// [-<paramref name="epsilon"/>, +<paramref name="epsilon"/>] it is set to zero, otherwise it 
    /// remains unchanged.
    /// </remarks>
    public static MatrixF ClampToZero(MatrixF matrix, float epsilon)
    {
      if (matrix == null)
        return null;

      MatrixF clampedMatrix = new MatrixF(matrix.NumberOfRows, matrix.NumberOfColumns);
      for (int r = 0; r < matrix.NumberOfRows; r++)
        for (int c = 0; c < matrix.NumberOfColumns; c++)
          clampedMatrix[r, c] = Numeric.ClampToZero(matrix[r, c], epsilon);

      return clampedMatrix;
    }


    /// <summary>
    /// Creates an identity matrix.
    /// </summary>
    /// <param name="numberOfRows">The number of rows.</param>
    /// <param name="numberOfColumns">The number of columns.</param>
    /// <returns>An identity matrix.</returns>
    /// <remarks>
    /// Elements in the main diagonal are set to <c>1</c>. Other elements are set to <c>0</c>.
    /// </remarks>
    public static MatrixF CreateIdentity(int numberOfRows, int numberOfColumns)
    {
      MatrixF identity = new MatrixF(numberOfRows, numberOfColumns);
      int diagonalLength = Math.Min(numberOfRows, numberOfColumns);
      for (int i = 0; i < diagonalLength; i++)
        identity[i, i] = 1.0f;

      return identity;
    }


    /// <summary>
    /// Solves the linear set of equations A * X = B.
    /// </summary>
    /// <param name="matrixA">
    /// The matrix A. (Can be rectangular. Number of rows ≥ number of columns.)
    /// </param>
    /// <param name="matrixB">
    /// The matrix B with the same number of rows as A and any number of columns.
    /// </param>
    /// <returns>The matrix X.</returns>
    /// <remarks>
    /// If A is a square matrix, the X contains the solutions. If A is not a square matrix, the 
    /// least squares solutions is returned.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="matrixA"/> or <paramref name="matrixB"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The number of rows does not match.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The number of rows in <paramref name="matrixA"/> must be greater than or equal to the number 
    /// of columns.
    /// </exception>
    /// <exception cref="MathematicsException">
    /// The matrix A does not have full rank.
    /// </exception>
    public static MatrixF SolveLinearEquations(MatrixF matrixA, MatrixF matrixB)
    {
      if (matrixA == null)
        throw new ArgumentNullException("matrixA");
      if (matrixB == null)
        throw new ArgumentNullException("matrixB");

      if (matrixA.IsSquare)
        return new LUDecompositionF(matrixA).SolveLinearEquations(matrixB);

      if (matrixA.NumberOfRows > matrixA.NumberOfColumns)
        return new QRDecompositionF(matrixA).SolveLinearEquations(matrixB);
      
      throw new ArgumentException("The number of rows must be greater than or equal to the number of columns.", "matrixA");
    }


    /// <summary>
    /// Solves the linear set of equations A * x = b.
    /// </summary>
    /// <param name="matrixA">
    /// The matrix A. (Can be rectangular. Number of rows ≥ number of columns.)
    /// </param>
    /// <param name="vectorB">The column vector b with as many rows as A.</param>
    /// <returns>The vector x.</returns>
    /// <remarks>
    /// If A is a square matrix, the x contains the solutions. If A is not a square matrix, the 
    /// least squares solutions is returned.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="vectorB"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The number of rows does not match.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The number of rows in matrix A must be greater than or equal to the number of columns.
    /// </exception>
    /// <exception cref="MathematicsException">
    /// The matrix A does not have full rank.
    /// </exception>
    public static VectorF SolveLinearEquations(MatrixF matrixA, VectorF vectorB)
    {
      if (vectorB == null)
        throw new ArgumentNullException("vectorB");

      return SolveLinearEquations(matrixA, vectorB.ToMatrixF()).GetColumn(0);
    }
    #endregion
  }
}
