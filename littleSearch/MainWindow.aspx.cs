using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using littleSearch.Code;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using PanGu;
using PanGu.HighLight;

namespace littleSearch
{
    public partial class MainWindow : System.Web.UI.Page
    {

        private string strIndexPath = string.Empty;
        //protected string txtTitle = string.Empty;
        protected string txtContent = string.Empty;
        protected long lSearchTime = 0;
        protected Lucene.Net.Store.Directory Lu_IndexDic {
            get
            { return Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDic)); }
                 }

        protected int time = 1;
        protected string State = string.Empty;
        protected IList<Record> list = new List<Record>();
        protected string txtPageFoot = string.Empty;
        //protected IndexManager indexManager;

        protected string PanGuXmlPath
        {
            get
            {
                return Server.MapPath("/PanGu/PanGu.xml");
            }
        }
        protected string IndexDic
        {
            get
            {
                return Server.MapPath("/IndexDic");
                //return "/IndexDic";
            }
        }
        protected string Action
        {
            get
            {
                if (Request.Form["action"] != null)
                {
                    return Request.Form["action"].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
        protected Analyzer PanGuAnalyzer
        {
            get { return new PanGuAnalyzer(); }
        }
        private int PageSize
        {
            get
            {
                if (Request.Form["pageSize"] != null)
                {
                    return Convert.ToInt32(Request.Form["pageSize"]);
                }
                else
                {
                    return 10;
                }
            }
        }
        /// <summary>
        /// 页码
        /// </summary>
        private int PageIndex
        {
            get
            {
                if (Request.Form["pageIndex"] != null)
                {
                    return Convert.ToInt32(Request.Form["pageIndex"]);
                }
                else
                {
                    return 1;
                }
            }
        }

       


        private void SearchIndex()
        {
            //State = "entery search";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            BooleanQuery bQuery = new BooleanQuery();
           
            string keyword = Request.Form["content"].ToString();
            string search = GetKeyWordsSplitBySpace(keyword);

            //Query bQuery = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_30, new string[] { keyword,keyword }, new string[] { "Title", "Content" }, PanGuAnalyzer);

            if (search != null && search != "")
            {
                //title = GetKeyWordsSplitBySpace(Request.Form["title"].ToString());
                //QueryParser parseTitle = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", PanGuAnalyzer);
                //parseTitle.DefaultOperator = QueryParser.Operator.OR;
                // Query queryT = parseTitle.Parse(search);

                //MultiFieldQueryParser multiFieldQueryParser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, new string[2] { "Title", "Content" }, PanGuAnalyzer);
                QueryParser parserContent = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Content", PanGuAnalyzer);
                parserContent.DefaultOperator = QueryParser.Operator.OR;
                Query queryC = parserContent.Parse(search);


                //bQuery.Add(queryT, Occur.MUST);
                bQuery.Add(queryC, Occur.MUST);

               // TermQuery t1 = new TermQuery(new Lucene.Net.Index.Term("Content", keyword));
               // TermQuery t2 = new TermQuery(new Lucene.Net.Index.Term("Title", keyword));
              //  bQuery.Add(t1, Occur.MUST);
               // bQuery.Add(t2, Occur.MUST);

                
                //bQuery.Add(queryC, Occur.MUST);
                dic.Add("title", keyword);
                txtContent = keyword;
                // txtTitle = Request.Form["title"].ToString();
            }
          
            if (bQuery != null )
            {

                //Lucene.Net.Store.Directory ad = Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDic));
                IndexSearcher searcher = new IndexSearcher(Lu_IndexDic, true);
                Stopwatch stopwatch = Stopwatch.StartNew();


                //Sort sort = new Sort(new SortField("Title", SortField.DOC, true));
                
                TopDocs docs = searcher.Search(bQuery, (Filter)null, PageSize * PageIndex);
                stopwatch.Stop();
                //State = "search num " + docs.TotalHits.ToString();
                if (docs != null && docs.TotalHits > 0)
                {
                    lSearchTime = stopwatch.ElapsedMilliseconds;
                    txtPageFoot = GetPageFoot(PageIndex, PageSize, docs.TotalHits, "sabrosus");
                    
                    for (int i = 0; i < docs.TotalHits; i++)
                    {
                        if (i >= (PageIndex - 1) * PageSize && i < PageIndex * PageSize)
                        {
                            Document doc = searcher.Doc(docs.ScoreDocs[i].Doc);
                            Record model = new Record()
                            {
                                Title = doc.Get("Title").ToString(),
                                Content = doc.Get("Content").ToString(),
                                // AddTime = doc.Get("AddTime").ToString(),
                                Uri = doc.Get("Uri").ToString()
                            };
                            // re += model.Title + "    " + model.Uri;
                            //Console.WriteLine(model.Title + "    " + model.Uri);
                            //re.Add(SetHighlighter(keyword, model));
                            list.Add(SetHighlighter(keyword, model));
                           // list.Add(model);
                        }
                    }
                }
            }
           
        }
        private Record SetHighlighter(string  Keywords, Record model)
        {
            SimpleHTMLFormatter simpleHTMLFormatter = new PanGu.HighLight.SimpleHTMLFormatter("<font color=\"red\">", "</font>");
            Highlighter highlighter = new PanGu.HighLight.Highlighter(simpleHTMLFormatter, new Segment());
            highlighter.FragmentSize = 50;
            string strTitle;
            string strContent;


          
           strTitle = highlighter.GetBestFragment(Keywords, model.Title);
            if (strTitle != "")
                model.Title = strTitle;
           
          strContent = highlighter.GetBestFragment(Keywords, model.Content);

            if (strContent != "")
                model.Content = strContent;
            else if(model.Content!="")
            {
                Random r = new Random();
                int start = r.Next(0, model.Content.Length - 1);
                int len = model.Content.Length-1 - start;
                if (len > 20)
                    model.Content = model.Content.Substring(start, 20);
                else
                    model.Content = model.Content.Substring(start);
            }
            
                
            return model;
        }

        private string GetKeyWordsSplitBySpace(string keywords)
        {
            PanGuTokenizer ktTokenizer = new PanGuTokenizer();
            StringBuilder result = new StringBuilder();
            ICollection<WordInfo> words = ktTokenizer.SegmentToWordInfos(keywords);
            foreach (WordInfo word in words)
            {
                if (word == null)
                {
                    continue;
                }
                result.AppendFormat("{0}^{1}.0 ", word.Word, (int)Math.Pow(3, word.Rank));
            }
            return result.ToString().Trim();
        }

        private string GetPageFoot(int currentPageIndex, int pageSize, int total, string cssName)
        {
            currentPageIndex = currentPageIndex <= 0 ? 1 : currentPageIndex;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            string options = string.Empty;
            int pageCount = 0;//总页数
            int pageVisibleCount = 10; // 显示数量
            if (total % pageSize == 0)
            {
                pageCount = total / pageSize;
            }
            else
            {
                pageCount = total / pageSize + 1;
            }
            //如果是整除的话,退后一页
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<div class=\"page_left\">一页显示<select id=\"pageSize\" name=\"pageSize\" onchange =\"SC.Page.ChangeSize();\">{0}</select>&nbsp;条&nbsp;&nbsp;&nbsp;总共{1}条</div>", SetOption(pageSize), total);
            sb.AppendFormat("<div class=\"page_right\">跳转到第<input type=\"text\" id=\"pageIndex\" name=\"pageIndex\" value=\"{0}\" />页<a href=\"#\" class=\"easyui-linkbutton\" plain=\"true\" iconCls=\"icon-redo\" onclick=\"SC.Page.GotoPage();\">Go</a>共<span id=\"pageCount\">" + pageCount + "</span>&nbsp;页</div><input type=\"hidden\" id=\"isSearch\" name=\"isSearch\" value=\"1\" />", currentPageIndex);

            sb.Append("<div class='" + cssName + "'>");// sbrosus分页样式，需要自己添加哇


            if (currentPageIndex == 1 || total < 1)
            {
                sb.Append("<span ><a href='javascript:void(0)'>首页</a></span>");
                sb.Append("<span ><a href='javascript:void(0)'>上一页</a></span>");
            }
            else
            {
                sb.Append("<span><a onclick=\"SC.Page.GetPage(1)\">首页</a></span>");
                sb.Append("<span><a onclick=\"SC.Page.GetPage(" + (currentPageIndex - 1).ToString() + ")\">上一页</a></span>");
            }
            int i = 1;
            int k = pageVisibleCount > pageCount ? pageCount : pageVisibleCount;
            if (currentPageIndex > pageVisibleCount)
            {
                i = currentPageIndex / pageVisibleCount * pageVisibleCount;
                k = (i + pageVisibleCount) > pageCount ? pageCount : (i + pageVisibleCount);
            }
            for (; i <= k; i++)//k*10防止k为负数
            {
                if (i == currentPageIndex)
                {
                    sb.AppendFormat("<span class='current' ><a href='javascript:void(0)'>{0}</a></span>&nbsp;", i);
                }
                else
                {
                    sb.AppendFormat("<span><a onclick=\"SC.Page.GetPage(" + i + ")\" >{0}</a></span>&nbsp;", i);
                }
            }
            if (currentPageIndex == pageCount || total < 1)
            {
                sb.Append("<span ><a href='javascript:void(0)'>下一页</a></span>");
                sb.Append("<span ><a href='javascript:void(0)'>尾页</a></span>");
            }
            else
            {
                sb.Append("<span><a onclick=\"SC.Page.GetPage(" + (currentPageIndex + 1).ToString() + ")\">下一页</a></span>");
                sb.Append("<span><a onclick=\"SC.Page.GetPage(" + pageCount + ")\">尾页</a></span></div>");
            }
            return sb.ToString();
        }

        private string SetOption(int pageSize)
        {
            StringBuilder sb_options = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (pageSize / 10 == i + 1)
                {
                    sb_options.AppendFormat("<option selected=\"selected\">{0}0</option>", (i + 1).ToString());
                }
                else
                {
                    sb_options.AppendFormat("<option>{0}0</option>", (i + 1).ToString());
                }
            }
            if (pageSize == 1000)
            {
                sb_options.Append("<option selected=\"selected\">1000</option>");
            }
            else
            {
                sb_options.Append("<option >1000</option>");
            }

            return sb_options.ToString();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            State = "page load" + time.ToString() ;
            //time += 1;
            //PanGu.Segment.Init(PanGuXmlPath);
            State = Action;
            switch (Action)
            {
                 
                //  case "CreateIndex": CreateIndex(Cover); break;
                case "SearchIndex":
                    {
                       
                        SearchIndex(); break; }
            }
        }


    }
}