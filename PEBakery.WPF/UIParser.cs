﻿/*
    Copyright (C) 2016-2017 Hajin Jang
    Licensed under GPL 3.0
 
    PEBakery is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using PEBakery.Exceptions;
using PEBakery.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PEBakery.Core
{
    public static class UIParser
    {
        public static List<UICommand> ParseRawLines(List<string> lines, SectionAddress addr, out List<LogInfo> errorLogs)
        {
            // Select Code sections and compile
            errorLogs = new List<LogInfo>();
            List<UICommand> uiCmdList = new List<UICommand>();
            for (int i = 0; i < lines.Count; i++)
            {
                try
                {
                    uiCmdList.Add(ParseUICommand(lines, addr, ref i));
                }
                catch (EmptyLineException) { } // Do nothing
                catch (InvalidUICommandException e)
                {
                    errorLogs.Add(new LogInfo(LogState.Error, $"{e.Message} [{e.UICmd.RawLine}]"));
                }
                catch (Exception e)
                {
                    errorLogs.Add(new LogInfo(LogState.Error, e));
                }
            }

            return uiCmdList.Where(x => x.Type != UIControlType.None).ToList();
        }

        public static UICommand ParseUICommand(List<string> rawLines, SectionAddress addr, ref int idx)
        {
            UIControlType type = UIControlType.None;
            string rawLine = rawLines[idx].Trim();

            // Check if rawCode is Empty
            if (string.Equals(rawLine, string.Empty))
                throw new EmptyLineException();

            // Comment Format : starts with '//' or '#', ';'
            if (rawLine.StartsWith("//") || rawLine.StartsWith("#") || rawLine.StartsWith(";"))
                throw new EmptyLineException();

            // Find key of interface control
            string key = string.Empty;
            int equalIdx = rawLine.IndexOf('=');
            if (equalIdx != -1)
            {
                key = rawLine.Substring(0, equalIdx);
                rawLine = rawLine.Substring(equalIdx + 1);
            }
            else
                throw new InvalidCommandException($"Interface control [{rawLine}] does not have name");

            // Split with spaces
            List<string> slices = rawLine.Split(',').ToList();

            // Parse Operands
            List<string> args = new List<string>();
            try
            {
                args = CodeParser.ParseArguments(slices, 0);
            }
            catch (InvalidCommandException e)
            {
                UICommand error = new UICommand(rawLine, addr, key);
                throw new InvalidUICommandException(e.Message, error);
            }

            // Check doublequote's occurence - must be 2n
            if (FileHelper.CountStringOccurrences(rawLine, "\"") % 2 == 1)
                throw new InvalidCommandException($"Interface control [{rawLine}]'s doublequotes mismatch");

            // Check if last operand is \ - MultiLine check - only if one or more operands exists
            if (0 < args.Count)
            {
                while (string.Equals(args.Last(), @"\", StringComparison.OrdinalIgnoreCase))
                { // Split next line and append to List<string> operands
                    if (rawLines.Count <= idx) // Section ended with \, invalid grammar!
                        throw new InvalidUICommandException($@"Last interface control [{rawLine}] cannot end with '\' ");
                    idx++;
                    args.AddRange(rawLines[idx].Trim().Split(','));
                }
            }

            // UICommand should have at least 7 operands
            //    Text, Visibility, Type, X, Y, width, height, [Optional]
            if (args.Count < 7)
                throw new InvalidCommandException($"Interface control [{rawLine}] must have at least 7 arguments");

            // Parse opcode
            type = UIParser.ParseControlType(args[2]);

            // Remove UIControlType from operands
            //   Leftover : Text, Visibility, X, Y, width, height, [Optional]
            args.RemoveAt(2);

            // Forge UICommand
            string text = StringEscaper.Unescape(args[0]);
            bool visibility = string.Equals(args[1], "1", StringComparison.Ordinal);
            int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x);
            int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y);
            int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width);
            int.TryParse(args[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height);
            Rect rect = new Rect(x, y, width, height);
            UIInfo info = ParseUICommandInfo(type, args);
            if (info.Valid == false)
                return new UICommand(rawLine, addr, key);
            return new UICommand(rawLine, addr, key, text, visibility, type, rect, info);
        }

        public static UIControlType ParseControlType(string typeStr)
        {
            // typeStr must be number
            if (!Regex.IsMatch(typeStr, @"^[0-9]+$", RegexOptions.Compiled))
                throw new InvalidCommandException("Only number can be used as UICommand type");

            bool failure = false;
            if (Enum.TryParse(typeStr, false, out UIControlType type) == false)
                failure = true;
            if (Enum.IsDefined(typeof(UIControlType), type) == false)
                failure = true;

            if (failure)
                type = UIControlType.None;

            return type;
        }

        private static UIInfo ParseUICommandInfo(UIControlType type, List<string> arguments)
        {
            // Only use fields starting from 8th operand
            List<string> args = arguments.Skip(6).ToList(); // Remove Text, Visibility, X, Y, width, height
            UIInfo error = new UIInfo(false, null);

            switch (type)
            {
                case UIControlType.TextBox:
                    {
                        const int minOpCount = 1;
                        const int maxOpCount = 1;
                        const int optOpCount = 1; // Tooltip
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        return new UIInfo_TextBox(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), StringEscaper.Unescape(args[0]));
                    }
                case UIControlType.TextLabel:
                    {
                        const int minOpCount = 1;
                        const int maxOpCount = 2;
                        const int optOpCount = 1; // Tooltip
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fontSize);
                        UIInfo_TextLabel_Style style = UIInfo_TextLabel_Style.Normal;
                        if (string.Equals(args[1], "Bold", StringComparison.OrdinalIgnoreCase))
                            style = UIInfo_TextLabel_Style.Bold;
                        else if (string.Equals(args[1], "Italic", StringComparison.OrdinalIgnoreCase))
                            style = UIInfo_TextLabel_Style.Italic;
                        else if (string.Equals(args[1], "Underline", StringComparison.OrdinalIgnoreCase))
                            style = UIInfo_TextLabel_Style.Underline;
                        else if (string.Equals(args[1], "Strike", StringComparison.OrdinalIgnoreCase))
                            style = UIInfo_TextLabel_Style.Strike;

                        return new UIInfo_TextLabel(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), fontSize, style);
                    }
                case UIControlType.NumberBox:
                    {
                        const int minOpCount = 4;
                        const int maxOpCount = 4;
                        const int optOpCount = 1; // [Tooltip]
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
                        int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min);
                        int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int max);
                        int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int interval);

                        return new UIInfo_NumberBox(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), value, min, max, interval);
                    }
                case UIControlType.CheckBox:
                    {
                        const int minOpCount = 1;
                        const int maxOpCount = 1;
                        const int optOpCount = 2; // [SectionToRun],[Tooltip] - 여태까지 CheckBox에 Section 달린 건 못 봤는데?
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        bool _checked = false;
                        if (string.Equals(args[0], "True", StringComparison.OrdinalIgnoreCase))
                            _checked = true;
                        else if (string.Equals(args[0], "False", StringComparison.OrdinalIgnoreCase) == false)
                            return error;
                        string sectionName = null;
                        if (maxOpCount < args.Count)
                            sectionName = args[maxOpCount];

                        return new UIInfo_CheckBox(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), _checked, sectionName);
                    }
                case UIControlType.ComboBox:
                    { // Variable Length
                        List<string> items = new List<string>();
                        string last = args.Last();
                        string toolTip = null;
                        int count = 0;
                        if (last.StartsWith("__", StringComparison.Ordinal))
                        {
                            toolTip = last;
                            count = args.Count - 1;
                        }
                        else
                        {
                            count = args.Count;
                        }

                        for (int i = 0; i < count; i++)
                            items.Add(args[i]);

                        int idx = items.IndexOf(arguments[0]);
                        if (idx == -1)
                            return error;

                        return new UIInfo_ComboBox(true, toolTip, items, idx);
                    }
                case UIControlType.Image:
                    {
                        const int minOpCount = 0;
                        const int maxOpCount = 0;
                        const int optOpCount = 2;  // [URL],[Tooltip]
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        string url = null;
                        if (maxOpCount < args.Count)
                            url = args[maxOpCount];

                        return new UIInfo_Image(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), url);
                    }
                case UIControlType.TextFile:
                    {
                        const int minOpCount = 0;
                        const int maxOpCount = 0;
                        const int optOpCount = 1; // [Tooltip]
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        return new UIInfo_TextFile(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1));
                    }
                case UIControlType.Button:
                    {
                        // Still had not figured why SectionName and ProgressShow duplicate
                        // It has 2 to 6 fixed operands. - Need more research.
                        // <SectionName><Picture>[ShowProgress][Boolean?][SectionName(?)][ShowProgress{?}][Tooltip]
                        const int minOpCount = 1;
                        const int maxOpCount = 2;
                        const int optOpCount = 5;
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        string sectionName = args[0];
                        string picture = null;
                        if (2 <= args.Count)
                        {
                            if (args[1].Equals("0", StringComparison.OrdinalIgnoreCase) == false)
                                picture = args[1];
                        }
                        bool showProgress = false;
                        if (3 <= args.Count)
                        {
                            if (string.Equals(args[2], "True", StringComparison.OrdinalIgnoreCase))
                                showProgress = true;
                            else if (string.Equals(args[2], "False", StringComparison.OrdinalIgnoreCase) == false)
                                return error;
                        }

                        return new UIInfo_Button(true, GetInfoTooltip(args, args.Count - 1), sectionName, picture, showProgress);
                    }
                case UIControlType.CheckList:
                    break;
                case UIControlType.WebLabel:
                    {
                        const int minOpCount = 1;
                        const int maxOpCount = 1;
                        const int optOpCount = 1;
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        return new UIInfo_WebLabel(true, GetInfoTooltip(args, maxOpCount + optOpCount), StringEscaper.Unescape(args[0]));
                    }
                case UIControlType.RadioButton:
                    {
                        const int minOpCount = 1;
                        const int maxOpCount = 1;
                        const int optOpCount = 2; // [SectionToRun],[Tooltip]
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        bool selected = false;
                        if (string.Equals(args[0], "True", StringComparison.OrdinalIgnoreCase))
                            selected = true;
                        else if (string.Equals(args[0], "False", StringComparison.OrdinalIgnoreCase) == false)
                            return error;
                        string sectionName = null;
                        if (maxOpCount < args.Count)
                            sectionName = args[maxOpCount];

                        return new UIInfo_RadioButton(true, GetInfoTooltip(args, maxOpCount + optOpCount - 1), selected, sectionName);
                    }
                case UIControlType.Bevel:
                    {
                        const int minOpCount = 0;
                        const int maxOpCount = 0;
                        const int optOpCount = 1;
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        return new UIInfo_Bevel(true, GetInfoTooltip(args, maxOpCount + optOpCount));
                    }
                case UIControlType.FileBox:
                    {
                        const int minOpCount = 0;
                        const int maxOpCount = 0;
                        const int optOpCount = 2;
                        if (CodeParser.CheckInfoArgumentCount(args, minOpCount, maxOpCount + optOpCount))
                            return error;

                        bool isFile = false;
                        if (maxOpCount < args.Count)
                        {
                            if (args[maxOpCount].StartsWith("__", StringComparison.Ordinal) == false)
                            {
                                if (string.Equals(args[maxOpCount], "FILE", StringComparison.OrdinalIgnoreCase))
                                    isFile = true;
                            }
                        }

                        return new UIInfo_FileBox(true, GetInfoTooltip(args, maxOpCount + optOpCount), isFile);
                    }
                case UIControlType.RadioGroup:
                    { // Variable Length
                        List<string> items = new List<string>();
                        string last = args.Last();
                        string toolTip = null;
                        int count = 0;
                        if (last.StartsWith("__", StringComparison.Ordinal))
                        {
                            toolTip = last;
                            count = args.Count - 2;
                        }
                        else
                        {
                            count = args.Count - 1;
                        }

                        for (int i = 0; i < count; i++)
                            items.Add(args[i]);
                        if (int.TryParse(args[count], NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) == false)
                            return error;

                        return new UIInfo_RadioGroup(true, toolTip, items, idx);
                    }
                default:
                    break;
            }
            return error;
        }

        /// <summary>
        /// Extract tooltip from operand
        /// </summary>
        /// <param name="op"></param>
        /// <param name="idx">Tooltip operator's operand index</param>
        /// <returns></returns>
        private static string GetInfoTooltip(List<string> op, int idx)
        {
            if (idx < op.Count && op[idx].StartsWith("__", StringComparison.Ordinal)) // Has tooltip
                return op[idx].Substring(2);
            else
                return null;
        }
    }
}
