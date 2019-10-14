using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

using Ifc2Json.Schema;
using Ifc2Json.Schema.DOC;

using BuildingSmart.Serialization;
using BuildingSmart.Serialization.Step;
using BuildingSmart.Serialization.Xml;

#if MDB
    using IfcDoc.Format.MDB;
#endif

namespace Ifc2Json
{
    class ifc2json_cmd
    {
        // 参照FormEdit，建立初始数据
        // file state
        string m_file, Text; // the path of the current file, or null if no file yet
        bool m_modified; // whether file has been modified such that user is prompted to save upon exiting or loading another file
        bool m_treesel; // tree selection changing, so don't react to initialization events

        // edit state

        DocProject m_project; // the root project object



        // maps type to image index for treeview
        internal static Type[] s_imagemap = new Type[]
        {
            null,
            typeof(DocAnnex),
            typeof(DocAttribute),
            typeof(DocConstant),
            typeof(DocDefined),
            typeof(DocEntity),
            typeof(DocEnumeration),
            typeof(DocFunction),
            typeof(DocGlobalRule),
            typeof(DocProperty),
            typeof(DocPropertySet),
            typeof(DocQuantity),
            typeof(DocQuantitySet),
            typeof(DocSchema),
            typeof(DocSection),
            typeof(DocSelect),
            typeof(DocTemplateItem),
            typeof(DocTemplateUsage),
            typeof(DocUniqueRule),
            typeof(DocWhereRule),
            typeof(DocReference),
            null,
            null,
            typeof(DocTemplateDefinition),
            typeof(DocModelView),
            typeof(DocExchangeDefinition),
            typeof(DocExchangeItem),
            typeof(DocModelRuleAttribute),
            typeof(DocModelRuleEntity),
            typeof(DocChangeSet),
            typeof(DocConceptRoot),
            typeof(DocExample),
            typeof(DocPropertyEnumeration),
            typeof(DocPropertyConstant),
            typeof(DocComment),
            typeof(DocPrimitive),
            typeof(DocPageTarget),
            typeof(DocPageSource),
            typeof(DocSchemaRef),
            typeof(DocDefinitionRef),
            typeof(DocTerm),
            typeof(DocAbbreviation),
            typeof(DocPublication),
            typeof(DocAnnotation),
        };

        private const int ImageIndexTemplateEntity = 20;
        private const int ImageIndexTemplateEnum = 21;
        private const int ImageIndexAttributeInverse = 22;
        private const int ImageIndexAttributeDerived = 27;


        public ifc2json_cmd()
        {

            this.m_file = null;
            this.m_modified = false;
            this.m_treesel = false;


            // 初始化变量，替代下面的函数
            // this.toolStripMenuItemFileNew_Click(this, EventArgs.Empty);
            this.SetCurrentFile(null);


            // init defaults
            this.m_project = new DocProject();


            // usage for command line arguments:
            // ifcdoc [filename] [output directory]

            // A. No arguments: new file
            // Example> ifcdoc.exe

            // B. One argument: loads .ifcdoc file (for launching file in Windows) or .ifc file for validating
            // ifcdoc.exe filepath.ifcdoc 
            // Example> ifcdoc.exe "C:\DOCS\COBIE-2012.ifcdoc"
            // Example> ifcdoc.exe "C:\bridge.ifc"

            // C. Two arguments: loads file, generates documentation, closes (for calling by server to generate html and mvdxml files)
            // Example> ifcdoc.exe "C:\CMSERVER\9dafdaf41f5b42db97479cfc578a4c2b\00000001.ifcdoc" "C:\CMSERVER\9dafdaf41f5b42db97479cfc578a4c2b\html\"

            //参数暂不处理
            /*
            if (args.Length >= 1)
            {
                string filepath = args[0];
                this.LoadFile(filepath);
            }

            if (args.Length == 2)
            {
                Properties.Settings.Default.OutputPath = args[1];
                this.GenerateDocumentation();
                this.Close();
            }
            */

            // 首先加载模板文件

            LoadFile("IFC2x3_TC1_Regenerated.ifcdoc");

            // 然后进行转换
        }



        /// <summary>
        /// Updates the current file path used for saving and for displaying in window caption, and resets modified flag.
        /// </summary>
        /// <param name="path"></param>
        private void SetCurrentFile(string path)
        {
            this.m_file = path;
            this.m_modified = false;

            string appname = "IFC Documentation Generator";
            if (this.m_file != null)
            {
                string name = System.IO.Path.GetFileName(this.m_file);
                this.Text = name + " - " + appname;
            }
            else
            {
                this.Text = appname;
            }
        }

