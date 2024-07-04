// See https://aka.ms/new-console-template for more information
using FUICompiler;

const string slnMark = "--sln";
const string projectMark = "--project";
const string outputMark = "--output";
const string bindingConfigMark = "--binding";
const string generatedPathMark = "--generated";
const string contextGenerateTypeMark = "--ctx_type";


var compiler = new Compiler();
//string workspace = "..\\..\\..\\..\\..\\";
//args = $"--sln={workspace}.\\FUI.sln --project=FUI.Test --output={workspace}.\\Library\\ScriptAssemblies\\FUI.Test.dll --binding={workspace}.\\Library\\Binding\\ --generated={workspace}.\\Temp\\BindingGenerated\\".Split(' ');
var param = ParseArgs(args);

if(param.contextGenerateType == BindingContextGenerateType.Mix || param.contextGenerateType == BindingContextGenerateType.Attribute)
{
    compiler.typeSyntaxRootGenerators.Add(new AttributeBindingContextGenerator());
}

if(param.contextGenerateType == BindingContextGenerateType.Mix || param.contextGenerateType == BindingContextGenerateType.Config)
{
    compiler.beforeCompilerSourcesGenerators.Add(new BindingContextGenerator(param.bindingPath, ".binding"));
}

compiler.typeSyntaxRootGenerators.Add(new BindablePropertyDelegateGenerator());
compiler.typeSyntaxModifiers.Add(new ObservableObjectKeywordModifier());
compiler.typeDefinationInjectors.Add(new PropertyChangedInjector());


Console.WriteLine($"start build sln:{param.solutionPath} project:{param.projectName} output:{param.output} binding:{param.bindingPath} context_generate_type:{param.contextGenerateType}");
await compiler.Build(param);

BuildParam ParseArgs(string[] args)
{
    var solutionPathIndex = Array.FindIndex(args, (k) => k.StartsWith(slnMark));
    var projectNameIndex = Array.FindIndex(args, (k) => k.StartsWith(projectMark));
    var outputIndex = Array.FindIndex(args, (k) => k.StartsWith(outputMark));
    var bindingPathIndex = Array.FindIndex(args, (k) => k.StartsWith(bindingConfigMark));
    var generatedPathIndex = Array.FindIndex(args, (k) => k.StartsWith(generatedPathMark));
    var contextGenerateTypeIndex = Array.FindIndex(args, (k) => k.StartsWith(contextGenerateTypeMark));

    if(solutionPathIndex == -1 || projectNameIndex == -1 || outputIndex == -1)
    {
        throw new ArgumentException($"Invalid args, \n required: --sln=your_sln_path --project=your_project_path --output=your_output_path \n optional: -- binding=your_binding_file_path  --generated=your_generated_cs_path --ctx_type=your_binding_context_generate_type");
    }

    return new BuildParam
    (
        solutionPath: args[solutionPathIndex].Substring(slnMark.Length + 1),
        projectName: args[projectNameIndex].Substring(projectMark.Length + 1),
        output: args[outputIndex].Substring(outputMark.Length + 1),
        bindingPath: bindingPathIndex == -1 ? string.Empty : args[bindingPathIndex].Substring(bindingConfigMark.Length + 1),
        generatedPath: generatedPathIndex == -1 ? string.Empty : args[generatedPathIndex].Substring(generatedPathMark.Length + 1),
        contextGenerateType: contextGenerateTypeIndex == -1 ? BindingContextGenerateType.Mix : Enum.Parse<BindingContextGenerateType>(args[contextGenerateTypeIndex].Substring(contextGenerateTypeMark.Length + 1))
    );
}