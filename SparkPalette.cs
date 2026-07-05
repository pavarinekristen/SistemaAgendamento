using System.Windows.Media;

namespace AgendamentoWpfApp;

/// <summary>
/// Cores da marca SparkCore (bege, laranja, preto e branco) para uso em code-behind e ViewModels.
/// A paleta das telas XAML fica em Styles/Colors.xaml; manter os dois em sincronia.
/// </summary>
internal static class SparkPalette
{
    public static readonly Brush Success = CriarBrush(201, 94, 12);   // laranja escuro (#C95E0C)
    public static readonly Brush Error = CriarBrush(192, 57, 43);     // vermelho (#C0392B)
    public static readonly Brush Muted = CriarBrush(138, 124, 108);   // bege escuro (#8A7C6C)

    private static Brush CriarBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
