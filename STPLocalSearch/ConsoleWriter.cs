using System.IO;
using System.Linq;
using System.Text;

namespace STPLocalSearch
{
    public class ConsoleWriter : TextWriter
    {
        private string _currentLine;
        private TextWriter _original;

        public ConsoleWriter(TextWriter original) : base()
        {
            _original = original;
        }

        public override Encoding Encoding { get { return Encoding.Default; } }

        public string CurrentLine => _currentLine;

        public override void WriteLine(string format, params object[] arg)
        {
            _original.WriteLine(format, arg);
            _currentLine = string.Format(format, arg);
        }

        public override void Write(char value)
        {
            _original.Write(value);
            if (value == '\n')
                _currentLine = "";
            else
                _currentLine += value;
        }

        public override void Write(char[] buffer)
        {
            _original.Write(buffer);

            foreach (var value in buffer)
            {
                if (value == '\n')
                    _currentLine = "";
                else
                    _currentLine += value;
            }
        }

        public override void Write(bool value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _original.Write(buffer, index, count);
            for (int i = 0; i < count; i++)
            {
                var value = buffer[index + i];
                if (value == '\n')
                    _currentLine = "";
                else
                    _currentLine += value;
            }
        }

        public override void Write(int value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(uint value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(long value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(ulong value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(float value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(double value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(decimal value)
        {
            _original.Write(value);
            _currentLine += value;
        }

        public override void Write(string value)
        {
            _original.Write(value);
            if (value.Contains("\n"))
                _currentLine = value.Split('\n').Last();
            else
                _currentLine += value;
        }

        public override void Write(object value)
        {
            string str = value.ToString();
            Write(str);
        }

        public override void Write(string format, object arg0)
        {
            string str = string.Format(format, arg0);
            Write(str);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            string str = string.Format(format, arg0, arg1);
            Write(str);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            string str = string.Format(format, arg0, arg1, arg2);
            Write(str);
        }

        public override void Write(string format, params object[] arg)
        {
            string str = string.Format(format, arg);
            Write(str);
        }

        public override void WriteLine()
        {
            _original.WriteLine();
            _currentLine = string.Empty;
        }

        public override void WriteLine(char value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(char[] buffer)
        {
            _original.WriteLine(buffer);
            _currentLine = string.Empty;
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            _original.WriteLine(buffer, index, count);
            _currentLine = string.Empty;
        }

        public override void WriteLine(bool value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(int value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(uint value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(long value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(ulong value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(float value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(double value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(decimal value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(string value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(object value)
        {
            _original.WriteLine(value);
            _currentLine = string.Empty;
        }

        public override void WriteLine(string format, object arg0)
        {
            _original.WriteLine(format, arg0);
            _currentLine = string.Empty;
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            _original.WriteLine(format, arg0, arg1);
            _currentLine = string.Empty;
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            _original.WriteLine(format, arg0, arg1, arg2);
            _currentLine = string.Empty;
        }
    }
}
