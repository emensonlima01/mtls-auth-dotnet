using Payment.Api.DTOs;

namespace Payment.Api.UseCases;

public interface ICreatePaymentUseCase
{
    Task Handle(CreatePaymentRequest request, CancellationToken cancellationToken = default);
}
