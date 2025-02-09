using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Bogus;
using Camunda.Worker.Client;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Camunda.Worker;

public class CompleteResultTest
{
    private readonly ExternalTask _externalTask;
    private readonly Mock<IExternalTaskClient> _clientMock = new();
    private readonly Mock<IExternalTaskContext> _contextMock = new();

    public CompleteResultTest()
    {
        _externalTask = new Faker<ExternalTask>()
            .CustomInstantiator(faker => new ExternalTask(
                faker.Random.Guid().ToString(),
                faker.Random.Word(),
                faker.Random.Word())
            )
            .Generate();
        _contextMock.Setup(ctx => ctx.Task).Returns(_externalTask);
        _contextMock.Setup(ctx => ctx.Client).Returns(_clientMock.Object);
        _contextMock.SetupGet(c => c.ServiceProvider).Returns(new ServiceCollection().BuildServiceProvider());
    }

    [Fact]
    public async Task TestExecuteResultAsync()
    {
        // Arrange
        _clientMock
            .Setup(client => client.CompleteAsync(
                _externalTask.Id, It.IsAny<CompleteRequest>(), default
            ))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var result = new CompleteResult
        {
            Variables = new Dictionary<string, Variable>()
        };

        //Act
        await result.ExecuteResultAsync(_contextMock.Object);

        // Assert
        _clientMock.Verify();
        _clientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestExecuteResultWithFailedCompletion()
    {
        // Arrange
        _clientMock
            .Setup(client => client.CompleteAsync(
                _externalTask.Id, It.IsAny<CompleteRequest>(), default
            ))
            .ThrowsAsync(new ClientException(new ErrorResponse
            {
                Type = "an error type",
                Message = "an error message"
            }, HttpStatusCode.InternalServerError))
            .Verifiable();

        _clientMock
            .Setup(client => client.ReportFailureAsync(
                _externalTask.Id, It.IsAny<ReportFailureRequest>(), default
            ))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var result = new CompleteResult
        {
            Variables = new Dictionary<string, Variable>()
        };

        // Act
        await result.ExecuteResultAsync(_contextMock.Object);

        // Assert
        _clientMock.Verify();
        _clientMock.VerifyNoOtherCalls();
    }
}
