using System;
using System.Diagnostics;
using System.Globalization;

namespace MaxReaderModel.Text
{
    /// <summary>
    /// An (Offset,Length)-pair.
    /// </summary>
    public interface ISegment
    {
        /// <summary>
        /// Gets the start offset of the segment.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Gets the length of the segment.
        /// </summary>
        /// <remarks>For line segments (IDocumentLine), the length does not include the line delimeter.</remarks>
        int Length { get; }

        /// <summary>
        /// Gets the end offset of the segment.
        /// </summary>
        /// <remarks>EndOffset = Offset + Length;</remarks>
        int EndOffset { get; }
    }

    /// <summary>
	/// Represents a simple segment (Offset,Length pair) that is not automatically updated
	/// on document changes.
	/// </summary>
	public struct SimpleSegment : IEquatable<SimpleSegment>, ISegment
    {
        public static readonly SimpleSegment Invalid = new SimpleSegment(-1, -1);
        public static bool IsInvalid(ISegment segment)
        {
            return segment.Offset == -1 && segment.Length == -1;
        }

        /// <summary>
        /// Gets the overlapping portion of the segments.
        /// Returns SimpleSegment.Invalid if the segments don't overlap.
        /// </summary>
        public static SimpleSegment GetOverlap(ISegment segment1, ISegment segment2)
        {
            int start = Math.Max(segment1.Offset, segment2.Offset);
            int end = Math.Min(segment1.EndOffset, segment2.EndOffset);
            if (end < start)
                return SimpleSegment.Invalid;
            else
                return new SimpleSegment(start, end - start);
        }

        public readonly int Offset, Length;

        int ISegment.Offset
        {
            get { return Offset; }
        }

        int ISegment.Length
        {
            get { return Length; }
        }

        public int EndOffset
        {
            get
            {
                return Offset + Length;
            }
        }

        public SimpleSegment(int offset, int length)
        {
            this.Offset = offset;
            this.Length = length;
        }

        public SimpleSegment(ISegment segment)
        {
            Debug.Assert(segment != null);
            this.Offset = segment.Offset;
            this.Length = segment.Length;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Offset + 10301 * Length;
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is SimpleSegment) && Equals((SimpleSegment)obj);
        }

        public bool Equals(SimpleSegment other)
        {
            return this.Offset == other.Offset && this.Length == other.Length;
        }

        public static bool operator ==(SimpleSegment left, SimpleSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SimpleSegment left, SimpleSegment right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", Length=" + Length.ToString(CultureInfo.InvariantCulture) + "]";
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ISegment"/>.
    /// </summary>
    public static class ISegmentExtensions
    {
        /// <summary>
        /// Gets whether <paramref name="segment"/> fully contains the specified segment.
        /// </summary>
        /// <remarks>
        /// Use <c>segment.Contains(offset, 0)</c> to detect whether a segment (end inclusive) contains offset;
        /// use <c>segment.Contains(offset, 1)</c> to detect whether a segment (end exclusive) contains offset.
        /// </remarks>
        public static bool Contains(this ISegment segment, int offset, int length)
        {
            return segment.Offset <= offset && offset + length <= segment.EndOffset;
        }

        /// <summary>
        /// Gets whether <paramref name="thisSegment"/> fully contains the specified segment.
        /// </summary>
        public static bool Contains(this ISegment thisSegment, ISegment segment)
        {
            return segment != null && thisSegment.Offset <= segment.Offset && segment.EndOffset <= thisSegment.EndOffset;
        }
    }
}