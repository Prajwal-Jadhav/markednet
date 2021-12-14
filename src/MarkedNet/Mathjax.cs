using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace MarkedNet
{
    public class Mathjax
    {
        //
        // The pattern for math delimiters and special symbols
        // needed for searching for math in the page.
        //
        private readonly Regex _mathJaxPattern = new Regex(@"(\$\$?|\\(?:begin|end)\{[a-z]*\*?\}|\\[\\{}$]|[{}]|(?:\n\s*)+|@@\d+@@|`+)", RegexOptions.IgnoreCase);

        // public static Regex MathJaxPattern { get { return _mathJaxPattern; } }


        // stores math for later use
        private List<string> math = new List<string>();

        private int? start;
        private int? last;
        private int? braces;

        private string[] blocks;

        private string end;

        private bool indent;
        private readonly string inline = "$"; // the inline math delimiter

        public string[] CapturingStringSplit(in string text)
        {
            var result = Regex.Replace(text, @"\r\n?", "\n");

            return _mathJaxPattern.Split(result);
        }


        public string RemoveMath(string text)
        {
            this.blocks = this.CapturingStringSplit(text);

            for (int i = 1, m = blocks.Length; i < m; i += 2)
            {
                string block = blocks[i];

                if (block[0] == '@')
                {
                    //
                    //  Things that look like our math markers will get
                    //  stored and then retrieved along with the math.
                    //
                    blocks[i] = "@@" + math.Count + "@@";
                    math.Add(block);
                }
                else if (this.start != null || this.start != 0)
                {
                    //
                    //  If we are in math or backticks,
                    //    look for the end delimiter,
                    //    but don't go past double line breaks,
                    //    and balance braces within the math,
                    //    but don't process math inside backticks.
                    //
                    if (block == this.end) {
                        if (braces > 0) {
                            this.last = i;
                        }
                        else if (braces == 0)
                        {
                            this.ProcessMath(start, i);
                        }
                        else
                        {
                            start = last = null;
                            end = null;
                        }
                    }
                    else if (Regex.IsMatch(block, @"\n.*\n") || i + 2 >= m)
                    {
                        if (last != null) 
                        {
                            i = (int)last;
                            if (braces >= 0)
                            {
                                ProcessMath(start, i);
                            }
                        }
                        start = last = null;
                        end = null;
                        braces = 0;
                    }
                    else if (block == "{" && braces >= 0)
                    {
                        ++braces;
                    }
                    else if (block == "}" && braces > 0)
                    {
                        --braces;
                    }
                }
                else
                {
                    //
                    //  Look for math start delimiters and when
                    //    found, set up the end delimiter.
                    //
                    if (block == inline || block == "$$")
                    {
                        start = i;
                        end = block;
                        braces = 0;
                    }
                    else if (block.Substring(1, 5) == "begin")
                    {
                        start = i;
                        end = "\\end" + block.Substring(6);
                        braces = 0;
                    }
                    else if (block[0] == '`')
                    {
                        start = last = i;
                        end = block;
                        braces = -1; // no brace balancing
                    }
                    else if (block[0] == '\n')
                    {
                        if (Regex.IsMatch(block, @"    $"))
                            indent = true;
                    }
                }
            }
            if (last != null || last != 0)
            {
                ProcessMath(start, last);
            }

            return String.Join("", blocks);
        }


        //
        //  The math is in blocks i through j, so
        //    collect it into one block and clear the others.
        //  Replace &, <, and > by named entities.
        //  For IE, put <br> at the ends of comments since IE removes \n.
        //  Clear the current math positions and store the index of the
        //    math, then push the math string onto the storage array.
        //
        private void ProcessMath(int? start, int? last)
        {
            var rawBlocks = new List<string>();

            for (int i = (int)start; i < (last + 1); i++)
            {
                rawBlocks.Add(this.blocks[i]);
            }

            string joinedBlock = String.Join("", rawBlocks);

            string cleanedBlock = joinedBlock.Replace("&", "&amp;")
                                    .Replace("<", "&lt;")
                                    .Replace(">", "&gt;");

            if (this.indent)
            {
                cleanedBlock = cleanedBlock.Replace("\n    ", "\n");
            }

            while (last > start)
            {
                this.blocks[(int)last] = "";
                --last;
            }

            this.blocks[(int)start] = "@@" + this.math.Count + "@@";
            math.Add(cleanedBlock);

            start = last = null;
            end = null;
        }

        public string ReplaceMath(string text)
        {
            var result = Regex.Replace(text, @"@@(\d+)@@", match => this.math[match.Index]);

            math = null;
            return result;
        }
    }
}