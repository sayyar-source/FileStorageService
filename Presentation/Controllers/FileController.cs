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

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] Guid? parentFolderId, [FromQuery] Guid? fileEntryId)
    {
        var userId = GetUserId();
        var fileDto = await _fileService.UploadFileAsync(file, userId, parentFolderId, fileEntryId);
        return Ok(fileDto);
       // return fileEntryId.HasValue ? Ok(fileDto) : CreatedAtAction(nameof(GetFileOrFolder), new { parentFolderId = fileDto.ParentFolderId }, fileDto);
    }

    [HttpPost("folder")]
    public async Task<IActionResult> CreateFolder([FromBody] string name, [FromQuery] Guid? parentFolderId)
    {
        var userId = GetUserId();
        var folderDto = await _fileService.CreateFolderAsync(name, userId, parentFolderId);
        return Ok(folderDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileOrFolder(Guid id)
    {
        var userId = GetUserId();
        var entry = await _fileService.GetFileOrFolderAsync(id, userId);
        return Ok(entry);
    }

    [HttpGet("share/{shareLink}")]
    public async Task<IActionResult> GetByShareLink(string shareLink)
    {
        var userId = GetUserId();
        var entry = await _fileService.GetByShareLinkAsync(shareLink, userId);
        return Ok(entry);
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFileOrFolder(Guid id, [FromQuery] Guid targetUserId, [FromQuery] AccessLevel accessLevel)
    {
        var ownerId = GetUserId();
        var response = await _fileService.ShareFileOrFolderAsync(id, ownerId, targetUserId, accessLevel);
        return Ok(response);
    }

    [HttpGet("contents")]
    public async Task<IActionResult> ListFolderContents([FromQuery] Guid? folderId)
    {
        var userId = GetUserId();
        var contents = await _fileService.ListFolderContentsAsync(folderId, userId);
        return Ok(contents);
    }

    [HttpGet("{fileId}/versions")]
    public async Task<IActionResult> GetFileVersions(Guid fileId)
    {
        var userId = GetUserId();
        var versions = await _fileService.GetFileVersionsAsync(fileId, userId);
        return Ok(versions);
    }

    [HttpPost("{fileId}/restore")]
    public async Task<IActionResult> RestoreVersion(Guid fileId, int VersionNumber)
    {
        var userId = GetUserId();
        var fileDto = await _fileService.RestoreFileVersionAsync(fileId, userId, VersionNumber);
        return Ok(fileDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFileOrFolder(Guid id)
    {
        var userId = GetUserId();
        await _fileService.DeleteFileOrFolderAsync(id, userId);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found."));
}
