using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SEE.DataModel.Runtime.IO
{
    /// <summary>
    /// Parses a file with DYN-extension.
    /// </summary>
    public abstract class DYNParser : IDisposable
    {
        /// <summary>
        /// Name of the linkage name attribute label.
        /// </summary>
        public const string LinkageName = "Linkage.Name";

        /// <summary>
        /// Name of the level attribute label.
        /// </summary>
        public const string Level = "Level";

        /// <summary>
        /// The filename to be parsed. Can not be set to <code>null</code>.
        /// </summary>
        protected string Filename;

        /// <summary>
        /// The debug logger.
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// The current line-number while parsing the predefined file. Is only used for
        /// error messages.
        /// </summary>
        private int currentLineNumber;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">The file to be parsed.</param>
        /// <param name="logger">The debug logger.</param>
        public DYNParser(string filename, ILogger logger = null)
        {
            this.Filename = filename ?? throw new ArgumentException("'filename' must not be null!");
            this.Logger = logger;
            currentLineNumber = 0;
        }

        #region Logging

        protected virtual void LogDebug(string message)
        {
            Logger?.LogDebug(message);
        }

        protected virtual void LogError(string message)
        {
            Logger?.LogError(Filename + ":" + currentLineNumber + ": " + message);
        }

        protected virtual void LogException(Exception exception)
        {
            Logger?.LogException(exception);
        }

        protected virtual void LogInfo(string message)
        {
            Logger?.LogInfo(message);
        }

        #endregion

        /// <summary>
        /// Parses file of <see cref="Filename"/> and calls for each element either
        /// <see cref="Categories(string[])"/> or <see cref="FunctionCall(string[])"/>.
        /// </summary>
        public void Load()
        {
            try
            {
                string[] lines = File.ReadAllLines(Filename);
                if (lines.Length < 2)
                {
                    throw new ArgumentException("Not enough entries!");
                }

                currentLineNumber = 1;
                string[] categories = Tokenize(lines[0]);
                int categoryCount = categories.Length;
                if (categoryCount < 2)
                {
                    throw new NotEnoughCategoriesException(categoryCount);
                }

                Categories(categories);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] attributes = Tokenize(lines[i]);
                    if (attributes.Length != categoryCount)
                    {
                        throw new IncorrectAttributeCountException(categoryCount, attributes.Length);
                    }
                    FunctionCall(attributes);
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        /// <summary>
        /// Splits given line into tokens and returns them as a string-array.
        /// </summary>
        /// <param name="line">The line to be tokenized.</param>
        /// <returns>The tokens.</returns>
        private static string[] Tokenize(string line)
        {
            List<string> tokens = new List<string>();

            bool inQuotes = false;
            int begin = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    if (inQuotes)
                    {
                        begin = i + 1;
                    }
                    else
                    {
                        int length = i - begin;
                        string token = line.Substring(begin, length);
                        tokens.Add(token);
                    }
                }
            }
            return tokens.ToArray();
        }

        /// <summary>
        /// Is called from <see cref="Load"/> in case a line of categories is parsed.
        /// </summary>
        /// <param name="categories">The categories of the dynamic call tree.</param>
        protected abstract void Categories(string[] categories);

        /// <summary>
        /// Is called from <see cref="Load"/> in case a function call is parsed.
        /// </summary>
        /// <param name="attributes">The attributes of the function call.</param>
        protected abstract void FunctionCall(string[] attributes);

        #region IDisposable Support
        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                Logger = null;
                Filename = null;
                disposed = true;
            }
        }
        #endregion
    }

}
