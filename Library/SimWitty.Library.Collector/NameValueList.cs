// <copyright file="NameValueList.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// NameValueList
    /// Represents a collection of Name-Value pairs, which can be looped thru to store and retrieve string data.
    /// NameValueList is the basis of the ConnectionString and QueryString classes.
    /// Internally, the Directive, Identifier, and Delimiter are Char arrays. 
    /// </summary>
    public class NameValueList : CollectionBase
    {
        /// <summary>
        /// The maximum length for delimiter character(s).
        /// </summary>
        private const int DelimiterMaxLength = 64;

        /// <summary>
        /// The maximum length for directive character(s).
        /// </summary>
        private const int DirectiveMaxLength = 64;

        /// <summary>
        /// The maximum length for identifier character(s).
        /// </summary>
        private const int IdentifierMaxLength = 64;
        
        /// <summary>
        /// The delimiter separates name=value pairs.
        /// </summary>
        private char[] delimiter = new char[] { '&' };

        /// <summary>
        /// The directive begins the name=value pairs. In URL query strings, this is the ? character.
        /// </summary>
        private char[] directive = new char[] { };

        /// <summary>
        /// The identifier identifies a name = value.
        /// </summary>
        private char[] identifier = new char[] { '=' };

        /// <summary>
        /// The original string used to load the object.
        /// </summary>
        private string loadString = string.Empty;
                        
        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueList"/> class.
        /// </summary>
        public NameValueList()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueList"/> class.
        /// </summary>
        /// <param name="loadString">String to load the collection from. </param>
        public NameValueList(string loadString)
        {
            this.Load(loadString);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueList"/> class.
        /// </summary>
        /// <param name="loadString">String to load the collection from. </param>
        /// <param name="loadDirective">Directive preamble in the string.</param>
        /// <param name="loadDelimiter">String that delimits Name Value pairs. </param>
        /// <param name="loadIdentifier">String that identifies the name and the value (=).</param>
        public NameValueList(string loadString, string loadDirective, string loadDelimiter, string loadIdentifier)
        {
            this.Directive = loadDirective;
            this.Delimiter = loadDelimiter;
            this.Identifier = loadIdentifier;
            this.Load(loadString);
        }

        /// <summary>
        /// Gets or sets the Delimiter string, maximum 64 characters. 
        /// </summary>
        public string Delimiter
        {
            get
            {
                return new string(this.delimiter);
            }

            set
            {
                if (value.Length > DelimiterMaxLength)
                {
                    this.delimiter = value.Substring(1, DelimiterMaxLength).ToCharArray();
                }
                else
                {
                    this.delimiter = value.ToCharArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets the string directive, maximum 64 characters. The default is blank, and it can be set to blank. The processing directive.
        /// </summary>
        public string Directive
        {
            get
            {
                return new string(this.directive);
            }

            set
            {
                if (value.Length > DirectiveMaxLength)
                {
                    this.directive = value.Substring(1, DirectiveMaxLength).ToCharArray();
                }
                else
                {
                    this.directive = value.ToCharArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets the string identifier, maximum 64 characters. The default is "=", and it cannot be set to blank. The character that identifies a variable with its value. 
        /// </summary>
        public string Identifier
        {
            get
            {
                return new string(this.identifier);
            }

            set
            {
                if (value.Length > IdentifierMaxLength)
                {
                    this.identifier = value.Substring(1, IdentifierMaxLength).ToCharArray();
                }
                else
                {
                    this.identifier = value.ToCharArray();
                }
            }
        }

        /// <summary>
        /// Gets the string that was used to load the object.
        /// </summary>
        public string LoadString
        {
            get
            {
                return this.loadString;
            }

            private set
            {
                this.loadString = value;
            }
        }

        /// <summary>
        /// Add a name value pair to the current collection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">Value for the field.</param>
        public void Add(string fieldName, string value)
        {
            InnerList.Add(new NameValuePair(fieldName, value));
        }

        /// <summary>
        /// Retrieve a value for a given name.
        /// </summary>
        /// <param name="fieldName">The name portion of a name value pair.</param>
        /// <returns>Returns the value portion of a name value pair.</returns>
        public string Item(string fieldName)
        {
            foreach (NameValuePair nvp in this.InnerList)
            {
                if (nvp.Name == fieldName) return nvp.Value;
            }

            // If we did not find anything, return an empty string
            return string.Empty;
        }

        /// <summary>
        /// Check for the existence of a given field name in the collection.
        /// </summary>
        /// <param name="fieldName">Name to check for. </param>
        /// <returns>True if a given field name exists in the collection.</returns>
        public bool NameExists(string fieldName)
        {
            foreach (NameValuePair nvp in this.InnerList)
            {
                if (nvp.Name == fieldName) return true;
            }

            return false;
        }

        /// <summary>
        /// Load a given string into the collection.
        /// </summary>
        /// <param name="loadString">String to load the collection from.</param>
        public void Load(string loadString)
        {
            // Save the loadString, preserve its original value
            this.LoadString = loadString;
            
            /*
             * First, we trim off the processing directive, if it exists.
             * i is the string length of the processing directive.
             * If i is more than 0, then there is a processing directive, so cut it out of the String1.
             */ 

            int i = this.directive.Length;
            if ((i >= 1) && (loadString.Length > i)) loadString = loadString.Substring(i);

            /*
             * Next, we exit if either the identifier or delimiter.
             * If the Identifier string length is 0, then exit the sub.
             * If the Delimiter string length is 0, then exit the sub.
             */ 

            if (this.identifier.Length == 0) return;
            if (this.delimiter.Length == 0) return;
            
            // Single-space the query string
            while (loadString.IndexOf("  ") != -1) loadString = loadString.Replace("  ", " ");

            /*
             * Now, we split up the querystring into an array of variable/value pairs.
             * Split the query string into an array of name-value pairs, based on the delimiter (the delimiter joins the name value pairs, right?)
             * I tested both Split and Regex. Split took on average 1/20th the time of Regex, with just slightly more memory consumed.
             */

            string[] pairs = loadString.Trim().Split(this.delimiter);
            System.Array.Sort(pairs);
            
            foreach (string pair in pairs)
            {
                // Split the name-value pairs on the identifier (e.g., =)
                string[] nameValue = pair.Split(this.identifier);

                if (nameValue[0].Length != 0)
                {
                    // Add the values as NamePair. Trim() is not necessary because the NameValuePair class does Trim() automatically.
                    switch (nameValue.Length)
                    {
                        case 1:
                            
                            // Add the name with a blank value
                            this.Add(nameValue[0], string.Empty);
                            break;

                        case 2:
                            // Add both the name and value
                            this.Add(nameValue[0], nameValue[1]);
                            break;

                        default:
                            // Skip this name-value pair
                            break;
                    }
                }                
            }
        }

        /// <summary>
        /// Remove Name-Value Pair by specifying the field name.
        /// </summary>
        /// <param name="fieldName">Name of the pair to remove.</param>
        public void Remove(string fieldName)
        {
            bool valueExists = false;
            int i = 0;

            foreach (NameValuePair nvp in this.InnerList)
            {
                if (nvp.Name == fieldName)
                {
                    valueExists = true;
                    break;
                }
            }

            if (valueExists == true) this.RemoveAt(i);
        }

        /// <summary>
        /// Converts the Name Value collection to an output string.
        /// </summary>
        /// <returns>Returns the name value collection in one string.</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder(this.Directive);

            foreach (NameValuePair nvp in this.InnerList)
            {
                output.Append(nvp.Name + this.Identifier + nvp.Value + this.Delimiter);
            }

            return output.ToString();
        }
    }

    /* QueryString
     * 
     * The QueryString has long been a part of web programming. Microsoft's Active Server 
     * Pages (ASP) first gave Visual Basic programmers the ability to work with this in the 
     * Request.QueryString. With ASP.NET, the Query String is now implemented as a 
     * System.Collections.Specialized.NameValueCollection object. 
     * 
     * The detached Query String class provides a container for storing and interacting with 
     * variables in the same way as the traditional Request.QueryString object. It is referred
     * to as "detached" because it is not attached to an HTTP Request.
     * 
     * Properties
     * ----------
     * Directive    The querystring processing directive.
     * Identifier   The character that identifies a variable with it’s value.
     * Delimiter    The character that delimits the variable/value pairs.
     * 
     * To understand these properties better, let us first look at the common URL (Uniform Resource Locator) syntax.
     * http://www.domain.com/folder/file.html
     * 
     * The URL indicates a specific protocol (http), a specific host (www.domain.com), and specific content 
     * (folder/file.html). This format is fine for static web pages. For dynamic pages, this can be extended with a
     * querystring.
     * 
     * http://www.domain.com/folder/file.asp?search=yes&page=1&count=15
     * 
     * The querystring (?search=yes&page=1&count=15) is an array of variable/value pairs. There are three reserved 
     * characters that define the size of this array.
     * 
     * The "?" is a processing directive that notifies the web server to process everything following as a query 
     * string.
     * 
     * The "=" character identifies the variable/value pair. In this example, the variable search equals yes.
     * 
     * The "&" symbol connects the variable/value pairs into the string array. In the example, the first variable 
     * is search, the next page, and the final count. 
     * 
     * This querystring example is based upon Microsoft Internet Information Services (IIS) web server, and ASP. 
     * Various web servers and different languages can implement query strings using alternate processing 
     * directives, identifiers, and delimiters. Furthermore, there is no guarantee that these have to be 
     * restricted to a single character, so the method accepts these arguments as strings.
     */

    /// <summary>
    /// The detached Query String class provides a container for storing and interacting with variables in the same way as the traditional Request.QueryString object. 
    /// It is referred to as "detached" because it is not attached to an HTTP Request.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public class QueryString : NameValueList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryString"/> class.
        /// </summary>
        /// <param name="queryString">A name value pair in the query string format.</param>
        public QueryString(string queryString)
        {
            this.Directive = "?";
            this.Delimiter = "&";
            this.Identifier = "=";
            this.Load(queryString);
        }        
    }

    /* ConnectionString
     * 
     * This class can be used to parse ConnectionString property common to many .Net data sources.
     * 
     * From the MSDN documentation: "The ConnectionString property is designed to match ODBC connection 
     * string format as closely as possible. The ConnectionString can be set only when the connection 
     * is closed, and once set it is passed, unchanged, to the Driver Manager and the underlying 
     * driver. Therefore, the syntax for the ConnectionString needs to exactly match what the Driver 
     * Manager and underlying driver support."
     */

    /// <summary>
    /// Connection String class can be used to parse ConnectionString property common to many .Net data sources.
    /// </summary>
    public class ConnectionString : NameValueList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionString"/> class.
        /// </summary>
        /// <param name="connectionString">A name value pair in the connection string format.</param>
        public ConnectionString(string connectionString)
        {
            this.Directive = string.Empty;
            this.Delimiter = ";";
            this.Identifier = "=";
            this.Load(connectionString);
        }
    }

    /// <summary>
    /// Name Value Pair string containing values in the format 'myFieldName=myValue'.
    /// </summary>
    public class NameValuePair
    {
        /// <summary>
        /// The name portion of the name=value pair.
        /// </summary>
        private string name = string.Empty;

        /// <summary>
        /// The value portion of the name=value pair.
        /// </summary>
        private string value = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValuePair"/> class.
        /// </summary>
        /// <param name="name">The name portion of the name=value pair.</param>
        /// <param name="value">The value portion of the name=value pair.</param>
        public NameValuePair(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the Name of the field ('myFieldName' in 'myFieldName=myValue').
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the Value of the field ('myValue' in 'myFieldName=myValue').
        /// </summary>
        public string Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.value = value.Trim();
            }
        }

        /// <summary>
        /// Convert the Name Value pair to an output string.
        /// </summary>
        /// <returns>Returns the name=value string.</returns>
        public override string ToString()
        {
            return this.name + "=" + this.value;
        }
    }
}
