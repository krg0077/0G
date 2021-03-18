using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _0G.Legacy
{
    [System.Serializable]
    public sealed class FrameSequence
    {
        // CONSTANTS

        public const int VERSION = 5;

        public const int INFINITE_PLAY_COUNT = 100;
        public const int NUMBER_MAX_LENGTH = 3;

        // SUB-CLASSES

        class FrameCommand
        {
            public bool isNumber, isGroupBegin, isGroupEnd, isExtender, isRange, isSeparator; // TODO: make this an enum
            public int[] numbers;
            public int depth;
        }

        // SERIALIZED FIELDS

        [SerializeField, HideInInspector, FormerlySerializedAs("m_serializedVersion")]
        private int _serializedVersion = VERSION;

        [Tooltip("An optional name you may give this frame sequence.")]
        [FormerlySerializedAs("m_name")]
        public string _name = default;

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary", expanded: false)]
#endif
        [SerializeField]
        [Tooltip("Actions to perform before the sequence starts.")]
        [Enum(typeof(FrameSequenceAction))]
        private List<int> _preSequenceActions = default;

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [Delayed]
        [Tooltip("Commas seperate frames/groups. 1-3-1 means 1,2,3,2,1. 1-3x2-1 means 1-3,3-1 means 1,2,3,3,2,1.")]
        public string _frames = default;

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [SerializeField, ReadOnly]
        [Tooltip("Count of frames in a single playthrough of this sequence.")]
#pragma warning disable IDE0052 // Remove unread private members
        private int _frameCount;
#pragma warning restore IDE0052 // Remove unread private members

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [SerializeField, ReadOnly]
        [Tooltip("This is how the code sees your frames input.")]
#pragma warning disable IDE0052 // Remove unread private members
        private string _interpretedFrames;
#pragma warning restore IDE0052 // Remove unread private members

        [SerializeField, HideInInspector]
        private List<int> _frameList = new List<int>();

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [SerializeField]
        [Tooltip("Count of playthroughs, or \"loops\", of this sequence." +
            " Anything with a value of 100 or higher will be considered an infinite loop" +
            " and will play indefinitely until code tells it otherwise.")]
        [FormerlySerializedAs("m_playCount")]
        private RangeInt _playCount = new RangeInt();

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [SerializeField]
        [Tooltip("Audio play style, as applicable.")]
        private AudioPlayStyle _audioPlayStyle = default;

#if ODIN_INSPECTOR
        [FoldoutGroup("$DataSummary")]
#endif
        [SerializeField]
        [Tooltip("Audio event to play upon running this sequence.")]
        [AudioEvent]
        private string _audioEvent = default;

        // DEPRECATED SERIALIZED FIELDS

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_doesCallCode"), FormerlySerializedAs("m_doesCallCode")]
        private bool m_ObsoleteDoesCallCode = false;

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_preSequenceAction")]
        private int m_ObsoletePreSequenceAction = default;

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_from"), FormerlySerializedAs("m_from")]
        private int m_ObsoleteFrom = default;

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_to"), FormerlySerializedAs("m_to")]
        private int m_ObsoleteTo = default;

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_fromFrame"), FormerlySerializedAs("m_fromFrame")]
        private RangeInt m_ObsoleteFromFrame = new RangeInt();

        [HideInInspector, SerializeField]
        [FormerlySerializedAs("_toFrame"), FormerlySerializedAs("m_toFrame")]
        private RangeInt m_ObsoleteToFrame = new RangeInt();

        // FIELDS: PRIVATE / ConvertFramesToFrameList

        private Queue<FrameCommand> _frameCommands = new Queue<FrameCommand>();

        private StringBuilder _number;

        // SHORTCUT PROPERTIES

        public string Name { get => _name; set => _name = value; }

        public string DataSummary => string.Format("{0} [{1}x]", _frames, _playCount.DataSummary);

        public ReadOnlyCollection<int> FrameList => _frameList.AsReadOnly();

        public int PlayCount => _playCount.randomValue;

        public bool PlayCountMinInclusive => _playCount.minInclusive;

        public bool PlayCountMaxInclusive => _playCount.maxInclusive;

        public int PlayCountMinValue => _playCount.minValue;

        public int PlayCountMaxValue => _playCount.maxValue;

        public List<int> PreSequenceActions => _preSequenceActions;

        public AudioPlayStyle AudioPlayStyle => _audioPlayStyle;

        public string AudioEvent => _audioEvent;

        // METHODS: PUBLIC

        public void OnValidate()
        {
            if (_name != null)
            {
                _name = _name.Trim();
            }

            // initialization
            if (_playCount.minValue == 0 && _playCount.maxValue == 0)
            {
                _playCount.minValue = 1;
            }

            UpdateSerializedVersion();

            // real validation
            _playCount.minValue = Mathf.Max(0, _playCount.minValue);

            ConvertFramesToFrameList();
        }

        // METHODS: UpdateSerializedVersion

        private void UpdateSerializedVersion()
        {
            while (_serializedVersion < VERSION)
            {
                switch (_serializedVersion)
                {
                    case 0:
                        if (m_ObsoleteFrom > 0)
                        {
                            m_ObsoleteFromFrame.minValue = m_ObsoleteFrom;
                            m_ObsoleteFromFrame.maxValue = m_ObsoleteFrom;
                            m_ObsoleteFrom = 0;
                        }
                        if (m_ObsoleteTo > 0)
                        {
                            m_ObsoleteToFrame.minValue = m_ObsoleteTo;
                            m_ObsoleteToFrame.maxValue = m_ObsoleteTo;
                            m_ObsoleteTo = 0;
                        }
                        break;
                    case 1:
                        if (m_ObsoleteDoesCallCode)
                        {
                            _name += " [DOES CALL CODE]";
                        }
                        m_ObsoleteDoesCallCode = false;
                        break;
                    case 2:
                        if (m_ObsoletePreSequenceAction != 0)
                        {
                            if (_preSequenceActions == null)
                            {
                                _preSequenceActions = new List<int>(1);
                            }
                            _preSequenceActions.Add(m_ObsoletePreSequenceAction);
                        }
                        m_ObsoletePreSequenceAction = 0;
                        break;
                    case 3:
                        m_ObsoleteFromFrame.Inclusivize();
                        m_ObsoleteToFrame.Inclusivize();
                        m_ObsoleteFromFrame.minValue = Mathf.Max(1, m_ObsoleteFromFrame.minValue);
                        m_ObsoleteToFrame.minValue = Mathf.Max(m_ObsoleteFromFrame.maxValue, m_ObsoleteToFrame.minValue);
                        string fromFrames = m_ObsoleteFromFrame.minValue.ToString();
                        if (m_ObsoleteFromFrame.minValue != m_ObsoleteFromFrame.maxValue)
                        {
                            fromFrames += "r" + m_ObsoleteFromFrame.maxValue.ToString();
                        }
                        string toFrames = m_ObsoleteToFrame.minValue.ToString();
                        if (m_ObsoleteToFrame.minValue != m_ObsoleteToFrame.maxValue)
                        {
                            toFrames += "r" + m_ObsoleteToFrame.maxValue.ToString();
                        }
                        string newFrames = fromFrames;
                        if (fromFrames != toFrames)
                        {
                            newFrames += "-" + toFrames;
                        }
                        if (string.IsNullOrWhiteSpace(_frames))
                        {
                            _frames = newFrames;
                        }
                        else if (newFrames != "1")
                        {
                            _frames += "/" + newFrames;
                        }
                        m_ObsoleteFromFrame.minValue = 1;
                        m_ObsoleteFromFrame.maxValue = 1;
                        m_ObsoleteToFrame.minValue = 1;
                        m_ObsoleteToFrame.maxValue = 1;
                        break;
                    case 4:
                        // no change
                        break;
                }
                ++_serializedVersion;
            }
        }

        // METHODS: ConvertFramesToFrameList

        private void ConvertFramesToFrameList()
        {
            _frameList.Clear();
            _frameCommands.Clear();
            _number = new StringBuilder();
            _frames = _frames?.Trim() ?? "";
            if (_frames == "") return;
            char c;
            int depth = 0, maxDepth = 0;
            for (int i = 0; i < _frames.Length; ++i)
            {
                c = _frames[i];
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if (_number.Length < NUMBER_MAX_LENGTH)
                        {
                            _number.Append(c);
                        }
                        else
                        {
                            Error("A number exceeds the max length: " + _number + "...");
                        }
                        break;
                    case '(':
                        FlushNumberToFrameCommands();
                        _frameCommands.Enqueue(new FrameCommand { isGroupBegin = true, depth = ++depth });
                        maxDepth = Mathf.Max(depth, maxDepth);
                        break;
                    case ')':
                        FlushNumberToFrameCommands();
                        _frameCommands.Enqueue(new FrameCommand { isGroupEnd = true, depth = depth-- });
                        break;
                    case 'x':
                        FlushNumberToFrameCommands();
                        _frameCommands.Enqueue(new FrameCommand { isExtender = true });
                        break;
                    case 't': // to
                    case '-':
                        FlushNumberToFrameCommands();
                        _frameCommands.Enqueue(new FrameCommand { isRange = true });
                        break;
                    case ',':
                        FlushNumberToFrameCommands();
                        _frameCommands.Enqueue(new FrameCommand { isSeparator = true });
                        break;
                }
            }
            FlushNumberToFrameCommands();
            for (int d = maxDepth; d >= 0; --d)
            {
                ProcessFrameCommandExtenders(d);
                ProcessFrameCommandRanges(d);
            }
            FlushCommandsToFrameList();
            _frameCount = _frameList.Count;
            _interpretedFrames = ListToString(_frameList);
        }

        private void FlushNumberToFrameCommands()
        {
            if (_number.Length > 0)
            {
                int[] numbers = new int[] { int.Parse(_number.ToString()) };
                _frameCommands.Enqueue(new FrameCommand { isNumber = true, numbers = numbers });
                _number.Clear();
            }
        }

        private void ProcessFrameCommandExtenders(int depth)
        {
            var q = _frameCommands; // the original queue of frame commands
            _frameCommands = new Queue<FrameCommand>(); // the new, processed queue of frame commands
            FrameCommand prev = null, curr, next; // previous, current, next
            bool inDepth = depth == 0;
            while (q.Count > 0)
            {
                curr = q.Dequeue();
                if (curr.isGroupBegin && curr.depth >= depth)
                {
                    if (curr.depth == depth)
                    {
                        inDepth = true;
                        _frameCommands.Enqueue(curr);
                        prev = null;
                    }
                    else
                    {
                        // do nothing; dispose of curr (group command)
                    }
                }
                else if (curr.isGroupEnd && curr.depth >= depth)
                {
                    if (curr.depth == depth)
                    {
                        inDepth = false;
                        _frameCommands.Enqueue(curr);
                        prev = null;
                    }
                    else
                    {
                        // do nothing; dispose of curr (group command)
                    }
                }
                else if (curr.isExtender && inDepth)
                {
                    if (IsBinaryOperator(prev, q, out next))
                    {
                        int oldLen = prev.numbers.Length;
                        int times = next.numbers[0];
                        int newLen = oldLen * times + (next.numbers.Length - 1); // parens contain remainder
                        int[] numbers = new int[newLen];
                        int pos = 0;
                        for (int i = 0; i < times; ++i)
                        {
                            for (int j = 0; j < oldLen; ++j)
                            {
                                numbers[pos++] = prev.numbers[j];
                            }
                        }
                        for (int k = 1; k < next.numbers.Length; ++k) // process remainder
                        {
                            numbers[pos++] = next.numbers[k];
                        }
                        prev.numbers = numbers;
                        // keep the updated prev; dispose of curr (extender command) and next
                    }
                }
                else
                {
                    _frameCommands.Enqueue(curr);
                    prev = curr;
                }
            }
        }

        private void ProcessFrameCommandRanges(int depth)
        {
            var q = _frameCommands; // the original queue of frame commands
            _frameCommands = new Queue<FrameCommand>(); // the new, processed queue of frame commands
            FrameCommand prev = null, curr, next; // previous, current, next
            bool inDepth = depth == 0;
            while (q.Count > 0)
            {
                curr = q.Dequeue();
                if (curr.isGroupBegin && curr.depth >= depth)
                {
                    if (curr.depth == depth)
                    {
                        inDepth = true;
                        _frameCommands.Enqueue(curr);
                        prev = null;
                    }
                    else
                    {
                        // do nothing; dispose of curr (group command)
                    }
                }
                else if (curr.isGroupEnd && curr.depth >= depth)
                {
                    if (curr.depth == depth)
                    {
                        inDepth = false;
                        _frameCommands.Enqueue(curr);
                        prev = null;
                    }
                    else
                    {
                        // do nothing; dispose of curr (group command)
                    }
                }
                else if (curr.isRange && inDepth)
                {
                    if (IsBinaryOperator(prev, q, out next))
                    {
                        int from = prev.numbers[prev.numbers.Length - 1];
                        int to = next.numbers[0];
                        if (from == to)
                        {
                            Error("Same _from_ and _to_. Ignoring second number. Use a comma if you want it twice.");
                            return;
                        }
                        int midLen = Mathf.Abs(from - to) - 1;
                        int newLen = prev.numbers.Length + midLen + next.numbers.Length;
                        int[] numbers = new int[newLen];
                        int pos = 0;
                        for (int i = 0; i < prev.numbers.Length; ++i)
                        {
                            numbers[pos++] = prev.numbers[i];
                        }
                        if (from < to)
                        {
                            for (int j = from + 1; j < to; ++j) numbers[pos++] = j;
                        }
                        else
                        {
                            for (int j = from - 1; j > to; --j) numbers[pos++] = j;
                        }
                        for (int k = 0; k < next.numbers.Length; ++k)
                        {
                            numbers[pos++] = next.numbers[k];
                        }
                        prev.numbers = numbers;
                        // keep the updated prev; dispose of curr (range command) and next
                    }
                }
                else if (curr.isSeparator && inDepth)
                {
                    if (IsBinaryOperator(prev, q, out next))
                    {
                        int newLen = prev.numbers.Length + next.numbers.Length;
                        int[] numbers = new int[newLen];
                        int pos = 0;
                        for (int i = 0; i < prev.numbers.Length; ++i)
                        {
                            numbers[pos++] = prev.numbers[i];
                        }
                        for (int k = 0; k < next.numbers.Length; ++k)
                        {
                            numbers[pos++] = next.numbers[k];
                        }
                        prev.numbers = numbers;
                        // keep the updated prev; dispose of curr (separator command) and next
                    }
                }
                else
                {
                    _frameCommands.Enqueue(curr);
                    prev = curr;
                }
            }
        }

        private void FlushCommandsToFrameList()
        {
            var q = _frameCommands; // the original queue of frame commands
            // this is the last process, so we're gonna affect the original queue this time
            FrameCommand curr; // current
            while (q.Count > 0)
            {
                curr = q.Dequeue();
                if (curr.isNumber)
                {
                    _frameList.AddRange(curr.numbers);
                }
                else
                {
                    Error("Unrecognized command. Should only be a number.");
                }
            }
        }

        private bool IsBinaryOperator(FrameCommand prev, Queue<FrameCommand> q, out FrameCommand next)
        {
            next = null;
            if (q.Count == 0)
            {
                Error("Missing right operator at end.");
                return false;
            }
            if (!q.Peek().isNumber) // peek at next, but don't dequeue it yet
            {
                Error("Missing right operator.");
                return false;
            }
            next = q.Dequeue(); // dequeue next, and set _out_ parameter
            if (prev == null)
            {
                Error("Missing left operator at beginning.");
                return false;
            }
            if (!prev.isNumber)
            {
                Error("Missing left operator.");
                return false;
            }
            return true;
        }

        private void Error(string message)
        {
            // TODO: display this information in the inspector
            G.U.Err("Error in FrameSequence {0} with frames {1}. \r\n{2}", _name, _frames, message);
        }

        private static string ListToString(List<int> list)
        {
            string s = "";
            for (int i = 0; i < list.Count; ++i)
            {
                s += list[i] + ",";
            }
            return s.TrimEnd(',');
        }
    }
}