using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    // Uploads a file to storage
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] Guid? parentFolderId, [FromQuery] Guid? fileEntryId)
    {
        var userId = GetUserId();
        var result = await _fileService.UploadFileAsync(file, userId, parentFolderId, fileEntryId);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    // Creates a new folder
    [HttpPost("folder")]
    public async Task<IActionResult> CreateFolder([FromBody] string name, [FromQuery] Guid? parentFolderId)
    {
        var userId = GetUserId();
        var result = await _fileService.CreateFolderAsync(name, userId, parentFolderId);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    // Gets details about a specific file or folder
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileOrFolder(Guid id)
    {
        var userId = GetUserId();
        var result = await _fileService.GetFileOrFolderAsync(id, userId);

        if (!result.IsSuccess)
        {
            if (result.Error == "Unauthorized access")
            {
                return Unauthorized(new { message = result.Error });
            }
            return NotFound(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    // Accesses a file or folder using a share link
    [HttpGet("share/{shareLink}")]
    public async Task<IActionResult> GetByShareLink(string shareLink)
    {
        var userId = GetUserId(); 
        var result = await _fileService.GetByShareLinkAsync(shareLink, userId);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    // Shares a file or folder with another user
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFileOrFolder(Guid id, [FromQuery] Guid targetUserId, [FromQuery] AccessLevel accessLevel)
    {
        // Gets the logged-in user’s ID (the owner)
        var ownerId = GetUserId();
        var result = await _fileService.ShareFileOrFolderAsync(id, ownerId, targetUserId, accessLevel);

        if (!result.IsSuccess)
        {
            if (result.Error == "User does not own this file or folder.")
            {
                return Unauthorized(new { message = result.Error });
            }

            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    // Lists all files and folders inside a folder
    [HttpGet("contents")]
    public async Task<IActionResult> ListFolderContents([FromQuery] Guid? folderId)
    {
        var userId = GetUserId();
        var result = await _fileService.ListFolderContentsAsync(folderId, userId);

        if (result == null)
        {
            return NotFound(new { message = "No contents found for the specified folder." });
        }
        return Ok(result);
    }

    // Shows all versions of a file
    [HttpGet("{fileId}/versions")]
    public async Task<IActionResult> GetFileVersions(Guid fileId)
    {
        var userId = GetUserId();
        var result = await _fileService.GetFileVersionsAsync(fileId, userId);

        if (result == null)
        {
            return NotFound(new { message = "No versions found for the specified file." });
        }

        return Ok(result);
    }

    // Restores a file to a previous version
    [HttpPost("{fileId}/restore")]
    public async Task<IActionResult> RestoreVersion(Guid fileId, int VersionNumber)
    {
        var userId = GetUserId();
        var result = await _fileService.RestoreFileVersionAsync(fileId, userId, VersionNumber);

        if (result == null)
        {
            return NotFound(new { message = "Version not found or restoration failed." });
        }

        return Ok(result);
    }

    // Deletes a file or folder
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFileOrFolder(Guid id)
    {
        var userId = GetUserId();
        await _fileService.DeleteFileOrFolderAsync(id, userId);
        return NoContent();
    }

    // Helper method to get the logged-in user’s ID from their authentication info
    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found."));
}
