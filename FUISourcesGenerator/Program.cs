// See https://aka.ms/new-console-template for more information
using FUISourcesGenerator;

const string slnMark = "--sln";
const string projectMark = "--project";
const string outputMark = "--output";
const string bindingConfigMark = "--binding";

Console.WriteLine("start generate");
var generator = new Generator();
var param = ParseArgs(args);

generator.typeSyntaxModifiers.Add(new KeywordModifier());
generator.typeDefinationInjectors.Add(new PropertyChangedInjector());
generator.beforeCompilerSourcesGenerators.Add(new DataBindingContextGenerator(param.bindingPath, ".binding"));
generator.typeDefinationSourcesGenerators.Add(new PropertyDelegateGenerator());

Console.WriteLine($"start generate sln:{param.solutionPath} project:{param.projectName} output:{param.output} binding:{param.bindingPath}");
await generator.Generate(param);

GenerateParam ParseArgs(string[] args)
{
    if(args.Length != 4)
    {
        throw new ArgumentException("Invalid args");
    }
    var param = new GenerateParam();

    foreach(var arg in args)
    {
        if(arg.StartsWith(slnMark))
        {
            param.solutionPath = arg.Split('=')[1];
        }

        if(arg.StartsWith(projectMark))
        {
            param.projectName = arg.Split('=')[1];
        }

        if(arg.StartsWith(outputMark))
        {
            param.output = arg.Split('=')[1];
        }

        if(arg.StartsWith(bindingConfigMark))
        {
            param.bindingPath = arg.Split('=')[1];
        }
    }
    
    return param;
}