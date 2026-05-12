using Discord;
using Discord.WebSocket;
using JifBot.Builders;
using JifBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JifBot
{
    public class SelectMenuHandler
    {
        public async Task HandleSelectMenu(SocketMessageComponent component)
        {
            string[] pieces = component.Data.CustomId.Split("-");
            var db = new BotBaseContext();
            var eventBuilder = new EventUIBuilder();

            if (pieces[0] == "event")
            {
                var id = long.Parse(pieces[2]);
                var ev = db.Event.Where(e => e.Id == id).First();
                var value = component.Data.Values.First();

                if (pieces[1] == "type")
                {
                    ev.EventType = value;
                }
                else if (pieces[1] == "entrant")
                {
                    ev.EntrantType = value;
                }
                else if (pieces[1] == "channel")
                {
                    ev.ForumChannelId = ulong.Parse(value);
                }

                db.SaveChanges();
                var newComp = eventBuilder.BuildComponent(ev);
                await component.UpdateAsync(m => m.Components = newComp);
            }
        }
    }
}
