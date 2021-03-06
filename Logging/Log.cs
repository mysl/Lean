﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Threading;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace QuantConnect.Logging 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Logging management class.
    /// </summary>
    public class Log 
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private static string _lastTraceText = "";
        private static string _lastErrorText = "";
        private const string _dateFormat = "yyyyMMdd HH:mm:ss";
        private static bool _debuggingEnabled = false;
        private static int _level = 1;
       
        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Global flag whether to enable debugging logging:
        /// </summary>
        public static bool DebuggingEnabled
        {
            get
            {
                return _debuggingEnabled;
            }
            set
            {
                _debuggingEnabled = value;
            }
        }


        /// <summary>
        /// Set the minimum message level:
        /// </summary>
        public static int DebuggingLevel
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="error">String Error</param>
        /// <param name="overrideMessageFloodProtection">Force sending a message, overriding the "do not flood" directive</param>
        public static void Error(string error, bool overrideMessageFloodProtection = false) 
        {
            try 
            {
                if (error == _lastErrorText && !overrideMessageFloodProtection) return;
                Console.WriteLine(DateTime.Now.ToString(_dateFormat) + " ERROR:: " + error);
                _lastErrorText = error; //Stop message flooding filling diskspace.

                //Log to system log:
                //Only run logger on Linux, this conditional copied from OS.IsLinux and then inverted
                var platform = (int)Environment.OSVersion.Platform;
                if (platform != 4 && platform != 6 && platform != 128) return;

                try
                {
                    var cExecutable = new ProcessStartInfo
                    {
                        FileName = "logger",
                        UseShellExecute = true,
                        RedirectStandardOutput = false,
                        Arguments = "'" + error + "'",
                    };
                    //Don't wait for exit:
                    Process.Start(cExecutable);
                }
                catch (Exception err)
                {
                    Console.WriteLine("Log.SystemLog(): Error with system log: " + err.Message);
                }
            } 
            catch (Exception err)
            {
                Console.WriteLine("Log.Error(): Error writing error: " + err.Message);
            }
        }


        /// <summary>
        /// Log trace
        /// </summary>
        public static void Trace(string traceText, bool overrideMessageFloodProtection = false) 
        { 
            try 
            {
                if (traceText == _lastTraceText && !overrideMessageFloodProtection) return;
                Console.WriteLine(DateTime.Now.ToString(_dateFormat) + " Trace:: " + traceText);
                _lastTraceText = traceText;
            } 
            catch (Exception err) 
            {
                Console.WriteLine("Log.Trace(): Error writing trace: "  +err.Message);
            }
        }

        /// <summary>
        /// Output to the console, and sleep the thread for a little period to monitor the results.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="level">debug level</param>
        /// <param name="delay"></param>
        public static void Debug(string text, int level = 1, int delay = 0)
        {
            if (!_debuggingEnabled || level < _level) return;
            Console.WriteLine(DateTime.Now.ToString(_dateFormat) + " DEBUGGING :: " + text);
            Thread.Sleep(delay);
        }

        /// <summary>
        /// C# Equivalent of Print_r in PHP:
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recursion"></param>
        /// <returns></returns>
        public static string VarDump(object obj, int recursion = 0) 
        {
            var result = new StringBuilder();

            // Protect the method against endless recursion
            if (recursion < 5)
            {
                // Determine object type
                var t = obj.GetType();

                // Get array with properties for this object
                var properties = t.GetProperties();

                foreach (var property in properties) 
                {
                    try
                    {
                        // Get the property value
                        var value = property.GetValue(obj, null);

                        // Create indenting string to put in front of properties of a deeper level
                        // We'll need this when we display the property name and value
                        var indent = String.Empty;
                        var spaces = "|   ";
                        var trail = "|...";

                        if (recursion > 0) 
                        {
                            indent = new StringBuilder(trail).Insert(0, spaces, recursion - 1).ToString();
                        }

                        if (value != null) 
                        {
                            // If the value is a string, add quotation marks
                            var displayValue = value.ToString();
                            if (value is string) displayValue = String.Concat('"', displayValue, '"');

                            // Add property name and value to return string
                            result.AppendFormat("{0}{1} = {2}\n", indent, property.Name, displayValue);

                            try 
                            {
                                if (!(value is ICollection)) 
                                {
                                    // Call var_dump() again to list child properties
                                    // This throws an exception if the current property value
                                    // is of an unsupported type (eg. it has not properties)
                                    result.Append(VarDump(value, recursion + 1));
                                } 
                                else 
                                {
                                    // 2009-07-29: added support for collections
                                    // The value is a collection (eg. it's an arraylist or generic list)
                                    // so loop through its elements and dump their properties
                                    var elementCount = 0;
                                    foreach (var element in ((ICollection)value)) 
                                    {
                                        var elementName = String.Format("{0}[{1}]", property.Name, elementCount);
                                        indent = new StringBuilder(trail).Insert(0, spaces, recursion).ToString();

                                        // Display the collection element name and type
                                        result.AppendFormat("{0}{1} = {2}\n", indent, elementName, element.ToString());

                                        // Display the child properties
                                        result.Append(VarDump(element, recursion + 2));
                                        elementCount++;
                                    }

                                    result.Append(VarDump(value, recursion + 1));
                                }
                            } catch { }
                        } 
                        else 
                        {
                            // Add empty (null) property to return string
                            result.AppendFormat("{0}{1} = {2}\n", indent, property.Name, "null");
                        }
                    } 
                    catch 
                    {
                        // Some properties will throw an exception on property.GetValue()
                        // I don't know exactly why this happens, so for now i will ignore them...
                    }
                }
            }

            return result.ToString();
        }
    }
}
