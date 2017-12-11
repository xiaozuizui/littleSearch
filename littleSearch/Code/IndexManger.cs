using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using PanGu;
using PanGu.HighLight;
using Lucene.Net.Analysis.PanGu;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;

namespace littleSearch.Code
{
    public class IndexManager
    {
        //public static readonly IndexManager bookIndex = new IndexManager();
        // public static readonly string indexPath = HttpContext.Current.Server.MapPath("~/IndexData");

        //private int PageIndex = 1;
        //private int PageSize = 10;
        private IndexWriter writer;
        protected Analyzer pgAnalyzer
        {
            get { return new PanGuAnalyzer(); }
        }
        //protected string IndexDic { get; set; }

        protected Lucene.Net.Store.Directory Lu_IndexDic { get; set; }


    

        public IndexManager(string IndexDic)
        {
            if (!System.IO.Directory.Exists(IndexDic))
            {
                System.IO.Directory.CreateDirectory(IndexDic);
                System.Console.WriteLine(IndexDic);
                // Lucene.Net.Store.Directory dd =  Lucene.Net.Store
            }
            Lu_IndexDic = Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDic));
        }


        public int SearchIndex(string keyword, Page pg, List<Record> re)
        {
            int tothit = 0;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            BooleanQuery bQuery = new BooleanQuery();

            string st = GetKeyWordsSplitBySpace(keyword);
            if (st != null && st != "")
            {
                //title = GetKeyWordsSplitBySpace(Request.Form["title"].ToString());

                //QueryParser parseTitle = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", pgAnalyzer);
                //parseTitle.DefaultOperator = QueryParser.Operator.AND;
                //Query queryT = parseTitle.Parse(GetKeyWordsSplitBySpace(st));
                //bQuery.Add(queryT, Occur.MUST);


                QueryParser parseContent = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Content", pgAnalyzer);
                parseContent.DefaultOperator = QueryParser.Operator.AND;
                // Query queryC = parseContent.Parse(GetKeyWordsSplitBySpace(st));
                Query queryC = parseContent.Parse(st);
                bQuery.Add(queryC, Occur.MUST);


                dic.Add("title", st);
                // txtTitle = Request.Form["title"].ToString();
            }
            //if (Request.Form["content"] != null && Request.Form["content"].ToString() != "")
            //{
            //    content = GetKeyWordsSplitBySpace(Request.Form["content"].ToString());
            //    QueryParser parse = new QueryParser("Content", PanGuAnalyzer);
            //    Query query = parse.Parse(content);
            //    parse.SetDefaultOperator(QueryParser.Operator.AND);
            //    bQuery.Add(query, BooleanClause.Occur.MUST);
            //    dic.Add("content", Request.Form["content"].ToString());
            //    txtContent = Request.Form["content"].ToString();
            //}
            if (bQuery != null && bQuery.GetClauses().Length > 0)
            {

                //Lucene.Net.Store.Directory ad = Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDic));
                IndexSearcher search = new IndexSearcher(Lu_IndexDic, true);
                Stopwatch stopwatch = Stopwatch.StartNew();

                Sort sort = new Sort(new SortField("Title", SortField.DOC, true));
                TopDocs docs = search.Search(bQuery, (Filter)null, pg.PageSize * pg.PageIndex, sort);
                stopwatch.Stop();
                if (docs != null && docs.TotalHits > 0)
                {
                    // lSearchTime = stopwatch.ElapsedMilliseconds;
                    //txtPageFoot = GetPageFoot(PageIndex, PageSize, docs.totalHits, "sabrosus");
                    tothit = docs.TotalHits;
                    for (int i = 0; i < docs.TotalHits; i++)
                    {
                        if (i >= (pg.PageIndex - 1) * pg.PageSize && i < pg.PageIndex * pg.PageSize)
                        {
                            Document doc = search.Doc(docs.ScoreDocs[i].Doc);
                            Record model = new Record()
                            {
                                Title = doc.Get("Title").ToString(),
                                Content = doc.Get("Content").ToString(),
                                // AddTime = doc.Get("AddTime").ToString(),
                                Uri = doc.Get("Uri").ToString()
                            };
                            // re += model.Title + "    " + model.Uri;
                            Console.WriteLine(model.Title + "    " + model.Uri);
                            re.Add(SetHighlighter(keyword, model));
                            //list.Add(SetHighlighter(dicKeywords, model));
                        }
                    }
                }
            }
            return tothit;
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

        private Record SetHighlighter(string Keywords, Record model)
        {
            SimpleHTMLFormatter simpleHTMLFormatter = new PanGu.HighLight.SimpleHTMLFormatter("<font color=\"red\">", "</font>");
            Highlighter highlighter = new PanGu.HighLight.Highlighter(simpleHTMLFormatter, new Segment());
            highlighter.FragmentSize = 50;
            // string strTitle = string.Empty;
            //string strContent = string.Empty;
            //dicKeywords.TryGetValue("title", out strTitle);
            //dicKeywords.TryGetValue("content", out strContent);
            // model.Title = highlighter.GetBestFragment(Keywords, model.Title);

            model.Content = highlighter.GetBestFragment(Keywords, model.Content);

            return model;
        }

        //请求队列 解决索引目录同时操作的并发问题

        /// <summary>
        /// 新增Books表信息时 添加邢增索引请求至队列
        /// </summary>
        /// <param name="books"></param>
        //public void Add(Books books)
        //{
        //    BookViewMode bvm = new BookViewMode();
        //    bvm.Id = books.Id;
        //    bvm.Title = books.Title;
        //    bvm.IT = IndexType.Insert;
        //    bvm.Content = books.ContentDescription;
        //    bookQueue.Enqueue(bvm);
        //}
        /// <summary>
        /// 删除Books表信息时 添加删除索引请求至队列
        /// </summary>
        /// <param name="bid"></param>

        /// <summary>
        /// 修改Books表信息时 添加修改索引(实质上是先删除原有索引 再新增修改后索引)请求至队列
        /// </summary>
        /// <param name="books"></param>
        //public void Mod(Books books)
        //{
        //    BookViewMode bvm = new BookViewMode();
        //    bvm.Id = books.Id;
        //    bvm.Title = books.Title;
        //    bvm.IT = IndexType.Modify;
        //    bvm.Content = books.ContentDescription;
        //    bookQueue.Enqueue(bvm);
        //}



        //定义一个线程 将队列中的数据取出来 插入索引库中

        /// <summary>
        /// 更新索引库操作
        /// </summary>
        //    private void CRUDIndex()
        //    {
        //        FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());
        //        bool isExist = IndexReader.IndexExists(directory);
        //        if (isExist)
        //        {
        //            if (IndexWriter.IsLocked(directory))
        //            {
        //                IndexWriter.Unlock(directory);
        //            }
        //        }

        //        IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), !isExist, IndexWriter.MaxFieldLength.UNLIMITED);
        //        while (bookQueue.Count > 0)
        //        {
        //            Document document = new Document();
        //            BookViewMode book = bookQueue.Dequeue();
        //            if (book.IT == IndexType.Insert)
        //            {
        //                document.Add(new Field("id", book.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
        //                document.Add(new Field("title", book.Title, Field.Store.YES, Field.Index.ANALYZED,
        //                                       Field.TermVector.WITH_POSITIONS_OFFSETS));
        //                document.Add(new Field("content", book.Content, Field.Store.YES, Field.Index.ANALYZED,
        //                                       Field.TermVector.WITH_POSITIONS_OFFSETS));
        //                writer.AddDocument(document);
        //            }
        //            else if (book.IT == IndexType.Delete)
        //            {
        //                writer.DeleteDocuments(new Term("id", book.Id.ToString()));
        //            }
        //            else if (book.IT == IndexType.Modify)
        //            {
        //                //先删除 再新增
        //                writer.DeleteDocuments(new Term("id", book.Id.ToString()));
        //                document.Add(new Field("id", book.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
        //                document.Add(new Field("title", book.Title, Field.Store.YES, Field.Index.ANALYZED,
        //                                       Field.TermVector.WITH_POSITIONS_OFFSETS));
        //                document.Add(new Field("content", book.Content, Field.Store.YES, Field.Index.ANALYZED,
        //                                       Field.TermVector.WITH_POSITIONS_OFFSETS));
        //                writer.AddDocument(document);
        //            }
        //        }
        //        writer.Dispose();
        //        directory.Dispose();
        //    }
        //}

        
    }
    public class Page
    {
        public Page(int size, int index)
        {
            PageSize = size;
            PageIndex = index;
        }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }

    public class Record
    {



        public string Title
        {
            set;
            get;
        }

        public string Content
        {
            set;
            get;
        }
        //public string AddTime { get;  set; }
        public string Uri { get; set; }
    }
}
