using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

using Mono.Cecil;

namespace FUICompiler
{
    /// <summary>
    /// 绑定上下文生成类型
    /// </summary>
    public enum BindingContextGenerateType
    {
        /// <summary>
        /// 通过特性生成
        /// </summary>
        Attribute,
        /// <summary>
        /// 通过配置文件生成
        /// </summary>
        Config,
        /// <summary>
        /// 混合生成
        /// </summary>
        Mix,
    }

    /// <summary>
    /// 编译参数
    /// </summary>
    public struct BuildParam
    {
        /// <summary>
        /// 解决方案路径
        /// </summary>
        public readonly string solutionPath;

        /// <summary>
        /// 工程名
        /// </summary>
        public readonly string projectName;

        /// <summary>
        /// 输出路径
        /// </summary>
        public readonly string output;

        /// <summary>
        /// 存放绑定配置的路径
        /// </summary>
        public readonly string bindingPath;

        /// <summary>
        /// 存放生成代码的路径 为空则不保存生成的代码
        /// </summary>
        public readonly string generatedPath;

        /// <summary>
        /// 绑定上下文生成类型
        /// </summary>
        public readonly BindingContextGenerateType contextGenerateType;

        /// <summary>
        /// 构造编译参数
        /// </summary>
        /// <param name="solutionPath">解决方案路径</param>
        /// <param name="projectName">工程名</param>
        /// <param name="output">工程输出路径</param>
        /// <param name="bindingPath">存放绑定资源的路径</param>
        /// <param name="generatedPath">存放生成的代码的路径</param>
        public BuildParam(string solutionPath, string projectName, string output, string bindingPath, string generatedPath, BindingContextGenerateType contextGenerateType)
        {
            this.solutionPath = solutionPath;
            this.projectName = projectName;
            this.output = output;
            this.bindingPath = bindingPath;
            this.generatedPath = generatedPath;
            this.contextGenerateType = contextGenerateType;
        }
    }

    internal class Compiler
    {
        public List<ITypeSyntaxNodeSourcesGenerator> typeSyntaxRootGenerators = new List<ITypeSyntaxNodeSourcesGenerator>();
        public List<ITypeSyntaxNodeModifier> typeSyntaxModifiers = new List<ITypeSyntaxNodeModifier>();
        public List<IBeforeCompilerSourcesGenerator> beforeCompilerSourcesGenerators = new List<IBeforeCompilerSourcesGenerator>();
        public List<ITypeDefinationInjector> typeDefinationInjectors = new List<ITypeDefinationInjector>();

        /// <summary>
        /// 编译一个工程
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task Build(BuildParam param)
        {
            if (MSBuildLocator.CanRegister)
            {
                foreach (var msbuild in MSBuildLocator.QueryVisualStudioInstances())
                {
                    Console.WriteLine($"VisualStudioInstance: {msbuild.MSBuildPath}");
                }
                MSBuildLocator.RegisterInstance(MSBuildLocator.QueryVisualStudioInstances().First());
            }

            var workspace = MSBuildWorkspace.Create();
            Console.WriteLine($"Loading solution {param.solutionPath}");
            var solution = await workspace.OpenSolutionAsync(param.solutionPath);
            var project = solution.Projects.FirstOrDefault(item => item.Name == param.projectName);

            if(project == null)
            {
                throw new Exception($"build error: project {param.projectName} not found in solution {param.solutionPath}");
            }

            //所有生成的代码
            List<Source> addition = new List<Source>();

            //修改类型语法树并生成代码
            project = await ModifyTypeSyntaxAndGenerate(project, addition);

            //编译前源代码生成器
            GenerateBeforeCompiler(addition);

            //将生成的代码添加到工程中
            foreach (var add in addition)
            {
                project = project.AddDocument(add.name, CSharpSyntaxTree.ParseText(add.Text).GetRoot()).Project; 
            }

            //编译工程
            var asm = await InternalBuild(project);

            //根据类型定义注入代码
            InjectByTypeDefination(asm);

            //输最终的dll
            asm.Write(param.output);

            //保存生成的代码文件
            SaveGenerated(addition, param.generatedPath);

            Console.WriteLine($"compiler complete at:{param.output}");
        }

        /// <summary>
        /// 编译一个工程
        /// </summary>
        /// <param name="project">工程</param>
        /// <returns></returns>
        async Task<AssemblyDefinition> InternalBuild(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var ms = new MemoryStream();

            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

                var errors = string.Empty;  
                foreach (Diagnostic diagnostic in failures)
                {
                    errors += $"{diagnostic.Id}:{diagnostic.GetMessage()}\n";
                }
                throw new Exception(errors);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return AssemblyDefinition.ReadAssembly(ms);
        }

        /// <summary>
        /// 添加源代码到追加列表中
        /// </summary>
        /// <param name="addition">追加列表</param>
        /// <param name="sources">生成的源码</param>
        void AddSources(List<Source> addition, Source?[] sources)
        {
            if (sources == null)
            {
                return;
            }

            foreach (var source in sources)
            {
                if (source != null)
                {
                    addition.Add(source.Value);
                }
            }
        }

        /// <summary>
        /// 修改类型语法树
        /// </summary>
        /// <param name="project">要修改的类型所在的工程</param>
        /// <param name="addition">生成的代码</param>
        /// <returns></returns>
        async Task<Project> ModifyTypeSyntaxAndGenerate(Project project, List<Source> addition)
        {
            List<(DocumentId remove, Document add)> temp = new List<(DocumentId remove, Document add)>();
            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                if (root == null)
                {
                    continue;
                }

                //修改类型语法树
                if (typeSyntaxModifiers.Count != 0)
                {
                    foreach (var typeModifier in typeSyntaxModifiers)
                    {
                        root = typeModifier.Modify(root);
                    }
                    var newDocument = document.WithSyntaxRoot(root);
                    temp.Add((document.Id, newDocument));
                }

                //根据类型语法树生成代码
                typeSyntaxRootGenerators.ForEach((item) => AddSources(addition, item.Generate(root)));
            }

            foreach (var item in temp)
            {
                project = project.RemoveDocument(item.remove).AddDocument(item.add.Name, item.add.GetTextAsync().Result).Project;
            }

            return project;
        }

        /// <summary>
        /// 在编译前生成代码
        /// </summary>
        /// <param name="addition"></param>
        void GenerateBeforeCompiler(List<Source> addition)
        {
            beforeCompilerSourcesGenerators.ForEach(item => AddSources(addition, item.Generate()));
        }

        /// <summary>
        /// 注入代码
        /// </summary>
        /// <param name="asm"></param>
        void InjectByTypeDefination(AssemblyDefinition asm)
        {
            //注入IL
            foreach (var injector in typeDefinationInjectors)
            {
                foreach (var module in asm.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        injector.Inject(module, type);
                    }
                }
            }
        }

        /// <summary>
        /// 保存生成的文件
        /// </summary>
        /// <param name="addition"></param>
        /// <param name="path"></param>
        void SaveGenerated(List<Source> addition, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var directoryInfo = Utility.GetOrCreateDirectory(path);
            foreach (var source in addition)
            {
                var fileName = $"{source.name}.cs";
                File.WriteAllText($"{directoryInfo.FullName}\\{fileName}", source.Text.ToString());
            }
        }
    }
}
