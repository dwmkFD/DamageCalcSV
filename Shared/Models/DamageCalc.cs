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
 *   0: リフレクター      1: ひかりのかべ      2: てだすけ
 *   3: きあいだめ        4: じゅうでん        5: そうでん
 *   6: はがねのせいしん  7: フラワーギフト    8: ハロウィン
 *   9: もりののろい     10: みずびたし       11: フレンドガード
 *  12: ダメ半減特性     13: しんかのきせき   14: テラスタルON
 *  15: 毒・猛毒         16: 火傷             17: 麻痺
 *  18: 眠り             19: 小さくなる       20: かたやぶり
 *  21: わざわいのうつわ 22: わざわいのおふだ 23: わざわいのたま
 *  24: わざわいのつるぎ
 */

namespace DamageCalcSV.Shared.Models
{
    public class DamageCalc
    {
        public static bool Gravity, WonderRoom, PlasmaShower, FairyAura, DarkAura, AuraBreak;
        public static int SelectedBattleStyle, SelectedWeatherSettings, SelectedFieldSettings;

        public PokemonMoveManager? MoveManager = null;
        private PokemonType? TypeCompatible = null;
        public DamageCalc()
        {
            MoveManager = new PokemonMoveManager();
            TypeCompatible = new PokemonType();
        }
        private long CorrectPower(ref PokemonMove move, PokemonDataReal atk, PokemonDataReal def)
        {
            // 技の威力が変わる場合に補正する処理
            long power = move.Power;

            // サイコフィールドでワイドフォースを撃つ場合は威力1.5倍かつ全体技
            if (move.Name == "ワイドフォース")
            {
                if (SelectedFieldSettings == 3) // フィールドとか天気の数値はdefine的な感じで書きたいがC#での書き方がわからん。。
                {
                    power *= 6144;
                    power += 2048;
                    power /= 4096;
                    move.Range = true; // サイコフィールドなら範囲攻撃
                }
                else
                {
                    move.Range = false; // サイコフィールドでないなら単体攻撃
                }
            }

            // からげんきを状態異常で撃つと威力2倍かつ火傷無効
            if (move.Name == "からげんき")
            {
                if (atk.Options[15] || atk.Options[16] || atk.Options[17] || atk.Options[18])
                {
                    power *= 2;
                }
            }

            // エレキボール、ジャイロボールは素早さを比較して威力決定
            if (move.Name == "エレキボール")
            {
                int atkS = atk.Speed;
                int defS = def.Speed;

                if (atkS <= defS)
                    power = 40;
                else if (atkS <= defS * 2)
                    power = 60;
                else if (atkS <= defS * 3)
                    power = 80;
                else if (atkS <= defS * 4)
                    power = 120;
                else
                    power = 150;
            }
            else if (move.Name == "ジャイロボール")
            {
                int atkS = atk.Speed;
                int defS = def.Speed;
                power = (25 * defS / atkS) + 1;
                power = Math.Min(power, 150);
            }

            // 体重依存技のために体重を計算しておく
            double aw = atk.Weight;
            double dw = def.Weight;
            if (atk.ability == "ライトメタル")
                aw /= 2;
            if (atk.ability == "ヘビーメタル")
                aw *= 2;
            if (def.ability == "ライトメタル")
                dw /= 2;
            if (def.ability == "ヘビーメタル")
                dw *= 2;

            // ヒートスタンプ、ヘビーボンバーは体重差によって威力決定
            if (move.Name == "ヒートスタンプ" || move.Name == "ヘビーボンバー")
            {
                if (dw * 5 <= aw)
                    power = 120;
                else if (dw * 4 <= aw)
                    power = 100;
                else if (dw * 3 <= aw)
                    power = 80;
                else if (dw * 2 <= aw)
                    power = 60;
                else
                    power = 40;
            }
            // けたぐり、くさむすびは相手の体重によって威力決定
            if (move.Name == "けたぐり" || move.Name == "くさむすび" )
            {
                if (dw <= 9.9)
                    power = 20;
                else if (dw <= 24.9)
                    power = 40;
                else if (dw <= 49.9)
                    power = 60;
                else if (dw <= 99.9)
                    power = 80;
                else if (dw <= 199.9)
                    power = 100;
                else
                    power = 120;
            }

            // 相手がちいさくなっていた場合、対象の技は威力2倍
            if (def.Options[19] && move.Minimize)
            {
                power *= 2;
            }

            // サイコブレイドはエレキフィールドで威力1.5倍
            if (SelectedFieldSettings == 1)
            {
                if (move.Name == "サイコブレイド")
                {
                    power *= (2048 + 4096);
                    power += 2048;
                    power /= 4096;
                }
            }

            // おはかまいりは仲間が倒された回数で威力変動
            // 同、ふんどのこぶしは攻撃を受けた回数で威力変動
            if (move.Name == "ふんどのこぶし" || move.Name == "おはかまいり")
            {
                power = 50 + atk.Special * 50;
            }

            // 無天候と晴れ以外の天候でのソーラービーム/ブレードは威力半減
            if (move.Name == "ソーラービーム" || move.Name == "ソーラーブレード")
            {
                if ((SelectedWeatherSettings != 0) && (SelectedWeatherSettings != 1))
                {
                    power *= 2048;
                    power /= 4096;
                }
            }

            // テラスタイプが有効で、テラスタイプ一致であり、威力が60以下の技は威力を60に上げる
            // -> 先制技等は除外なので、本当はこの条件じゃダメ！！！
            if ( atk.type.Contains( atk.TeraType ) && atk.Options[14] && power <= 60 )
            {
                power = 60;
            }

            // 特性「テクニシャン」で威力60以下の技は威力1.5倍
            if (atk.ability == "テクニシャン")
            {
                if (power <= 60)
                {
                    power *= (2048 + 4096);
                    power += 2048;
                    power /= 4096;
                }
            }

            // ミストフィールドでドラゴン技を使うと威力半減（ダメージ半減？どっち？）
            if (SelectedFieldSettings == 4)
            {
                if (move.Type == "ドラゴン")
                {
                    power *= 2048;
                    power /= 4096;
                }
            }

            // 特性「メガランチャー」で波動技は威力1.5倍
            if ( atk.ability == "メガランチャー" && move.Pulse )
            {
                power *= (2048 + 4096);
                power += 2048;
                power /= 4096;
            }

            // 特性「がんじょうあご」で噛みつく技は威力1.5倍
            if ( atk.ability == "がんじょうあご" && move.Bite )
            {
                power *= (2048 + 4096);
                power += 2048;
                power /= 4096;
            }

            // スキン系特性による威力強化(テラスタイプは考慮しない)
            if ( atk.ability.Contains( "スキン" ) )
            {
                if ( atk.ability != "ミラクルスキン" && atk.ability != "ノーマルスキン" )
                {
                    // ノーマルタイプの技を使った場合に威力1.2倍
                    if ( move.Type == "ノーマル" )
                    {
                        power *= 4915;
                        power /= 4096;
                    }
                }
            }

            // はがねのせいしん
            if (atk.ability == "はがねのせいしん" && move.Type == "はがね")
            {
                // はがねタイプの技威力1.5倍（重複する）-> 四捨五入のタイミングは逐次？最後？
                for (int i = 0; i < atk.Special; ++i)
                {
                    power *= (4096 + 2048);
                    power += 2048;
                    power /= 4096;
                }
            }

            // オーラ系(フェアリー、ダーク、ブレイク)
            if ( FairyAura && move.Type == "フェアリー" )
            {
                if (AuraBreak)
                {
                    // ブレイクも有効なら威力を逆にする
                    power *= 3072;
                    power += 2048;
                    power /= 4096;
                }
                else
                {
                    power *= 5448;
                    power += 2048;
                    power /= 4096;
                }
            }
            if ( DarkAura && move.Type == "あく")
            {
                if (AuraBreak)
                {
                    power *= 3072;
                    power += 2048;
                    power /= 4096;
                }
                else
                {
                    power *= 5448;
                    power += 2048;
                    power /= 4096;
                }
            }

            // ちからずく
            if ( atk.ability == "ちからずく" &&
                move.StrExp.Contains( "通常攻撃" ) == false ) // 判定これじゃダメなので見直し(追加効果フラグが必要？)
            {
                // 追加効果がある場合は威力1.3倍
                power *= 5325;
                power += 2048;
                power /= 4096;
            }

            // パンクロックで音技を使うと威力1.3倍
            if ( atk.ability == "パンクロック" && move.Sound )
            {
                power *= 5325;
                power += 2048;
                power /= 4096;
            }

            // てつのこぶしでパンチ技を使うと威力1.2倍
            if ( move.Punch )
            {
                if (atk.ability == "てつのこぶし")
                {
                    // てつのこぶしでパンチ技を使った場合は4915/4096倍
                    power *= 4915;
                    power += 2048;
                    power /= 4096;
                }
                if ( atk.Item == "パンチグローブ" )
                {
                    // パンチグローブでパンチ技を使う時は4506/4096倍
                    power *= 4516;
                    power += 2048;
                    power /= 4096;
                }
            }

            // そうだいしょうは仲間が倒された数×10％（最大50％）威力上昇
            if (atk.ability == "そうだいしょう")
            {
                power *= (4096 + ( 4096 * (long)Math.Min( atk.Special, 5 ) / 10) );
                power += 2048;
                power /= 4096;
            }

            // たいねつ相手の炎技は威力半減
            if ( def.ability == "たいねつ" && move.Type == "ほのお" )
            {
                power *= 2048;
                power += 2048;
                power /= 4096;
            }

            // かんそうはだ相手の炎技は威力1.25倍
            if (def.ability == "かんそうはだ" && move.Type == "ほのお")
            {
                power *= 5120;
                power += 2048;
                power /= 4096;
            }

            // アイテムによる威力強化
            if (atk.Item == "タイプ強化")
            {
                // タイプ強化アイテムなら威力4915/4096倍
                power *= 4915;
                power += 2048;
                power /= 4096;
            }
            if ((move.Category == 1)
             && (atk.Item == "ちからのハチマキ"))
            {
                // 物理技でちからのハチマキを持っている時は威力4505/4096倍
                power *= 4505;
                power += 2048;
                power /= 4096;
            }
            if ((move.Category == 2)
             && (atk.Item == "ものしりメガネ"))
            {
                // 特殊技でものしりメガネを持っている時は威力4505/4096倍
                power *= 4505;
                power += 2048;
                power /= 4096;
            }

            // てだすけされた時は威力1.5倍
            if (atk.Options[2])
            {
                power *= 6144;
                power += 2048;
                power /= 4096;
            }

            if ( SelectedFieldSettings == 1 )
            {
                if ( move.Type == "でんき" )
                {
                    power *= 5325;
                    power += 2048;
                    power /= 4096;
                }
            }

            if ( SelectedFieldSettings == 2)
            {
                if (move.Type == "くさ")
                {
                    power *= 5325;
                    power += 2048;
                    power /= 4096;
                }
                if ( move.Name == "じしん" || move.Name == "じならし" )
                {
                    // グラスフィールド下ではいくつかの地面技は威力半減
                    power *= 2048;
                    power += 2048;
                    power /= 4096;
                }
            }

            if ( SelectedFieldSettings == 3)
            {
                if (move.Type == "エスパー")
                {
                    power *= 5325;
                    power += 2048;
                    power /= 4096;
                }
            }

            // 特性「きれあじ」で切る技なら威力1.5倍
            if (atk.ability == "きれあじ" && move.Slice)
            {
                power *= 6144;
                power += 2048;
                power /= 4096;
            }

            // オーガポンで、みどりのめん以外の場合は全ての技の威力が1.2倍
            // マジックルーム中はこの効果も失われるらしいが、現状マジックルームはまず対戦で使われないので放置しておく
            if ( atk.Name.Contains( "オーガポン" ) && atk.Name.Contains( "みどり" ) == false )
            {
                power *= 4915;
                power += 2048;
                power /= 4096;
            }

            return (power);
        }

