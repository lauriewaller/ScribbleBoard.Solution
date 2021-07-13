using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScribbleBoardApi.Models; 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScribbleBoardApi.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class ImagesController : ControllerBase
  {
    private readonly ScribbleBoardApiContext _db;
    public ImagesController(ScribbleBoardApiContext db)
    {
      _db = db;
    }
    // [HttpGet]
    // public async Task<ActionResult<IEnumerable<Image>>> Get(string userId, string userName)
    // {
    //   var query = _db.Images.AsQueryable();
    //   if (userId != null)
    //   {
    //     query = query.Where(e => e.UserId == userId);
    //   }
    //   if (userName != null)
    //   {
    //     query = query.Where(e => e.UserName == userName);
    //   }
    //   return await query.ToListAsync();
    // }
    [HttpPost]
    public async Task<ActionResult<Image>> Post(Image image)
    {
      _db.Images.Add(image);
      await _db.SaveChangesAsync();
      return CreatedAtAction(nameof(GetImage), new {id = image.ImageId}, image);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<Image>> GetImage(int id)
    {
      Image img = await _db.Images.FindAsync(id);
      if (img == null)
      {
        return NotFound();
      }
      return img;
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Image image)
    {
      if (id != image.ImageId)
      {
        return BadRequest();
      }
      _db.Entry(image).State = EntityState.Modified;
      try
      {
        await _db.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!ImageExists(id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }
      return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
      var img = await _db.Images.FindAsync(id);
      if (img == null)
      {
        return NotFound();
      }
      _db.Images.Remove(img);
      await _db.SaveChangesAsync();
      return NoContent();
    }
    // uploading directly to API 
    [HttpPost]
    [Route("[action]")]
    public async Task<ActionResult<Image>> UploadDirect(IFormFile image)
    {
      using (var stream = new MemoryStream())
      {
        image.CopyTo(stream);
        var img = new Image()
        {
          Data = "data:image/jpeg;base64," + Convert.ToBase64String(stream.ToArray()),
          UserId = "directUploadID",
          UserName = "directUpload",
          Title = image.FileName,
          Description = $"Description for {image.FileName}",
          CreatedAt = DateTime.Now
        };
        _db.Images.Add(img);
        await _db.SaveChangesAsync();
        return CreatedAtAction("UploadDirect", new { id = img.ImageId}, img);
      }
    }
   private bool ImageExists(int id)
    {
      return _db.Images.Any(e => e.ImageId == id);
    }
    // pagination experiment
    [HttpGet]
    // [Route("[action]")]
    public async Task<IActionResult> Get([FromQuery] PaginationFilter filter)
    {
      var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize, filter.UserName);
      var query = _db.Images.AsQueryable();
      if (validFilter.UserName != null)
      {
        query = query.Where(e => e.UserName == validFilter.UserName);
      }
      // var data = await query.Skip((validFilter.PageNumber-1) * validFilter.PageSize)
      //                       .Take(validFilter.PageSize).ToListAsync();

      // var data = query.Skip((validFilter.PageNumber-1) * validFilter.PageSize)
      //                       .Take(validFilter.PageSize);
      // returns a PagedList
      var pagedData = await PagedList<Image>.ToPagedListAsync(query, filter.PageNumber, filter.PageSize);
      var metadata = new
      {
        pagedData.TotalCount,
        pagedData.PageSize,
        pagedData.CurrentPage,
        pagedData.TotalPages,
        pagedData.HasNext,
        pagedData.HasPrevious
      };
      Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(metadata));
      return Ok(pagedData);
    }
  }
}