namespace DamageCalcSV.Shared.Models
{
    public class PokemonData
    {
        public string Name { get; set; }

        public PokemonData(string name)
        {
            Name = name;
        }
    }

    public class  PokemonDataManager
    {
        private List<PokemonData> PokemonList = new List<PokemonData>();
        private List<string> PokemonNameList = new List<string>();
        public List<string> AllPokemonName()
        {
            return (PokemonNameList);
        }

        // �f�[�^�x�[�X���g���̂͂���ǂ����Ȃ̂ŁA�S�������ɏ����Ă��܂�
        private void AddPokemonData(string name, int HP, int Attack, int Block, int Constant, int Deffence, int Speed,
            string type1, string type2, params string[] movelist)
        {
            PokemonList.Add( new PokemonData(name) );
            PokemonNameList.Add(name);
        }

        public void InitializePokemonData()
        {
            AddPokemonData("�t�V�M�_�l", 10, 20, 30, 40, 50, 60, "����", "�ǂ�", "����������", "���[�t�X�g�[��");
            AddPokemonData("�t�V�M�o�i", 10, 20, 30, 40, 50, 60, "����", "�ǂ�", "����������", "���[�t�X�g�[��");
        }
    }
}