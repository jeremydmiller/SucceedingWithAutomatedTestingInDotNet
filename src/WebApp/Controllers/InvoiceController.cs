using System;
using System.Threading.Tasks;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class InvoiceCreated
    {
        public Guid InvoiceId { get; set; }
    }

    public class CreateInvoice
    {
        public string Purchaser { get; set; }
        public double Amount { get; set; }
        public string Item { get; set; }
    }

    public class Invoice
    {
        public DateTime Time { get; set; }
        public string Purchaser { get; set; }
        public double Amount { get; set; }
        public string Item { get; set; }
        public Guid Id { get; set; }
    }
    

    public interface IQueue
    {
        Task Send<T>(T message);
    }
    
    
    
    
    public class InvoiceController : Controller
    {
        private readonly IDocumentStore _store;
        private readonly IQueue _queue;

        public InvoiceController(IDocumentStore store, IQueue queue)
        {
            _store = store;
            _queue = queue;
        }

        [HttpPost("/marten/invoice/create")]
        public async Task<IActionResult> CreateInvoice(CreateInvoice command)
        {
            // There'd be some kind of validation here in real life

            var document = new Invoice
            {
                Amount = command.Amount,
                Item = command.Item,
                Purchaser = command.Purchaser,
                Time = DateTime.UtcNow
            };

            using (var session = _store.LightweightSession())
            {
                session.Store(document);
                await session.SaveChangesAsync();
            }

            await _queue.Send(new InvoiceCreated {InvoiceId = document.Id});

            return Ok();
        }
    }
}