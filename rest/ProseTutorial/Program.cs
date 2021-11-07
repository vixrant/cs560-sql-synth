using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
public class TableSchema {
    public string name { get; set; }
    public IList<string> columns { get; set; }
}
public class ExampleSchema {
    public IList<TableSchema> inputs { get; set; }
    public IList<string> output { get; set; }
}

namespace ProseTutorial
{
    internal class Program
    {
        private static readonly Grammar Grammar = DSLCompiler.Compile(new CompilerOptions
        {
            InputGrammarText = File.ReadAllText("synthesis/grammar/substring.grammar"),
            References = CompilerReference.FromAssemblyFiles(typeof(Program).GetTypeInfo().Assembly)
        }).Value;

        private static SynthesisEngine _prose;

        private static readonly Dictionary<State, object> Examples = new Dictionary<State, object>();
        private static ProgramNode _topProgram;

        private static List<string[]> parsefile(string filename) {
            List<string[]> filecontents = new List<string[]>();
            using (StreamReader reader = new StreamReader(filename)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                    var arr = CSVParser.Split(line);
                    for (int i=0;i<arr.Length;i++) arr[i]=arr[i].Trim();
                    if (filecontents.Count==0 || arr.Length==filecontents[0].Length)
                        filecontents.Add(CSVParser.Split(line));
                }
            }
            return filecontents;
        }
        private static void Main(string[] args) {
            _prose = ConfigureSynthesis();
            string input;
            //input arguments seems to be 0-indexed when using dotnet run... weird...
            if (args.Length>0) { input = args[0]; } else {
                Console.Out.WriteLine("Please specify a path to a directory containing a schema and some examples");
                input = Console.ReadLine();
            }
            // input = Path.Combine(new String[] {System.IO.Directory.GetCurrentDirectory(),input});
            if (!Directory.Exists(input)) { Console.Out.WriteLine("Invalid input: {0} isn't a directory",input);return;}
            string schemapath = Path.Combine(new String[] {input,"schema.json"});
            if (!File.Exists(schemapath)) { Console.Out.WriteLine("Invalid input: {0} doesn't exist",schemapath);return;}
            ExampleSchema schema = JsonConvert.DeserializeObject<ExampleSchema>(File.ReadAllText(schemapath));
            foreach (string subfile in Directory.GetDirectories(input)) {
                List<List<string[]>> intables = new List<List<string[]>>();
                foreach (TableSchema tableschema in schema.inputs) {
                    string inputpath = Path.Combine(new String[] {subfile,"input_tables",tableschema.name+".csv"});
                    if (!File.Exists(inputpath)) { Console.Out.WriteLine("Required file not found: {0} doesn't exist",inputpath);return;}
                    intables.Add(parsefile(inputpath));
                }
                string outputpath = Path.Combine(new String[] {subfile,"output_table.csv"});
                if (!File.Exists(outputpath)) { Console.Out.WriteLine("Required file not found: {0} doesn't exist",outputpath);return;}
                List<string[]> outable = parsefile(outputpath);
                State inputState = State.CreateForExecution(Grammar.InputSymbol, intables);
                Examples.Add(inputState, outable);
            }

            var spec = new ExampleSpec(Examples);
            Console.Out.WriteLine("Learning a program for {0} examples...",Examples.Count);
            var scoreFeature = new RankingScore(Grammar);
            ProgramSet topPrograms = _prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            if (topPrograms.IsEmpty) throw new Exception("No program was found for this specification.");
            _topProgram = topPrograms.RealizedPrograms.First();
            Console.Out.WriteLine("Top 4 learned programs:");
            var counter = 1;
            foreach (ProgramNode program in topPrograms.RealizedPrograms) {
                if (counter > 4) break;
                Console.Out.WriteLine("==========================");
                Console.Out.WriteLine("Program {0}: ", counter);
                Console.Out.WriteLine(program.PrintAST(ASTSerializationFormat.HumanReadable));
                counter++;
            }
        }

        private static void RunOnNewInput()
        {
            if (_topProgram == null)
                throw new Exception("No program was synthesized. Try to provide new examples first.");
            Console.Out.WriteLine("Top program: {0}", _topProgram);

            try
            {
                Console.Out.Write("Insert a new input: ");
                string newInput = Console.ReadLine();
                if (newInput != null)
                {
                    int startFirstExample = newInput.IndexOf("\"", StringComparison.Ordinal) + 1;
                    int endFirstExample = newInput.IndexOf("\"", startFirstExample + 1, StringComparison.Ordinal) + 1;
                    newInput = newInput.Substring(startFirstExample, endFirstExample - startFirstExample - 1);
                    State newInputState = State.CreateForExecution(Grammar.InputSymbol, newInput);
                    Console.Out.WriteLine("RESULT: \"{0}\" -> \"{1}\"", newInput, _topProgram.Invoke(newInputState));
                }
            }
            catch (Exception)
            {
                throw new Exception("The execution of the program on this input thrown an exception");
            }
        }

        public static SynthesisEngine ConfigureSynthesis()
        {
            var witnessFunctions = new WitnessFunctions(Grammar);
            var deductiveSynthesis = new DeductiveSynthesis(witnessFunctions);
            var synthesisExtrategies = new ISynthesisStrategy[] {deductiveSynthesis};
            var synthesisConfig = new SynthesisEngine.Config {Strategies = synthesisExtrategies};
            var prose = new SynthesisEngine(Grammar, synthesisConfig);
            return prose;
        }
    }
}