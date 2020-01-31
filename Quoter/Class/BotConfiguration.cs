using NetEscapades.Configuration.Validation;

namespace Quoter.Class
{
    public class BotConfiguration : IValidatable
    {
        public string BotToken { get; set; }
        public string BotId { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.BotToken))
            {
                throw new SettingsValidationException(nameof(BotConfiguration), nameof(this.BotToken), "must be a non-empty string");
            }
            if (string.IsNullOrWhiteSpace(this.BotId))
            {
                throw new SettingsValidationException(nameof(BotConfiguration), nameof(this.BotId), "must be a non-empty string");
            }
        }
    }
}
