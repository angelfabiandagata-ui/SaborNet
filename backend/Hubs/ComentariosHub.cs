using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class ComentariosHub : Hub
    {
        public async Task EnviarComentario(string comentario)
        {
            // deberia enviar a la p
            await Clients.All.SendAsync("RecibirComentario", comentario);
        }
    }
}