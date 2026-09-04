using System;
using Application.Models.Common;

namespace Application.Models;

public class UserNotLikeProfileDto:BaseDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long NotLikedId { get; set; }
}
