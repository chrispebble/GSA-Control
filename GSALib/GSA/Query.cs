/********************************************************************************
 *  
 *  Product: GSALib
 *  Description: A C# API for accessing the Google Search Appliance.
 *
 *  (c) Copyright 2008 Michael Cizmar + Associates Ltd.  (MC+A)
 *  
********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GSALib.Utils;
using GSALib.Constants;

namespace GSALib.GSA
{
    /// <summary>
    /// Class creates Query object that will be used for submitting http requests to GSA
    /// <para>Author Albert Ghukasyan</para>
    /// </summary>
    [Serializable]
    public sealed class Query
    {
        #region Variables

        public static int MAX_RESULTS = 1000;
        public static int MAX_RESULTS_PER_QUERY = 100;
        private QueryBuilder query;
        public QueryTerm queryTerm { get; set; }

        public string[] SiteCollections { get { return query.sites; } set { query.sites = value; } }
        public string Frontend { get { return query.client; } set { query.client = value; }}
        public string OutputFormat { get { return query.output; } set { query.output = value; } }
        public int MaxResults { get { return query.num; } set { query.num = Math.Min(value, MAX_RESULTS); } }
        public byte KeyMatches { get { return query.numgm; } set { query.numgm = value; } }
        public string SearchScope { get { return query.as_occt; } set { query.as_occt = value; } }
        public char Filter { get { return query.filter; } set { query.filter = value; } }
        public string QueryTerm { get { return query.q; } set { query.q = value; } }
        public string[] OrQueryTerms { get { return query.as_oq; } set { query.as_oq = value; } }
        public string[] AndQueryTerms { get { return query.as_q; } set { query.as_q = value; } }
        public string ExactPhraseQueryTerm { get { return query.as_epq; } set { query.as_epq = value; } }
        public string ExcludedQueryTerms { get { return query.as_eq; } set { query.as_eq = value; } }
        public string InputEncoding { get { return query.ie; } set { query.ie = value; } }
        public string OutputEncoding { get { return query.oe; } set { query.oe = value; } }
        public string Language { get { return query.lr; } set { query.lr = value; } }
        public string Sort { get { return query.sort; } set { query.sort = value; } }
        public long ScrollAhead { get { return query.start; } set { query.start = value; } }
        public Access Access { get { return new Access(query.access); } set { query.access = value.getValue(); } }
        public string ProxyCustom { get { return query.proxycustom; } set { query.proxycustom = value; } }
        public string ProxyStylesheet { get { return query.proxystylesheet; } set { query.proxystylesheet = value; } }
        public bool ProxyReload { get { return query.proxyreload; } set { query.proxyreload = value; } }
        public string SiteSearch { get { return query.as_sitesearch; } set { query.as_sitesearch = value; } }

        public Query()
        {
            queryTerm = new QueryTerm();
            query = new QueryBuilder("", null);
            query.setOutput(Output.XML_NO_DTD.getValue());            
        }

        public string[] GetMetaDataFields { get { return query.getfields; } set { query.getfields = value; } }
        public void setRequiredMetaFields(Hashtable requiredFields)
        {
            query.setRequiredfields(requiredFields, true);
        }

        public void setRequiredMetaFields(Hashtable requiredFields, bool orIfTrueAndIfFalse)
        {
            query.setRequiredfields(requiredFields, orIfTrueAndIfFalse);
        }

        public void setPartialMetaFields(Hashtable partialFields)
        {
            query.setPartialfields(partialFields, true);
        }

        public void setPartialMetaFields(Hashtable partialFields, bool orIfTrueAndIfFalse)
        {
            query.setPartialfields(partialFields, orIfTrueAndIfFalse);
        }
        
        public void setSortByDate(bool asc, char mode)
        {
            query.setSort("date:" + (asc
                    ? "A:"
                    : "D:") + mode + ":d1");
        }
        
        public void unsetSortByDate()
        {
            query.setSort(null);
        }

        public String getValue()
        {
            query.setQ(queryTerm.getValue());
            return query.getValue();
        }

        public String getQueryString()
        {
            return queryTerm.getValue();
        }
        #endregion
    }

    /// <summary>
    /// Class creates QueryTerm object that will be used for submitting http requests to GSA
    /// <para> Class Object should be used to specify additional options of Query</para>
    /// Author Albert Ghukasyan
    /// </summary>
    [Serializable]
    public sealed class QueryTerm
    {
        #region Variables
        private ArrayList inTitleTerms = new ArrayList();
        private ArrayList notInTitleTerms = new ArrayList();
        private ArrayList inUrlTerms = new ArrayList();
        private ArrayList notInUrlTerms = new ArrayList();
        private ArrayList includeFiletype = new ArrayList();
        private ArrayList excludeFiletype = new ArrayList();
        private ArrayList allInTitleTerms = new ArrayList();
        private ArrayList allInUrlTerms = new ArrayList();
        private String site;
        private bool includeSite;
        private String dateRange;
        private String webDocLocation;
        private String cacheDocLocation;
        private String link;
        private String queryString;

        private const String _IN_TITLE = "intitle:";
        private const String _NOT_IN_TITLE = "-intitle:";
        private const String _IN_URL = "inurl:";
        private const String _NOT_IN_URL = "-inurl:";
        private const String _INCLUDE_FILETYPE = "filetype:";
        private const String _EXCLUDE_FILETYPE = "-filetype:";
        private const String _INCLUDE_SITE = "site:";
        private const String _EXCLUDE_SITE = "-site:";
        private const String _DATERANGE = "daterange:";
        private const String _ALL_IN_TITLE = "allintitle:";
        private const String _ALL_IN_URL = "allinurl:";
        private const String _INFO = "info:";
        private const String _CACHE = "cache:";
        private const String _LINK = "link:";
        private const String _OR = " OR ";
        private const String _SP = " ";
        #endregion

        #region Constructor
        public QueryTerm()
        {

        }

        public QueryTerm(string queryString)
        {
            this.Populate(queryString);
        }

        /// <summary>
        /// Extracts QueryTerm properties from a querystring.
        /// </summary>
        /// <param name="queryString"></param>
        public void Populate(string queryString)
        {
            // pad the querystring with a space to make our job easier
            queryString += _SP;

            string[] flagArray = new string[] { _IN_TITLE, _NOT_IN_TITLE, _IN_URL, _NOT_IN_URL, _INCLUDE_FILETYPE, _EXCLUDE_FILETYPE, _INCLUDE_SITE, _EXCLUDE_SITE, _CACHE, _INFO, _LINK, _ALL_IN_TITLE, _ALL_IN_URL };
            foreach (string flag in flagArray) {
                // loop through all instances of the flag
                while (queryString.Contains(flag)) {

                    // extract the flag and value from our querystring for further processing
                    int startIndex = queryString.IndexOf(flag);
                    string extract = null;

                    if (flag == _ALL_IN_URL || flag == _ALL_IN_TITLE) {
                        // capture from flag till the next flag or the end of the line
                        int nextFlagIndex = queryString.IndexOfAny(flagArray);
                        extract = queryString.Substring(startIndex, nextFlagIndex != -1 ? nextFlagIndex : queryString.Length - startIndex);
                    } else { // capture till first space
                        extract = queryString.Substring(startIndex, queryString.IndexOf(_SP, startIndex));
                    }

                    // remove the extract from our querystring
                    queryString.Remove(startIndex, extract.Length);

                    // update the appropriate property
                    string value = extract.Remove(0, flag.Length);
                    switch (flag) {
                        case _IN_TITLE:
                            allInTitleTerms.Add(value);
                            break;
                        case _NOT_IN_TITLE:
                            notInTitleTerms.Add(value);
                            break;
                        case _IN_URL:
                            inUrlTerms.Add(value);
                            break;
                        case _NOT_IN_URL:
                            notInTitleTerms.Add(value);
                            break;
                        case _INCLUDE_FILETYPE:
                            includeFiletype.Add(value);
                            break;
                        case _EXCLUDE_FILETYPE:
                            excludeFiletype.Add(value);
                            break;
                        case _INCLUDE_SITE:
                            includeSite = true;
                            site = value;
                            break;
                        case _EXCLUDE_SITE:
                            includeSite = false;
                            site = value;
                            break;
                        case _LINK:
                            link = value;
                            break;
                        case _INFO:
                            webDocLocation = value;
                            break;
                        case _CACHE:
                            cacheDocLocation = value;
                            break;
                        default:
                            throw new NotImplementedException();
                            break;
                    }
                }
            }

            // anything that is left over is the querystring
            this.queryString = queryString;
        }
        #endregion

        #region Get/Set Properties
        public void setQueryString(String queryString)
        {
            this.queryString = queryString;
        }

        public void setInTitle(ArrayList inTitleTerms)
        {
            this.inTitleTerms = inTitleTerms;
        }

        public void setNotInTitle(ArrayList notInTitleTerms)
        {
            this.notInTitleTerms = notInTitleTerms;
        }

        public QueryTerm addInTitle(String term, bool include)
        {
            if (include) inTitleTerms.Add(term);
            else notInTitleTerms.Add(term);
            return this;
        }

        public void setAllInTitle(ArrayList allInTitleTerms)
        {
            this.allInTitleTerms = allInTitleTerms;
        }
               
        public void setInUrl(ArrayList inUrlTerms)
        {
            this.inUrlTerms = inUrlTerms;
        }

        public void setNotInUrl(ArrayList notInUrlTerms)
        {
            this.notInUrlTerms = notInUrlTerms;
        }

        public QueryTerm addInUrl(String term, bool include)
        {
            if (include) inUrlTerms.Add(term);
            else notInUrlTerms.Add(term);
            return this;
        }

        public void setAllInUrl(ArrayList allInUrlTerms)
        {
            this.allInUrlTerms = allInUrlTerms;
        }

        public void setIncludeFileType(ArrayList filetype)
        {
            this.includeFiletype = filetype;
        }

        public void setExcludeFileType(ArrayList filetype)
        {
            this.excludeFiletype = filetype;
        }
       
        public QueryTerm addFileType(String term, bool include)
        {
            if (include)
            {
                if (null == includeFiletype) includeFiletype = new ArrayList();
                includeFiletype.Add(term);
            }
            else
            {
                if (null == excludeFiletype) excludeFiletype = new ArrayList();
                excludeFiletype.Add(term);
            }
            return this;
        }

        public void setSite(String site, bool include)
        {
            this.includeSite = include;
            this.site = site;
        }

        public void setWebDocument(String docLocation)
        {
            this.webDocLocation = docLocation;
        }

        public void setCachedDocument(String docLocation)
        {
            this.cacheDocLocation = docLocation;
        }

        public void setWithLinksTo(String link)
        {
            this.link = link;
        }

        public void setDateRange(DateTime fromDate, DateTime toDate)
        {
            StringBuilder dateRange = new StringBuilder(fromDate.ToString("YYYY-MM-DD"));
            dateRange.Append("..");
            dateRange.Append(toDate.ToString("YYYY-MM-DD"));
            this.dateRange = dateRange.ToString();
        }
        #endregion

        #region getValue
        public String getValue()
        {
            String retval = null;
            StringBuilder qbuf = new StringBuilder();
            if (allInTitleTerms != null && allInTitleTerms.Count > 0) {
                qbuf.Append(_ALL_IN_TITLE).Append(Util.SeparatedString(allInTitleTerms, null, _SP)).Append(' ');
            }
            if (allInUrlTerms != null && allInUrlTerms.Count > 0) {
                qbuf.Append(_ALL_IN_URL).Append(Util.SeparatedString(allInUrlTerms, null, _SP)).Append(' ');
            }

            if (webDocLocation != null && webDocLocation.Length > 0) {
                qbuf.Append(_INFO).Append(webDocLocation).Append(' ');
            }

            if (cacheDocLocation != null && cacheDocLocation.Length > 0) {
                qbuf.Append(_CACHE).Append(cacheDocLocation).Append(' ');
            }
            if (link != null && link.Length > 0) {
                qbuf.Append(_LINK).Append(link);
            }

            if (inTitleTerms != null && inTitleTerms.Count > 0) {
                qbuf.Append(Util.SeparatedString(inTitleTerms, _IN_TITLE, _SP)).Append(' ');
            }
            if (notInTitleTerms != null && notInTitleTerms.Count > 0) {
                qbuf.Append(Util.SeparatedString(notInTitleTerms, _NOT_IN_TITLE, _SP)).Append(' ');
            }

            if (inUrlTerms != null && inUrlTerms.Count > 0) {
                qbuf.Append(Util.SeparatedString(inUrlTerms, _IN_URL, _SP)).Append(' ');
            }
            if (notInUrlTerms != null && notInUrlTerms.Count > 0) {
                qbuf.Append(Util.SeparatedString(notInUrlTerms, _NOT_IN_URL, _SP)).Append(' ');
            }

            if (site != null && site.Length > 0) {
                qbuf.Append(includeSite
                        ? _INCLUDE_SITE
                        : _EXCLUDE_SITE).Append(site).Append(' ');
            }

            if (includeFiletype != null && includeFiletype.Count > 0) {
                qbuf.Append(Util.SeparatedString(includeFiletype, _INCLUDE_FILETYPE, _OR)).Append(' ');
            }
            if (excludeFiletype != null && excludeFiletype.Count > 0) {
                qbuf.Append(Util.SeparatedString(excludeFiletype, _EXCLUDE_FILETYPE, _SP)).Append(' ');
            }
            if (dateRange != null) {
                qbuf.Append(_DATERANGE).Append(dateRange).Append(' ');
            }

            if (queryString != null) qbuf.Append(queryString);

            retval = qbuf.ToString();
            return retval;
        }
        #endregion       
    }
}
