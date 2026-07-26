namespace Terror
{
    /// <summary>
    /// Nombres de las escenas del shell, en un solo lugar. Antes andaban sueltos
    /// como strings en Scriptcambio ("Historia") y en el borrador del menu de pausa
    /// ("01_Menu"), donde un typo solo se descubre al ejecutar.
    ///
    /// Ojo: LeyendaDefinicion.nombreEscena NO pasa por aca a proposito. Ese campo es
    /// dato de autor — se llena por leyenda en su .asset — y meterlo en codigo
    /// anularia el sentido del ScriptableObject.
    /// </summary>
    public static class Escenas
    {
        public const string Boot = "00_Boot";
        public const string Menu = "01_Menu";
        public const string L1Intro = "L1_Intro";
        public const string L1Juego = "L1_Juego";
    }
}
