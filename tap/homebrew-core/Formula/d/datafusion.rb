class Datafusion < Formula
  desc "Apache Arrow DataFusion and Ballista query engines"
  homepage "https://arrow.apache.org/datafusion"
  url "https://www.apache.org/dyn/closer.lua?path=datafusion/datafusion-56.0.0/apache-datafusion-56.0.0.tar.gz"
  mirror "https://archive.apache.org/dist/datafusion/datafusion-56.0.0/apache-datafusion-56.0.0.tar.gz"
  sha256 "89187ce4a9d6ea65e2edca2503b690e1dc5853c3ee3bb6c672a05bf9b1ecbbf3"
  license "Apache-2.0"
  head "https://github.com/apache/datafusion.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d8dd469e82fb3a61c19ad7948036f92a82956ef18e1d4a57e7f66427d8af43d2"
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
