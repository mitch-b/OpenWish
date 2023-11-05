using System;

namespace OpenWish.Common.Models;

public class Comment
{
    [Key]
    public Guid CommentId { get; set; }
    public string Text { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; }
}