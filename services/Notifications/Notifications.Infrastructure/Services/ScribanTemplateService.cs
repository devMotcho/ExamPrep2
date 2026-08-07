using System.Reflection;
namespace Notifications.Infrastructure.Services;

public class ScribanTemplateService : ITemplateService
{
    public async Task<string> RenderAsync(string templateName, object model)
    {
        var templatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Templates", $"{templateName}.html");
        
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template '{templateName}.html' not found at '{templatePath}'.");
        }

        var source = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(source);
        
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Error parsing template {templateName}: {string.Join(", ", template.Messages)}");
        }

        return await template.RenderAsync(model);
    }
}
