using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudDiff.Processor
{
    public class NaturalBreaks
    {
        #region ValueCountTuple
        private class ValueCountTuple : IComparable, IComparable<ValueCountTuple>, IEquatable<ValueCountTuple>
        {
            public double Value { get; }
            public int Count { get; set; }

            public ValueCountTuple(double value, int count)
            {
                Value = value;
                Count = count;
            }

            public int CompareTo(object obj)
            {
                if (ReferenceEquals(this, obj))
                    return 0;
                return obj is null ? 1 : CompareTo(obj as ValueCountTuple);
            }

            public int CompareTo(ValueCountTuple other)
            {
                if (ReferenceEquals(this, other))
                    return 0;
                if (other is null)
                    return 1;

                var result = Value.CompareTo(other.Value);
                return result != 0 ? result : Count.CompareTo(other.Count);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = 1519435568;
                    hashCode = (hashCode * 397) ^ Value.GetHashCode();
                    hashCode = (hashCode * 397) ^ Count.GetHashCode();
                    return hashCode;
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                    return true;
                return !(obj is null) && Equals(obj as ValueCountTuple);
            }

            public bool Equals(ValueCountTuple other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (other is null)
                    return false;

                //  ReSharper disable once CompareOfFloatsByEqualityOperator
                return Value == other.Value && Count == other.Count;
            }

            public override string ToString()
            {
                return $"{Value} [{Count}]";
            }
        }
        #endregion

        private readonly List<ValueCountTuple> _values;
        private readonly int _numBreaks;
        private readonly int _bufferSize;
        private List<double> _previousSsm;
        private List<double> _currentSsm;
        private readonly int[] _classBreaks;
        private int _classBreaksIndex;
        private int _completedRows;

        public static List<double> CreateNaturalBreaksArray(List<double> values, int numBreaks)
        {
            var tuples = BuildValueCountTuples(values);
            var breaks = tuples.Count > numBreaks ? ClassifyByJenksFisher(numBreaks, tuples) : tuples.Select(x => x.Value).ToList();
            return breaks;
        }
        
        private NaturalBreaks(List<ValueCountTuple> tuples, int numBreaks)
        {
            _values = new List<ValueCountTuple>();
            var numValues = tuples.Count;
            _numBreaks = numBreaks;
            _bufferSize = tuples.Count - (_numBreaks - 1);
            _previousSsm = new List<double>(_bufferSize);
            _currentSsm = new List<double>(_bufferSize);
            _classBreaks = new int[_bufferSize * (_numBreaks - 1)];

            var cwv = 0.0;
            var cw = 0;

            //  Avoid array <-> list conversations in future
            _previousSsm.AddRange(Enumerable.Repeat(0.0d, _bufferSize));
            _currentSsm.AddRange(Enumerable.Repeat(0.0d, _bufferSize));

            for (var i = 0; i != numValues; i++)
            {
                var currPair = tuples[i];
                var w = currPair.Count;
                cw += w;
                cwv += w * currPair.Value;
                _values.Add(new ValueCountTuple(cwv, cw));
                if (i < _bufferSize)
                    //  Prepare sum of squared means for first class. Last (k-1) values are omitted
                    _previousSsm[i] = cwv * cwv / cw;
            }
        }
        
        private int SumOfWeights(int beginIndex, int endIndex)
        {
            var res = _values[endIndex].Count;
            res -= _values[beginIndex - 1].Count;
            return res;
        }
        
        private double SumOfWeightedValues(int beginIndex, int endIndex)
        {
            var res = _values[endIndex].Value;
            res -= _values[beginIndex - 1].Value;
            return res;
        }
        
        private double Ssm(int beginIndex, int endIndex)
        {
            var res = SumOfWeightedValues(beginIndex, endIndex);
            return res * res / SumOfWeights(beginIndex, endIndex);
        }
        
        private int FindMaxBreakIndex(int i, int bp, int ep)
        {
            var minSsm = _previousSsm[bp] + Ssm(bp + _completedRows, i + _completedRows);
            var foundP = bp;
            while (++bp < ep)
            {
                var curSsm = _previousSsm[bp] + Ssm(bp + _completedRows, i + _completedRows);
                if (curSsm > minSsm)
                {
                    minSsm = curSsm;
                    foundP = bp;
                }
            }

            _currentSsm[i] = minSsm;
            return foundP;
        }
        
        private void CalculateRange(int bi, int ei, int bp, int ep)
        {
            if (bi == ei)
                return;

            var mi = (int)Math.Floor((bi + ei) * 0.5);
            var mp = FindMaxBreakIndex(mi, bp, Math.Min(ep, mi + 1));

            //  Solve first half of the sub-problems with lower 'half' of possible outcomes
            CalculateRange(bi, mi, bp, Math.Min(mi, mp + 1));

            //  Store result for the middle element.
            _classBreaks[_classBreaksIndex + mi] = mp;

            //  ReSharper disable once TailRecursiveCall
            //  Sovle second half of the sub-problems with upper 'half' of possible outcomes
            CalculateRange(mi + 1, ei, mp, ep);
        }
        
        private void CalculateAll()
        {
            if (_numBreaks >= 2)
            {
                _classBreaksIndex = 0;
                for (_completedRows = 1; _completedRows < _numBreaks - 1; _completedRows++)
                {
                    CalculateRange(0, _bufferSize, 0, _bufferSize);

                    //  Swap ssm lists
                    var temp = _previousSsm;
                    _previousSsm = _currentSsm;
                    _currentSsm = temp;

                    _classBreaksIndex += _bufferSize;
                }
            }
        }
        
        private static List<double> ClassifyByJenksFisher(int numBreaks, List<ValueCountTuple> tuples)
        {
            var breaksArray = new List<double>(numBreaks);
            if (numBreaks == 0)
                return breaksArray;
            //  Avoid array <-> list conversations
            breaksArray.AddRange(Enumerable.Repeat(0.0d, numBreaks));

            var classificator = new NaturalBreaks(tuples, numBreaks);
            if (numBreaks > 1)
            {
                //  Runs the actual calculation
                classificator.CalculateAll();
                var lastClassBreakIndex = classificator.FindMaxBreakIndex(classificator._bufferSize - 1, 0, classificator._bufferSize);
                while (--numBreaks != 0)
                {
                    //  Assign the break values to the result
                    breaksArray[numBreaks] = tuples[lastClassBreakIndex + numBreaks].Value;

                    if (numBreaks > 1)
                    {
                        classificator._classBreaksIndex -= classificator._bufferSize;
                        lastClassBreakIndex = classificator._classBreaks[classificator._classBreaksIndex + lastClassBreakIndex];
                    }
                }
            }

            breaksArray[0] = tuples[0].Value;
            return breaksArray;
        }
        
        private static List<ValueCountTuple> BuildValueCountTuples(List<double> values)
        {
            var valuesDict = new Dictionary<double, ValueCountTuple>();
            foreach (var value in values)
            {
                if (valuesDict.TryGetValue(value, out var tuple))
                    tuple.Count++;
                else
                    valuesDict.Add(value, new ValueCountTuple(value, 1));
            }

            var result = valuesDict.Values.ToList();
            result.Sort();
            return result;
        }
    }
}
