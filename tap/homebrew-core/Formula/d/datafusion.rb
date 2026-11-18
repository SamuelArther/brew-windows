class Datafusion < Formula
  desc "Apache Arrow DataFusion and Ballista query engines"
  homepage "https://arrow.apache.org/datafusion"
  url "https://www.apache.org/dyn/closer.lua?path=datafusion/datafusion-55.3.0/apache-datafusion-55.3.0.tar.gz"
  mirror "https://archive.apache.org/dist/datafusion/datafusion-55.3.0/apache-datafusion-55.3.0.tar.gz"
  sha256 "94e460e7ca1434d8d50a9efbb3fc8dfa2aa7b9ff19759cd733be03e808e9802c"
  license "Apache-2.0"
  head "https://github.com/apache/datafusion.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fd8ea644082bdb03cab2d4b39e0dc629ad1124b5a64839231adf1db4ef4869d0"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    # Avoid OOM on GitHub runners
    inreplace "Cargo.toml", /^lto = true$/, 'lto = "thin"' if OS.linux? && ENV["HOMEBREW_GITHUB_ACTIONS"]

    system "cargo", "install", *std_cargo_args(path: "datafusion-cli")
  end

  test do
    (testpath/"datafusion_test.sql").write <<~SQL
      select 1+2 as n;
    SQL
    assert_equal "[{\"n\":3}]", shell_output("#{bin}/datafusion-cli -q --format json -f datafusion_test.sql").strip
  end
end
