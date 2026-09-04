using System;
using Domain.Common;

namespace Domain;

public class UserNotLikeProfile : BaseEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long NotLikedId { get; set; }
}
