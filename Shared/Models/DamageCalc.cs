using DamageCalcSV.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static MudBlazor.CategoryTypes;
using static System.Net.Mime.MediaTypeNames;

/*
 *  Options[x]のメモ
 *   0: リフレクター     1: ひかりのかべ    2: てだすけ
 *   3: きあいだめ       4: じゅうでん      5: そうでん
 *   6: はがねのせいしん 7: フラワーギフト  8: ハロウィン
 *   9: もりののろい    10: みずびたし     11: フレンドガード
 *  12: ダメ半減特性    13: しんかのきせき 14: テラスタルON
 *  15: 毒・猛毒        16: 火傷           17: 麻痺
 *  18: 眠り            19: 小さくなる
 */

namespace DamageCalcSV.Shared.Models
{
    public class DamageCalc
    {
        public static bool Gravity, WonderRoom, PlasmaShower, FairyAura, DarkAura, AuraBreak;
        public static int SelectedBattleStyle, SelectedWeatherSettings, SelectedFieldSettings;

        private PokemonMoveManager? MoveManager = null;
        private PokemonType? TypeCompatible = null;
        public DamageCalc()
        {
            MoveManager = new PokemonMoveManager();
            TypeCompatible = new PokemonType();
        }
        private int CorrectPower(string name, PokemonDataReal atk, PokemonDataReal def)
        {
            // 技の威力が変わる場合に補正する処理
            var MoveInfo = MoveManager.GetMoveInfo(name);
            int power = MoveInfo.Power;

            // サイコフィールドでワイドフォースを撃つ場合は威力2倍かつ全体技
            // -> 技データベースの方を書き換えると面倒なのをどうするか…
            //  -> ここでは威力だけ補正して、ダブル判定etcは本体の関数でやる(たぶん)
            if (SelectedFieldSettings == 3) // フィールドとか天気の数値はdefine的な感じで書きたいがC#での書き方がわからん。。
            {
                if (name == "ワイドフォース")
                {
                    return (power * 2);
                }
            }

            // エレキボール、ジャイロボールは素早さを比較して威力決定
            if (name == "エレキボール")
            {
                int atkS = atk.Speed;
                int defS = def.Speed;
            }
            else if (name == "ジャイロボール")
            {
                int atkS = atk.Speed;
                int defS = def.Speed;
            }

            // サイコブレイドはエレキフィールドで威力1.5倍
            if (SelectedFieldSettings == 1)
            {
                if (name == "サイコブレイド")
                {
                }
            }

            // イナズマドライブ/アクセルブレイクは弱点をつくと威力1.33倍
            // -> ここで弱点計算するのは面倒なので、「弱点をついた」フラグをONにするのが良いかも
            //  -> むしろ弱点処理する方でやるべきか…？
            if (name == "アクセルブレイク" || name == "イナズマドライブ")
            {
            }

            // 特性「テクニシャン」で威力60以下の技は威力1.5倍
            if (atk.ability == "テクニシャン")
            {
                if (power <= 60)
                {
                    power *= (2048 + 4096);
                    power += 0; // 切り捨てだった気がする？
                    power /= 4096;
                }
            }

            // ミストフィールドでドラゴン技を使うと威力半減（ダメージ半減？どっち？）
            if (SelectedFieldSettings == 4)
            {
                if (MoveInfo.Type == "ドラゴン")
                {
                    power *= 2048;
                    power /= 4096;
                }
            }

            // ヒートスタンプ、けたぐり、ヘビーボンバーは体重差によって威力決定
            if (name == "ヒートスタンプ" || name == "けたぐり" || name == "ヘビーボンバー")
            {
            }

            // 無天候と晴れ以外の天候でのソーラービーム/ブレードは威力半減
            if (name == "ソーラービーム" || name == "ソーラーブレード")
            {
                if ((SelectedWeatherSettings != 0) && (SelectedWeatherSettings != 1))
                {
                    power *= 2048;
                    power /= 4096;
                }
            }

            // おはかまいりは仲間が倒された回数で威力変動(専用の入力欄が必要 or おはかまいり(0-10くらいまで別々の技として計算しても良いけど))
            // 同、ふんどのこぶしは攻撃を受けた回数で威力変動

            // テラスタイプが有効で、テラスタイプ一致であり、威力が60以下の技は威力を60に上げる

            return (power);
        }

