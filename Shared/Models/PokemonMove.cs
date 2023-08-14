namespace DamageCalcSV.Shared.Models
{
    public class PokemonMove
    {
        public string Name { get; set; }

        public PokemonMove(string name)
        {
            Name = name;
        }
    }
}