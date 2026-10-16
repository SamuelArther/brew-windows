class Xan < Formula
  desc "CSV CLI magician written in Rust"
  homepage "https://github.com/medialab/xan"
  url "https://github.com/medialab/xan/archive/refs/tags/0.61.0.tar.gz"
  sha256 "cd675a4ce734438f5b6b0eba28f5f1e275fdd58c387f03afc9eb37146779e18c"
  license any_of: ["MIT", "Unlicense"]
  head "https://github.com/medialab/xan.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c6a0b64c76f53b481d7c41d3931f5cf0f7ee1a50941e5cab996577ec8e4a6377"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(features: "parquet")
    generate_completions_from_executable(bin/"xan", "completions", shells: [:bash, :zsh])
  end

  test do
    (testpath/"test.csv").write("first header,second header")
    system bin/"xan", "stats", "test.csv"
    assert_match version.to_s, shell_output("#{bin}/xan --version").chomp
  end
end