        double CalcCriticalProbability(string name, PokemonDataReal atk, PokemonDataReal def)
        {
            // option条件下で急所に当たる確率を計算する
            // -> 急所に当たりやすい技、急所ランク(作ってない気がする…)、持ち物などを考慮 -> option はPokemonData.opt の方にしないとダメだと思うけど、暫定で
            double result = 1.0;
            int rank = 0;
            var MoveInfo = MoveManager.GetMoveInfo(name);
            rank += MoveInfo.Critical; // 急所に当たりやすい技なら、急所ランクを上げる(確定急所技は+3、それ以外は+1）

            if (atk.ability == "きょううん") // 攻撃側の特性が強運
            {
                ++rank;
            }
            if ((atk.ability == "ひとでなし") // 攻撃側の特性が人でなしで、
            && (def.Options[15] == true)) // 防御側が毒/猛毒状態 -> あとで書き直す
            {
                rank += 3;
            }
            if ((atk.Item == "ピントレンズ") // ピントレンズ/するどいツメを持っている
                || (atk.Item == "するどいツメ"))
            {
                ++rank;
            }

            // rank >= 3 なら確定急所
            if (rank >= 2)
            {
                result /= 2.0;
            }
            else if (rank >= 1)
            {
                result /= 8.0;
            }
            else
            {
                result /= 24.0;
            }

            return (result);
        }
        Tuple<long, long> calcAD( long A, long D, PokemonDataReal atk, PokemonDataReal def, int category ) {
            if (category == 1 )
            {
                // 物理技の時は、攻撃側の「攻撃」と防御側の「防御」を使う
                A *= atk.Attack; D *= def.Block;
                if ((atk.ability == "ちからもち")
                    || (atk.ability == "ヨガパワー")
                //|| ( atk.m_option.m_ability & PokemonAbility::ABILITY_XXXXX ) // 張り込みを入れるべきか否か… でも張り込みは特攻も上がるらしいから別枠か
                )
                {
                    A *= 2; // 力持ち or ヨガパワーなら攻撃を2倍にする
                }
                if (atk.Item == "こだわりハチマキ" )
                {
                    // 持ち物がこだわりハチマキなら攻撃を1.5倍(四捨五入)する
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }
            if (category == 2 )
            {
                // 特殊技の時は、攻撃側の「特攻」と防御側の「特防」を使う
                A *= atk.Constant; D *= def.Deffence;
                if (atk.Item == "こだわりメガネ")
                {
                    // 持ち物がこだわりメガネなら特攻を1.5倍(四捨五入)する
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }
            return (Tuple.Create( A, D ));
        }
        private long damage_base(long a, long d, int p, long dmg, PokemonMove move) {
            dmg = dmg * (p * a) / d; dmg = (dmg / 4096) * 4096;
            dmg /= 50; dmg = (dmg / 4096) * 4096;
            dmg += 8192; dmg = (dmg / 4096) * 4096;
            /* STEP3. 範囲補正 */
            // 切り捨て、四捨五入、五捨五超入は関数化したいね
            if (SelectedBattleStyle == 1 && move.Range)
            {
                // ダブル補正があり、範囲技でもある場合、ダメージを75％にする
                dmg *= (2048 + 1024);
                dmg /= 4096;
                dmg += 2047;
                dmg /= 4096; dmg *= 4096;
            }
            else if (false) // サイコフィールドでワイドフォースを使った場合は m_moveDB[atkmove].m_rangeはfalseだが、ダブルバトルならダブル補正する必要あり
            {
            }

            /* STEP4. 親子愛補正は第九世代には存在しない */

            /* STEP5. 天気補正 */
            if (SelectedWeatherSettings == 1)
            {
                // 晴れの時、炎技は1.5倍、水技は0.5倍
                if (move.Type == "ほのお")
                {
                    dmg *= (4096 + 2048);
                    dmg += 2047;
                    dmg /= 4096;
                }
                else if (move.Type == "みず") // -> ウネルミナモの専用技は晴れでも威力1.5倍だった気がする -> 威力？ダメージ？ここで補正して良い？
                {
                    if ( move.Name == "ハイドロスチーム")
                    {
                        dmg *= (4096 + 2048);
                        dmg += 2047;
                        dmg /= 4096;
                    }
                    else
                    {
                        dmg *= 2048;
                        dmg += 2047;
                        dmg /= 4096;
                    }
                }
            }
            else if ( SelectedWeatherSettings == 2)
            {
                // 雨の時、炎技は0.5倍、水技は1.5倍
                // 晴れの時、炎技は1.5倍、水技は0.5倍
                if (move.Type == "みず")
                {
                    dmg *= (4096 + 2048);
                    dmg += 2047;
                    dmg /= 4096;
                }
                else if (move.Type == "ほのお")
                {
                    dmg *= 2048;
                    dmg += 2047;
                    dmg /= 4096;
                }
            }

            return (dmg);
        }

        public Dictionary<string, List<int>> calc(PokemonDataReal atk, PokemonDataReal def)
        {
            Dictionary<string, List<int>> result = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> result_critical = new Dictionary<string, List<int>>();

            foreach ( var atkmove in atk.MoveList )
            {
                var move = MoveManager.GetMoveInfo(atkmove);
                if (move == null) // 万が一、指定された技がデータベース（？）に存在しなかった場合は無視する
                    continue;

                if ((move.Category & 0x3) == 0)
                {
                    continue; // 変化技、あるいはデータベースに登録されていない技は処理しない
                }
                if (result.ContainsKey( atkmove ) )
                {
                    // この技のダメージは計算済みなのでスキップ
                    continue;
                }

                // テラバーストの設定を変える
                if (move.Name == "テラバースト")
                {
                    if (atk.TeraType.IsNullOrEmpty() == false && atk.Options[14] )
                    {
                        // テラスタイプが入力されている＆テラスタルしている
                        move.Type = atk.TeraType;

                        // ランク補正を含めてA>Cなら物理技に切り替える
                        // -> ランク補正済みステータスを計算するサブ関数を実装すること！！！！！
                    }
                    else
                    {
                        // テラスタルしてない場合はノーマル・特殊技として扱う(ノーマルがタイプ一致の場合もこれで安心)
                        move.Type = "ノーマル";
                        move.Category = 0x2;
                    }
                }

                // ↓↓↓ダメージ計算式にフィールドの補正が入ってないけど、どこで補正されるんだ…？？？

                // ダメージ計算式↓
                //  (((レベル×2/5+2)×威力×A/D)/50+2)×範囲補正×おやこあい補正×天気補正×急所補正×乱数補正×タイプ一致補正×相性補正×やけど補正×M×Mprotect
                // A = 攻撃側の攻撃(物理) or 特攻(特殊)
                // D = 防御側の防御(物理) or 特防(特殊)
                // かっこ内は計算するたびに小数点以下を切り捨て。範囲補正から急所補正までは各計算の後、小数点以下を逐一五捨五超入する。乱数補正計算後は切り捨てる。タイプ一致補正計算後は五捨五超入する。相性補正計算後は切り捨てる。やけど補正計算後は五捨五超入する。M計算後は五捨五超入する。Mprotect計算後は五捨五超入する。

                // M = 壁補正×ブレインフォース補正×スナイパー補正×いろめがね補正×もふもふほのお補正×Mhalf×Mfilter×フレンドガード補正×たつじんのおび補正×メトロノーム補正×いのちのたま補正×半減の実補正×Mtwice
                // Mの各補正値の計算では、小数点以下を逐一四捨五入する。
                // Mhalf = ダメージ半減特性による補正(0.5倍): こおりのりんぷん、パンクロック、ファントムガード、マルチスケイル、もふもふ直接
                // Mfilter = 効果バツグンのダメージを軽減する特性による補正(0.75倍): ハードロック、フィルター、プリズムアーマー
                // MTwice = 穴を掘る中の地震/マグニチュード、ダイビング中の波乗り、小さくなるへのふみつけ等 -> データベースに入れた方が良いかも
                // MProtectは現状の第九世代では存在しない（守るに対するZ技等の1/4補正）
                // 乱数補正 = 85 ～ 100の乱数をかけ、その後100で割る
                // 相性補正は全ての相性倍率を出して、掛け算したあと切り捨てる
                // 壁補正は、防御側の場に1匹なら0.5倍、2匹以上出していれば2732/4096倍
                // 特定タイプの火力を上げるアイテム(神秘のしずく等)は、威力を4915/4096倍する

                // サイコショック等の計算が特殊な技は、A/D/M等を独自計算する必要があるので、あとで実装する
                // 4096は12bit固定小数点演算のため（整数で扱うと、1.0＝4096となる）
                // 急所の有無それぞれについて、乱数による16パターンのダメージを算出する
                long damage = 4096;
                long A = 1, D = 1, M = 1;
                long Mhalf = 1, Mfilter = 1, MTwice = 1;

                /* STEP1. A/Dを決定 */ // --> 要確認！！！　ランク補正ってここのA/Dを直接いじる？ -> もう一個、こだわり系はステータス1.5倍だよね？ここ？？
                ( A, D ) = calcAD(A, D, atk, def, move.Category);

                // STEP1-1. サイコショックとサイコブレイクは攻撃側の特攻、防御側の防御を使う
                // categoryを物理・特殊・変化だけじゃなくて、物理(特殊計算)、特殊(物理計算)みたいなものも入れたら良いかも…
                if ((atkmove == "サイコショック") || (atkmove == "サイコブレイク"))
                {
                    A = atk.Constant; D = def.Block;
                }

                // STEP1-2. フォトンゲイザーとシェルアームズはここで補正？
                // 攻撃・特攻と防御・特防を比較して、一番ダメージが大きくなるようにA/Dを決めるんだっけ？

                // STEP1-3. ワンダールームの場合はDを再計算する処理を入れる（防御と特防を入れ替える）
                if ( WonderRoom == true )
                {
                    long A_dummy = 1;
                    D = 1; // Aは変わらないのでダミーで計算して結果は捨て、Dはここでの結果を採用する
                    (A_dummy, D ) = calcAD(A_dummy, D, atk, def, move.Category ^ 0x3); // 物理/特殊の判定を入れ替えてDだけ再計算する
                }

                // ソーラービームは威力が変わるから、範囲補正より先にそっちかも…？
                // 威力変化系の技は、別途専用関数作って計算した方が良さそう

                /* STEP2. 最初の()内を計算 */
                /* STEP2-1. 威力を決定 */
                int power = move.Power;
                /* 以下、サイコフィールドでワイドフォースとか、ジャイロボールとか、そういうやつも計算する */
                // ↓これも関数に入れた方が良い？？
                if (atk.Item == "タイプ強化" )
                {
                    // タイプ強化アイテムなら威力4915/4096倍
                    power *= 4915; power /= 4096;
                }
                if ((move.Category == 1 )
                 && (atk.Item == "ちからのハチマキ" ))
                {
                    // 物理技でちからのハチマキを持っている時は威力4505/4096倍
                    power *= 4505; power /= 4096;
                }
                if ((move.Category == 2 )
                 && (atk.Item == "ものしりメガネ"))
                {
                    // 特殊技でものしりメガネを持っている時は威力4505/4096倍
                    power *= 4505; power /= 4096;
                }

                /* STEP2-2. A/Dにランク補正を入れるのはここ？ */
                int rank1 = 2, rank2 = 2;
                long A_critical = A, D_critical = D;
                if (atk.Rank[(move.Category == 1 ? 0 : 2)] > 0) // 物理/特殊のカテゴリだけで見れない技をどうするか…(テラバーストとか)
                {
                    rank1 += atk.Rank[(move.Category == 1 ? 0 : 2)];
                }
                else if (atk.Rank[(move.Category == 1 ? 0 : 2)] < 0)
                {
                    rank2 += atk.Rank[(move.Category == 1 ? 0 : 2)];
                }
                A = A * rank1; A = A / rank2;
                if ((double)rank1 / rank2 > 1.0)
                {
                    A_critical = A; // 急所に当たる場合、有利な効果(攻撃側の攻撃ランク上昇)だけ残す
                }

                if (def.Rank[(move.Category == 1 ? 1 : 3)] > 0)
                {
                    rank1 += def.Rank[(move.Category == 1 ? 1 : 3)];
                }
                else if (def.Rank[(move.Category == 1 ? 1 : 3)] < 0)
                {
                    rank2 += def.Rank[(move.Category == 1 ? 1 : 3)];
                }
                D = D * rank1; D = D / rank2;
                if ((double)rank1 / rank2 < 1.0)
                {
                    D_critical = D; // 急所に当たる場合、有利(防御側の防御ランク低下)な効果だけ残す
                }

                /* STEP2-3. ランク補正とは別のステータス上昇(総大将、クォークチャージ、古代活性、ハドロンエンジン、ヒヒイロの鼓動) */
                // -> これはPokemonDataの数値でもらう仕様にしたんだっけ？

                /* STEP2-LAST. 計算した威力を使って残りを計算 */

                // -> 急所に当たる場合は不利な効果を無視するので、別々に計算する
                long damage_critical;
                damage *= ((atk.Level * 2) / 5); damage = (damage / 4096) * 4096; damage += 8192;
                damage_critical = damage;
                damage = damage_base(A, D, power, damage, move);
                damage_critical = damage_base(A_critical, D_critical, power, damage_critical, move);

                /* STEP6. 急所補正 */
                damage_critical = damage_critical * (2048 + 4096) / 4096;
                damage_critical += 2047; damage_critical /= 4096; damage_critical *= 4096;

                /* STEP7. 乱数補正 */
                result[move.Name] = new List<int>();
                result_critical[move.Name] = new List<int>();
                for (int i = 0; i < 16; ++i)
                {
                    long tmp = damage * (85 + i); tmp /= 100; tmp /= 4096; tmp *= 4096;
                    long tmp_critical = damage_critical * (85 + i); tmp_critical /= 100; tmp_critical /= 4096; tmp_critical *= 4096;
                    result[move.Name].Add( (int)tmp );
                    result_critical[move.Name].Add( (int)tmp_critical );
                }

                /* STEP8. タイプ一致補正 */
                // -> テラスタイプ一致の計算もここでやる？
                // テラバーストはテラスタルしている場合は必ずタイプ一致(未実装)、テラスタルしていなければノーマルでタイプ一致、ノーマルタイプがノーマルにテラスタルした時は…？
                // そういえば、フライングプレスは「格闘」でタイプ一致判定するんだっけ？(ルチャブル専用技だった気がするから、いずれにせよタイプ一致だが、飛行にテラスタルすると事情が変わる…）
                List<string> TypeCheck = new List<string>();
                foreach( var t in atk.type )
                {
                    TypeCheck.Add(t);
                }
                TypeCheck.Add(atk.TeraType);
                long type_match_attack = 0;
                foreach ( var t in TypeCheck )
                {
                    if ( t == move.Type )
                    {
                        type_match_attack += 2048;
                    }
                }

                for (int i = 0; i < 16; ++i)
                {
                    result[move.Name][i] *= (4096 + (int)type_match_attack); // タイプ不一致なら1.0倍、タイプ一致なら1.5倍、テラスタイプ一致なら2.0倍になる
                    result[move.Name][i] /= 4096;
                    result[move.Name][i] += 2047;
                    result[move.Name][i] /= 4096; result[move.Name][i] *= 4096;

                    result_critical[move.Name][i] *= (4096 + (int)type_match_attack);
                    result_critical[move.Name][i] /= 4096;
                    result_critical[move.Name][i] += 2047;
                    result_critical[move.Name][i] /= 4096; result_critical[move.Name][i] *= 4096;
                }

                /* STEP9. 相性補正 */
                double typecomp_res = 1.0;
                if ( def.TeraType.IsNullOrEmpty() == false )
                {
                    // 防御側にテラスタイプが設定されている場合は、相性はテラスタイプを使って計算する
                    typecomp_res *= TypeCompatible.CompatibilityCheck( move.Type, def.TeraType );
                }
                else
                {
                    // テラスタイプ未設定の場合は、本来持つタイプで計算する
                    foreach ( var type in def.type )
                    {
                        typecomp_res *= TypeCompatible.CompatibilityCheck( move.Type, type );
                    }
                }

                for (int i = 0; i < 16; ++i) // STEP毎にループ書くの微妙なんだけどね…
                {
                    result[move.Name][i] = (int)( result[move.Name][i] * typecomp_res );
                    result[move.Name][i] /= 4096;
                    result[move.Name][i] *= 4096;

                    result_critical[move.Name][i] = (int)(result_critical[move.Name][i] * typecomp_res);
                    result_critical[move.Name][i] /= 4096;
                    result_critical[move.Name][i] *= 4096;
                }

                /* STEP10. 火傷補正 */
                for (int i = 0; i < 16; ++i)
                {
                    if ( atk.Options[16] ) // -> 後で直す！！！！！！！！
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2047;
                        result[move.Name][i] /= 4096;
                    }
                }

                /* STEP11. Mを計算する */ // -> Mを計算する別関数を作った方が良い気がする。以下Mhalf等も同様。
                                 // 今更だけど、型破りを考慮してない…
                                 // -> 型破りフラグがONなら、残りの特性系ビットを全部OFFにすれば良いような気もする
                /* STEP11-1. 壁補正 */
                if ( (move.Category == 1 && def.Options[0])
                    || ( move.Category == 2 && def.Options[1]) )
                {
                    // 分類と壁の有無が一致
                    // →テラバーストとかフォトンゲイザーが困るか…
                    //   bitには空きがあるし、専用bit入れるか。テラバースト物理、テラバースト特殊(壁)、みたいな
                    for (int i = 0; i < 16; ++i) // 急所に当たったら壁は無視されるので、前半16パターンだけ補正する
                    {
                        if ( SelectedBattleStyle == 1 )
                        {
                            // ダブル補正がある時は2732/4096倍
                            // これって相手が2体いれば単体攻撃でも補正変わるんだっけ？
                            // 相手が1体の時はシングルと同じ扱いだっけ？
                            result[move.Name][i] *= 2732;
                        }
                        else
                        {
                            // シングルなら0.5倍
                            result[move.Name][i] *= 2048;
                        }
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-2. ブレインフォース補正は第九世代には存在しない */

               /* STEP11-3. スナイパー補正 */
                if (atk.ability == "スナイパー" )
                {
                    for (int i = 0; i < 16; ++i) // 急所に当たった時、更に威力が1.5倍
                    {
                        result_critical[move.Name][i] *= (2048 + 4096);
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-4. いろめがね補正 */
                if ((atk.ability == "いろめがね" )
                    && (typecomp_res < 1.0))
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        // 効果が今ひとつ以下の場合はダメージ2倍
                        result[move.Name][i] *= 8192;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 8192;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-5. もふもふ(炎技被弾)補正 */
                if ((def.ability == "もふもふ" )
                    && (move.Type == "ほのお"))
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        // 特性もふもふで炎技を被弾した場合はダメージ2倍(ただし弱点ではない)
                        // これは防御側の特性を参照するので実装に注意！！！！！
                        result[move.Name][i] *= 8192;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 8192;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-6. Mhalf補正 */
                /* STEP11-6-1. 氷の鱗粉補正 */
                if ((def.ability == "こおりのりんぷん")
                    && ( move.Category == 2 ))
                {
                    // 氷の鱗粉で特殊技を受ける時はダメージ半減
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-6-2. ファントムガード、マルチスケイル補正 */
                if (def.ability == "ファントムガード")
                {
                    // ファントムガード、マルチスケイルが発動する時はダメージ半減
                    // -> ツールとしてはチェックボックスのON/OFFで切り替えるのでHP判定はしない
                    // ファントムガードは、シャドーレイやメテオドライブの特性貫通効果や型破りを無視して半減するので、
                    // 別々に計算した方が良いかも
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }
                if (def.ability == "マルチスケイル" )
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-6-3. パンクロック補正 */
                if ( (def.ability == "パンクロック" )
                    && ( move.Sound ) ) // 音の技の時
                {
                    // 音の技を受ける時はダメージ半減
                    // 逆に音技を使う時は[威力]上昇？ -> 威力計算の時にやる？
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-6-4. もふもふ(接触技)補正 */
                if ((def.ability == "もふもふ" )
                    && (move.Direct))
                {
                    // 接触技を受ける時はダメージ半減
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-7. Mfilter補正 */
                /* STEP11-7-1. ハードロック/フィルター補正 */
                if ( ( ( def.ability == "ハードロック" ) || ( def.ability == "フィルター" ) )
                    && (typecomp_res > 1.0))
                {
                    // ハードロック/フィルターが発動する時はダメージ0.75倍
                    // プリズムアーマーは、シャドーレイやメテオドライブの特性貫通を無視して軽減するので、別々に計算
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= (2048 + 1024);
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= (2048 + 1024);
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }
                /* STEP11-7-2. プリズムアーマー補正 */
                if ((def.ability == "プリズムアーマー" )
                    && (typecomp_res > 1.0))
                {
                    // プリズムアーマーが発動する時はダメージ0.75倍
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= (2048 + 1024);
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= (2048 + 1024);
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-8. フレンドガード補正 */
                if (def.Options[11])
                {
                    // フレンドガードが発動する時はダメージ0.75倍
                    // -> ツールとしてはチェックボックスのON/OFFで切り替える
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= (2048 + 1024);
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= (2048 + 1024);
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-9. 達人の帯補正 */
                if ((atk.Item == "たつじんのおび" )
                    && (typecomp_res > 1.0))
                {
                    // 達人の帯が発動する時はダメージ1.2倍 -> 正確にはいくつ？4915？4916？
                }

                /* STEP11-10. メトロノーム補正 */
                if (atk.Item.Contains("メトロノーム") )
                {
                    // メトロノームで同じ技をN回使ったらダメージ上昇
                    int[] gain = { 4096, 4096, 4096, 4096, 4096, 8192 };
                    for (int i = 0; i < 6; ++i)
                    {
                        string s = (i + 1).ToString();
                        if (atk.Item.Contains( s ))
                        {
                            for (int j = 0; j < 16; ++j )
                            {
                                result[move.Name][j] *= gain[i];
                                result[move.Name][j] += 2048;
                                result[move.Name][j] /= 4096;

                                result_critical[move.Name][j] *= gain[i];
                                result_critical[move.Name][j] += 2048;
                                result_critical[move.Name][j] /= 4096;
                            }
                            break;
                        }
                    }
                }

                /* STEP11-11. 命の珠補正 */
                if (atk.Item == "いのちのたま" )
                {
                    // 命の珠ならダメージ1.3倍 -> 正確には5324/4096倍？
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 5324;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 5324;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-12. 半減実補正 */
                if ((def.Item == "半減木の実")
                    && (typecomp_res > 1.0))
                {
                    // 弱点半減の実ならダメージ0.5倍 -> これは正確に2048/4096？
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-13. Mtwice補正 */
                if (move.Minimize && def.Options[19])
                {
                    // 特定条件下でダメージ2倍の技を使用した
                    for (int i = 0; i < 32; ++i)
                    {
                        result[move.Name][i] *= 8192;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 8192;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP12. Mprotect補正は第九世代には存在しない */
                // -> 守る状態に対するZ技、ダイマックス技など

                /* STEP13. 計算結果を4096で割る */
                // ここまでlong longで計算したのでint型に変換(別に全部long longにしても良いと思うんだけど…)
                for (int i = 0; i < 16; ++i)
                {
                    result[move.Name][i] /= 4096;
                    result_critical[move.Name][i] /= 4096;
                }

                /* STEP14. 期待値を計算する */
                double tmp_exp = 0.0;
                for (int i = 0; i < 16; ++i)
                {
                    // 基本ダメージは、計算結果 × 急所に"当たらない"確率 × 技の命中率
                    tmp_exp += (result[move.Name][i] / 16.0) * (1.0 - CalcCriticalProbability(atkmove, atk, def)) * (move.Accuracy / 100.0);
                }
                for (int i = 0; i < 16; ++i)
                {
                    // 急所に当たった場合のダメージは、計算結果 × 急所に"当たる"確率 × 技の命中率
                    tmp_exp += (result_critical[move.Name][i] / 16.0) * CalcCriticalProbability(atkmove, atk, def) * (move.Accuracy / 100.0);
                }
                result[move.Name].AddRange(result_critical[move.Name] );
                result[move.Name].Add( (int)tmp_exp );

                /* LAST STEP. 計算結果を結果配列に突っ込む */
                /* LAST STEP2. 後始末 */
                // 特殊な計算をしたフラグをクリアする
            }

            return (result);
        }

    }
}
