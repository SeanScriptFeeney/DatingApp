using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers {
    [Authorize]
    [Route ("api/users/{userid}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase {
        private readonly Cloudinary _cloudinary;

        public IDatingRepository _repo { get; }
        public IMapper _mapper { get; }
        public IOptions<CloudinarySettings> _cloudinaryConfig { get; }
        public PhotosController (IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig) {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repo;

            Account acc = new Account (
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary (acc);

        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id) {
            
            var photoFromrepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForCreationDto>(photoFromrepo);
            return Ok(photo);
        }


        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser (int userId, PhotoForCreationDto photoForCreationDto) {

            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            var userFromRepo = await _repo.GetUser (userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult ();

            if (file.Length > 0) {
                using (var stream = file.OpenReadStream ()) {
                    var uploadParams = new ImageUploadParams () {
                    File = new FileDescription (file.FileName, stream),
                    Transformation = new Transformation ().Width (500).Height (500).Crop ("fill").Gravity ("face")
                    };

                    uploadResult = _cloudinary.Upload (uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if(!userFromRepo.Photos.Any(u => u.IsMain)){
                photo.IsMain = true;
            }

            userFromRepo.Photos.Add(photo);            

            if(await _repo.SaveAll()){
                var photoToReturn = _mapper.Map<PhotoForCreationDto>(photo);
                return CreatedAtRoute("GetPhoto", new { userId = userId, id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could Not add the photo");

        }
    }
}