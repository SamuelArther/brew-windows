class Goku < Formula
  desc "HTTP load testing tool"
  homepage "https://goku.observabilityinsight.com/"
  url "https://github.com/jcaromiq/goku/archive/refs/tags/v3.0.0.tar.gz"
  sha256 "95635c42cf748b64a4d1a04b5460397331366a78ddd890f7e13bf0e811c27976"
  license "MIT"
  head "https://github.com/jcaromiq/goku.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0911a98db4c1beab534afbf206ffb92106a4f91e787b42f75699aece29adf1ba"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "cli")
  end

  test do
    output = shell_output("#{bin}/goku --target https://httpbin.org/get")
    assert_match "kamehameha to https://httpbin.org/get with 1 concurrent clients and 1 total iterations", output

    assert_match version.to_s, shell_output("#{bin}/goku --version")
  end
end
