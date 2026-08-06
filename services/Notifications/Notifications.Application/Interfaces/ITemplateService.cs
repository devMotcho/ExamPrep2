namespace Notifications.Application.Interfaces;

public interface ITemplateService
{
    Task<string> RenderAsync(string templateName, object model);
}