        /// <summary>
        /// Prompts to save if file has been modified.
        /// </summary>
        /// <returns>True if ok to proceed (user clicked Yes or No), False to not continue (user clicked Cancel)</returns>

        private void LoadFile(string filename)
        {
            this.SetCurrentFile(filename);


            this.m_project = null;

            List<DocChangeAction> listChange = new List<DocChangeAction>(); //temp

            Dictionary<long, object> instances = null;
            string ext = System.IO.Path.GetExtension(this.m_file).ToLower();
            try
            {
                switch (ext)
                {
                    case ".ifcdoc":
                        using (FileStream streamDoc = new FileStream(this.m_file, FileMode.Open, FileAccess.Read))
                        {
                            StepSerializer formatDoc = new StepSerializer(typeof(DocProject), SchemaDOC.Types);
                            this.m_project = (DocProject)formatDoc.ReadObject(streamDoc, out instances);
                        }
                        break;

#if MDB
                    case ".mdb":
                        using (FormatMDB format = new FormatMDB(this.m_file, SchemaDOC.Types, this.m_instances))
                        {
                            format.Load();
                        }
                        break;
#endif
                }
            }
            catch (Exception x)
            {
                //MessageBox.Show(this, x.Message, "Error", MessageBoxButtons.OK);

                // force new as state is now invalid
                Console.WriteLine("此目录下未发现正确的IFC2x3_TC1_Regenerated.ifcdoc文件！");
                this.m_modified = false;
                // this.toolStripMenuItemFileNew_Click(this, EventArgs.Empty);
                // 退出程序
                Environment.Exit(0);

            }

            List<SEntity> listDelete = new List<SEntity>();
            List<DocTemplateDefinition> listTemplate = new List<DocTemplateDefinition>();

            foreach (object o in instances.Values)
            {
                if (o is DocSchema)
                {
                    DocSchema docSchema = (DocSchema)o;

                    // renumber page references
                    foreach (DocPageTarget docTarget in docSchema.PageTargets)
                    {
                        if (docTarget.Definition != null) // fix it up -- NULL bug from older .ifcdoc files
                        {
                            int page = docSchema.GetDefinitionPageNumber(docTarget);
                            int item = docSchema.GetPageTargetItemNumber(docTarget);
                            docTarget.Name = page + "," + item + " " + docTarget.Definition.Name;

                            foreach (DocPageSource docSource in docTarget.Sources)
                            {
                                docSource.Name = docTarget.Name;
                            }
                        }
                    }
                }
                else if (o is DocExchangeDefinition)
                {
                    // files before V4.9 had Description field; no longer needed so use regular Documentation field again.
                    DocExchangeDefinition docexchange = (DocExchangeDefinition)o;
                    if (docexchange._Description != null)
                    {
                        docexchange.Documentation = docexchange._Description;
                        docexchange._Description = null;
                    }
                }
                else if (o is DocTemplateDefinition)
                {
                    // files before V5.0 had Description field; no longer needed so use regular Documentation field again.
                    DocTemplateDefinition doctemplate = (DocTemplateDefinition)o;
                    if (doctemplate._Description != null)
                    {
                        doctemplate.Documentation = doctemplate._Description;
                        doctemplate._Description = null;
                    }

                    listTemplate.Add((DocTemplateDefinition)o);
                }
                else if (o is DocConceptRoot)
                {
                    // V12.0: ensure template is defined
                    DocConceptRoot docConcRoot = (DocConceptRoot)o;
                    if (docConcRoot.ApplicableTemplate == null && docConcRoot.ApplicableEntity != null)
                    {
                        docConcRoot.ApplicableTemplate = new DocTemplateDefinition();
                        docConcRoot.ApplicableTemplate.Type = docConcRoot.ApplicableEntity.Name;
                    }
                }
                else if (o is DocTemplateUsage)
                {
                    // V12.0: ensure template is defined
                    DocTemplateUsage docUsage = (DocTemplateUsage)o;
                    if (docUsage.Definition == null)
                    {
                        docUsage.Definition = new DocTemplateDefinition();
                    }
                }
                else if (o is DocChangeAction)
                {
                    listChange.Add((DocChangeAction)o);
                }


                // ensure all objects have valid guid
                if (o is DocObject)
                {
                    DocObject docobj = (DocObject)o;
                    if (docobj.Uuid == Guid.Empty)
                    {
                        docobj.Uuid = Guid.NewGuid();
                    }
                }
            }

            if (this.m_project == null)
            {
                //MessageBox.Show(this, "File is invalid; no project is defined.", "Error", MessageBoxButtons.OK);
                return;
            }

            foreach (DocModelView docModelView in this.m_project.ModelViews)
            {
                // sort alphabetically (V11.3+)
                docModelView.SortConceptRoots();
            }

            // upgrade to Publications (V9.6)
            if (this.m_project.Annotations.Count == 4)
            {
                this.m_project.Publications.Clear();

                DocAnnotation docCover = this.m_project.Annotations[0];
                DocAnnotation docContents = this.m_project.Annotations[1];
                DocAnnotation docForeword = this.m_project.Annotations[2];
                DocAnnotation docIntro = this.m_project.Annotations[3];

                DocPublication docPub = new DocPublication();
                docPub.Name = "Default";
                docPub.Documentation = docCover.Documentation;
                docPub.Owner = docCover.Owner;
                docPub.Author = docCover.Author;
                docPub.Code = docCover.Code;
                docPub.Copyright = docCover.Copyright;
                docPub.Status = docCover.Status;
                docPub.Version = docCover.Version;

                docPub.Annotations.Add(docForeword);
                docPub.Annotations.Add(docIntro);

                this.m_project.Publications.Add(docPub);

                docCover.Delete();
                docContents.Delete();
                this.m_project.Annotations.Clear();
            }

            // V11.3: sort terms, references
            this.m_project.SortTerms();
            this.m_project.SortAbbreviations();
            this.m_project.SortNormativeReferences();
            this.m_project.SortInformativeReferences();

            // by jifeng
            // LoadTree();
        }

