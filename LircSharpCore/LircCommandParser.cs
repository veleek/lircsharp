using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ben.LircSharp
{
    public class LircCommandParser
    {
        private enum ParserState
        {
            Begin,
            Command,
            Result,
            DataOrEnd,
            DataCount,
            DataLine,
            End,
        }

        private ParserState state = ParserState.Begin;
        private object parseLock = new object();
        private StringBuilder currentToken = new StringBuilder();
        private LircCommand currentCommand;
        private int dataLinesLeft = 0;

        public event EventHandler<LircCommandEventArgs> CommandParsed;

        public void Parse(byte[] data, int offset, int length)
        {
            // Just serialize any data parsing
            lock (parseLock)
            {
                for (int i = offset; i < length; i++)
                {
                    byte d = data[i];
                    if (d == (byte)'\n')
                    {
                        try
                        {
                            ParseToken(currentToken.ToString());
                        }
                        finally
                        {
                            currentToken = new StringBuilder();
                        }
                    }
                    else
                    {
                        currentToken.Append((char)d);
                    }
                }
            }
        }

        protected void ParseToken(string token)
        {
            try
            {
                switch (state)
                {
                    case ParserState.Begin:
                        AssertToken("BEGIN", token);
                        state = ParserState.Command;
                        break;
                    case ParserState.Command:
                        currentCommand = LircCommandFactory.Create(token);
                        state = ParserState.Result;
                        break;
                    case ParserState.Result:
                        AssertAnyToken(new[] { "SUCCESS", "ERROR" }, token);
                        currentCommand.Succeeded = token == "SUCCESS";
                        state = ParserState.DataOrEnd;
                        break;
                    case ParserState.DataOrEnd:
                        AssertAnyToken(new[] { "DATA", "END" }, token);
                        if (token == "END")
                        {
                            try
                            {
                                OnCommandParsed(currentCommand);
                            }
                            finally
                            {
                                currentCommand = null;
                                state = ParserState.Begin;
                            }
                        }
                        else
                        {
                            state = ParserState.DataCount;
                        }
                        break;
                    case ParserState.DataCount:
                        if (!Int32.TryParse(token, out dataLinesLeft))
                        {
                            throw new LircParsingException("Unable to parse data line count from token " + token);
                        }
                        if(dataLinesLeft == 0)
                        {
                            state = ParserState.End;
                        }
                        else
                        {
                            state = ParserState.DataLine;
                            currentCommand.Data = new List<string>();
                        }
                        break;
                    case ParserState.DataLine:
                        currentCommand.Data.Add(token);
                        if (--dataLinesLeft == 0)
                        {
                            state = ParserState.End;
                        }
                        break;
                    case ParserState.End:
                        AssertToken("END", token);
                        try
                        {
                            OnCommandParsed(currentCommand);
                        }
                        finally
                        {
                            currentCommand = null;
                            state = ParserState.Begin;
                        }
                        break;
                    default:
                        throw new LircParsingException("Parsing engine in unknown state: " + state);
                }
            }
            catch (LircParsingException)
            {
                if (state != ParserState.Begin)
                {
                    state = ParserState.Begin;
                    throw;
                }
            }
        }

        protected void OnCommandParsed(LircCommand command)
        {
            var commandParsed = this.CommandParsed;
            if (commandParsed != null)
            {
                commandParsed(this, new LircCommandEventArgs(command));
            }
        }
        
        private void AssertToken(string expectedToken, string actualToken)
        {
            if (actualToken != expectedToken)
            {
                throw new LircParsingException(string.Format("Expected {0} token, got {1} token", expectedToken, actualToken));
            }
        }

        private void AssertAnyToken(IEnumerable<string> expectedTokens, string actualToken)
        {
            if (!expectedTokens.Contains(actualToken))
            {
                throw new LircParsingException(string.Format("Expected any of {0} tokens, got {1} token", expectedTokens.Aggregate((a,b) => a + ", " + b), actualToken));
            }
        }
    }
}
