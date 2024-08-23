using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using JifBot.Models;

namespace JifBot
{
    public class ModalHandler
    {
        public async Task HandleModalSubmitted(SocketModal modal)
        {
            var id = modal.Data.CustomId;
            if (id.StartsWith("character_description"))
            {
                // I know this is scuffed as hell leave me alone
                var key = id.Split(":")[1];
                var db = new BotBaseContext();
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                character.Description = modal.Data.Components.First(x => x.CustomId == "description").Value;
                db.SaveChanges();
                await modal.RespondAsync($"{key} successfully updated", ephemeral: true);
            }
        }
    }
}
