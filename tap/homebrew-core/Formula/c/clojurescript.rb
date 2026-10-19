class Clojurescript < Formula
  desc "Clojure to JS compiler"
  homepage "https://github.com/clojure/clojurescript"
  url "https://github.com/clojure/clojurescript/releases/download/r1.12.134/cljs.jar"
  sha256 "89ba5a16fa3b0e74b1206f652c0d14eda5157fdcf8c42f51fc175a4d4c10c48a"
  license "EPL-1.0"
  head "https://github.com/clojure/clojurescript.git", branch: "master"

  livecheck do
    url :stable
    regex(/r?(\d+(?:\.\d+)+)/i)
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "488f53ffb9cf8f2e1dbfce0f324c245eddc62801203fc8f463bb2667197caa1b"
  end

  depends_on "node" => :test
  depends_on "openjdk"

  def install
    libexec.install "cljs.jar"
    bin.write_jar_script libexec/"cljs.jar", "cljsc"
  end

  def caveats
    <<~EOS
      This formula is useful if you need to use the ClojureScript compiler directly.
      For a more integrated workflow use Leiningen, Boot, or Maven.
    EOS
  end

  test do
    (testpath/"hello.cljs").write <<~CLOJURE
      (ns hello)
      (println "Hello world!")
    CLOJURE

    assert_equal "Hello world!\n", shell_output("#{bin}/cljsc --target node #{testpath}/hello.cljs")
  end
end
