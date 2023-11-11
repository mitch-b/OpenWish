using System;
using System.ComponentModel.DataAnnotations;

namespace OpenWish.Entities;

public class Comment
{
    [Key]
    public Guid CommentId { get; set; }
    public string Text { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
}