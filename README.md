# RFIDproject
This is a project at Sasase Laboratory where we use a RFID system to read tags in real time.


## Information from the wiki
 RFID
前提知識：タグのメモリ領域
タグにはEPC, TID, UserMemoryと，書き込めるメモリ領域が分かれている

    EPC: EPCは物を認識するためのID．任意の値を設定可能 (ワインに固有のIDがあればそれなど，自分が認識できる名前に設定するのがよいと思います)
    TID: TIDはタグそのものを認識するためのIDで通常は初期値のまま使用する
    UserMemory: 付加データを書き込む領域．任意の値を設定可能 (ワインのURLなど)

タグの読み書きをする
1. 開発環境をダウンロード (Xamarin-studio)
https://www.xamarin.com/studio

2. Impinjが公開しているプログラムのexampleをダウンロード: Octane-SDKの.NET版をダウンロード (Javaでも出来ますが，.NETが書き易くてよいと思います)
(https://support.impinj.com/hc/en-us/articles/202755268-Octane-SDK)

3. Xamarin-studioを起動し，2のexamplesを読み込む

4. 手始めに読み取るアプリケーションをビルド (Examples -> ReadTags -> Build)

    Examples -> ReadTags
    SolutionConstraints.cs内を以下のように設定
    public const string ReaderHostname = "speedwayr-10-dc-c5.local";
    Examples -> ReadTags -> Build

5. 実行

タグの書き込みをする
1. Examples内のWrite...を参照

    基本的に1回で1タグに書き込み
    書き込むタグのみアンテナの上に載せるのがよいです


タグの位置を測る
Examples内のXArray...をビルドして実行し，どんなことが出来るか確認してください．(到来方向推定，ローカライゼーションなど)

センサ付きタグの読み取り
上記のOctane-SDKを使用しても読めないことはないですが，ややトリッキーです．なので，販売元が公開しているアプリケーションを使うのがベターです．

1. AnyReaderをダウンロード
http://www.farsens.com/en/software-tools/software/anyreader-windows/

2. 読み取る
