using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DamageCalcSV.Shared.Models
{
    public class PokemonType
    {
        public PokemonType() {
            // ノーマル（弱点：なし、半減：いわ・はがね、無効：ゴースト）
            TypeCompatibility["ノーマル"] = new Dictionary<string, double>();
            TypeCompatibility["ノーマル"]["いわ"]
            = TypeCompatibility["ノーマル"]["はがね"] = 0.5;
            TypeCompatibility["ノーマル"]["ゴースト"] = 0;

            // ほのお（弱点：くさ・こおり・むし・はがね、半減：みず・いわ・ドラゴン、無効：なし）
            TypeCompatibility["ほのお"] = new Dictionary<string, double>();
            TypeCompatibility["ほのお"]["くさ"]
                = TypeCompatibility["ほのお"]["こおり"]
                = TypeCompatibility["ほのお"]["むし"]
                = TypeCompatibility["ほのお"]["はがね"] = 2;
            TypeCompatibility["ほのお"]["みず"]
                = TypeCompatibility["ほのお"]["いわ"]
                = TypeCompatibility["ほのお"]["ドラゴン"] = 0.5;

            // みず（弱点：ほのお・じめん・いわ、半減：みず・ドラゴン、無効：なし）
            TypeCompatibility["みず"] = new Dictionary<string, double>();
            TypeCompatibility["みず"]["ほのお"]
                = TypeCompatibility["みず"]["じめん"]
                = TypeCompatibility["みず"]["いわ"] = 2;
            TypeCompatibility["みず"]["みず"]
                = TypeCompatibility["みず"]["ドラゴン"] = 0.5;

            // でんき（弱点：みず・ひこう、半減：くさ・でんき・ドラゴン、無効：じめん）
            TypeCompatibility["でんき"] = new Dictionary<string, double>();
            TypeCompatibility["でんき"]["みず"]
                = TypeCompatibility["でんき"]["ひこう"] = 2;
            TypeCompatibility["でんき"]["くさ"]
                = TypeCompatibility["でんき"]["でんき"]
                = TypeCompatibility["でんき"]["ドラゴン"] = 0.5;
            TypeCompatibility["でんき"]["じめん"] = 0;

            // くさ（弱点：みず・じめん・いわ、半減：ほのお・くさ・どく・ひこう・むし・ドラゴン・はがね、無効：なし）
            TypeCompatibility["くさ"] = new Dictionary<string, double>();
            TypeCompatibility["くさ"]["みず"]
                = TypeCompatibility["くさ"]["じめん"]
                = TypeCompatibility["くさ"]["いわ"] = 2;
            TypeCompatibility["くさ"]["ほのお"]
                = TypeCompatibility["くさ"]["くさ"]
                = TypeCompatibility["くさ"]["どく"]
                = TypeCompatibility["くさ"]["ひこう"]
                = TypeCompatibility["くさ"]["むし"]
                = TypeCompatibility["くさ"]["ドラゴン"]
                = TypeCompatibility["くさ"]["はがね"] = 0.5;

            // こおり（弱点：くさ・じめん・ひこう・ドラゴン、半減：ほのお、みず、こおり、はがね、無効：なし）
            TypeCompatibility["こおり"] = new Dictionary<string, double>();
            TypeCompatibility["こおり"]["くさ"]
                = TypeCompatibility["こおり"]["じめん"]
                = TypeCompatibility["こおり"]["ひこう"]
                = TypeCompatibility["こおり"]["ドラゴン"] = 2;
            TypeCompatibility["こおり"]["ほのお"]
                = TypeCompatibility["こおり"]["みず"]
                = TypeCompatibility["こおり"]["こおり"]
                = TypeCompatibility["こおり"]["はがね"] = 0.5;

            // かくとう（弱点：ノーマル・こおり・いわ・あく・はがね、半減：どく・ひこう・エスパー・むし・フェアリー、無効：ゴースト）
            TypeCompatibility["かくとう"] = new Dictionary<string, double>();
            TypeCompatibility["かくとう"]["ノーマル"]
                = TypeCompatibility["かくとう"]["こおり"]
                = TypeCompatibility["かくとう"]["いわ"]
                = TypeCompatibility["かくとう"]["あく"]
                = TypeCompatibility["かくとう"]["はがね"] = 2;
            TypeCompatibility["かくとう"]["どく"]
                = TypeCompatibility["かくとう"]["ひこう"]
                = TypeCompatibility["かくとう"]["エスパー"]
                = TypeCompatibility["かくとう"]["むし"]
                = TypeCompatibility["かくとう"]["フェアリー"] = 0.5;
            TypeCompatibility["かくとう"]["ゴースト"] = 0;

            // どく（弱点：くさ・フェアリー、半減：どく・じめん・いわ・ゴースト、無効：はがね）
            TypeCompatibility["どく"] = new Dictionary<string, double>();
            TypeCompatibility["どく"]["くさ"]
                = TypeCompatibility["どく"]["フェアリー"] = 2;
            TypeCompatibility["どく"]["どく"]
                = TypeCompatibility["どく"]["じめん"]
                = TypeCompatibility["どく"]["いわ"]
                = TypeCompatibility["どく"]["ゴースト"] = 0.5;
            TypeCompatibility["どく"]["はがね"] = 0;

            // じめん（弱点：ほのお・でんき・どく・いわ・はがね、半減：くさ・むし、無効：ひこう）
            TypeCompatibility["じめん"] = new Dictionary<string, double>();
            TypeCompatibility["じめん"]["ほのお"]
                = TypeCompatibility["じめん"]["でんき"]
                = TypeCompatibility["じめん"]["どく"]
                = TypeCompatibility["じめん"]["いわ"]
                = TypeCompatibility["じめん"]["はがね"] = 2;
            TypeCompatibility["じめん"]["くさ"]
                = TypeCompatibility["じめん"]["むし"] = 0.5;
            TypeCompatibility["じめん"]["ひこう"] = 0;

            // ひこう（弱点：くさ・かくとう・むし、半減：いわ・はがね、無効：なし）
            TypeCompatibility["ひこう"] = new Dictionary<string, double>();
            TypeCompatibility["ひこう"]["くさ"]
                = TypeCompatibility["ひこう"]["かくとう"]
                = TypeCompatibility["ひこう"]["むし"] = 2;
            TypeCompatibility["ひこう"]["いわ"]
                = TypeCompatibility["ひこう"]["はがね"] = 0.5;

            // エスパー（弱点：かくとう・どく、半減：エスパー・はがね、無効：あく）
            TypeCompatibility["エスパー"] = new Dictionary<string, double>();
            TypeCompatibility["エスパー"]["かくとう"]
                = TypeCompatibility["エスパー"]["どく"] = 2;
            TypeCompatibility["エスパー"]["エスパー"]
                = TypeCompatibility["エスパー"]["はがね"] = 0.5;
            TypeCompatibility["エスパー"]["あく"] = 0;

            // むし（弱点：くさ・エスパー・あく、半減：ほのお・かくとう・どく・ひこう・ゴースト・はがね・フェアリー、無効：なし）
            TypeCompatibility["むし"] = new Dictionary<string, double>();
            TypeCompatibility["むし"]["くさ"]
                = TypeCompatibility["むし"]["エスパー"]
                = TypeCompatibility["むし"]["あく"] = 2;
            TypeCompatibility["むし"]["ほのお"]
                = TypeCompatibility["むし"]["かくとう"]
                = TypeCompatibility["むし"]["どく"]
                = TypeCompatibility["むし"]["ひこう"]
                = TypeCompatibility["むし"]["ゴースト"]
                = TypeCompatibility["むし"]["はがね"]
                = TypeCompatibility["むし"]["フェアリー"] = 0.5;

            // いわ（弱点：ほのお・こおり・ひこう・むし、半減：かくとう・じめん・はがね、無効：なし）
            TypeCompatibility["いわ"] = new Dictionary<string, double>();
            TypeCompatibility["いわ"]["ほのお"]
                = TypeCompatibility["いわ"]["こおり"]
                = TypeCompatibility["いわ"]["ひこう"]
                = TypeCompatibility["いわ"]["むし"] = 2;
            TypeCompatibility["いわ"]["かくとう"]
                = TypeCompatibility["いわ"]["じめん"]
                = TypeCompatibility["いわ"]["はがね"] = 0.5;

            // ゴースト（弱点：エスパー・ゴースト、半減：あく、無効：ノーマル）
            TypeCompatibility["ゴースト"] = new Dictionary<string, double>();
            TypeCompatibility["ゴースト"]["ゴースト"]
                = TypeCompatibility["ゴースト"]["エスパー"] = 2;
            TypeCompatibility["ゴースト"]["あく"] = 0.5;
            TypeCompatibility["ゴースト"]["ノーマル"] = 0;

            // ドラゴン（弱点：ドラゴン、半減：はがね、無効：フェアリー）
            TypeCompatibility["ドラゴン"] = new Dictionary<string, double>();
            TypeCompatibility["ドラゴン"]["ドラゴン"] = 2;
            TypeCompatibility["ドラゴン"]["はがね"] = 0.5;
            TypeCompatibility["ドラゴン"]["フェアリー"] = 0;

            // あく（弱点：エスパー・ゴースト、半減：あく・フェアリー、無効：なし）
            TypeCompatibility["あく"] = new Dictionary<string, double>();
            TypeCompatibility["あく"]["エスパー"]
                = TypeCompatibility["あく"]["ゴースト"] = 2;
            TypeCompatibility["あく"]["あく"]
                = TypeCompatibility["あく"]["フェアリー"] = 0.5;

            // はがね（弱点：こおり・いわ・フェアリー、半減：ほのお・みず・でんき・はがね、無効：なし）
            TypeCompatibility["はがね"] = new Dictionary<string, double>();
            TypeCompatibility["はがね"]["こおり"]
                = TypeCompatibility["はがね"]["いわ"]
                = TypeCompatibility["はがね"]["フェアリー"] = 2;
            TypeCompatibility["はがね"]["ほのお"]
                = TypeCompatibility["はがね"]["みず"]
                = TypeCompatibility["はがね"]["でんき"]
                = TypeCompatibility["はがね"]["はがね"] = 0.5;

            // フェアリー（弱点：かくとう・ドラゴン・あく、半減：ほのお・どく・はがね、無効：なし）
            TypeCompatibility["フェアリー"] = new Dictionary<string, double>();
            TypeCompatibility["フェアリー"]["かくとう"]
                = TypeCompatibility["フェアリー"]["ドラゴン"]
                = TypeCompatibility["フェアリー"]["あく"] = 2;
            TypeCompatibility["フェアリー"]["ほのお"]
                = TypeCompatibility["フェアリー"]["どく"]
                = TypeCompatibility["フェアリー"]["はがね"] = 0.5;
        }

        public double CompatibilityCheck( string atktype, string deftype ) // atk -> defの倍率を返す
        {
            if (TypeCompatibility[atktype].ContainsKey( deftype ) )
            {
                return (TypeCompatibility[atktype][deftype]);
            }

            // atk -> def が未定義の場合は一律で等倍
            return (1);
        }

        Dictionary<string, Dictionary<string, double>> TypeCompatibility = new Dictionary<string, Dictionary<string, double>>();
    }
}