        PokemonDataReal CorrectRank(in PokemonDataReal p )
        {
            int[] status = { p.Attack, p.Block, p.Contact, p.Defense, p.Speed };
            for ( int i = 0; i < p.Rank.Length; ++i )
            {
                int rank1 = 2, rank2 = 2;
                if (p.Rank[i] > 0)
                    rank1 += p.Rank[i];
                else
                    rank2 -= p.Rank[i];

                status[i] = status[i] * rank1;
                status[i] = status[i] / rank2;
            }

            p.Attack = status[0]; p.Block = status[1];
            p.Contact = status[2]; p.Defense = status[3];
            p.Speed = status[4];

            if ( p.ability == "はりきり" )
            {
                // 特性がはりきりなら攻撃1.5倍(切り捨て)
                p.Attack *= 6144;
                p.Attack /= 4096;
            }

            return ( p );
        }

        PokemonDataReal CorrectRankCritical( in PokemonDataReal p )
        {
            // 急所に当たる場合のランク補正
            int[] status = { p.Attack, p.Block, p.Contact, p.Defense };
            for (int i = 0; i < p.Rank.Length - 1; ++i)
            {
                int rank1 = 2, rank2 = 2;
                if (p.Rank[i] > 0)
                    rank1 += p.Rank[i];
                else
                    rank2 -= p.Rank[i];

                if ( i % 2 == 0 ) // 攻撃系ステータス
                {
                    if ( (double)rank1 / (double)rank2 > 1.0 )
                    {
                        // 有利なステータスなら採用する
                        status[i] = status[i] * rank1;
                        status[i] = status[i] / rank2;
                    }
                }
                else
                {
                    //防御系ステータス
                    if ( (double)rank1 / (double)rank2 <= 1.0 )
                    {
                        status[i] = status[i] * rank1;
                        status[i] = status[i] / rank2;
                    }
                }
            }

            p.Attack = status[0]; p.Block = status[1];
            p.Contact = status[2]; p.Defense = status[3];

            if (p.ability == "はりきり")
            {
                // 特性がはりきりなら攻撃1.5倍(切り捨て)
                p.Attack *= 6144;
                p.Attack /= 4096;
            }

            return ( p );
        }

