# unity-LiquidChain
# 糸を引く液体のスクリプト
![liquidChainSampleImage](https://user-images.githubusercontent.com/39085780/105623709-17da0400-5e5f-11eb-9d0f-7fa4aebd428e.jpg)

Unity標準のLineRendererを用いて、点Aと点Bの2点間に糸を引く液体を描画するためのスクリプトです。

# 使い方
1.点Aの設定  
[LineRenderer]コンポーネントを持つGameObjectに[LiquidChain]をアタッチします。このGameObjectが点Aになります。

2.点B候補を作成  
TargetSettings.TargetTagがタグとして割り当てられたGameObjectを1つ以上作成します。これが点Bの候補になります。

3.点Bの探索  
2で作成した点Bの候補のうち、点Aに一定距離以内(TargetSettings.TouchDistance)に近づいた点が存在すれば、それを点Bとして糸を引く液体が描画されます。

※糸が表示されている間は、他の点B候補が点Aに一定距離以内に近づいたとしても何も起こりません。

# パラメータ

## Solver(物理設定)
### Solver.IterationCount
演算回数です。数を増やすほど糸が垂れにくくなります。

### Solver.NumOfPoints
頂点数です。頂点数が多いほど滑らかになりますが、糸が垂れやすくなります。

### Solver.BreakThreshold
糸の切れるしきい値です。小さければ小さいほど切れにくくなります。

### Solver.StraightRange
糸がまっすぐになる範囲です。

### Solver.GravityMultiplier
重力係数(-9.8)に乗算されます。

1だと落ちるのが早すぎるので、0.2くらいが良いかもしれません。

### Solver.LiquidQuantity
両端から下に流れる液体の量です。

ラインの太さに影響します。

### Solver.LifeTimeFramesOfBrokenChain
糸が切れてからの生存フレーム数です。

## LineRenderSetting(ライン描画設定)
### LineRenderSetting.Width
糸の基準太さです。

### LineRenderSetting.MinWidth
糸の最小の太さです。

### LineRenderSetting.BaseLength
糸が伸びるほど細くなるときの、基準の長さです。

## TargetSettings(ターゲット[点B]探索設定)
### TargetSettings.TouchDistance
対象が触れたと判定される距離です。

### TargetSettings.TargetTag
対象を検索する際のTag名です。

デフォルトはLiquidChainTargetなので、このタグが存在しない場合は作成する必要があります。