        // 辅助判断ifc文件中换行符合法性，这个字符串必须全部为数字
        private bool isValidIfc(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            const string pattern = "^[0-9]*$";
            Regex rx = new Regex(pattern);
            return rx.IsMatch(s);
        }


        public int StartConvert(string InputFile, string OutputFile)
        {
            // 统计时间
            DateTime startT = DateTime.Now;
            DateTime endT;
            TimeSpan ts;
            // check for schema
            DocEntity docProject = this.m_project.GetDefinition("IfcProject") as DocEntity;
            if (docProject == null)
            {
                //MessageBox.Show(this, "Conversion requires an IFC schema to be defined, with IfcProject in scope at a minimum. " +
                //    "Before using this functionality, use File/Open to open an IFC baseline definition file, which may be found at www.buildingsmart-tech.org", "Convert File");

                return -1;
            }

            Type typeProject = Compiler.CompileProject(this.m_project);
            object project = null;
            try
            {
                using (FileStream streamSource = new FileStream(InputFile, FileMode.Open))
                {
                    // 需要先对ifc文件标准化，主要是去除不必要的回车行，目前发现SmartPlant导出的ifc文件随意断行

                    Console.WriteLine("扫描ifc文件中不合法的回车符！");

                    byte[] bytes = new byte[streamSource.Length];
                    streamSource.Read(bytes, 0, bytes.Length);
                    streamSource.Close();

                    // 清除bytes中不合理的回车符；
                    string str1 = System.Text.Encoding.Default.GetString(bytes);

                    int pos = str1.IndexOf("\r\nDATA;\r\n");
                    if (pos < 0)
                    {
                        Console.WriteLine("没有找到ifc文件的DATA头！ 转换失败！！");
                        return -1; // 不是正常的ifc文件
                    }

                    pos += 9;

                    int endPos = str1.IndexOf("\r\nENDSEC;", pos);
                    if (endPos < 0)
                    {
                        Console.WriteLine("没有找到ifc文件DATA对应的ENDSEC！ 转换失败！！");
                        return -1;  // 不是正常的ifc文件
                    }

                    string str2 = str1.Substring(0, pos);

                    string str3 = str1.Substring(pos, endPos - pos);
                    string strResult = System.Text.RegularExpressions.Regex.Replace(str3, @"\r\n#", "YaOJiFeNg");
                    str3 = System.Text.RegularExpressions.Regex.Replace(strResult, @"\r\n", "");
                    strResult = System.Text.RegularExpressions.Regex.Replace(str3, @"YaOJiFeNg", "\r\n#");
                    str2 += strResult + str1.Substring(endPos);



                    /*
                                        // 辅助统计
                                        int totallines = Regex.Matches(str1, @"\r\n").Count;
                                        int currentlines = 0;

                                        int pos0 = 0, pos1 = 0,len;
                                        int pos =str1.IndexOf("\r\nDATA;\r\n");
                                        if (pos < 0) {
                                            Console.WriteLine("没有找到ifc文件的DATA头！ 转换失败！！");
                                            return; // 不是正常的ifc文件
                                        }
                                        pos += 9;  // 停到后面正文位置
                                        len = pos;
                                        string str2=str1.Substring(pos0,len);

                                        int endPos = str1.IndexOf("\r\nENDSEC;", pos);
                                        if (endPos < 0)
                                        {
                                            Console.WriteLine("没有找到ifc文件DATA对应的ENDSEC！ 转换失败！！");
                                            return;  // 不是正常的ifc文件
                                        }

                                        pos0 = pos;
                                        pos = str1.IndexOf("\r\n", pos0);

                                        string strTmp;
                                        while (pos < endPos)
                                        {
                                            currentlines++;
                                            if (currentlines % 5000 == 0)
                                            {
                                                endT = DateTime.Now;
                                                ts = endT - startT;
                                                startT = endT;
                                                Console.WriteLine("总共{0}行，已扫描完成{1}行,耗时{2}秒,预计还需要{3}秒。", totallines, currentlines,ts,ts.TotalSeconds*(totallines-currentlines)/5000);
                                            }

                                            // 判断是否为有效的回车符,仅当回车符后面是 “#???=”格式才是合法的字符

                                            if (str1[pos + 2] != '#')
                                            {
                                                str2 += str1.Substring(pos0, pos - pos0);
                                                pos += 2;
                                                pos0 = pos;
                                                pos=str1.IndexOf("\r\n", pos0);
                                                continue;
                                            }

                                            // 是‘#’但不是#?????=格式
                                            pos1 = pos + 3;
                                            if (!isValidIfc(str1.Substring(pos1, str1.IndexOf('=', pos1) - pos1)))
                                            {
                                                str2 += str1.Substring(pos0, pos - pos0);
                                                pos += 2;
                                                pos0 = pos;
                                                pos = str1.IndexOf("\r\n", pos0);
                                                continue;
                                            }
                                            pos = str1.IndexOf("\r\n", pos+2);

                                        }

                                        str2 += str1.Substring(pos0);
                                        //测试判断ifc回车符
                                        //System.IO.FileStream fsstream = System.IO.File.OpenWrite(OutputFile);
                                        //byte[] writebytes = Encoding.UTF8.GetBytes(str2); //将字符串转换为字节数组
                                        //fsstream.Write(writebytes, 0, writebytes.Length);
                                        //return;
                     ******/

                    byte[] array = Encoding.ASCII.GetBytes(str2);
                    MemoryStream streamInput = new MemoryStream(array);             //convert stream 2 string      
                    endT = DateTime.Now;
                    ts = endT - startT;
                    Console.WriteLine("扫描结束，总时间：   {0}秒！\r\n", ts.TotalSeconds.ToString("0.00"));
                    startT = endT;


                    Serializer formatSource = null;
                    formatSource = new StepSerializer(typeProject);
                    endT = DateTime.Now;
                    ts = endT - startT;
                    Console.WriteLine("初始化工作时间：   {0}秒！\r\n", ts.TotalSeconds.ToString("0.00"));
                    startT = endT;
                    project = formatSource.ReadObject(streamInput);

                    endT = DateTime.Now;
                    ts = endT - startT;
                    Console.WriteLine("转换为step工作时间：   {0}秒！\r\n", ts.TotalSeconds.ToString("0.00"));


                    //Serializer formatTarget = null;
                    //formatTarget = new XmlSerializer(typeProject);
                    GetProperties formatTarget = new GetProperties(typeProject);
                    if (formatTarget != null)
                    {
                        using (System.IO.FileStream streamTarget = System.IO.File.OpenWrite(OutputFile))
                        {
                            formatTarget.WriteJson(streamTarget, project);
                            streamTarget.Close();//关闭流
                        }
                    }

                }

            }
            catch (Exception xx)
            {
                //MessageBox.Show(xx.Message);
                Console.WriteLine(xx.Message);
                return -1;
            }
            return 0;
        }
    }
}