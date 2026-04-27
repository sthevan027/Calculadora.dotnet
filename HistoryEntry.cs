using System;

namespace Calculadora;

public sealed record HistoryEntry(DateTimeOffset At, string Expression, string Result);

