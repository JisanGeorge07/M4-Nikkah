using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Application.Interfaces.Persistence;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Http;

namespace Persistence.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IFileService _fileService;
        private readonly IRepository<UserNotLikeProfile> _userNotLikeProfileRepo;

        public ProfileService(IRegistrationRepository registrationRepository, IFileService fileService,
            IRepository<UserNotLikeProfile> userNotLikeProfileRepo)
        {
            _registrationRepository = registrationRepository;
            _fileService = fileService;
            _userNotLikeProfileRepo = userNotLikeProfileRepo;
        }

        public async Task<string> AddProfilePicAsync(long userId, IFormFile? image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            string folderPath = "Uploads/Registration";
            var existingRegistration = await _registrationRepository.GetRegistrationByIdAsync(userId);
            if (existingRegistration == null)
            {
                throw new Exception("User not found");
            }

            // Delete the existing file if it exists
            if (!string.IsNullOrEmpty(existingRegistration.ImagePath))
            {
                await _fileService.DeleteFile(existingRegistration.ImagePath);
            }

            // Upload the new file
            string newImagePath = await _fileService.SaveFile(image, folderPath);

            // Update the registration record with the new file path
            existingRegistration.ImagePath = newImagePath;
            await _registrationRepository.UpdateRegistrationAsync(existingRegistration);

            return newImagePath;
        }

        public async Task<bool> NotLikeProfileAsync(long userId, long notLikedProfileId)
        {
            // Check if this profile entry exists (active or inactive)
            var existing = await _userNotLikeProfileRepo.FirstOrDefault(
                x => x.UserId == userId && x.NotLikedId == notLikedProfileId);

            if (existing != null)
            {
                // Toggle IsActive status
                existing.IsActive = !existing.IsActive;
                existing.ModifiedOn = DateTime.Now;
                await _userNotLikeProfileRepo.Update(existing);
                await _userNotLikeProfileRepo.SaveChanges();
                return true;
            }

            var notLikeEntry = new UserNotLikeProfile
            {
                UserId = userId,
                NotLikedId = notLikedProfileId,
                IsActive = true,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            };

            await _userNotLikeProfileRepo.Add(notLikeEntry);
            await _userNotLikeProfileRepo.SaveChanges();
            return true;
        }

        public async Task<List<long>> GetNotLikedProfileIdsAsync(long userId)
        {
            var notLikedProfiles = await _userNotLikeProfileRepo.WhereActive(
                x => x.UserId == userId);

            return notLikedProfiles.Select(x => x.NotLikedId).ToList();
        }
    }
}
