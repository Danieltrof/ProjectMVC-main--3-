using Microsoft.EntityFrameworkCore;
using Xunit;
using ProjectMVC.DAL;
using ProjectMVC.DAL.Entities;
using ProjectMVC.DAL.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PostRepositoryTests
{
    //Helper method to create a new in-memory database context with a unique name
    //Unsure why i got issues across tests. Had to make a unique database for each test to ensure that the errors i previously got, didnt occur again.
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()) //Use a unique database name for each test
            .Options;

        return new ApplicationDbContext(options);
    }

    //Testing if GetAllPostsAsync method returns list of posts when there are existing posts in database. Positive test for READ
    [Fact]
    public async Task GetAllPostsAsync_ShouldReturnListOfPosts_WhenPostsExist()
    {
        //Arrange: Setting up in-memory context and seed the data
        var context = GetInMemoryDbContext();

        //Creating two users for the posts
        var user1 = new ApplicationUser { Id = "user1", Name = "Test User 1" };
        var user2 = new ApplicationUser { Id = "user2", Name = "Test User 2" };

        //Adding the users we made to the context
        context.Users.AddRange(user1, user2);

        //Adding posts to the users we made
        context.Posts.AddRange(new List<Post>
        {
            new Post { Id = 1, Title = "First Post", UserId = "user1", User = user1 },
            new Post { Id = 2, Title = "Second Post", UserId = "user2", User = user2 }
        });

        //Saving the changes to the database for this test
        await context.SaveChangesAsync();

        //Creating a repository instance
        var repository = new PostRepository(context);

        //Act: Calling the method to retrieve list with all posts
        var result = await repository.GetAllPostsAsync();

        //Assert: Verifying that the result is not null and contains the two posts we made earlier
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    //Checking if GetAllPostsAsync method still returns a list even though there is no posts yet. Negative test for READ
    [Fact]
    public async Task GetAllPostsAsync_ShouldReturnEmptyList_WhenNoPostsExist()
    {
        //Setting up an in-memory context with no data
        var context = GetInMemoryDbContext();
        var repository = new PostRepository(context);

        //retrieve all posts
        var result = await repository.GetAllPostsAsync();

        //Verifying that the result is not null AND contains no posts, hence an empty list.
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    //TEsting if GetPostByIdAsync method can return post when post exists
    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnPost_WhenPostExists()
    {
        var context = GetInMemoryDbContext();

        var user = new ApplicationUser { Id = "user1", Name = "Test User" };

        context.Users.Add(user);

        var post = new Post { Id = 1, Title = "Test Post", UserId = "user1", User = user };
        context.Posts.Add(post);

        await context.SaveChangesAsync();

        var repository = new PostRepository(context);

        //Calling the method to retrieve the post with ID = 1
        var result = await repository.GetPostByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test Post", result.Title); //Checking if title matches ensuring the correct post and not a previously made post with id=1
    }

    //Same as previous test but with a non-existent post
    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnNull_WhenPostDoesNotExist()
    {
        var context = GetInMemoryDbContext();
        var repository = new PostRepository(context);

        var result = await repository.GetPostByIdAsync(999); //Non-existent ID

        //Verifying that the result is null
        Assert.Null(result);
    }

    //Tests if AddPostAsync lets user create new posts when they have valid input. Positive test for CREATE
    [Fact]
    public async Task AddPostAsync_ShouldAddPost_WhenPostIsValid()
    {
        var context = GetInMemoryDbContext();
        var repository = new PostRepository(context);

        //Creating a new post
        var post = new Post { Id = 1, Title = "New Post", UserId = "user1" };

        //Act: Add the post to the database
        await repository.AddPostAsync(post);

        // Assert: Verify that the post was added successfully
        var addedPost = await context.Posts.FindAsync(1);
        Assert.NotNull(addedPost);
        Assert.Equal("New Post", addedPost.Title);
    }

    //Tests if AddPostAsync recognizes null input when creating posts. Negative test for CREATE
    [Fact]
    public async Task AddPostAsync_ShouldThrowException_WhenPostIsInvalid()
    {
        var context = GetInMemoryDbContext();
        var repository = new PostRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await repository.AddPostAsync(null));
    }

    //Tests if UpdatePostAsync lets users update their posts. Positive test for UPDATE
    [Fact]
    public async Task UpdatePostAsync_ShouldUpdatePost_WhenPostExists()
    {
        //Making a new post that represents an "Old post" by user1
        var context = GetInMemoryDbContext();
        var post = new Post { Id = 1, Title = "Old Post", UserId = "user1" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var repository = new PostRepository(context);

        //Updating the post title to "Update Post" and save changes
        post.Title = "Updated Post";
        await repository.UpdatePostAsync(post);

        //Verifying that the post was updated successfully
        var updatedPost = await context.Posts.FindAsync(1);
        Assert.NotNull(updatedPost);
        Assert.Equal("Updated Post", updatedPost.Title);
    }

    //Tests if the DeletePostAsync can delete posts that exist. Positive test for DELETE
    [Fact]
    public async Task DeletePostAsync_ShouldRemovePost_WhenPostExists()
    {
        //Making a post that we are going to delete later
        var context = GetInMemoryDbContext();
        var post = new Post { Id = 1, Title = "Post to Delete", UserId = "user1" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var repository = new PostRepository(context);

        //Deleting the post from the database by calling the method
        await repository.DeletePostAsync(post);

        //Verifying that the post was removed successfully
        var deletedPost = await context.Posts.FindAsync(1);
        Assert.Null(deletedPost);
    }
}
