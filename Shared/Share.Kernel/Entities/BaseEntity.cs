using Share.Kernel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Kernel.Entities
{
    public abstract class BaseEntity : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public Guid Id { get; protected set; } = Guid.NewGuid();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