        int CorrectParadoxRankPos( in PokemonDataReal p )
        {
            List<Tuple<int, int>> tmp = new List<Tuple<int, int>> {
                Tuple.Create( 1, p.Attack ),
                Tuple.Create( 2, p.Block ),
                Tuple.Create( 3, p.Contact ),
                Tuple.Create( 4, p.Defense ),
                Tuple.Create( 5, p.Speed ),
            };

            tmp.Sort( ( x, y ) => y.Item2 - x.Item2 );
            return ( tmp[0].Item1 );
        }
        Tuple<PokemonDataReal, PokemonDataReal> CorrectParadoxRank( in PokemonDataReal p, in PokemonDataReal p_cri)
        {
            if ((p.ability == "こだいかっせい" && (SelectedWeatherSettings == 1 || p.Item == "ブーストエナジー"))
                || (p.ability == "クォークチャージ" && (SelectedFieldSettings == 1 || p.Item == "ブーストエナジー")))
            {
                int pos = CorrectParadoxRankPos(p); // ランク補正済みのステータスに対して補正する
                switch (pos)
                {
                    case 1:
                        p.Attack *= 5325; p.Attack += 2048; p.Attack /= 4096;
                        p_cri.Attack *= 5325; p_cri.Attack += 2048; p_cri.Attack /= 4096; break;
                    case 2:
                        p.Block *= 5325; p.Block += 2048; p.Block /= 4096;
                        p_cri.Block *= 5325; p_cri.Block += 2048; p_cri.Block /= 4096; break;
                    case 3:
                        p.Contact *= 5325; p.Contact += 2048; p.Contact /= 4096;
                        p_cri.Contact *= 5325; p_cri.Contact += 2048; p_cri.Contact /= 4096; break;
                    case 4:
                        p.Defense *= 5325; p.Defense += 2048; p.Defense /= 4096;
                        p_cri.Defense *= 5325; p_cri.Defense += 2048; p_cri.Defense /= 4096; break;
                    case 5:
                        p.Speed *= 6144; p.Speed += 2048; p.Speed /= 4096;
                        p_cri.Speed *= 6144; p_cri.Speed += 2048; p_cri.Speed /= 4096; break;
                    default: break;
                }
            }

            return (Tuple.Create(p, p_cri));
        }

