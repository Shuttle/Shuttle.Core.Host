using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Shuttle.ESB.Host
{
    internal class Arguments
    {
        private readonly StringDictionary parameters;

        private readonly Regex remover = new Regex(@"^['""]?(.*?)['""]?$",
                                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Regex spliter = new Regex(@"^-{1,2}|^/|=|:",
                                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Arguments(params string[] args)
            : this((IEnumerable<string>)args)
        {
        }

        public Arguments(IEnumerable<string> args)
        {
            parameters = new StringDictionary();

            string parameter = null;
            string[] parts;

            foreach (var input in args)
            {
                parts = spliter.Split(input, 3);

                switch (parts.Length)
                {
                    case 1:
                        {
                            if (parameter != null)
                            {
                                if (!parameters.ContainsKey(parameter))
                                {
                                    parts[0] = remover.Replace(parts[0], "$1");

                                    parameters.Add(parameter.ToLower(), parts[0]);
                                }

                                parameter = null;
                            }

                            break;
                        }
                    case 2:
                        {
                            if (parameter != null)
                            {
                                if (!parameters.ContainsKey(parameter))
                                {
                                    parameters.Add(parameter.ToLower(), "true");
                                }
                            }

                            parameter = parts[1];

                            break;
                        }
                    case 3:
                        {
                            if (parameter != null)
                            {
                                if (!parameters.ContainsKey(parameter))
                                {
                                    parameters.Add(parameter.ToLower(), "true");
                                }
                            }

                            parameter = parts[1];

                            if (!parameters.ContainsKey(parameter))
                            {
                                parts[2] = remover.Replace(parts[2], "$1");

                                parameters.Add(parameter.ToLower(), parts[2]);
                            }

                            parameter = null;

                            break;
                        }
                }
            }

            if (parameter == null)
            {
                return;
            }

            if (!parameters.ContainsKey(parameter))
            {
                parameters.Add(parameter.ToLower(), "true");
            }
        }

        public string this[string name]
        {
            get
            {
                return (parameters[name]);
            }
        }

        public T Get<T>(string name, T @default)
        {
            if (!parameters.ContainsKey(name.ToLower()))
            {
                return @default;
            }

            return (T)Convert.ChangeType(parameters[name.ToLower()], typeof (T));
        }

        public bool ShouldShowHelp()
        {
            return (Get("help", false) || Get("h", false) || Get("?", false));
        }
    }
}