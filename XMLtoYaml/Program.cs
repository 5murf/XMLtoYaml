using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;

Main();


static void Main()
{
    Console.WriteLine("Enter the directory path to search for XML files:");
    string directoryPath = Console.ReadLine();

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine("Directory does not exist.");
        return;
    }
    Console.WriteLine("Enter the trial allowance (ISO 8601 Duration format, e.g., PT1H for one hour):");
    string trialAllowance = Console.ReadLine();
    if(String.IsNullOrEmpty(trialAllowance))
    {
        trialAllowance = "PT1H";
    }

    Console.WriteLine("Enter the trial window (ISO 8601 Duration format, e.g., P7D for seven days):");
    string trialWindow = Console.ReadLine();
    if (String.IsNullOrEmpty(trialWindow))
    {
        trialWindow = "P7D";
    }
    var xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);
    foreach (var file in xmlFiles)
    {
        Console.WriteLine($"\nProcessing: {file}");
        try
        {
            XDocument doc = XDocument.Load(file);
            string yamlOutput = ConvertXmlToYaml(doc, trialAllowance, trialWindow);
            SaveYamlToFile(yamlOutput, file);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }
}

static string ConvertXmlToYaml(XDocument doc, string trialAllowance, string trialWindow)
{
    StringBuilder yaml = new StringBuilder();
    XElement root = doc.Element("manifest");

    string name = root.Element("name")?.Value;
    XElement hiddenElement = root.Element("hidden");

    bool setHidden = name.Contains("lite", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("trial", StringComparison.OrdinalIgnoreCase) ||
                    (hiddenElement != null && string.Equals(hiddenElement.Value, "true", StringComparison.OrdinalIgnoreCase));

    string priceValue = root.Element("price")?.Value ?? "0.00";
    double price = double.Parse(priceValue);

    yaml.AppendLine($"mainClass: {root.Element("main-class")?.Value}");
    yaml.AppendLine($"name: {name}");
    yaml.AppendLine($"internalId: {root.Element("internal-id")?.Value}");
    yaml.AppendLine($"tagline: {root.Element("tag-line")?.Value}");
    yaml.AppendLine($"description: {root.Element("description")?.Value}");
    yaml.AppendLine($"version: {root.Element("version")?.Value}");
    yaml.AppendLine($"access: {root.Element("access")?.Value}");
    yaml.AppendLine($"hidden: {setHidden.ToString().ToLower()}");
    yaml.AppendLine($"price: {root.Element("price")?.Value}");

    yaml.Append("compatibility:\n");
    foreach (var elem in root.Element("compatibility")?.Elements("game-type") ?? Enumerable.Empty<XElement>())
    {
        yaml.AppendLine($"  - {elem.Value}");
    }

    yaml.Append("categories:\n");
    foreach (var elem in root.Element("categories")?.Elements("category") ?? Enumerable.Empty<XElement>())
    {
        yaml.AppendLine($"  - {elem.Value}");
    }

    var resources = root.Element("resources")?.Elements().ToList();
    if (resources != null && resources.Count > 0)
    {
        yaml.Append("resources:\n");
        foreach (var resource in resources)
        {
            yaml.AppendLine($"  - {resource.Value}");
        }
    }

    var features = root.Element("features")?.Elements("feature").ToList();
    if (features != null && features.Count > 0)
    {
        yaml.Append("features:\n");
        foreach (var feature in features)
        {
            yaml.AppendLine($"  - type: {feature.Value}");
            yaml.AppendLine($"    mode: {feature.Attribute("mode")?.Value}");
        }
    }

    yaml.Append("tags:\n");
    foreach (var tag in root.Element("tags")?.Elements("tag") ?? Enumerable.Empty<XElement>())
    {
        yaml.AppendLine($"  - {tag.Value}");
    }

    if (price > 0.00)
    {
        yaml.AppendLine("trial:");
        yaml.AppendLine($"  allowance: {trialAllowance}");
        yaml.AppendLine($"  window: {trialWindow}");
    }

    var obfuscationElements = root.Element("obfuscation")?.Elements().ToList();
    if (obfuscationElements != null && obfuscationElements.Count > 0)
    {
        yaml.Append("obfuscation:\n");
        foreach (var obf in obfuscationElements)
        {
            yaml.AppendLine($"  - {obf.Value}");
        }
    }

    return yaml.ToString();
}

static void SaveYamlToFile(string yamlOutput, string originalFilePath)
{
    string newYamlFileName = Path.GetFileNameWithoutExtension(originalFilePath) + ".manifest.yml";
    string newYamlFilePath = Path.Combine(Path.GetDirectoryName(originalFilePath), newYamlFileName);

    File.WriteAllText(newYamlFilePath, yamlOutput);
    Console.WriteLine($"YAML file saved as: {newYamlFilePath}");
}

