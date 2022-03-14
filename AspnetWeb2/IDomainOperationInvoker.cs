// ReuqstDelegate

interface IDomainOperationInvoker
{
    Task Invoke(HttpContext context);
}
