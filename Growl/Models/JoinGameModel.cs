namespace Growl.Models
{
    using System.ComponentModel.DataAnnotations;

    public class JoinGameModel
    {
        [Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^[A-Za-z]{6}$", ErrorMessage = "Game code should be 6 letters")]
        public string GameCode { get; set; }
    }
}