        double CalcCriticalProbability(PokemonMove move, PokemonDataReal atk, PokemonDataReal def)
        {
            // option条件下で急所に当たる確率を計算する
            // -> 急所に当たりやすい技、急所ランク(作ってない気がする…)、持ち物などを考慮 -> option はPokemonData.opt の方にしないとダメだと思うけど、暫定で
            double result = 1.0;
            int rank = 0;
            rank += move.Critical; // 急所に当たりやすい技なら、急所ランクを上げる(確定急所技は+3、それ以外は+1）

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
        Tuple<long, long> calcAD( long A, long D, PokemonDataReal atk, PokemonDataReal def, PokemonMove move, int category ) {
            if (category == 1 )
            {
                // 物理技の時は、攻撃側の「攻撃」と防御側の「防御」を使う
                D *= def.Block;

                if (move.Name == "ボディプレス")
                {
                    // ボディプレスは防御をAとして計算する
                    A *= atk.Block;
                }
                else if (move.Name == "イカサマ")
                {
                    // イカサマの時は相手の攻撃をAとして計算する
                    A *= def.Attack;
                }
                else
                {
                    A *= atk.Attack;
                }

                if ((atk.ability == "ちからもち")
                    || (atk.ability == "ヨガパワー")
                //|| ( atk.m_option.m_ability & PokemonAbility::ABILITY_XXXXX ) // 張り込みを入れるべきか否か… でも張り込みは特攻も上がるらしいから別枠か
                )
                {
                    A *= 2; // 力持ち or ヨガパワーなら攻撃を2倍にする
                }

                if ( ( atk.ability == "ひひいろのこどう") && SelectedWeatherSettings == 1 )
                {
                    // 天候が晴れなら攻撃を1.33倍する
                    A *= 5461;
                    A += 2048;
                    A /= 4096;
                }

                if (atk.Options[7] && SelectedWeatherSettings == 1 )
                {
                    // 晴れならフラワーギフトで攻撃1.5倍
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }

                if ( (atk.Options[22] || def.ability == "わざわいのおふだ" ))
                {
                    // チェックボックスがON(隣にいる味方を想定)または相手の特性がわざわい
                    A *= 3072;
                    A += 2048;
                    A /= 4096;
                }

                if (def.Options[24] || atk.ability == "わざわいのつるぎ")
                {
                    // 攻撃系のわざわい特性は自分の特性を考慮する必要がある
                    D *= 3072;
                    D += 2048;
                    D /= 4096;
                }

                if ( atk.ability == "こんじょう" )
                {
                    if (atk.Options[15] || atk.Options[16] || atk.Options[17] || atk.Options[18] )
                    {
                        // こんじょうかつ状態異常なら攻撃1.5倍かつ火傷無効 -> 火傷の処理は本体でやる
                        A *= 6144;
                        A += 2048;
                        A /= 4096;
                    }
                }

                if (atk.ability == "ごりむちゅう" )
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }

                if ( ( atk.Item == "ふといホネ" )
                    && ( atk.Name == "カラカラ" || atk.Name.Contains( "ガラガラ" ) ) )
                {
                    // カラカラ・ガラガラなら攻撃2倍
                    A *= 2;
                }

                if (atk.Item == "こだわりハチマキ")
                {
                    // 持ち物がこだわりハチマキなら攻撃を1.5倍(四捨五入)する
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }

                if (def.ability == "ふしぎなうろこ")
                {
                    if (def.Options[15] || def.Options[16] || def.Options[17] || def.Options[18])
                    {
                        // ふしぎなうろこかつ状態異常なら防御1.5倍
                        D *= 6144;
                        D += 2048;
                        D /= 4096;
                    }
                }

                if ( def.ability == "くさのけがわ" && SelectedFieldSettings == 2 )
                {
                    // くさのけがわでグラスフィールドなら防御1.5倍
                    D *= 6144;
                    D += 2048;
                    D /= 4096;
                }

                if ( def.ability == "ファーコート" )
                {
                    // ファーコートで物理技を受ける時は防御2倍
                    D *= 2;
                }

                // 防御側が「こおり」タイプを持っていて雪の場合は防御を1.5倍する
                if ( ( ( def.type.Contains( "こおり" ) && ( def.Options[14] == false ) ) || ( def.TeraType == "こおり" && def.Options[14]) ) && SelectedWeatherSettings == 4 )
                {
                    D *= 6144;
                    D += 2048;
                    D /= 4096;
                }
            }
            if (category == 2 )
            {
                // 特殊技の時は、攻撃側の「特攻」と防御側の「特防」を使う
                A *= atk.Contact;

                // categoryを物理・特殊・変化だけじゃなくて、物理(特殊計算)、特殊(物理計算)みたいなものも入れたら良いかも…
                if ((move.Name == "サイコショック") || (move.Name == "サイコブレイク"))
                {
                    // 相手の防御を使って計算する技の時は特殊処理
                    D *= def.Block;

                    // 防御側が「こおり」タイプを持っていて雪の場合は防御を1.5倍する
                    if ( ( ( def.type.Contains("こおり") && ( def.Options[14] == false ) ) || (def.TeraType == "こおり" && def.Options[14]) ) && SelectedWeatherSettings == 4)
                    {
                        D *= 6144;
                        D += 2048;
                        D /= 4096;
                    }
                }
                else
                {
                    D *= def.Defense;
                }

                if ((atk.ability == "ハドロンエンジン") && SelectedFieldSettings == 1 )
                {
                    // エレキフィールドなら特攻を1.33倍する
                    A *= 5461;
                    A += 2048;
                    A /= 4096;
                }

                if ((atk.Options[21] || def.ability == "わざわいのうつわ"))
                {
                    // チェックボックスがON(隣にいる味方を想定)または相手の特性がわざわい
                    A *= 3072;
                    A += 2048;
                    A /= 4096;
                }

                if (def.Options[23] || atk.ability == "わざわいのたま")
                {
                    // 攻撃系のわざわい特性は自分の特性を考慮する必要がある
                    D *= 3072;
                    D += 2048;
                    D /= 4096;
                }

                if (def.Options[7] && SelectedWeatherSettings == 1)
                {
                    // 晴れならフラワーギフトで特防1.5倍
                    D *= 6144;
                    D += 2048;
                    D /= 4096;
                }

                if ( atk.ability == "サンパワー" && SelectedWeatherSettings == 1 )
                {
                    // 晴れなら特攻1.5倍
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }

                if (atk.Item == "こだわりメガネ")
                {
                    // 持ち物がこだわりメガネなら特攻を1.5倍(四捨五入)する
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }

                // 防御側が「いわ」タイプを持っていて砂嵐の場合は特防を1.5倍する
                if ( ( ( def.type.Contains("いわ") && ( def.Options[14] == false ) ) || (def.TeraType == "いわ" && def.Options[14]) ) && SelectedWeatherSettings == 3)
                {
                    D *= 6144;
                    D += 2048;
                    D /= 4096;
                }

                if ( def.Item == "とつげきチョッキ" )
                {
                    D *= 6144;
                    D += 2048;
                    D /= 4096;
                }
            }

            if ((atk.Item == "でんきだま")
                && (atk.Name == "ピカチュウ"))
            {
                // ピカチュウなら攻撃・特攻2倍
                A *= 2;
            }

            if ( atk.ability == "すいほう" && move.Type == "みず" )
            {
                A *= 2;
            }

            if ( def.ability == "あついしぼう" && ( move.Type == "ほのお" || move.Type == "こおり") )
            {
                A *= 2048;
                A += 2048;
                A /= 4096;
            }

            if (def.ability == "きよめのしお" && move.Type == "ゴースト" )
            {
                A *= 2048;
                A += 2048;
                A /= 4096;
            }

            // しんかのきせき持ちの進化前ポケモンは防御・特防1.5倍
            if ( def.Item == "しんかのきせき" )
            {
                D *= 6144;
                D += 2048;
                D /= 4096;
            }

            // 特性有効なら、それぞれ対応するタイプの技は威力1.5倍(実際には攻撃系ステータスを1.5倍っぽい？)
            if (atk.ability == "しんりょく" && atk.Options[12] )
            {
                if ( move.Type == "くさ" )
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }
            if (( atk.ability == "もうか" || atk.ability == "もらいび" ) && atk.Options[12])
            {
                if (move.Type == "ほのお")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }
            if (atk.ability == "げきりゅう" && atk.Options[12])
            {
                if (move.Type == "みず")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }
            if ( atk.ability == "むしのしらせ" && atk.Options[12])
            {
                if (move.Type == "むし")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }

            // トランジスタは第九世代では1.3倍に弱体化
            if (atk.ability == "トランジスタ" )
            {
                if (move.Type == "でんき")
                {
                    A *= 5325;
                    A += 2048;
                    A /= 4096;
                }
            }

            if (atk.ability == "いわはこび")
            {
                if (move.Type == "いわ")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }

            if (atk.ability == "はがねつかい")
            {
                if (move.Type == "はがね")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }

            if (atk.ability == "りゅうのあぎと")
            {
                if (move.Type == "ドラゴン")
                {
                    A *= 6144;
                    A += 2048;
                    A /= 4096;
                }
            }

            return (Tuple.Create( A, D ));
        }
        private long damage_base(long a, long d, long p, long dmg, PokemonMove move) {
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

        public Dictionary<string, List<long>> calc(PokemonDataReal Atk, PokemonDataReal Def)
        {
            Dictionary<string, List<long>> result = new Dictionary<string, List<long>>();
            Dictionary<string, List<long>> result_critical = new Dictionary<string, List<long>>();

            // ステータスをランク補正する
            PokemonDataReal atk_tmp = new PokemonDataReal(Atk.Name, Atk.type[0], Atk.type[1], Atk.TeraType, Atk.Level,
                Atk.HP, Atk.Attack, Atk.Block, Atk.Contact, Atk.Defense, Atk.Speed, Atk.ZukanNo, Atk.Height, Atk.Weight,
                Atk.ability, Atk.Item, Atk.Rank, Atk.Options, Atk.Special, Atk.MoveList);
            CorrectRank(atk_tmp);
            PokemonDataReal def_tmp = new PokemonDataReal(Def.Name, Def.type[0], Def.type[1], Def.TeraType, Def.Level,
                Def.HP, Def.Attack, Def.Block, Def.Contact, Def.Defense, Def.Speed, Def.ZukanNo, Def.Height, Def.Weight,
                Def.ability, Def.Item, Def.Rank, Def.Options, Def.Special, Def.MoveList);
            CorrectRank(def_tmp);
            PokemonDataReal atk_cri_tmp = new PokemonDataReal(Atk.Name, Atk.type[0], Atk.type[1], Atk.TeraType, Atk.Level,
                Atk.HP, Atk.Attack, Atk.Block, Atk.Contact, Atk.Defense, Atk.Speed, Atk.ZukanNo, Atk.Height, Atk.Weight,
                Atk.ability, Atk.Item, Atk.Rank, Atk.Options, Atk.Special, Atk.MoveList);
            PokemonDataReal def_cri_tmp = new PokemonDataReal(Def.Name, Def.type[0], Def.type[1], Def.TeraType, Def.Level,
                Def.HP, Def.Attack, Def.Block, Def.Contact, Def.Defense, Def.Speed, Def.ZukanNo, Def.Height, Def.Weight,
                Def.ability, Def.Item, Def.Rank, Def.Options, Def.Special, Def.MoveList);
            CorrectRankCritical(atk_cri_tmp);
            CorrectRankCritical(def_cri_tmp);

            // こだいかっせい/クォークチャージが発動する場合のステータス補正
            PokemonDataReal atk, def, atk_cri, def_cri;
            ( atk, atk_cri ) = CorrectParadoxRank(atk_tmp, atk_cri_tmp);
            ( def, def_cri ) = CorrectParadoxRank(def_tmp, def_cri_tmp);

            foreach ( var atkmove in Atk.MoveList )
            {
                atk.Options[20] = def.Options[20] = false; // かたやぶりをOFFにする(技にかたやぶり効果が付いているものへの対応)

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

                // 特性の影響を受けない技の場合はかたやぶりオプションを有効にする
                if ( move.StrExp.Contains("特性の影響を受けず" ) )
                {
                    atk.Options[20] = true;
                }

                // テラバーストの設定を変える
                if (move.Name == "テラバースト")
                {
                    if (atk.TeraType.IsNullOrEmpty() == false && atk.Options[14] )
                    {
                        // テラスタイプが入力されている＆テラスタルしている
                        move.Type = atk.TeraType;

                        // ランク補正を含めてA>Cなら物理技に切り替える
                        if ( atk.Attack > atk.Contact )
                        {
                            move.Category = 1;
                        }
                        else
                        {
                            move.Category = 2;
                        }

                        // テラスタイプがステラの場合は威力が100になる
                        if ( atk.TeraType == "ステラ" )
                        {
                            move.Power = 100;
                        }
                        else
                        {
                            move.Power = 80;
                        }
                    }
                    else
                    {
                        // テラスタルしてない場合はノーマル・特殊技として扱う(ノーマルがタイプ一致の場合もこれで安心)
                        move.Type = "ノーマル";
                        move.Category = 0x2;
                        move.Power = 80;
                    }
                }

                // テラクラスターの設定を変える
                if (move.Name == "テラクラスター")
                {
                    if (atk.TeraType.IsNullOrEmpty() == false && atk.Options[14])
                    {
                        // テラスタイプが入力されている＆テラスタルしている
                        move.Type = "ステラ";
                        move.Range = true; // 範囲技になる
                    }
                    else
                    {
                        move.Type = "ノーマル";
                        move.Range = false; // 単体攻撃になる
                    }
                }

                // ツタこんぼうのタイプを変える
                if ( move.Name == "ツタこんぼう" && atk.Name.Contains( "オーガポン" ) )
                {
                    // オーガポンの時のみ、持ち物によってタイプを変える
                    if ( atk.Name.Contains( "いど" ) )
                    {
                        move.Type = "みず";
                    }
                    else if ( atk.Name.Contains( "かまど") )
                    {
                        move.Type = "ほのお";
                    }
                    else if ( atk.Name.Contains( "いしずえ" ) )
                    {
                        move.Type = "いわ";
                    }
                    else
                    {
                        move.Type = "くさ";
                    }
                }

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

                // 4096は12bit固定小数点演算のため（整数で扱うと、1.0＝4096となる）
                // 急所の有無それぞれについて、乱数による16パターンのダメージを算出する
                long damage = 4096;
                long A = 1, D = 1;
                long A_critical = 1, D_critical = 1;
                long M = 1;
                long Mhalf = 1, Mfilter = 1, MTwice = 1;

                /* STEP1. A/Dを決定 */ // --> 要確認！！！　ランク補正ってここのA/Dを直接いじる？ -> もう一個、こだわり系はステータス1.5倍だよね？ここ？？
                ( A, D ) = calcAD(A, D, atk, def, move, move.Category);
                ( A_critical, D_critical ) = calcAD( A_critical, D_critical, atk_cri, def_cri, move, move.Category);

                // STEP1-1. サイコショックとサイコブレイクは攻撃側の特攻、防御側の防御を使う
                // -> calcADの中に移動

                // STEP1-2. フォトンゲイザーとシェルアームズはここで補正？
                // 攻撃・特攻と防御・特防を比較して、一番ダメージが大きくなるようにA/Dを決めるんだっけ？

                // STEP1-3. ワンダールームの場合はDを再計算する処理を入れる（防御と特防を入れ替える）
                if ( WonderRoom == true )
                {
                    long A_dummy = 1;
                    D = D_critical = 1; // Aは変わらないのでダミーで計算して結果は捨て、Dはここでの結果を採用する
                    (A_dummy, D) = calcAD(A_dummy, D, atk, def, move, move.Category ^ 0x3); // 物理/特殊の判定を入れ替えてDだけ再計算する
                    (A_dummy, D_critical) = calcAD(A_dummy, D_critical, atk_cri, def_cri, move, move.Category ^ 0x3); // 物理/特殊の判定を入れ替えてDだけ再計算する
                }

                /* STEP2. 最初の()内を計算 */
                /* STEP2-1. 威力を決定 */
                long power = CorrectPower(ref move, atk, def);

                /* STEP2-2. ランク補正済みA/Dに対して急所判定する */
                // -> すでに別途計算済みなので割愛

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
                result[move.Name] = new List<long>();
                result_critical[move.Name] = new List<long>();
                for (int i = 0; i < 16; ++i)
                {
                    long tmp = damage * (85 + i); tmp /= 100; tmp /= 4096; tmp *= 4096;
                    long tmp_critical = damage_critical * (85 + i); tmp_critical /= 100; tmp_critical /= 4096; tmp_critical *= 4096;
                    result[move.Name].Add( tmp );
                    result_critical[move.Name].Add( tmp_critical );
                }

                // STEP8-0-1. スキン系特性の場合、ノーマルタイプの技をスキンのタイプに変更する
                // ただし、テラスタイプがノーマルで、テラスタルした状態でテラバーストを撃つ時は、そのままノーマルタイプの技として撃つので除外する
                bool isSkinAbility = true;
                if (move.Type == "ノーマル" && ( ( move.Name == "テラバースト" ) && ( ( atk.TeraType == "ノーマル" ) && atk.Options[14] ) ) == false )
                {
                    switch (atk.ability)
                    {
                        case "フェアリースキン": move.Type = "フェアリー"; break;
                        case "エレキスキン": move.Type = "でんき"; break;
                        case "フリーズスキン": move.Type = "こおり"; break;
                        case "スカイスキン": move.Type = "ひこう"; break;
                        default: isSkinAbility = false; break;
                    }
                }
                else
                {
                    isSkinAbility = false;
                }

                // STEP8-0-2. うるおいボイスの場合、音技をみずタイプに変更する
                String orgSoundType = "";
                if ( move.Sound && atk.ability == "うるおいボイス" )
                {
                    orgSoundType = move.Type; // 元のタイプをバックアップしておく
                    move.Type = "みず";
                }

                /* STEP8. タイプ一致補正 */
                // -> テラスタイプ一致の計算もここでやる？
                // テラバーストはテラスタルしている場合は必ずタイプ一致(未実装)、テラスタルしていなければノーマルでタイプ一致、ノーマルタイプがノーマルにテラスタルした時は…？
                // そういえば、フライングプレスは「格闘」でタイプ一致判定するんだっけ？(ルチャブル専用技だった気がするから、いずれにせよタイプ一致だが、飛行にテラスタルすると事情が変わる…）
                List<string> TypeCheck = new List<string>();
                if (atk.Options[10] == false || atk.Options[14] )
                {
                    // みずびたしを受けていない
                    // or 
                    // テラスタルしている場合はタイプ変更技無効
                    foreach (var t in atk.type)
                    {
                        TypeCheck.Add(t);
                    }
                }
                else
                {
                    TypeCheck.Add("みず"); // みずびたし中は強制的にみず単タイプに変更
                }

                if (atk.Options[8])
                {
                    // ハロウィンを受けた場合はゴーストタイプを追加する
                    // -> 複雑な処理は面倒なので、みずびたしと同時にONにされた場合は、
                    //    みずびたしの後に受けたことにする。もりののろいも同様。
                    if ( TypeCheck.Contains("ゴースト") == false )
                        TypeCheck.Add("ゴースト");
                }

                if (atk.Options[9])
                {
                    // もりののろいを受けた場合はくさタイプを追加する
                    if (TypeCheck.Contains("くさ") == false)
                        TypeCheck.Add("くさ");
                }

                if (atk.Options[14])
                {
                    // テラスタルしてるならテラスタイプを判定条件に追加する
                    // -> これたぶんタイプ変更技の後にテラスタルすると、テラスタイプ一致の補正がかかるはず？
                    //    例：もりののろいを受けたリザードンが使うソーラービームはタイプ一致補正が1.5倍から2倍になるはず？(交代するまでは)
                    //      -> 変幻自在とかリベロの仕様と一緒なら
                    if (atk.TeraType != "ステラ")
                    {
                        // ステラの場合、テラバーストはタイプ一致ではない（倍率1.2倍になる）ため、除外する
                        TypeCheck.Add(atk.TeraType);
                    }
                }

                long type_match_attack = 0;
                if ( atk.ability == "てきおうりょく" && atk.Options[14] )
                {
                    // てきおうりょくで、テラスタイプと元タイプが一致する場合は 9216 / 4096倍
                    // 同、テラスタイプと元タイプが不一致なら、テラスタイプと一致した時は 8192 / 4096倍、不一致なら 6144 / 4096倍
                    if ( atk.type.Contains( atk.TeraType ) )
                    {
                        if ( move.Type == atk.TeraType )
                        {
                            type_match_attack += 5120;
                        }
                        else if ( atk.type.Contains( move.Type ) )
                        {
                            type_match_attack += 2048; // テラスタイプと技のタイプが一致せず、ただし元タイプと技のタイプが一致する場合は、通常のタイプ一致ボーナス
                        }
                    }
                    else
                    {
                        if ( move.Type == atk.TeraType )
                        {
                            type_match_attack += 4096; // テラスタイプと一致なら2.0倍
                        }
                        else if (atk.type.Contains(move.Type))
                        {
                            type_match_attack += 2048;
                        }
                    }
                }
                else if (atk.ability != "へんげんじざい" && atk.ability != "リベロ")
                {
                    foreach (var t in TypeCheck)
                    {
                        if (t == move.Type)
                        {
                            // へんげんじざいとリベロ以外の特性なら単なるタイプ一致判定（1.5倍）
                            type_match_attack += 2048;
                            if ( atk.TeraType == "ステラ" && atk.Options[14] )
                            {
                                // テラスタイプがステラでテラスタルしている時、タイプ一致ボーナスは2倍
                                // 本当は一度きりで、その後は通常通り1.5倍だが、計算機上は別に良い(ステラを解除すれば1.5倍で計算できるから)
                                type_match_attack += 2048;
                            }
                        }
                        else
                        {
                            if (atk.TeraType == "ステラ" && atk.Options[14])
                            {
                                // テラスタイプがステラでテラスタルしている時、タイプ不一致でもボーナス1.2倍がつく
                                type_match_attack += 819;
                            }
                        }
                    }
                }
                else
                {
                    // へんげんじざいとリベロの場合は全ての攻撃が必ずタイプ一致
                    // -> ノーマルスキンも事実上全ての技がタイプ一致だけど、実戦でエネコロロとか使ってる人は皆無なので、とりあえず放置
                    if (atk.Options[14] == false)
                    {
                        type_match_attack += 2048;
                    }
                    else
                    {
                        // テラスタル中はテラスタイプと元タイプで判定する
                        // -> 元タイプはすでに変更済みなので、判定式自体は他特性と同じもので良い(もっとキレイなコードを書きたい。。)
                        if ( atk.TeraType.IsNullOrEmpty() == false )
                        {
                            foreach (var t in TypeCheck)
                            {
                                if (t == move.Type)
                                {
                                    type_match_attack += 2048;
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < 16; ++i)
                {
                    result[move.Name][i] *= (4096 + (int)type_match_attack); // タイプ不一致なら1.0倍、タイプ一致なら1.5倍、テラスタイプ一致なら2.0倍になる
                    result[move.Name][i] /= 4096;
                    result[move.Name][i] += 2047;
                    result[move.Name][i] /= 4096;
                    result[move.Name][i] *= 4096;

                    result_critical[move.Name][i] *= (4096 + (int)type_match_attack);
                    result_critical[move.Name][i] /= 4096;
                    result_critical[move.Name][i] += 2047;
                    result_critical[move.Name][i] /= 4096;
                    result_critical[move.Name][i] *= 4096;
                }

                /* STEP9. 相性補正 */
                double typecomp_res = 1.0;
                if ( def.TeraType.IsNullOrEmpty() == false && ( def.Options[14] && def.TeraType != "ステラ" ) )
                {
                    // 防御側にテラスタイプが設定されている場合は、相性はテラスタイプを使って計算する
                    typecomp_res *= TypeCompatible.CompatibilityCheck( move.Type, def.TeraType );
                }
                else
                {
                    // テラスタイプ未設定の場合は、本来持つタイプで計算する
                    // 防御側がテラスタルしているが、テラスタイプがステラの場合は、元の相性で計算する
                    List<string> TypeCheck_def = new List<string>();
                    if (def.Options[10])
                    {
                        // みずびたし中はみず単タイプとして扱う
                        TypeCheck_def.Add("みず");
                    }
                    else
                    {
                        foreach (var type in def.type)
                        {
                            TypeCheck_def.Add(type);
                        }
                    }

                    if (def.Options[8])
                    {
                        // ハロウィンを受けた場合はゴーストタイプを追加する
                        TypeCheck_def.Add( "ゴースト" );
                    }

                    if (def.Options[9])
                    {
                        // もりののろいを受けた場合はくさタイプを追加する
                        TypeCheck_def.Add("くさ");
                    }

                    foreach (var type in TypeCheck_def)
                    {
                        typecomp_res *= TypeCompatible.CompatibilityCheck(move.Type, type);
                    }
                }

                // 攻撃側のテラスタイプがステラでテラスタルしている時
                if (atk.TeraType == "ステラ" && atk.Options[14])
                {
                    if (move.Type == "ステラ")
                    {
                        if (def.Options[14])
                        {
                            typecomp_res = 2.0; // タイプがステラの攻撃は、防御側がテラスタルしていれば問答無用で抜群となる
                        }
                        else
                        {
                            typecomp_res = 1.0; // そうでなければ問答無用で等倍となる
                        }
                    }
                }

                // 防御側の特性がテラスシェルで、HPが満タンの場合、全て今ひとつになる
                if (def.ability == "テラスシェル")
                {
                    typecomp_res = 0.5;
                }

                for (int i = 0; i < 16; ++i) // STEP毎にループ書くの微妙なんだけどね…
                {
                    result[move.Name][i] = (long)( result[move.Name][i] * typecomp_res );
                    result[move.Name][i] /= 4096;
                    result[move.Name][i] *= 4096;

                    result_critical[move.Name][i] = (long)(result_critical[move.Name][i] * typecomp_res);
                    result_critical[move.Name][i] /= 4096;
                    result_critical[move.Name][i] *= 4096;
                }

                // STEP9-LAST-1. スキン系補正を適用した技のタイプをノーマルに戻す
                if ( isSkinAbility )
                {
                    move.Type = "ノーマル";
                }

                // STEP9-LAST-2. うるおいボイスを適用した技のタイプをもとに戻す
                if ( orgSoundType.IsNullOrEmpty() == false )
                {
                    move.Type = orgSoundType;
                }

                /* STEP10. 火傷補正 */
                for (int i = 0; i < 16; ++i)
                {
                    // 物理技で火傷状態ならダメージ半減(こんじょう、からげんきは除く)
                    if ( atk.Options[16] && move.Category == 1 && move.Name != "からげんき" && atk.ability != "こんじょう" )
                    {
                        result[move.Name][i] *= 2048;
                        result[move.Name][i] += 2047;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 2048;
                        result_critical[move.Name][i] += 2047;
                        result_critical[move.Name][i] /= 4096;
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
                    // →テラバーストとかフォトンゲイザーが困るか… -> 修正したのでたぶん困らない
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

                // STEP11-2.(第九世代) イナズマドライブ/アクセルブレイクは弱点をつくと威力1.33倍
                if (move.Name == "アクセルブレイク" || move.Name == "イナズマドライブ")
                {
                    if (typecomp_res > 1.0 )
                    {
                        for ( int i = 0; i < 16; ++i )
                        {
                            result[move.Name][i] *= 5461;
                            result[move.Name][i] += 2048;
                            result[move.Name][i] /= 4096;

                            result_critical[move.Name][i] *= 5461;
                            result_critical[move.Name][i] += 2048;
                            result_critical[move.Name][i] /= 4096;
                        }
                    }
                }

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
                    && ( move.Category == 2) && atk.Options[20] == false )
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
                if (def.ability == "ファントムガード" && def.Options[12] ) // Options[12]を特性有効bitみたいに使った方が良かったかも。。
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
                if (def.ability == "マルチスケイル" && def.Options[12] && atk.Options[20] == false )
                {
                    // 型破りは専用optionsをONにした方が良いかも。シャドーレイとかと一緒に処理できて楽だし。
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
                    && ( move.Sound) && atk.Options[20] == false ) // 音の技の時
                {
                    // 音の技を受ける時はダメージ半減
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
                    && (move.Direct) && atk.Options[20] == false )
                {
                    if (move.Punch && atk.Item == "パンチグローブ")
                        ; // パンチグローブでパンチ技を使う時は非接触判定になる
                    else
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
                }

                /* STEP11-7. Mfilter補正 */
                /* STEP11-7-1. ハードロック/フィルター補正 */
                if ( ( ( def.ability == "ハードロック" ) || ( def.ability == "フィルター" ) )
                    && (typecomp_res > 1.0) && atk.Options[20] == false )
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
                if (def.Options[11] && atk.Options[20] == false )
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
                    // 達人の帯が発動する時はダメージ1.2倍
                    for (int i = 0; i < 16; ++i)
                    {
                        result[move.Name][i] *= 4915;
                        result[move.Name][i] += 2048;
                        result[move.Name][i] /= 4096;

                        result_critical[move.Name][i] *= 4915;
                        result_critical[move.Name][i] += 2048;
                        result_critical[move.Name][i] /= 4096;
                    }
                }

                /* STEP11-10. メトロノーム補正 */
                if (atk.Item.Contains("メトロノーム") )
                {
                    // メトロノームで同じ技をN回使ったらダメージ上昇
                    int[] gain = { 4096, 4915, 5734, 6553, 7372, 8192 };
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
                    // 特定条件下でダメージ2倍の技を使用した(ここの条件はちいさくなるに対する2倍の技だけ。他も要対応)
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
                    if ( result[move.Name][i] <= 0 && typecomp_res > 0 )
                    {
                        result[move.Name][i] = 1; // 相性で無効化されないなら、最低ダメージ1が保証される
                    }

                    result_critical[move.Name][i] /= 4096;
                    if ( result_critical[move.Name][i] <= 0 && typecomp_res > 0 )
                    {
                        result_critical[move.Name][i] = 1;
                    }
                }

                /* STEP14. 期待値を計算する */
                // -> トレ天の計算結果と違うんだけど何で…？
                double tmp_exp = 0.0;
                double hustle_acc = 1.0;
                if ( atk.ability == "はりきり" )
                {
                    // 特性はりきりなら命中率0.8倍
                    hustle_acc = 0.8;
                }
                for (int i = 0; i < 16; ++i)
                {
                    // 基本ダメージは、計算結果 × 急所に"当たらない"確率 × 技の命中率
                    tmp_exp += (result[move.Name][i] / 16.0) * (1.0 - CalcCriticalProbability(move, atk, def)) * ( (move.Accuracy / 100.0) * hustle_acc );
                }
                for (int i = 0; i < 16; ++i)
                {
                    // 急所に当たった場合のダメージは、計算結果 × 急所に"当たる"確率 × 技の命中率
                    tmp_exp += (result_critical[move.Name][i] / 16.0) * CalcCriticalProbability(move, atk, def) * ( (move.Accuracy / 100.0) * hustle_acc );
                }
                result[move.Name].AddRange(result_critical[move.Name] );
                result[move.Name].Add( (long)tmp_exp );

                /* LAST STEP. 計算結果を結果配列に突っ込む */
                /* LAST STEP2. 後始末 */
                // 特殊な計算をしたフラグをクリアする
            }

            return (result);
        }

    }
}
