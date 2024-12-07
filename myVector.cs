using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Linq;
using System.Globalization;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1)]
public class Vector : INullable, IBinarySerialize
{
    private float[] _values;

    // Default constructor
    public Vector()
    {
        _values = Array.Empty<float>();
    }

    public int Size()
    {
        return _values.Length;
    }
    
    public bool IsNull { get; private set; }

    public static Vector Null
    {
        get
        {
            var vector = new Vector { IsNull = true };
            return vector;
        }
    }

    // Parse from a comma-delimited string or a simple JSON-like format
    public static Vector Parse(SqlString input)
    {
        if (input.IsNull || string.IsNullOrWhiteSpace(input.Value))
            return Null;

        var vector = new Vector();
        string inputValue = input.Value.Trim();

        try
        {
            if (inputValue.StartsWith("[") && inputValue.EndsWith("]"))
            {
                // Remove initial and final brackets
                string trimmedInput = inputValue.Substring(1, inputValue.Length - 2);

                // Split elements by commas and convert them to float
                vector._values = trimmedInput
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                    .ToArray();
            }
            else
            {
                // if it is not JSON-like, interpret as a comma separated list
                vector._values = inputValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                    .ToArray();
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid input format.");
        }

        vector.IsNull = false;
        return vector;
    }

    public override string ToString()
    {
        if (_values == null || _values.Length == 0)
            return "[]";

        // Format values and join them in a single line
        var formattedValues = _values
            .Select(v => v.ToString("e7", CultureInfo.InvariantCulture)) // Always use '.' as separator
            .ToArray();

        return "[" + string.Join(",", formattedValues) + "]";
    }

    // Methods for binary serialization (IBinarySerialize)
    public void Read(System.IO.BinaryReader reader)
    {
        int length = reader.ReadInt32();
        _values = new float[length];
        for (int i = 0; i < length; i++)
        {
            _values[i] = reader.ReadSingle();
        }
    }

    public void Write(System.IO.BinaryWriter writer)
    {
        writer.Write(_values.Length);
        foreach (var value in _values)
        {
            writer.Write(value);
        }
    }

    // Function to calculate distance between two vectors
    public static float VectorDistance(string distanceMetric, Vector vector1, Vector vector2)
    {
        if (vector1 == null || vector2 == null || vector1._values.Length != vector2._values.Length)
        {
            throw new ArgumentException("Vectors must be non-null and of the same length.");
        }

        distanceMetric = distanceMetric.ToLower();

        switch (distanceMetric)
        {
            case "cosine":
                return CosineDistance(vector1._values, vector2._values);
            case "euclidean":
                return EuclideanDistance(vector1._values, vector2._values);
            case "dot":
                return -DotProduct(vector1._values, vector2._values);
            case "manhattan":
                return ManhattanDistance(vector1._values, vector2._values);
            default:
                throw new ArgumentException($"Unsupported distance metric: {distanceMetric}");
        }
    }

    // Distance methods and operations between vectors...
    private static float CosineDistance(float[] v1, float[] v2)
    {
        float dot = DotProduct(v1, v2);
        float norm1 = (float)Math.Sqrt(DotProduct(v1, v1));
        float norm2 = (float)Math.Sqrt(DotProduct(v2, v2));

        if (norm1 == 0 || norm2 == 0)
            return 1.0f;

        return 1.0f - (dot / (norm1 * norm2));
    }

    private static float EuclideanDistance(float[] v1, float[] v2)
    {
        float sum = 0.0f;
        for (int i = 0; i < v1.Length; i++)
        {
            float diff = v1[i] - v2[i];
            sum += diff * diff;
        }
        return (float)Math.Sqrt(sum);
    }

    private static float DotProduct(float[] v1, float[] v2)
    {
        float result = 0.0f;
        for (int i = 0; i < v1.Length; i++)
        {
            result += v1[i] * v2[i];
        }
        return result;
    }

    private static float ManhattanDistance(float[] v1, float[] v2)
    {
        float distance = 0.0f;
        for (int i = 0; i < v1.Length; i++)
        {
            distance += Math.Abs(v1[i] - v2[i]);
        }
        return distance;
    }
}
