using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary> Defines the name of a table and list of columns that it contains. </summary>
public class TableSchema
{
    public string name { get; set; }
    public IList<string> columns { get; set; }
}


/// <summary> Input: Tables, Output: string? </summary>
public class ExampleSchema
{
    public IList<TableSchema> inputs { get; set; }
    public IList<string> output { get; set; }
}

namespace Rest560
{
    internal class Program
    {
        private static readonly Grammar Grammar = DSLCompiler.Compile(new CompilerOptions
        {
            InputGrammarText = File.ReadAllText("synthesis/grammar/substring.grammar"),
            References = CompilerReference.FromAssemblyFiles(typeof(Program).GetTypeInfo().Assembly)
        }).Value;
        private static readonly Dictionary<State, object> Examples = new Dictionary<State, object>();
        private static SynthesisEngine _prose;

        private static ProgramNode _topFirstProgram;
        public static SynthesisEngine ConfigureSynthesis()
        {
            var witnessFunctions = new WitnessFunctions(Grammar);
            var deductiveSynthesis = new DeductiveSynthesis(witnessFunctions);
            var synthesisExtrategies = new ISynthesisStrategy[] { deductiveSynthesis };
            var synthesisConfig = new SynthesisEngine.Config { Strategies = synthesisExtrategies };
            var prose = new SynthesisEngine(Grammar, synthesisConfig);
            return prose;
        }

        private static List<string[]> parsefile(string filename)
        {
            var filecontents = new List<string[]>();
            var CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            using (var reader = new StreamReader(filename))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var arr = CSVParser.Split(line);
                    for (int i = 0; i < arr.Length; i++)
                        arr[i] = arr[i].Trim();
                    if (filecontents.Count == 0 || arr.Length == filecontents[0].Length)
                        filecontents.Add(CSVParser.Split(line));
                }
            }
            return filecontents;
        }

        /// <summary>
        /// Program can take input either from command line or using a prompt.
        /// If the input directory doesn't exist, or the schema.json file is missing, then
        /// we throw an InvalidOperationException to notify that the we cannot synthesize
        /// a program for that directory.
        /// </summary>
        private static Tuple<string, string> getInputDirectory(string[] args)
        {
            string input;

            if (args.Length > 0)
                input = args[0];
            else
            {
                Console.Out.WriteLine("Please specify a path to a directory containing a schema and some examples");
                input = Console.ReadLine();
            }

            // input = Path.Combine(new String[] {System.IO.Directory.GetCurrentDirectory(), input});

            if (!Directory.Exists(input))
                throw new InvalidOperationException("Invalid input: {0} isn't a directory" + input);

            var schemaPath = Path.Combine(new String[] { input, "schema.json" });

            if (!File.Exists(schemaPath))
                throw new InvalidOperationException("Invalid input: {0} doesn't exist" + schemaPath);

            return Tuple.Create(input, schemaPath);
        }

        private static void Main(string[] args)
        {
            _prose = ConfigureSynthesis();
            var inputDirectory = getInputDirectory(args);
            var input = inputDirectory.Item1;
            var schemaPath = inputDirectory.Item2;

            var schema = JsonConvert.DeserializeObject<ExampleSchema>(File.ReadAllText(schemaPath));
            foreach (var subfile in Directory.GetDirectories(input))
            {
                // Inputs
                var inTables = new List<List<string[]>>();
                foreach (var tableSchema in schema.inputs)
                {
                    var inputpath = Path.Combine(new String[] { subfile, "input_tables", tableSchema.name + ".csv" });
                    if (!File.Exists(inputpath))
                    {
                        Console.Out.WriteLine("Required file not found: {0} doesn't exist", inputpath);
                        return;
                    }
                    inTables.Add(parsefile(inputpath));
                }

                // Outputs
                var outputPath = Path.Combine(new String[] { subfile, "output_table.csv" });
                if (!File.Exists(outputPath))
                {
                    Console.Out.WriteLine("Required file not found: {0} doesn't exist", outputPath);
                    return;
                }

                var inputState = State.CreateForExecution(Grammar.InputSymbol, inTables);
                var outputState = parsefile(outputPath);
                Examples.Add(inputState, outputState);
            }

            // Learning
            Console.Out.WriteLine("Learning a program for {0} examples...", Examples.Count);
            var spec = new ExampleSpec(Examples);
            var scoreFeature = new RankingScore(Grammar);
            var topPrograms = _prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            if (topPrograms.IsEmpty)
                throw new Exception("No program was found for this specification.");
            _topFirstProgram = topPrograms.RealizedPrograms.First();

            Console.Out.WriteLine("Top 4 learned programs:");
            var counter = 1;
            foreach (ProgramNode program in topPrograms.RealizedPrograms)
            {
                if (counter > 4) break;
                Console.Out.WriteLine("==========================");
                Console.Out.WriteLine("Program {0}: ", counter);
                Console.Out.WriteLine(program.PrintAST(ASTSerializationFormat.HumanReadable));
                counter++;
            }
        }

        
        // From tutorial, a function that executes programs on new input.
        // Not for this project.
        //
        // private static void RunOnNewInput()
        // {
        //     if (_topFirstProgram == null)
        //         throw new Exception("No program was synthesized. Try to provide new examples first.");
        //     Console.Out.WriteLine("Top program: {0}", _topFirstProgram);

        //     Console.Out.Write("Insert a new input: ");
        //     var newInput = Console.ReadLine();
        //     if (newInput != null)
        //     {
        //         int startFirstExample = newInput.IndexOf("\"", StringComparison.Ordinal) + 1;
        //         int endFirstExample = newInput.IndexOf("\"", startFirstExample + 1, StringComparison.Ordinal) + 1;
        //         newInput = newInput.Substring(startFirstExample, endFirstExample - startFirstExample - 1);
        //         State newInputState = State.CreateForExecution(Grammar.InputSymbol, newInput);
        //         Console.Out.WriteLine("RESULT: \"{0}\" -> \"{1}\"", newInput, _topFirstProgram.Invoke(newInputState));
        //     }
        // }
    }
}