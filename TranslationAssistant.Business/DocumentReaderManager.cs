// // ----------------------------------------------------------------------
// // <copyright file="DocumentReaderManager.cs" company="Microsoft Corporation">
// // Copyright (c) Microsoft Corporation.
// // All rights reserved.
// // THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// // KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// // IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// // PARTICULAR PURPOSE.
// // </copyright>
// // ----------------------------------------------------------------------
// // <summary>DocumentReaderManager.cs</summary>
// // ----------------------------------------------------------------------

namespace TranslationAssistant.Business
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Microsoft.Office.Core;
    using Microsoft.Office.Interop.Word;

    using TranslationAssistant.TranslationServices.Core;

    using Comment = DocumentFormat.OpenXml.Spreadsheet.Comment;

    #endregion

    public class DocumentReaderResult
    {
        public DocumentReaderResult(DocumentText texts)
        {
            Texts = texts;
        }
        public DocumentText Texts { get; set; }

    }

    public class DocumentText
    {
        private List<string> _textList;
        public IReadOnlyList<string> TextList => _textList;
        public DocumentText() => _textList = new List<string> { };

        public void Add(List<string> texts) => _textList.AddRange(texts);
    }

    /// <summary>
    ///     The get document text manager.
    /// </summary>
    public class DocumentReaderManager
    {
        #region Public Properties

        #endregion
        #region Public Methods and Operators

        /// <summary>
        ///     Do the get document text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDir">The is dir.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target langauge.</param>
        public static IEnumerable<DocumentReaderResult> GetDocumentText(string path, bool isDir, string targetLanguage, bool ignoreHidden = false)
        {
            return GetAllDocumentsToProcess(path, targetLanguage)
                .Select(t => GetDocumentTextInternal(t, ignoreHidden));
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The close process.
        /// </summary>
        /// <param name="processLike">
        ///     The process like.
        /// </param>
        private static void CloseProcess(string processLike)
        {
            Process[] ps = Process.GetProcesses();
            foreach (Process p in ps)
            {
                if (p.ProcessName.Contains(processLike))
                {
                    p.Kill();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        /// <summary>
        ///     The convert to docx.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>
        ///     The System.String.
        /// </returns>
        private static string ConvertToDocx(string fullPath, string targetLanguage)
        {
            LoggingManager.LogMessage("Converting the document " + fullPath + " from doc or pdf to docx.");
            object nullvalue = Type.Missing;

            //Microsoft.Office.Interop.Word.Application wordApp = (Microsoft.Office.Interop.Word.Application) 
            //Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("000209FF-0000-0000-C000-000000000046")));
            Microsoft.Office.Interop.Word.Application wordApp =
                new Microsoft.Office.Interop.Word.Application
                {
                    Visible = false
                };
            object file2 = GetOutputDocumentFullName(fullPath, targetLanguage);
            try
            {
                wordApp.Visible = false;
                object filee = fullPath;
                Microsoft.Office.Interop.Word.Document wordDoc = wordApp.Documents.Open(
                    ref filee,
                    nullvalue,
                    Missing.Value,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue,
                    nullvalue);

                if (fullPath.ToLowerInvariant().EndsWith(".doc"))
                {
                    wordDoc.Convert();
                }

                wordDoc.SaveAs(
                    ref file2,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue,
                    ref nullvalue);
                wordDoc.Close(ref nullvalue, nullvalue, nullvalue);
            }
            finally
            {
                // wordApp.Documents.Close(ref nullvalue, ref nullvalue, ref nullvalue);
                wordApp.Quit(ref nullvalue, ref nullvalue, ref nullvalue);
            }

            LoggingManager.LogMessage("Converted the document " + fullPath + " from doc or pdf to docx.");
            return file2.ToString();
        }

        /// <summary>
        ///     The convert to pptx.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>
        ///     The System.String.
        /// </returns>
        private static string ConvertToPptx(string fullPath, string targetLanguage)
        {
            LoggingManager.LogMessage("Converting the document " + fullPath + " from ppt to pptx.");
            
            object file2 = GetOutputDocumentFullName(fullPath, targetLanguage);
            Microsoft.Office.Interop.PowerPoint.Application powerPointApp =
                new Microsoft.Office.Interop.PowerPoint.Application();

            try
            {
                Microsoft.Office.Interop.PowerPoint.Presentation presentation =
                    powerPointApp.Presentations.Open(
                        fullPath,
                        MsoTriState.msoFalse,
                        MsoTriState.msoFalse,
                        MsoTriState.msoFalse);

                presentation.SaveAs(
                    file2.ToString(),
                    Microsoft.Office.Interop.PowerPoint.PpSaveAsFileType.ppSaveAsDefault,
                    MsoTriState.msoTriStateMixed);
                presentation.Close();
            }
            finally
            {
                powerPointApp.Quit();
            }

            LoggingManager.LogMessage("Converted the document " + fullPath + " from ppt to pptx.");
            return file2.ToString();
        }

        /// <summary>
        ///     The convert to xlsx.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>
        ///     The System.String.
        /// </returns>
        private static string ConvertToXlsx(string fullPath, string targetLanguage)
        {
            LoggingManager.LogMessage("Converting the document " + fullPath + " from xls to xlsx.");
            
            object file2 = GetOutputDocumentFullName(fullPath, targetLanguage);
                Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application
                                                                          {
                                                                              Visible = false
                                                                          };
            try
            {

                Microsoft.Office.Interop.Excel.Workbook eWorkbook = excelApp.Workbooks.Open(
                    fullPath,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing);

                eWorkbook.SaveAs(
                    file2,
                    Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing);

                eWorkbook.Close(false, Type.Missing, Type.Missing);
            }
            finally
            {
                excelApp.Quit();
            }

            LoggingManager.LogMessage("Converted the document " + fullPath + " from xls to xlsx.");
            return file2.ToString();
        }

        /// <summary>
        ///     The get document text internal.
        /// </summary>
        /// <param name="fullNameForDocumentToProcess">The full name for document to process.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target langauge.</param>
        private static DocumentReaderResult GetDocumentTextInternal(
            string fullNameForDocumentToProcess,
            bool ignoreHidden = false)
        {
            DocumentReaderResult result = null;
            try
            {
                if (fullNameForDocumentToProcess.ToLowerInvariant().EndsWith(".docx"))
                {
                    result = ProcessWordDocument(fullNameForDocumentToProcess, ignoreHidden);
                }
                else if (fullNameForDocumentToProcess.ToLowerInvariant().EndsWith(".xlsx"))
                {
                    result = ProcessExcelDocument(fullNameForDocumentToProcess, ignoreHidden);
                }
                else if (fullNameForDocumentToProcess.ToLowerInvariant().EndsWith(".pptx"))
                {
                    result = ProcessPowerPointDocument(fullNameForDocumentToProcess, ignoreHidden);
                }
                else if (fullNameForDocumentToProcess.ToLowerInvariant().EndsWith(".txt") || fullNameForDocumentToProcess.ToLowerInvariant().EndsWith(".text"))
                {
                    result = ProcessTextDocument(fullNameForDocumentToProcess);
                }
            }
            catch (AggregateException ae)
            {
                var errorMessage = String.Empty;
                foreach (var ex in ae.InnerExceptions)
                {
                    errorMessage = errorMessage + " " + ex.Message;
                    LoggingManager.LogError(string.Format("{0}:{1}", fullNameForDocumentToProcess, ex.Message + ex.StackTrace));
                }

                throw new Exception(errorMessage);
            }
            catch (Exception ex)
            {
                LoggingManager.LogError(
                    string.Format("{0}:{1}", fullNameForDocumentToProcess, ex.Message + ex.StackTrace));
                throw;
            }
            return result;
        }

        /// <summary>
        ///     Gets all documents to process.
        /// </summary>
        /// <param name="documentPath">The document path.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>All documents to process.</returns>
        private static List<string> GetAllDocumentsToProcess(string documentPath, string targetLanguage)
        {
            List<string> allFiles = new List<string>();
            var expectedOutputDocumentName = GetOutputDocumentFullName(documentPath, targetLanguage);
            if (File.Exists(expectedOutputDocumentName))
            {
                File.Delete(expectedOutputDocumentName);
            }
            var outputDocumentName = GetOutputDocumentFullName(documentPath, targetLanguage);

            if (documentPath.ToLowerInvariant().EndsWith(".doc") || documentPath.ToLowerInvariant().EndsWith(".pdf"))
            {
                var convertedDocumentPath = ConvertToDocx(documentPath, targetLanguage);
                allFiles.Add(convertedDocumentPath);
            }
            else if (documentPath.ToLowerInvariant().EndsWith(".ppt"))
            {
                var convertedDocumentPath = ConvertToPptx(documentPath, targetLanguage);
                allFiles.Add(convertedDocumentPath);
            }
            else if (documentPath.ToLowerInvariant().EndsWith(".xls"))
            {
                var convertedDocumentPath = ConvertToXlsx(documentPath, targetLanguage);
                allFiles.Add(convertedDocumentPath);
            }
            else
            {
                File.Copy(documentPath, outputDocumentName);
                allFiles.Add(outputDocumentName);
            }

            return allFiles;
        }

        /// <summary>
        ///     The get output document full name.
        /// </summary>
        /// <param name="documentFullName">The document full name.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>
        ///     The System.String.
        /// </returns>
        private static string GetOutputDocumentFullName(string documentFullName, string targetLanguage)
        {
            var outputDocumentNameWithoutExtension = documentFullName.Substring(0, documentFullName.LastIndexOf(".", StringComparison.Ordinal))
                                                     + "." + TranslationServiceFacade.LanguageNameToLanguageCode(targetLanguage);
            if (documentFullName.ToLowerInvariant().EndsWith(".xls") || documentFullName.ToLowerInvariant().EndsWith(".xlsx"))
            {
                return outputDocumentNameWithoutExtension + ".xlsx";
            }

            if (documentFullName.ToLowerInvariant().EndsWith(".ppt") || documentFullName.ToLowerInvariant().EndsWith(".pptx"))
            {
                return outputDocumentNameWithoutExtension + ".pptx";
            }

            if (documentFullName.ToLowerInvariant().EndsWith(".txt"))
            {
                return outputDocumentNameWithoutExtension + ".txt";
            }

            if (documentFullName.ToLowerInvariant().EndsWith(".doc") || documentFullName.ToLowerInvariant().EndsWith(".docx") || documentFullName.ToLowerInvariant().EndsWith(".pdf"))
            {
                return outputDocumentNameWithoutExtension + ".docx";
            }

            return outputDocumentNameWithoutExtension + documentFullName.Substring(documentFullName.LastIndexOf(".", StringComparison.Ordinal));
        }


        private static void ProcessHTMLDocument(string fullNameForDocumentToProcess, string sourceLanguage, string targetLanguage)
        {
            HTMLTranslationManager.DoTranslation(fullNameForDocumentToProcess, sourceLanguage, targetLanguage);
        }

        private static void ProcessSRTDocument(string fullNameForDocumentToProcess, string sourceLanguage, string targetLanguage)
        {
            SRTTranslationManager.DoTranslation(fullNameForDocumentToProcess, sourceLanguage, targetLanguage);
        }


        /// <summary>
        /// Translates a plain text document in UTF8 encoding to the target language.
        /// </summary>
        /// <param name="fullNameForDocumentToProcess">SOurce document file name</param>
        /// <param name="sourceLanguage">From language</param>
        /// <param name="targetLanguage">To language</param>
        private static DocumentReaderResult ProcessTextDocument(string fullNameForDocumentToProcess)
        {
            CharCounts counts = new CharCounts();
            DocumentText extractedText = new DocumentText();
            var document = File.ReadAllLines(fullNameForDocumentToProcess, Encoding.UTF8);
            List<string> lstTexts = new List<string>(document);
            extractedText.Add(lstTexts);
            File.Delete(fullNameForDocumentToProcess);

            return new DocumentReaderResult(extractedText);
        }


        private static DocumentReaderResult ProcessExcelDocument(
            string outputDocumentFullName,
            bool ignoreHidden = false)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(outputDocumentFullName, true))
            {
                CharCounts counts = new CharCounts();
                DocumentText extractedText = new DocumentText();
                //document.WorkbookPart.SharedStringTablePart.PutXDocument();
                List<DocumentFormat.OpenXml.Spreadsheet.Text> lstTexts = new List<DocumentFormat.OpenXml.Spreadsheet.Text>();
                foreach (SharedStringItem si in document.WorkbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>())
                {
                    if (si != null && si.Text != null && !String.IsNullOrEmpty(si.Text.Text))
                    {
                        lstTexts.Add(si.Text);
                    }
                    else if (si != null)
                    {
                        lstTexts.AddRange(si.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Where(item => (item != null && item.Text != null && !String.IsNullOrEmpty(item.Text.Text))).Select(item => item.Text));
                    }
                }

                var batch = lstTexts.Select(item => item.Text);
                IEnumerable<string> values = batch as string[] ?? batch.ToArray();
                extractedText.Add(values.ToList());

                // Get comments
                WorkbookPart workBookPart = document.WorkbookPart;
                List<DocumentFormat.OpenXml.Spreadsheet.Comment> lstComments = new List<DocumentFormat.OpenXml.Spreadsheet.Comment>();
                foreach (WorksheetCommentsPart commentsPart in workBookPart.WorksheetParts.SelectMany(sheet => sheet.GetPartsOfType<WorksheetCommentsPart>()))
                {
                    lstComments.AddRange(commentsPart.Comments.CommentList.Cast<Comment>());
                }
                var batchComments = lstComments.Select(item => item.InnerText);
                extractedText.Add(batchComments.ToList());

                return new DocumentReaderResult (extractedText);
            }
        }

        private static DocumentReaderResult ProcessPowerPointDocument(string outputDocumentFullName, bool ignoreHidden = false)
        {
            using (PresentationDocument doc = PresentationDocument.Open(outputDocumentFullName, true))
            {
                //doc.PresentationPart.PutXDocument();
                CharCounts counts = new CharCounts();
                DocumentText extractedText = new DocumentText();
                List<DocumentFormat.OpenXml.Drawing.Text> texts = new List<DocumentFormat.OpenXml.Drawing.Text>();
                List<DocumentFormat.OpenXml.Drawing.Text> notes = new List<DocumentFormat.OpenXml.Drawing.Text>();
                List<DocumentFormat.OpenXml.Presentation.Comment> lstComments = new List<DocumentFormat.OpenXml.Presentation.Comment>();
           
                var slideParts = doc.PresentationPart.SlideParts;
                if (slideParts != null)
                {
                    foreach (var slidePart in slideParts)
                    {
                        if (slidePart.Slide != null)
                        {
                            var slide = slidePart.Slide;
                            ExtractTextContent(texts, slide);

                            var commentsPart = slidePart.SlideCommentsPart;
                            if (commentsPart != null)
                            {
                                lstComments.AddRange(commentsPart.CommentList.Cast<DocumentFormat.OpenXml.Presentation.Comment>());
                            }

                            var notesPart = slidePart.NotesSlidePart;
                            if (notesPart != null)
                            {
                                ExtractTextContent(notes, notesPart.NotesSlide);
                            }
                        }
                    }

                    var batchText = texts.Select(text => text.Text);
                    extractedText.Add(batchText.ToList());

                    var batchNotes = notes.Select(text => text.Text);
                    extractedText.Add(batchNotes.ToList());

                    var batchComments = lstComments.Select(text => text.InnerText);
                    extractedText.Add(batchComments.ToList());
                }
                    
                //doc.PresentationPart.PutXDocument();
                return new DocumentReaderResult(extractedText);
            }
        }

        private static void ExtractTextContent(List<DocumentFormat.OpenXml.Drawing.Text> textList, DocumentFormat.OpenXml.OpenXmlElement element)
        {
            foreach (DocumentFormat.OpenXml.Drawing.Paragraph para in element.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
            {
                textList.AddRange(para.Elements<DocumentFormat.OpenXml.Drawing.Run>().Where(item => (item != null && item.Text != null && !String.IsNullOrEmpty(item.Text.Text))).Select(item => item.Text));
            }
        }

        private static DocumentReaderResult ProcessWordDocument(
            string outputDocumentFullName,
            bool ignoreHidden = false)
        {
            DocumentText extractedText = new DocumentText();
            CharCounts counts = new CharCounts();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(outputDocumentFullName, true))
            {

                OpenXmlPowerTools.SimplifyMarkupSettings settings = new OpenXmlPowerTools.SimplifyMarkupSettings
                {
                    AcceptRevisions=true,
                    NormalizeXml = true,         //setting this to false reduces translation quality, but if true some documents have XML format errors when opening
                    RemoveBookmarks = true,
                    RemoveComments = true,
                    RemoveContentControls = true,
                    RemoveEndAndFootNotes = true,
                    RemoveFieldCodes = true,
                    RemoveGoBackBookmark = true,
                    //RemoveHyperlinks = false,
                    RemoveLastRenderedPageBreak = true,
                    RemoveMarkupForDocumentComparison = true,
                    RemovePermissions = false,
                    RemoveProof = true,
                    RemoveRsidInfo = true,
                    RemoveSmartTags = true,
                    RemoveSoftHyphens = true,
                    RemoveWebHidden = true,
                    ReplaceTabsWithSpaces = false
                };
                OpenXmlPowerTools.MarkupSimplifier.SimplifyMarkup(doc, settings);
            }

            List<DocumentFormat.OpenXml.Wordprocessing.Text> texts = new List<DocumentFormat.OpenXml.Wordprocessing.Text>();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(outputDocumentFullName, true))
            {
                var body = doc.MainDocumentPart.Document.Body;
                texts.AddRange(body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                    .Where(text => !String.IsNullOrEmpty(text.Text) && text.Text.Length > 0));

                var headers = doc.MainDocumentPart.HeaderParts.Select(p => p.Header);
                foreach (var header in headers)
                {
                    texts.AddRange(header.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Where(text => !String.IsNullOrEmpty(text.Text) && text.Text.Length > 0));
                }

                var footers = doc.MainDocumentPart.FooterParts.Select(p => p.Footer);
                foreach (var footer in footers)
                {
                    texts.AddRange(footer.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Where(text => !String.IsNullOrEmpty(text.Text) && text.Text.Length > 0));
                }

                if (ignoreHidden)
                {
                    texts.RemoveAll(t => t.Parent.Descendants<Vanish>().Any());
                }

                var exceptions = new ConcurrentQueue<Exception>();

                // Extract Text for Translation
                var batch = texts.Select(text => text.Text);
                extractedText.Add(batch.ToList());

            }
            return new DocumentReaderResult(extractedText);
        }


        /// <summary>
        /// Splits the list.
        /// </summary>
        /// <param name="values">
        ///  The values to be split.
        /// </param>
        /// <param name="groupSize">
        ///  The group size.
        /// </param>
        /// <param name="maxSize">
        ///  The max size.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///  The System.Collections.Generic.List`1[T -&gt; System.Collections.Generic.List`1[T -&gt; T]].
        /// </returns>
        private static List<List<T>> SplitList<T>(IEnumerable<T> values, int groupSize, int maxSize)
        {
            List<List<T>> result = new List<List<T>>();
            List<T> valueList = values.ToList();
            int startIndex = 0;
            int count = valueList.Count;

            while (startIndex < count)
            {
                int elementCount = (startIndex + groupSize > count) ? count - startIndex : groupSize;
                while (true)
                {
                    var aggregatedSize =
                        valueList.GetRange(startIndex, elementCount)
                            .Aggregate(
                                new StringBuilder(),
                                (s, i) => s.Length < maxSize ? s.Append(i) : s,
                                s => s.ToString())
                            .Length;
                    if (aggregatedSize >= maxSize)
                    {
                        if (elementCount == 1) break;
                        elementCount = elementCount - 1;
                    }
                    else
                    {
                        break;
                    }
                }

                result.Add(valueList.GetRange(startIndex, elementCount));
                startIndex += elementCount;
            }

            return result;
        }

        #endregion
    }
}
