using System;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClassIsScore.Tests;

public class ScoreServiceTests
{
    private readonly Mock<IStudentService> _studentServiceMock;
    private readonly Mock<IGroupService> _groupServiceMock;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<ILogger<ScoreService>> _loggerMock;

    public ScoreServiceTests()
    {
        _studentServiceMock = new Mock<IStudentService>();
        _groupServiceMock = new Mock<IGroupService>();
        _adminServiceMock = new Mock<IAdminService>();
        _loggerMock = new Mock<ILogger<ScoreService>>();
    }

    [Fact]
    public async Task AddScoreAsync_UpdatesStudentScoreAndFiresEvent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, Name = "Test Student", Score = 10 };
        
        _studentServiceMock.Setup(s => s.GetStudentByIdAsync(studentId))
            .ReturnsAsync(student);

        var scoreService = new ScoreService(
            _loggerMock.Object, 
            _studentServiceMock.Object, 
            _groupServiceMock.Object, 
            _adminServiceMock.Object);

        bool eventFired = false;
        scoreService.ScoreChanged += (s, e) =>
        {
            if (e.StudentId == studentId && e.ScoreChange == 5 && e.NewScore == 15)
            {
                eventFired = true;
            }
        };

        // Act
        await scoreService.AddScoreAsync(studentId, 5, "Good job");

        // Assert
        Assert.True(eventFired);
        Assert.Equal(15, student.Score);
        _studentServiceMock.Verify(s => s.UpdateStudentAsync(student), Times.Once);
    }
}
