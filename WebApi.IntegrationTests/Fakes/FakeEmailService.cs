using Application.Constants;
using Application.Entities;
using Application.Entities.Templates;
using Application.Interfaces.Services;

namespace WebApi.IntegrationTests.Fakes;

public class FakeEmailService : IEmailService
{
    public List<TemplateEmailMessage> SentTemplateEmails { get; } = [];

    public Task SendTemplateEmailAsync(
        TemplateEmailMessage messageToSend,
        EmailTemplate emailTemplate,
        CancellationToken token = default)
    {
        SentTemplateEmails.Add(messageToSend);

        return Task.CompletedTask;
    }

    public Task SendSimpleEmailAsync(
        SimpleEmailMessage message,
        CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public string GetInviteToken(string email)
    {
        var message = SentTemplateEmails
            .Last(x => x.ToEmail == email);

        var template =
            Assert.IsType<InviteTemplateData>(
                message.TemplateData);

        var uri = new Uri(template.InviteLink);

        var query = System.Web.HttpUtility
            .ParseQueryString(uri.Query);

        return query["token"]
            ?? throw new InvalidOperationException(
                "Invite token was not found.");
    }
}