using Aoiro;

namespace Gameplay
{
    public static class ProcedureExtensions
    {
        public static void CancelWhenProcedureDestroy<T>(this T cancelToken, IProcedureContext context) where T : struct, ICancelToken
        {
            context.CancelWhenDestroy(new CancelToken<T>(cancelToken));
        }

        public static void CancelWhenProcedureDestroy(this ICancelToken cancelToken, IProcedureContext context)
        {
            context.CancelWhenDestroy(cancelToken);
        }
    }
}
