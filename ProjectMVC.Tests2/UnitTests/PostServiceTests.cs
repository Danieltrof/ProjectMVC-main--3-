using Moq;
using Xunit;
using ProjectMVC.Services;
using ProjectMVC.DAL.Repository.Interfaces;
using ProjectMVC.DTO;
using ProjectMVC.DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class PostServiceTests
{
    //Mock dependencies
    private Mock<IPostRepository> _postRepositoryMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock; //Mock for accessing HTTP Context
    private Mock<IAuthorizationService> _authorizationServiceMock;
    private Mock<ILogger<PostService>> _loggerMock;
    private PostService _postService;

    //Constructor
    public PostServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _loggerMock = new Mock<ILogger<PostService>>();
        _postService = new PostService(_postRepositoryMock.Object, _httpContextAccessorMock.Object, _authorizationServiceMock.Object, _loggerMock.Object);
    }

    //Tests the GetAllPostsAsync method when the user is authenticated (Positive test READ)
    [Fact]
    public async Task GetAllPostsAsync_ShouldReturnPosts_WhenUserIsAuthenticated()
    {
        // Arrange: Setting up test data and mocking 
        var userId = "user123";
        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(mockUser);

        var user = new ApplicationUser { Id = userId, Name = "Test User", ProfilePicture = "/images/user.jpg" };

        var posts = new List<Post>
        {
            new Post 
            { 
                Id = 1, 
                Title = "Post 1", 
                UserId = userId, 
                User = user, 
                Comments = new List<Comment> 
                {
                    new Comment 
                    { 
                        Id = 1, 
                        Content = "Nice post!", 
                        User = user, 
                        Created = DateTime.UtcNow 
                    }
                } 
            },
            new Post 
            { 
                Id = 2, 
                Title = "Post 2", 
                UserId = userId, 
                User = user, 
                Comments = new List<Comment>() 
            }
        };

        _postRepositoryMock.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(posts);
        _postRepositoryMock.Setup(repo => repo.GetLikedPostIdsAsync(userId)).ReturnsAsync(new List<int> { 1 });

        _authorizationServiceMock.Setup(auth => auth.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, "CanEdit"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act: Calling the method being tested
        var result = await _postService.GetAllPostsAsync();

        // Assert: Verifying the outcome
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); //The ammount of posts should be equal to two, because we made two posts in our test data
        Assert.True(result[0].IsLikedByUser);
        Assert.Single(result[0].Comments);
    }

    //Testing the CreatePostAsync method when the user is NOT authenticated (Negative test CREATE)
    [Fact]
    public async Task CreatePostAsync_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        //Notice how we dont make a new application user, which means we are trying to upload without authentication
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns((ClaimsPrincipal)null);
        var dto = new PostCreateDto { Title = "New Post" };

        var result = await _postService.CreatePostAsync(dto);

        //Verifying the error response
        Assert.False(result.Succeeded);
        Assert.Equal("You need to be logged in to create posts", result.Error);
    }

    //Testing the CreatePostAsync method when the input is valid (positive CREATE)
    [Fact]
    public async Task CreatePostAsync_ShouldCreatePost_WhenValidInputProvided()
    {
        //test data and mock
        var userId = "user123";
        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(mockUser);

        var dto = new PostCreateDto { Title = "New Post", ImageFile = null };

        _postRepositoryMock.Setup(repo => repo.AddPostAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);

        //Calling method
        var result = await _postService.CreatePostAsync(dto);

        //Verifying if post is created
        Assert.True(result.Succeeded);
        Assert.Equal("Post created successfully", result.SuccessMessage);
        _postRepositoryMock.Verify(repo => repo.AddPostAsync(It.IsAny<Post>()), Times.Once);
    }

    //Testing the UpdatePostAsync method if post doesnt exist (negative UPDATE)
    [Fact]
    public async Task UpdatePostAsync_ShouldReturnFailure_WhenPostDoesNotExist()
    {
        //Trying to update title for a non existent post, hence null
        _postRepositoryMock.Setup(repo => repo.GetPostByIdAsync(1)).ReturnsAsync((Post)null);
        var dto = new PostUpdateDto { Title = "Updated Title" };

        var result = await _postService.UpdatePostAsync(1, dto);

        Assert.False(result.Succeeded);
        Assert.Equal("Post does not exist", result.Error);
    }

    //Testing the deletepostAsync method when the user is NOT authorized, e.g. if ordinary user can delete posts of other ordinary users. (Negative DELETE)
    [Fact]
    public async Task DeletePostAsync_ShouldReturnFailure_WhenUserNotAuthorized()
    {
        //Setting up two different users and one post
        var userId = "user123";
        var post = new Post { Id = 1, UserId = "differentUser" };

        _httpContextAccessorMock.Setup(h => h.HttpContext.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) })));
        _postRepositoryMock.Setup(repo => repo.GetPostByIdAsync(1)).ReturnsAsync(post);

        _authorizationServiceMock.Setup(auth => auth.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), post, "CanEdit"))
            .ReturnsAsync(AuthorizationResult.Failed());

        var result = await _postService.DeletePostAsync(1);

        Assert.False(result.Succeeded);
        Assert.Equal("You are not authorized to delete this post", result.Error);
    }
}