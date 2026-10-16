class Goku < Formula
  desc "HTTP load testing tool"
  homepage "https://goku.observabilityinsight.com/"
  url "https://github.com/jcaromiq/goku/archive/refs/tags/v3.0.0.tar.gz"
  sha256 "95635c42cf748b64a4d1a04b5460397331366a78ddd890f7e13bf0e811c27976"
  license "MIT"
  head "https://github.com/jcaromiq/goku.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8df9d34e197851629878e4bfefbc97955c01d1e7ba0395bd6a9dcfdda83e01a9"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "cli")
  end

  test do
    output = shell_output("#{bin}/goku --target https://httpbin.org/get")
    assert_match "kamehameha to https://httpbin.org/get with 1 concurrent clients and 1 total iterations", output

    assert_match version.to_s, shell_output("#{bin}/goku --version")
  end
end
